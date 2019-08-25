﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using RCNet.MathTools;
using RCNet.MathTools.MatrixMath;
using RCNet.Extensions;
using RCNet.Neural.Activation;
using System.Collections.Concurrent;
using RCNet.RandomValue;
using RCNet.Neural.Network.SM.Neuron;
using RCNet.Neural.Network.SM.Synapse;

namespace RCNet.Neural.Network.SM.Preprocessing
{
    /// <summary>
    /// Implements reservoir supporting analog and spiking neurons working together.
    /// </summary>
    [Serializable]
    public class Reservoir
    {
        //Attributes
        /// <summary>
        /// Reservoir's input neurons.
        /// </summary>
        private readonly INeuron[] _inputNeuronCollection;
        /// <summary>
        /// Neurons within the pools.
        /// </summary>
        private readonly List<INeuron[]> _poolNeuronsCollection;
        /// <summary>
        /// Ratio of the excitatory neurons within the pool
        /// </summary>
        private readonly double[] _poolExcitatoryNeuronsRatioCollection;
        /// <summary>
        /// Reservoir's all internal neurons (flat structure).
        /// </summary>
        private readonly INeuron[] _reservoirNeuronCollection;
        /// <summary>
        /// Ratio of the excitatory neurons within the reservoir
        /// </summary>
        private readonly double _reservoirExcitatoryNeuronsRatio;
        /// <summary>
        /// Input connections
        /// </summary>
        private readonly SortedList<int, ISynapse>[] _neuronInputConnectionsCollection;
        /// <summary>
        /// Internal connections
        /// </summary>
        private readonly SortedList<int, ISynapse>[] _neuronNeuronConnectionsCollection;

        //Attribute properties
        /// <summary>
        /// Reservoir's instance definition
        /// </summary>
        public NeuralPreprocessorSettings.ReservoirInstanceDefinition InstanceDefinition { get; }
        /// <summary>
        /// Reservoir's predictor neurons.
        /// </summary>
        public List<PredictorNeuron> PredictorNeuronCollection { get; }
        /// <summary>
        /// Number of reservoir's output predictors
        /// </summary>
        public int NumOfOutputPredictors { get; }
        /// <summary>
        /// Number of internal synapses
        /// </summary>
        public int NumOfInternalSynapses { get; private set; }

        //Attributes
        private readonly List<Tuple<int, int>> _parallelRanges;
        private readonly int _numOfAnalogNeurons;


        //Constructor
        /// <summary>
        /// Instantiates the reservoir
        /// </summary>
        /// <param name="instanceDefinition">Reservoir instance definition</param>
        /// <param name="inputRange">Range of input values</param>
        /// <param name="rand">Random object to be used for random part initialization </param>
        public Reservoir(NeuralPreprocessorSettings.ReservoirInstanceDefinition instanceDefinition, Interval inputRange, Random rand)
        {
            int numOfInputNodes = instanceDefinition.NPInputFieldIdxCollection.Count;
            //Copy settings
            InstanceDefinition = instanceDefinition.DeepClone();

            //-----------------------------------------------------------------------------
            //Initialization of neurons
            //-----------------------------------------------------------------------------
            //Input neurons
            _inputNeuronCollection = new INeuron[numOfInputNodes];
            for (int i = 0; i < numOfInputNodes; i++)
            {
                _inputNeuronCollection[i] = new InputNeuron(InstanceDefinition.Settings.InputEntryPoint, i, inputRange);
            }

            //-----------------------------------------------------------------------------
            //Reservoir neurons
            //-----------------------------------------------------------------------------
            int neuronReservoirFlatIdx = 0;
            _numOfAnalogNeurons = 0;
            List<INeuron> allNeurons = new List<INeuron>();
            _poolNeuronsCollection = new List<INeuron[]>(InstanceDefinition.Settings.PoolSettingsCollection.Count);
            _poolExcitatoryNeuronsRatioCollection = new double[InstanceDefinition.Settings.PoolSettingsCollection.Count];
            _reservoirExcitatoryNeuronsRatio = 0d;
            PredictorNeuronCollection = new List<PredictorNeuron>();
            for (int poolID = 0; poolID < InstanceDefinition.Settings.PoolSettingsCollection.Count; poolID++)
            {
                PoolSettings poolSettings = InstanceDefinition.Settings.PoolSettingsCollection[poolID];
                _poolExcitatoryNeuronsRatioCollection[poolID] = 0;
                //------------------------------------------------------------------------------------
                //Neuron groups within the pool
                int groupID = 0, idx = 0;
                List<int> analogActivationIdxs = new List<int>();
                List<NeuronCreationParams> neuronParamsCollection = new List<NeuronCreationParams>();
                foreach (PoolSettings.NeuronGroupSettings ngs in poolSettings.NeuronGroups)
                {
                    //Group neuron params
                    for (int i = 0; i < ngs.Count; i++)
                    {
                        NeuronCreationParams neuronParams = new NeuronCreationParams
                        {
                            Activation = ActivationFactory.Create(ngs.ActivationCfg, rand),
                            Role = ngs.Role,
                            Bias = rand.NextDouble(ngs.BiasCfg),
                            GroupID = groupID,
                            RetainmentRate = 0,
                            UseAsPredictor = false
                        };
                        if (neuronParams.Role == CommonEnums.NeuronRole.Excitatory)
                        {
                            ++_poolExcitatoryNeuronsRatioCollection[poolID];
                            ++_reservoirExcitatoryNeuronsRatio;
                        }
                        if (neuronParams.Activation.OutputSignalType == CommonEnums.NeuronSignalType.Analog)
                        {
                            ++_numOfAnalogNeurons;
                            analogActivationIdxs.Add(idx);
                        }
                        neuronParamsCollection.Add(neuronParams);
                        ++idx;
                    }
                    ++groupID;
                }//ngs
                //Finalize ratio of the excitatory neurons within the pool
                _poolExcitatoryNeuronsRatioCollection[poolID] /= poolSettings.Dim.Size;
                //Setup of retainment rates
                if (poolSettings.RetainmentNeuronsFeature)
                {
                    int numOfRetNeurons = (int)Math.Round(poolSettings.RetainmentNeuronsDensity * analogActivationIdxs.Count, 0);
                    rand.Shuffle(analogActivationIdxs);
                    for (int i = 0; i < numOfRetNeurons; i++)
                    {
                        neuronParamsCollection[analogActivationIdxs[i]].RetainmentRate = rand.NextDouble(poolSettings.RetainmentRate);
                    }
                }
                //Setup of readout neurons
                if (poolSettings.ReadoutNeuronsDensity > 0)
                {
                    int numOfReadoutneurons = (int)Math.Round(poolSettings.Dim.Size * poolSettings.ReadoutNeuronsDensity);
                    rand.Shuffle(neuronParamsCollection);
                    for (int i = 0; i < numOfReadoutneurons; i++)
                    {
                        neuronParamsCollection[i].UseAsPredictor = true;
                    }
                }
                //Randomize order before sequential instantiation
                rand.Shuffle(neuronParamsCollection);
                //Instantiate neurons
                INeuron[] poolNeurons = new INeuron[poolSettings.Dim.Size];
                int neuronPoolFlatIdx = 0;
                for (int x = 0; x < poolSettings.Dim.DimX; x++)
                {
                    for (int y = 0; y < poolSettings.Dim.DimY; y++)
                    {
                        for (int z = 0; z < poolSettings.Dim.DimZ; z++)
                        {
                            NeuronPlacement placement = new NeuronPlacement(InstanceDefinition.InstanceID, neuronReservoirFlatIdx, poolID, neuronPoolFlatIdx, neuronParamsCollection[neuronPoolFlatIdx].GroupID, poolSettings.Dim.X + x, poolSettings.Dim.Y + y, poolSettings.Dim.Z + z);
                            //Neuron instance
                            if (neuronParamsCollection[neuronPoolFlatIdx].Activation.OutputSignalType == CommonEnums.NeuronSignalType.Spike)
                            {
                                //Spiking neuron
                                poolNeurons[neuronPoolFlatIdx] = new SpikingNeuron(placement,
                                                                                   neuronParamsCollection[neuronPoolFlatIdx].Role,
                                                                                   neuronParamsCollection[neuronPoolFlatIdx].Activation,
                                                                                   neuronParamsCollection[neuronPoolFlatIdx].Bias
                                                                                   );
                            }
                            else
                            {
                                //Analog neuron
                                poolNeurons[neuronPoolFlatIdx] = new AnalogNeuron(placement,
                                                                                  neuronParamsCollection[neuronPoolFlatIdx].Role,
                                                                                  neuronParamsCollection[neuronPoolFlatIdx].Activation,
                                                                                  neuronParamsCollection[neuronPoolFlatIdx].Bias,
                                                                                  neuronParamsCollection[neuronPoolFlatIdx].RetainmentRate
                                                                                  );
                            }
                            allNeurons.Add(poolNeurons[neuronPoolFlatIdx]);
                            if (neuronParamsCollection[neuronPoolFlatIdx].UseAsPredictor)
                            {
                                PredictorNeuron pn = new PredictorNeuron
                                {
                                    Neuron = poolNeurons[neuronPoolFlatIdx],
                                    UseSecondaryPredictor = (instanceDefinition.AugmentedStates && poolSettings.NeuronGroups[neuronParamsCollection[neuronPoolFlatIdx].GroupID].AugmentedStates)
                                };
                                PredictorNeuronCollection.Add(pn);
                                NumOfOutputPredictors += pn.UseSecondaryPredictor ? 2 : 1;
                            }
                            ++neuronPoolFlatIdx;
                            ++neuronReservoirFlatIdx;
                        }//z
                    }//y
                }//x
                _poolNeuronsCollection.Add(poolNeurons);
            }//PoolID
            //All neurons flat structure
            _reservoirNeuronCollection = allNeurons.ToArray();
            //Ratio of the excitatory neurons within the reservoir
            _reservoirExcitatoryNeuronsRatio /= _reservoirNeuronCollection.Length;
            //Parallel processing ranges
            var rangePartitioner = Partitioner.Create(0, _reservoirNeuronCollection.Length);
            _parallelRanges = new List<Tuple<int, int>>(rangePartitioner.GetDynamicPartitions());


            //-----------------------------------------------------------------------------
            //Interconnections
            //-----------------------------------------------------------------------------
            NumOfInternalSynapses = 0;
            //Connection banks allocations
            _neuronInputConnectionsCollection = new SortedList<int, ISynapse>[_reservoirNeuronCollection.Length];
            _neuronNeuronConnectionsCollection = new SortedList<int, ISynapse>[_reservoirNeuronCollection.Length];
            for (int n = 0; n < _reservoirNeuronCollection.Length; n++)
            {
                _neuronInputConnectionsCollection[n] = new SortedList<int, ISynapse>();
                _neuronNeuronConnectionsCollection[n] = new SortedList<int, ISynapse>();
            }

            //-----------------------------------------------------------------------------
            //Pools connections
            for (int poolID = 0; poolID < _poolNeuronsCollection.Count; poolID++)
            {
                //Input connection
                SetPoolInputConnections(rand, poolID, instanceDefinition.InputConnectionCollection);
                SetPoolInterconnections(rand, poolID);
            }

            //-----------------------------------------------------------------------------
            //Add Pool to pool connections
            foreach (ReservoirSettings.PoolsInterconnection poolsInterConn in InstanceDefinition.Settings.PoolsInterconnectionCollection)
            {
                SetPool2PoolInterconnections(rand, poolsInterConn);
            }

            //-----------------------------------------------------------------------------
            //Setup of the synaptic delays
            //Build the distances statistics
            BasicStat distanceStat = new BasicStat();
            foreach (SortedList<int, ISynapse> synapses in _neuronInputConnectionsCollection)
            {
                distanceStat.AddSampleValues((from synapse in synapses.Values select synapse.Distance));
            }
            foreach (SortedList<int, ISynapse> synapses in _neuronNeuronConnectionsCollection)
            {
                distanceStat.AddSampleValues((from synapse in synapses.Values select synapse.Distance));
            }
            //Delay setup on input synapses
            Parallel.ForEach(_neuronInputConnectionsCollection, synapses =>
            {
                foreach (ISynapse synapse in synapses.Values)
                {
                    //Compute appropriate delay and set it
                    if (instanceDefinition.Settings.SynapticDelayMethod == CommonEnums.SynapticDelayMethod.Distance)
                    {
                        double relDistance = (synapse.Distance - distanceStat.Min) / distanceStat.Span;
                        int delay = (int)Math.Round(instanceDefinition.Settings.MaxInputDelay * relDistance);
                        synapse.SetDelay(delay);
                    }
                    else
                    {
                        synapse.SetDelay(rand.Next(instanceDefinition.Settings.MaxInputDelay + 1));
                    }
                }
            });

            //-----------------------------------------------------------------------------
            //Spectral radius
            //Apply only for the analog part of the reservoir
            if (_numOfAnalogNeurons > 0)
            {
                ApplySpectralRadius(true);
            }

            //Finished
            return;
        }

        //Properties
        /// <summary>
        /// Reservoir size. (Number of neurons in the reservoir)
        /// </summary>
        public int Size { get { return _reservoirNeuronCollection.Length; } }

        //Methods
        /// <summary>
        /// Scales weights of synapses to achieve requiered spectral radius on specified subset of weights
        /// </summary>
        /// <param name="scaleAnalogOnly">Specifies whether to scale weights of synapses targeting analog neurons only (true) or weights of all synapses (false)</param>
        private void ApplySpectralRadius(bool scaleAnalogOnly)
        {
            //Create reservoir's weight matrix
            Matrix wMatrix = new Matrix(_reservoirNeuronCollection.Length, _reservoirNeuronCollection.Length);
            Parallel.ForEach(from neuron in _reservoirNeuronCollection where !scaleAnalogOnly || neuron.OutputType == CommonEnums.NeuronSignalType.Analog select neuron, neuron =>
            {
                foreach (ISynapse synapse in _neuronNeuronConnectionsCollection[neuron.Placement.ReservoirFlatIdx].Values)
                {
                    wMatrix.Data[neuron.Placement.ReservoirFlatIdx][synapse.SourceNeuron.Placement.ReservoirFlatIdx] = synapse.Weight;
                }
            });
            double largestEigenValue = Math.Abs(wMatrix.EstimateLargestEigenValue(out double[] eigenVector));
            if (largestEigenValue == 0)
            {
                throw new Exception("Invalid reservoir weights or specified subset of weights. Largest eigenvalue is 0.");
            }
            double scale = InstanceDefinition.Settings.SpectralRadius / largestEigenValue;
            //Scale weights of synapses targeting analog neurons
            Parallel.ForEach(from neuron in _reservoirNeuronCollection where !scaleAnalogOnly || neuron.OutputType == CommonEnums.NeuronSignalType.Analog select neuron, neuron =>
            {
                foreach (ISynapse synapse in _neuronNeuronConnectionsCollection[neuron.Placement.ReservoirFlatIdx].Values)
                {
                    synapse.Rescale(scale);
                }
            });
            return;
        }

        /// <summary>
        /// This general function adds new synapse into the connections bank.
        /// </summary>
        /// <param name="connectionsCollection">Bank of connections</param>
        /// <param name="synapse">A synapse to be added into the bank</param>
        /// <returns>Success/Unsuccess</returns>
        private bool AddInterconnection(SortedList<int, ISynapse>[] connectionsCollection, ISynapse synapse)
        {
            //Add new connection
            lock (connectionsCollection[synapse.TargetNeuron.Placement.ReservoirFlatIdx])
            {
                try
                {
                    connectionsCollection[synapse.TargetNeuron.Placement.ReservoirFlatIdx].Add(synapse.SourceNeuron.Placement.ReservoirFlatIdx, synapse);
                    return true;
                }
                catch
                {
                    //Connection already exists
                    return false;
                }
            }
        }

        private void SetPoolInputConnections(Random rand, int poolID, List<NeuralPreprocessorSettings.ReservoirInstanceDefinition.InputConnection> inputConnectionCollection)
        {
            //Select available targets (spiking inhibitory neurons are forbidden)
            List<INeuron> targetNeurons = new List<INeuron>(from neuron in _poolNeuronsCollection[poolID]
                                                            where neuron.OutputType == CommonEnums.NeuronSignalType.Analog ||
                                                                  (neuron.OutputType == CommonEnums.NeuronSignalType.Spike && neuron.Role == CommonEnums.NeuronRole.Excitatory)
                                                            select neuron
                                                            );
            //Create connections
            foreach (NeuralPreprocessorSettings.ReservoirInstanceDefinition.InputConnection inputConnection in inputConnectionCollection)
            {
                if (inputConnection.PoolID == poolID)
                {
                    int connectionsPerInput = (int)Math.Round(InstanceDefinition.Settings.PoolSettingsCollection[poolID].Dim.Size * inputConnection.Density, 0);
                    if(connectionsPerInput > targetNeurons.Count)
                    {
                        connectionsPerInput = targetNeurons.Count;
                    }
                    if (connectionsPerInput > 0)
                    {
                        int[] indices = new int[targetNeurons.Count];
                        indices.Indices();
                        rand.Shuffle(indices);
                        for (int i = 0; i < connectionsPerInput; i++)
                        {
                            int targetNeuronIdx = indices[i];
                            double weight = (targetNeurons[targetNeuronIdx].OutputType == CommonEnums.NeuronSignalType.Spike ? rand.NextDouble(inputConnection.SynapseCfg.SpikingTargetWeightCfg) : rand.NextDouble(inputConnection.SynapseCfg.AnalogTargetWeightCfg));
                            ISynapse synapse = new InputSynapse(_inputNeuronCollection[inputConnection.FieldIdx],
                                                                targetNeurons[targetNeuronIdx],
                                                                weight
                                                                );
                            AddInterconnection(_neuronInputConnectionsCollection, synapse);
                        }
                    }
                }
            }
            return;
        }


        private void ConnectNeurons(Random rand,
                                    int sourcePoolID,
                                    CommonEnums.NeuronRole sourceNeuronRole,
                                    int numOfSourceNeurons,
                                    int targetPoolID,
                                    CommonEnums.NeuronRole targetNeuronRole,
                                    int totalNumOfConnections,
                                    bool constantNumOfNeuronConnections,
                                    InternalSynapseSettings synapseCfg
                                    )
        {
            //Initial condition
            if (totalNumOfConnections <= 0 || numOfSourceNeurons == 0)
            {
                return;
            }
            PoolSettings sourcePoolSettings = InstanceDefinition.Settings.PoolSettingsCollection[sourcePoolID];
            PoolSettings targetPoolSettings = InstanceDefinition.Settings.PoolSettingsCollection[targetPoolID];
            //////////////////////////////////////////////////////////////////////////////////////
            //Collect all possible source neurons
            List<NeuronConnCount> sourceNeuronCollection = (from neuron in _poolNeuronsCollection[sourcePoolID]
                                                            where neuron.Role == sourceNeuronRole
                                                            select new NeuronConnCount { Neuron = neuron, ConnCount = 0 }
                                                            ).ToList();
            //Randomize source neurons order
            rand.Shuffle(sourceNeuronCollection);
            if (numOfSourceNeurons < 0 || numOfSourceNeurons > sourceNeuronCollection.Count)
            {
                //Set numOfSourceNeurons according to the length of the sourceNeuronCollection
                numOfSourceNeurons = sourceNeuronCollection.Count;
            }
            else
            {
                //Cut the list according to numOfSourceNeurons
                sourceNeuronCollection.RemoveRange(numOfSourceNeurons, sourceNeuronCollection.Count - numOfSourceNeurons);
            }
            //////////////////////////////////////////////////////////////////////////////////////
            //Collect all possible target neurons
            List<INeuron> targetNeuronCollection = (from neuron in _poolNeuronsCollection[targetPoolID]
                                                    where neuron.Role == targetNeuronRole
                                                    select neuron
                                                    ).ToList();
            //////////////////////////////////////////////////////////////////////////////////////
            //Plan number of connections per each source neuron
            bool excludeSourceNeuronFromTarget = (sourcePoolID == targetPoolID && sourceNeuronRole == targetNeuronRole && !sourcePoolSettings.InterconnectionCfg.AllowSelfConnection);
            int maxPhysicalConnCountPerNeuron = targetNeuronCollection.Count - ((excludeSourceNeuronFromTarget) ? 1 : 0);
            //Check condition
            if (maxPhysicalConnCountPerNeuron == 0)
            {
                //No connections will be created
                return;
            }
            int averageConnectionsPerNeuron = (int)Math.Round((double)totalNumOfConnections / (double)sourceNeuronCollection.Count);
            if (averageConnectionsPerNeuron < 1) averageConnectionsPerNeuron = 1;
            if (averageConnectionsPerNeuron > maxPhysicalConnCountPerNeuron) averageConnectionsPerNeuron = maxPhysicalConnCountPerNeuron;
            int connectionsCountDown = sourceNeuronCollection.Count * averageConnectionsPerNeuron;
            if(connectionsCountDown > totalNumOfConnections) connectionsCountDown = totalNumOfConnections;
            int minConnCount = int.MaxValue;
            int maxConnCount = int.MinValue;
            //Build plan of the connections distribution
            foreach (NeuronConnCount ncc in sourceNeuronCollection)
            {
                //Number of connections for current source neuron
                int connCount = constantNumOfNeuronConnections ? averageConnectionsPerNeuron : (int)Math.Round(rand.NextBoundedGaussianDouble(averageConnectionsPerNeuron - 1d, averageConnectionsPerNeuron + 1d));
                if (connCount > maxPhysicalConnCountPerNeuron) connCount = maxPhysicalConnCountPerNeuron;
                if (connCount > connectionsCountDown) connCount = connectionsCountDown;
                ncc.ConnCount = connCount;
                minConnCount = Math.Min(minConnCount, connCount);
                maxConnCount = Math.Max(maxConnCount, connCount);
                connectionsCountDown -= connCount;
                if (connectionsCountDown == 0)
                {
                    break;
                }
            }
            //Allow only small deviation around averageConnectionsPerNeuron to avoid reservoir neurons' unstability
            if (minConnCount < averageConnectionsPerNeuron - 1)
            {
                sourceNeuronCollection.Sort(NeuronConnCount.CmpSortDesc);
                while (sourceNeuronCollection[sourceNeuronCollection.Count - 1].ConnCount < averageConnectionsPerNeuron - 1)
                {
                    ++sourceNeuronCollection[sourceNeuronCollection.Count - 1].ConnCount;
                    --sourceNeuronCollection[0].ConnCount;
                    sourceNeuronCollection.Sort(NeuronConnCount.CmpSortDesc);
                }
                minConnCount = sourceNeuronCollection[sourceNeuronCollection.Count - 1].ConnCount;
                maxConnCount = sourceNeuronCollection[0].ConnCount;
                rand.Shuffle(sourceNeuronCollection);
            }


            //////////////////////////////////////////////////////////////////////////////////////
            //Create physical connections
            bool byDistance = (sourcePoolID == targetPoolID && sourcePoolSettings.InterconnectionCfg.AvgDistance > 0);
            List<NeuronConnCount> sourceNeurons = new List<NeuronConnCount>(from item in sourceNeuronCollection where item.ConnCount > 0 select item);
            Random[] randFarm = new Random[sourceNeurons.Count];
            for(int i = 0; i < sourceNeurons.Count; i++)
            {
                int seed = rand.Next();
                randFarm[i] = new Random(seed);
            }
            int[] synapsesCounter = new int[sourceNeurons.Count];
            synapsesCounter.Populate(0);
            Parallel.For(0, sourceNeurons.Count, sourceNeuronIdx =>
            //for(int sourceNeuronIdx = 0; sourceNeuronIdx < sourceNeurons.Count; sourceNeuronIdx++)
            {
                NeuronConnCount nccSource = sourceNeurons[sourceNeuronIdx];
                Random threadRandObj = randFarm[sourceNeuronIdx];
                //Copy all possible target neurons and compute distances if necessary
                List<RelatedNeuron> tmpRelTargetNeuronCollection = new List<RelatedNeuron>(from neuron in targetNeuronCollection
                                                                                           where (!excludeSourceNeuronFromTarget || (excludeSourceNeuronFromTarget && neuron != nccSource.Neuron))
                                                                                           select new RelatedNeuron
                                                                                           {
                                                                                               Neuron = neuron,
                                                                                               Distance = byDistance ? EuclideanDistance.Compute(nccSource.Neuron.Placement.ReservoirCoordinates, neuron.Placement.ReservoirCoordinates) : 0
                                                                                           });
                //Make connections of source neurons
                for (int connNum = 0; connNum < nccSource.ConnCount; connNum++)
                {
                    int targetNeuronIndex = -1;
                    //Select target neuron to be connected
                    if (byDistance)
                    {
                        //Selection based on average distance
                        double gaussianDistance = threadRandObj.NextGaussianDouble(sourcePoolSettings.InterconnectionCfg.AvgDistance);
                        //Find neuron having closest distance to gaussian distance
                        double minDiff = double.MaxValue;
                        for (int i = 0; i < tmpRelTargetNeuronCollection.Count; i++)
                        {
                            double err = Math.Abs(tmpRelTargetNeuronCollection[i].Distance - gaussianDistance);
                            if (err < minDiff)
                            {
                                targetNeuronIndex = i;
                                minDiff = err;
                            }
                        }
                    }
                    else
                    {
                        //Pure random selection
                        targetNeuronIndex = threadRandObj.Next(tmpRelTargetNeuronCollection.Count);
                    }
                    //Establish connection
                    InternalSynapseSettings.DynamicsSettings dynamicsSettings = synapseCfg.GetDynamicsSettings(nccSource.Neuron.OutputType,
                                                                                                               nccSource.Neuron.Role,
                                                                                                               tmpRelTargetNeuronCollection[targetNeuronIndex].Neuron.OutputType,
                                                                                                               tmpRelTargetNeuronCollection[targetNeuronIndex].Neuron.Role
                                                                                                               );
                    ISynapse synapse = new InternalSynapse(nccSource.Neuron,
                                                           tmpRelTargetNeuronCollection[targetNeuronIndex].Neuron,
                                                           threadRandObj.NextDouble(dynamicsSettings.WeightCfg),
                                                           dynamicsSettings.TauFacilitation,
                                                           dynamicsSettings.TauDepression,
                                                           dynamicsSettings.RestingEfficacy,
                                                           dynamicsSettings.TauPostSynapticCurrentDecay,
                                                           dynamicsSettings.ApplyShortTermPlasticity,
                                                           dynamicsSettings.ApplyPostSynapticCurrent
                                                           );
                    if(AddInterconnection(_neuronNeuronConnectionsCollection, synapse))
                    {
                        ++synapsesCounter[sourceNeuronIdx];
                    }
                    //Remove targetNeuron from tmp collection
                    tmpRelTargetNeuronCollection.RemoveAt(targetNeuronIndex);
                }//connNum
            });
            //Increment total number of internal synapses
            foreach (int count in synapsesCounter)
            {
                NumOfInternalSynapses += count;
            }
            return;
        }


        private void SetPoolInterconnections(Random rand, int poolID)
        {
            PoolSettings poolSettings = InstanceDefinition.Settings.PoolSettingsCollection[poolID];
            //Determine counts
            int intendedNumOfSynapses = (int)(Math.Round(((double)poolSettings.Dim.Size)).Power(2) * poolSettings.InterconnectionCfg.Density);
            int countE2E = (int)Math.Round(poolSettings.InterconnectionCfg.RatioEE * intendedNumOfSynapses);
            int countE2I = (int)Math.Round(poolSettings.InterconnectionCfg.RatioEI * intendedNumOfSynapses);
            int countI2E = (int)Math.Round(poolSettings.InterconnectionCfg.RatioIE * intendedNumOfSynapses);
            int countI2I = (int)Math.Round(poolSettings.InterconnectionCfg.RatioII * intendedNumOfSynapses);
            //Connections E2E
            ConnectNeurons(rand,
                           poolID,
                           CommonEnums.NeuronRole.Excitatory,
                           -1,
                           poolID,
                           CommonEnums.NeuronRole.Excitatory,
                           countE2E,
                           poolSettings.InterconnectionCfg.ConstantNumOfConnections,
                           poolSettings.InterconnectionCfg.SynapseCfg
                           );
            //Connections E2I
            ConnectNeurons(rand,
                           poolID,
                           CommonEnums.NeuronRole.Excitatory,
                           -1,
                           poolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           countE2I,
                           poolSettings.InterconnectionCfg.ConstantNumOfConnections,
                           poolSettings.InterconnectionCfg.SynapseCfg
                           );
            //Connections I2E
            ConnectNeurons(rand,
                           poolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           -1,
                           poolID,
                           CommonEnums.NeuronRole.Excitatory,
                           countI2E,
                           poolSettings.InterconnectionCfg.ConstantNumOfConnections,
                           poolSettings.InterconnectionCfg.SynapseCfg
                           );
            //Connections I2I
            ConnectNeurons(rand,
                           poolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           -1,
                           poolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           countI2I,
                           poolSettings.InterconnectionCfg.ConstantNumOfConnections,
                           poolSettings.InterconnectionCfg.SynapseCfg
                           );
            return;
        }



        private void SetPool2PoolInterconnections(Random rand, ReservoirSettings.PoolsInterconnection cfg)
        {
            PoolSettings sourcePoolSettings = InstanceDefinition.Settings.PoolSettingsCollection[cfg.SourcePoolID];
            PoolSettings targetPoolSettings = InstanceDefinition.Settings.PoolSettingsCollection[cfg.TargetPoolID];
            //Determine counts
            int totalNumOfSourceNeurons = (int)(Math.Round(((double)sourcePoolSettings.Dim.Size)) * cfg.SourceConnectionDensity);
            double numOfTargetNeuronsPerSourceNeuron = ((double)targetPoolSettings.Dim.Size) * cfg.TargetConnectionDensity;
            int totalNumOfSynapses = (int)(Math.Round(totalNumOfSourceNeurons * numOfTargetNeuronsPerSourceNeuron));
            int countE2E = (int)Math.Round(cfg.RatioEE * totalNumOfSynapses);
            int countE2I = (int)Math.Round(cfg.RatioEI * totalNumOfSynapses);
            int countI2E = (int)Math.Round(cfg.RatioIE * totalNumOfSynapses);
            int countI2I = (int)Math.Round(cfg.RatioII * totalNumOfSynapses);
            //Connections E2E
            ConnectNeurons(rand,
                           cfg.SourcePoolID,
                           CommonEnums.NeuronRole.Excitatory,
                           (int)Math.Round(countE2E/ numOfTargetNeuronsPerSourceNeuron),
                           cfg.TargetPoolID,
                           CommonEnums.NeuronRole.Excitatory,
                           countE2E,
                           cfg.ConstantNumOfConnections,
                           cfg.SynapseCfg
                           );
            //Connections E2I
            ConnectNeurons(rand,
                           cfg.SourcePoolID,
                           CommonEnums.NeuronRole.Excitatory,
                           (int)Math.Round(countE2I / numOfTargetNeuronsPerSourceNeuron),
                           cfg.TargetPoolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           countE2I,
                           cfg.ConstantNumOfConnections,
                           cfg.SynapseCfg
                           );
            //Connections I2E
            ConnectNeurons(rand,
                           cfg.SourcePoolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           (int)Math.Round(countI2E / numOfTargetNeuronsPerSourceNeuron),
                           cfg.TargetPoolID,
                           CommonEnums.NeuronRole.Excitatory,
                           countI2E,
                           cfg.ConstantNumOfConnections,
                           cfg.SynapseCfg
                           );
            //Connections I2I
            ConnectNeurons(rand,
                           cfg.SourcePoolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           (int)Math.Round(countI2I / numOfTargetNeuronsPerSourceNeuron),
                           cfg.TargetPoolID,
                           CommonEnums.NeuronRole.Inhibitory,
                           countI2I,
                           cfg.ConstantNumOfConnections,
                           cfg.SynapseCfg
                           );
            return;
        }

        /// <summary>
        /// Collects key statistics related to reservoir neurons states
        /// </summary>
        public ReservoirStat CollectStatistics()
        {
            ReservoirStat stats = new ReservoirStat(InstanceDefinition.InstanceName,
                                                    InstanceDefinition.Settings.SettingsName,
                                                    Size,
                                                    _reservoirExcitatoryNeuronsRatio,
                                                    NumOfOutputPredictors,
                                                    NumOfInternalSynapses
                                                    );
            int poolID = 0;
            foreach (PoolSettings poolSettings in InstanceDefinition.Settings.PoolSettingsCollection)
            {
                ReservoirStat.PoolStat poolStat = new ReservoirStat.PoolStat(poolSettings, _poolNeuronsCollection[poolID].Length, _poolExcitatoryNeuronsRatioCollection[poolID]);
                //Neurons statistics
                foreach (INeuron neuron in _poolNeuronsCollection[poolID])
                {
                    poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].AvgActivationStatesStat.AddSampleValue(neuron.Statistics.NormalizedActivationStateStat.ArithAvg);
                    poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MaxActivationStatesStat.AddSampleValue(neuron.Statistics.NormalizedActivationStateStat.Max);
                    poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MinActivationStatesStat.AddSampleValue(neuron.Statistics.NormalizedActivationStateStat.Min);
                    poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].ActivationStateSpansStat.AddSampleValue(neuron.Statistics.NormalizedActivationStateStat.Span);
                    if (neuron.Statistics.InputStimuliStat.NumOfNonzeroSamples > 0)
                    {
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].AvgIStimuliStat.AddSampleValue(neuron.Statistics.InputStimuliStat.ArithAvg);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MaxIStimuliStat.AddSampleValue(neuron.Statistics.InputStimuliStat.Max);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MinIStimuliStat.AddSampleValue(neuron.Statistics.InputStimuliStat.Min);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].IStimuliSpansStat.AddSampleValue(neuron.Statistics.InputStimuliStat.Span);
                    }
                    if (neuron.Statistics.ReservoirStimuliStat.NumOfNonzeroSamples > 0)
                    {
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].AvgRStimuliStat.AddSampleValue(neuron.Statistics.ReservoirStimuliStat.ArithAvg);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MaxRStimuliStat.AddSampleValue(neuron.Statistics.ReservoirStimuliStat.Max);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MinRStimuliStat.AddSampleValue(neuron.Statistics.ReservoirStimuliStat.Min);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].RStimuliSpansStat.AddSampleValue(neuron.Statistics.ReservoirStimuliStat.Span);
                    }
                    if (neuron.Statistics.TotalStimuliStat.NumOfNonzeroSamples > 0)
                    {
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].AvgTStimuliStat.AddSampleValue(neuron.Statistics.TotalStimuliStat.ArithAvg);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MaxTStimuliStat.AddSampleValue(neuron.Statistics.TotalStimuliStat.Max);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MinTStimuliStat.AddSampleValue(neuron.Statistics.TotalStimuliStat.Min);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].TStimuliSpansStat.AddSampleValue(neuron.Statistics.TotalStimuliStat.Span);
                    }
                    poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].AvgOutputSignalStat.AddSampleValue(neuron.Statistics.OutputSignalStat.ArithAvg);
                    poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MaxOutputSignalStat.AddSampleValue(neuron.Statistics.OutputSignalStat.Max);
                    poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MinOutputSignalStat.AddSampleValue(neuron.Statistics.OutputSignalStat.Min);
                    //Synapses efficacy statistics
                    foreach (ISynapse rSynapse in _neuronNeuronConnectionsCollection[neuron.Placement.ReservoirFlatIdx].Values)
                    {
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].AvgSynEfficacyStat.AddSampleValue(rSynapse.EfficacyStat.ArithAvg);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MaxSynEfficacyStat.AddSampleValue(rSynapse.EfficacyStat.Max);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].MinSynEfficacyStat.AddSampleValue(rSynapse.EfficacyStat.Min);
                        poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].SynEfficacySpansStat.AddSampleValue(rSynapse.EfficacyStat.Span);
                    }
                    if (neuron.Statistics.ReservoirStimuliStat.NumOfNonzeroSamples == 0)
                    {
                        ++poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].NumOfNoRStimuliNeurons;
                        ++poolStat.NumOfNoRStimuliNeurons;
                        ++stats.NumOfNoRStimuliNeurons;
                    }
                    if (neuron.Statistics.OutputSignalStat.NumOfNonzeroSamples == 0)
                    {
                        ++poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].NumOfNoOutputSignalNeurons;
                        ++poolStat.NumOfNoOutputSignalNeurons;
                        ++stats.NumOfNoOutputSignalNeurons;
                    }
                    if (neuron.Statistics.OutputSignalStat.Span == 0)
                    {
                        ++poolStat.NeuronGroupStatCollection[neuron.Placement.PoolGroupID].NumOfConstOutputSignalNeurons;
                        ++poolStat.NumOfConstOutputSignalNeurons;
                        ++stats.NumOfConstOutputSignalNeurons;
                    }
                }
                //Weights statistics
                //Input weights
                foreach (SortedList<int, ISynapse> synapses in _neuronInputConnectionsCollection)
                {
                    foreach (ISynapse synapse in synapses.Values)
                    {
                        if (synapse.TargetNeuron.Placement.PoolID == poolID)
                        {
                            poolStat.InputWeightsStat.AddSampleValue(synapse.Weight);
                        }
                    }
                }
                //Internal weights
                foreach (SortedList<int, ISynapse> synapses in _neuronNeuronConnectionsCollection)
                {
                    foreach (ISynapse synapse in synapses.Values)
                    {
                        if (synapse.TargetNeuron.Placement.PoolID == poolID)
                        {
                            poolStat.InternalWeightsStat.AddSampleValue(synapse.Weight);
                        }
                    }
                }
                stats.PoolStatCollection.Add(poolStat);
                ++poolID;
            }
            return stats;
        }

        /// <summary>
        /// Resets all reservoir neurons and other components to their initial state.
        /// Function does not affect weights or internal structure of the resservoir.
        /// </summary>
        /// <param name="resetStatistics">Specifies whether to reset internal statistics</param>
        public void Reset(bool resetStatistics)
        {
            //Input neurons
            foreach(INeuron neuron in _inputNeuronCollection)
            {
                neuron.Reset(resetStatistics);
            }
            //Reservoir neurons and all linked synapses
            Parallel.For(0, _reservoirNeuronCollection.Length, n =>
            {
                _reservoirNeuronCollection[n].Reset(resetStatistics);
                //Linked input synapses
                foreach (ISynapse synapse in _neuronInputConnectionsCollection[n].Values)
                {
                    synapse.Reset(resetStatistics);
                }
                //Linked internal synapses
                foreach (ISynapse synapse in _neuronNeuronConnectionsCollection[n].Values)
                {
                    synapse.Reset(resetStatistics);
                }
            });
            return;
        }

        /// <summary>
        /// Computes reservoir neurons states.
        /// </summary>
        /// <param name="input">
        /// Array of input values.
        /// </param>
        /// <param name="updateStatistics">
        /// Specifies whether to update neurons statistics.
        /// Specify "false" during the booting phase and "true" after the booting phase.
        /// </param>
        public void Compute(double[] input, bool updateStatistics)
        {
            //Set input values to input neurons
            for (int i = 0; i < input.Length; i++)
            {
                _inputNeuronCollection[i].NewStimuli(input[i], 0);
                _inputNeuronCollection[i].NewState(updateStatistics);
            }
            //Perform reservoir's computation cycle
            //Collect new stimulation for each reservoir neuron
            Parallel.ForEach(_parallelRanges, range =>
            {
                for (int neuronIdx = range.Item1; neuronIdx < range.Item2; neuronIdx++)
                {
                    //Stimulation from input neurons
                    double iStimuli = 0;
                    foreach (ISynapse synapse in _neuronInputConnectionsCollection[neuronIdx].Values)
                    {
                        iStimuli += synapse.GetSignal(updateStatistics);
                    }
                    //Stimulation from connected reservoir neurons
                    double rStimuli = 0;
                    foreach (ISynapse synapse in _neuronNeuronConnectionsCollection[neuronIdx].Values)
                    {
                        rStimuli += synapse.GetSignal(updateStatistics);
                    }
                    //Store new neuron's stimulation
                    _reservoirNeuronCollection[neuronIdx].NewStimuli(iStimuli, rStimuli);
                }
            });
            //Recompute state of all reservoir neurons
            Parallel.ForEach(_parallelRanges, range =>
            {
                for (int neuronIdx = range.Item1; neuronIdx < range.Item2; neuronIdx++)
                {
                    _reservoirNeuronCollection[neuronIdx].NewState(updateStatistics);
                }
            });
            return;
        }

        /// <summary>
        /// Copies all reservoir predictors to a given buffer starting from the specified possition
        /// </summary>
        public void CopyPredictorsTo(double[] buffer, int fromOffset)
        {
            int bufferIdx = fromOffset;
            foreach(PredictorNeuron pn in PredictorNeuronCollection)
            {
                buffer[bufferIdx] = pn.Neuron.PrimaryPredictor;
                ++bufferIdx;
                if (pn.UseSecondaryPredictor)
                {
                    buffer[bufferIdx] = pn.Neuron.SecondaryPredictor;
                    ++bufferIdx;
                }
            }
            return;
        }

        //Inner classes
        private class NeuronCreationParams
        {
            public CommonEnums.NeuronRole Role { get; set; }
            public IActivationFunction Activation { get; set; }
            public double Bias { get; set; }
            public int GroupID { get; set; }
            public double RetainmentRate { get; set; }
            public bool UseAsPredictor { get; set; }
        }

        /// <summary>
        /// Structure contains information about the neuron from which are extracted predictors
        /// </summary>
        [Serializable]
        public class PredictorNeuron
        {
            /// <summary>
            /// Neuron from which is extracted primary predictor
            /// </summary>
            public INeuron Neuron { get; set; }
            /// <summary>
            /// Indicates whether to extract also additional secondary predictor from the neuron (neuron will give two predictors)
            /// </summary>
            public bool UseSecondaryPredictor { get; set; }
        }

        private class RelatedNeuron
        {
            //Attribute properties
            public INeuron Neuron { get; set; }
            public double Distance { get; set; }

            //Methods
            public static int CompareByDistanceAsc(RelatedNeuron item1, RelatedNeuron item2)
            {
                if(item1.Distance < item2.Distance)
                {
                    return -1;
                }
                else if(item1.Distance > item2.Distance)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            //Inner classes
            public class DistanceAscComparer : IComparer<RelatedNeuron>
            {
                public int Compare(RelatedNeuron item1, RelatedNeuron item2)
                {
                    return CompareByDistanceAsc(item1, item2);
                }
            }
        }

        private class NeuronConnCount
        {
            public INeuron Neuron { get; set; }
            public int ConnCount { get; set; }

            public static int CmpSortDesc(NeuronConnCount item1, NeuronConnCount item2)
            {
                if (item1.ConnCount > item2.ConnCount)
                {
                    return -1;
                }
                else if (item1.ConnCount < item2.ConnCount)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

    }//Reservoir

    /// <summary>
    /// Key statistics of the reservoir
    /// </summary>
    [Serializable]
    public class ReservoirStat
    {
        //Attributes
        /// <summary>
        /// Name of the reservoir instance
        /// </summary>
        public string ReservoirInstanceName { get; }
        /// <summary>
        /// Name of the reservoir configuration settings
        /// </summary>
        public string ReservoirSettingsName { get; }
        /// <summary>
        /// Total number of neurons within the reservoir
        /// </summary>
        public int TotalNumOfNeurons { get; }
        /// <summary>
        /// Ratio of the excitatory neurons within the reservoir
        /// </summary>
        public double ExcitatoryNeuronsRatio;
        /// <summary>
        /// Total number of predictors
        /// </summary>
        public int TotalNumOfPredictors { get; }
        /// <summary>
        /// Total number of internal synapses
        /// </summary>
        public int TotalNumOfInternalSynapses { get; }
        /// <summary>
        /// Collection of resrvoir pools stats
        /// </summary>
        public List<PoolStat> PoolStatCollection { get; }
        /// <summary>
        /// Number of neurons getting no stimulation from connected reservoir's neurons
        /// </summary>
        public int NumOfNoRStimuliNeurons { get; set; }
        /// <summary>
        /// Number of neurons emitting no output signal
        /// </summary>
        public int NumOfNoOutputSignalNeurons { get; set; }
        /// <summary>
        /// Number of neurons emitting constant output signal
        /// </summary>
        public int NumOfConstOutputSignalNeurons { get; set; }

        //Constructor
        /// <summary>
        /// Creates an unitialized instance
        /// </summary>
        /// <param name="reservoirInstanceName">Name of the reservoir instance</param>
        /// <param name="reservoirSettingsName">Name of the reservoir configuration settings</param>
        /// <param name="numOfNeurons">Total number of neurons</param>
        /// <param name="numOfPredictors">Total number of predictors</param>
        /// <param name="numOfInternalSynapses">Total number of synapses</param>
        public ReservoirStat(string reservoirInstanceName,
                             string reservoirSettingsName,
                             int numOfNeurons,
                             double excitatoryNeuronsRatio,
                             int numOfPredictors,
                             int numOfInternalSynapses
                             )
        {
            ReservoirInstanceName = reservoirInstanceName;
            ReservoirSettingsName = reservoirSettingsName;
            TotalNumOfNeurons = numOfNeurons;
            ExcitatoryNeuronsRatio = excitatoryNeuronsRatio;
            TotalNumOfPredictors = numOfPredictors;
            TotalNumOfInternalSynapses = numOfInternalSynapses;
            PoolStatCollection = new List<PoolStat>();
            NumOfNoRStimuliNeurons = 0;
            NumOfNoOutputSignalNeurons = 0;
            NumOfConstOutputSignalNeurons = 0;
            return;
        }

        //Inner classes
        /// <summary>
        /// Key statistics of the pool of neurons
        /// </summary>
        [Serializable]
        public class PoolStat
        {
            /// <summary>
            /// Name of the pool
            /// </summary>
            public string PoolName { get; }

            /// <summary>
            /// Number of neurons within the pool
            /// </summary>
            public int NumOfNeurons { get; }

            /// <summary>
            /// Number of neurons within the pool
            /// </summary>
            public double ExcitatoryNeuronsRatio { get; }

            /// <summary>
            /// Collection of the neuron group statistics
            /// </summary>
            public NeuronGroupStat[] NeuronGroupStatCollection { get; }
            /// <summary>
            /// Number of neurons getting no stimulation from connected reservoir's neurons
            /// </summary>
            public int NumOfNoRStimuliNeurons { get; set; }
            /// <summary>
            /// Number of neurons emitting no output signal
            /// </summary>
            public int NumOfNoOutputSignalNeurons { get; set; }
            /// <summary>
            /// Number of neurons emitting constant output signal
            /// </summary>
            public int NumOfConstOutputSignalNeurons { get; set; }

            /// <summary>
            /// Input weights statistics
            /// </summary>
            public BasicStat InputWeightsStat { get; }

            /// <summary>
            /// Internal weights statistics
            /// </summary>
            public BasicStat InternalWeightsStat { get; }

            //Constructor
            /// <summary>
            /// Creates an unitialized instance
            /// </summary>
            /// <param name="poolSettings">Settings of the neuron pool</param>
            /// <param name="numOfNeurons">Number of neurons within the pool</param>
            public PoolStat(PoolSettings poolSettings,
                            int numOfNeurons,
                            double excitatoryNeuronsRatio
                            )
            {
                PoolName = poolSettings.Name;
                NumOfNeurons = numOfNeurons;
                ExcitatoryNeuronsRatio = excitatoryNeuronsRatio;
                NeuronGroupStatCollection = new NeuronGroupStat[poolSettings.NeuronGroups.Count];
                for(int i = 0; i < poolSettings.NeuronGroups.Count; i++)
                {
                    NeuronGroupStatCollection[i] = new NeuronGroupStat(poolSettings.NeuronGroups[i].Name);
                }
                NumOfNoRStimuliNeurons = 0;
                NumOfNoOutputSignalNeurons = 0;
                NumOfConstOutputSignalNeurons = 0;
                InputWeightsStat = new BasicStat();
                InternalWeightsStat = new BasicStat();
                return;
            }

            //Inner classes
            /// <summary>
            /// Key statistics of the group of neurons
            /// </summary>
            [Serializable]
            public class NeuronGroupStat
            {
                /// <summary>
                /// Name of the pool instance
                /// </summary>
                public string GroupName { get; }
                /// <summary>
                /// Statistics of neurons' activation min states
                /// </summary>
                public BasicStat MinActivationStatesStat { get; }
                /// <summary>
                /// Statistics of neurons' activation max states
                /// </summary>
                public BasicStat MaxActivationStatesStat { get; }
                /// <summary>
                /// Statistics of neurons' activation avg states
                /// </summary>
                public BasicStat AvgActivationStatesStat { get; }
                /// <summary>
                /// Statistics of spans of the neurons' activation states
                /// </summary>
                public BasicStat ActivationStateSpansStat { get; }
                /// <summary>
                /// Statistics of the neurons' average input stimuli passed to activation function
                /// </summary>
                public BasicStat AvgIStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' max input stimuli passed to activation function
                /// </summary>
                public BasicStat MaxIStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' min input stimuli passed to activation function
                /// </summary>
                public BasicStat MinIStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' span of the input stimuli passed to activation function
                /// </summary>
                public BasicStat IStimuliSpansStat { get; }
                /// <summary>
                /// Statistics of the neurons' average stimuli related to part coming from connected reservoir's neurons
                /// </summary>
                public BasicStat AvgRStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' max stimuli related to part coming from connected reservoir's neurons
                /// </summary>
                public BasicStat MaxRStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' min stimuli related to part coming from connected reservoir's neurons
                /// </summary>
                public BasicStat MinRStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' stimuli span related to part coming from connected reservoir's neurons
                /// </summary>
                public BasicStat RStimuliSpansStat { get; }
                /// <summary>
                /// Statistics of the neurons' average total stimuli (all components)
                /// </summary>
                public BasicStat AvgTStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' max total stimuli (all components)
                /// </summary>
                public BasicStat MaxTStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' min total stimuli (all components)
                /// </summary>
                public BasicStat MinTStimuliStat { get; }
                /// <summary>
                /// Statistics of the neurons' total stimuli span (all components)
                /// </summary>
                public BasicStat TStimuliSpansStat { get; }
                /// <summary>
                /// Statistics of neurons' average output signals
                /// </summary>
                public BasicStat AvgOutputSignalStat { get; }
                /// <summary>
                /// Statistics of neurons' max output signals
                /// </summary>
                public BasicStat MaxOutputSignalStat { get; }
                /// <summary>
                /// Statistics of neurons' min output signals
                /// </summary>
                public BasicStat MinOutputSignalStat { get; }
                /// <summary>
                /// Statistics of the synapses' average efficacy
                /// </summary>
                public BasicStat AvgSynEfficacyStat { get; }
                /// <summary>
                /// Statistics of the synapses' max efficacy
                /// </summary>
                public BasicStat MaxSynEfficacyStat { get; }
                /// <summary>
                /// Statistics of the synapses' min efficacy
                /// </summary>
                public BasicStat MinSynEfficacyStat { get; }
                /// <summary>
                /// Statistics of the synapses' efficacy span
                /// </summary>
                public BasicStat SynEfficacySpansStat { get; }
                /// <summary>
                /// Number of neurons getting no stimulation from connected reservoir's neurons
                /// </summary>
                public int NumOfNoRStimuliNeurons { get; set; }
                /// <summary>
                /// Number of neurons emitting no output signal
                /// </summary>
                public int NumOfNoOutputSignalNeurons { get; set; }
                /// <summary>
                /// Number of neurons emitting constant output signal
                /// </summary>
                public int NumOfConstOutputSignalNeurons { get; set; }


                //Constructor
                /// <summary>
                /// Creates an unitialized instance
                /// </summary>
                /// <param name="groupName">Name of the neuron group</param>
                public NeuronGroupStat(string groupName)
                {
                    GroupName = groupName;
                    MinActivationStatesStat = new BasicStat();
                    MaxActivationStatesStat = new BasicStat();
                    AvgActivationStatesStat = new BasicStat();
                    ActivationStateSpansStat = new BasicStat();
                    AvgTStimuliStat = new BasicStat();
                    MaxTStimuliStat = new BasicStat();
                    MinTStimuliStat = new BasicStat();
                    TStimuliSpansStat = new BasicStat();
                    AvgRStimuliStat = new BasicStat();
                    MaxRStimuliStat = new BasicStat();
                    MinRStimuliStat = new BasicStat();
                    RStimuliSpansStat = new BasicStat();
                    AvgIStimuliStat = new BasicStat();
                    MaxIStimuliStat = new BasicStat();
                    MinIStimuliStat = new BasicStat();
                    IStimuliSpansStat = new BasicStat();
                    AvgOutputSignalStat = new BasicStat();
                    MaxOutputSignalStat = new BasicStat();
                    MinOutputSignalStat = new BasicStat();
                    AvgSynEfficacyStat = new BasicStat();
                    MaxSynEfficacyStat = new BasicStat();
                    MinSynEfficacyStat = new BasicStat();
                    SynEfficacySpansStat = new BasicStat();
                    NumOfNoRStimuliNeurons = 0;
                    NumOfNoOutputSignalNeurons = 0;
                    NumOfConstOutputSignalNeurons = 0;
                    return;
                }

            }//NeuronGroupStat

        }//PoolStat

    }//ReservoirStat

}//Namespace
