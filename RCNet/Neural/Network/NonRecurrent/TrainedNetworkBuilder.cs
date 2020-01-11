﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.MathTools;
using RCNet.Extensions;
using RCNet.Neural.Network.NonRecurrent.FF;
using RCNet.Neural.Network.NonRecurrent.PP;
using RCNet.Neural.Data;
using System.Globalization;

namespace RCNet.Neural.Network.NonRecurrent
{
    /// <summary>
    /// Builds trained non-recurrent network
    /// </summary>
    public class TrainedNetworkBuilder
    {
        //Delegates
        /// <summary>
        /// This is the control function of the regression process and is called
        /// after the completion of each regression training epoch.
        /// The goal of the regression process is to train a network
        /// that will give good results both on the training data and the test data.
        /// RegressionControlInArgs object passed to the callback function contains the best error statistics so far
        /// and the latest statistics. The primary purpose of this function is to decide whether the latest statistics
        /// are better than the best statistics so far.
        /// The function can also tell the regression process that it does not make any sense to continue the regression.
        /// It can terminate the current regression attempt or whole regression process.
        /// </summary>
        /// <param name="buildingState">Contains all the necessary information to control the regression.</param>
        /// <returns>Instructions for the regression process.</returns>
        public delegate BuildingInstr RegressionControllerDelegate(BuildingState buildingState);

        /// <summary>
        /// Delegate of RegressionEpochDone event handler.
        /// </summary>
        /// <param name="buildingState">Current state of the regression process</param>
        /// <param name="foundBetter">Indicates that the best network was found as a result of the performed epoch</param>
        public delegate void RegressionEpochDoneHandler(BuildingState buildingState, bool foundBetter);

        /// <summary>
        /// This informative event occurs every time the regression epoch is done
        /// </summary>
        public event RegressionEpochDoneHandler RegressionEpochDone;

        //Constants

        //Attributes
        private readonly string _networkName;
        private readonly object _networkSettings;
        private readonly int _foldNum;
        private readonly int _numOfFolds;
        private readonly VectorBundle _trainingBundle;
        private readonly VectorBundle _testingBundle;
        private readonly double _binBorder;
        private readonly Random _rand;
        private readonly RegressionControllerDelegate _controller;

        //Constructor
        /// <summary>
        /// Creates an instance ready to start building trained non-recurrent network
        /// </summary>
        /// <param name="networkName">Name of the network to be built</param>
        /// <param name="networkSettings">Network configuration (FeedForwardNetworkSettings or ParallelPerceptronSettings object)</param>
        /// <param name="foldNum">Current fold number</param>
        /// <param name="numOfFolds">Total number of the folds</param>
        /// <param name="trainingBundle">Bundle of predictors and ideal values to be used for training purposes</param>
        /// <param name="testingBundle">Bundle of predictors and ideal values to be used for testing purposes</param>
        /// <param name="binBorder">If specified, it indicates that the whole network output is binary and specifies numeric border where GE network output is decided as a 1 and LT output as a 0.</param>
        /// <param name="rand">Random generator to be used (optional)</param>
        /// <param name="controller">Regression controller (optional)</param>
        public TrainedNetworkBuilder(string networkName,
                                     object networkSettings,
                                     int foldNum,
                                     int numOfFolds,
                                     VectorBundle trainingBundle,
                                     VectorBundle testingBundle,
                                     double binBorder = double.NaN,
                                     Random rand = null,
                                     RegressionControllerDelegate controller = null
                                     )
        {
            _networkName = networkName;
            _networkSettings = networkSettings;
            _foldNum = foldNum;
            _numOfFolds = numOfFolds;
            _trainingBundle = trainingBundle;
            _testingBundle = testingBundle;
            _binBorder = binBorder;
            _rand = rand ?? new Random(0);
            _controller = controller ?? DefaultRegressionController;
            return;
        }

        //Properties
        /// <summary>
        /// Indicates that the whole network output is binary
        /// </summary>
        public bool BinaryOutput { get { return !double.IsNaN(_binBorder); } }
        /// <summary>
        /// Indicates the network is FeedForwardNetwork
        /// </summary>
        public bool IsFF { get { return (_networkSettings.GetType() == typeof(FeedForwardNetworkSettings)); } }
        /// <summary>
        /// Indicates the network is ParallelPerceptron
        /// </summary>
        public bool IsPP { get { return (_networkSettings.GetType() == typeof(ParallelPerceptronSettings)); } }


        //Static methods
        /// <summary>
        /// Default implementation of an evaluation if the tested network is better than currently the best network
        /// </summary>
        /// <param name="binaryOutput">Indicates the whole network output is binary</param>
        /// <param name="candidate">Network to be evaluated</param>
        /// <param name="currentBest">For now the best network</param>
        public static bool IsBetter(bool binaryOutput, TrainedNetwork candidate, TrainedNetwork currentBest)
        {
            if(binaryOutput)
            {
                if (candidate.CombinedBinaryError < currentBest.CombinedBinaryError ||
                    (candidate.CombinedBinaryError == currentBest.CombinedBinaryError &&
                     candidate.CombinedPrecisionError < currentBest.CombinedPrecisionError)
                   )
                {
                    return true;
                }
                return false;
            }
            else
            {
                return (candidate.CombinedPrecisionError < currentBest.CombinedPrecisionError);
            }
        }

        //Methods
        private BuildingInstr DefaultRegressionController(BuildingState regrState)
        {
            const double stopAttemptBorder = 0.25d;
            BuildingInstr instructions = new BuildingInstr
            {
                CurrentIsBetter = IsBetter(regrState.BinaryOutput,
                                           regrState.CurrNetwork,
                                           regrState.BestNetwork
                                           ),
                StopCurrentAttempt = (((double)(regrState.Epoch - regrState.LastImprovementEpoch) / (double)regrState.MaxEpochs) >= stopAttemptBorder),
                StopProcess = (BinaryOutput &&
                               regrState.BestNetwork.TrainingBinErrorStat.TotalErrStat.Sum == 0 &&
                               regrState.BestNetwork.TestingBinErrorStat.TotalErrStat.Sum == 0 &&
                               regrState.CurrNetwork.CombinedPrecisionError > regrState.BestNetwork.CombinedPrecisionError
                               )
            };
            return instructions;
        }

        /// <summary>
        /// Creates new network and associated trainer.
        /// </summary>
        /// <param name="net">Created network</param>
        /// <param name="trainer">Created associated trainer</param>
        private void NewNetworkAndTrainer(out INonRecurrentNetwork net, out INonRecurrentNetworkTrainer trainer)
        {
            if (IsFF)
            {
                //Feed forward network
                FeedForwardNetworkSettings netCfg = (FeedForwardNetworkSettings)_networkSettings;
                FeedForwardNetwork ffn = new FeedForwardNetwork(_trainingBundle.InputVectorCollection[0].Length, _trainingBundle.OutputVectorCollection[0].Length, netCfg);
                net = ffn;
                if (netCfg.TrainerCfg.GetType() == typeof(QRDRegrTrainerSettings))
                {
                    trainer = new QRDRegrTrainer(ffn, _trainingBundle.InputVectorCollection, _trainingBundle.OutputVectorCollection, (QRDRegrTrainerSettings)netCfg.TrainerCfg, _rand);
                }
                else if (netCfg.TrainerCfg.GetType() == typeof(RidgeRegrTrainerSettings))
                {
                    trainer = new RidgeRegrTrainer(ffn, _trainingBundle.InputVectorCollection, _trainingBundle.OutputVectorCollection, (RidgeRegrTrainerSettings)netCfg.TrainerCfg, _rand);
                }
                else if (netCfg.TrainerCfg.GetType() == typeof(ElasticRegrTrainerSettings))
                {
                    trainer = new ElasticRegrTrainer(ffn, _trainingBundle.InputVectorCollection, _trainingBundle.OutputVectorCollection, (ElasticRegrTrainerSettings)netCfg.TrainerCfg);
                }
                else if (netCfg.TrainerCfg.GetType() == typeof(RPropTrainerSettings))
                {
                    trainer = new RPropTrainer(ffn, _trainingBundle.InputVectorCollection, _trainingBundle.OutputVectorCollection, (RPropTrainerSettings)netCfg.TrainerCfg, _rand);
                }
                else
                {
                    throw new ArgumentException($"Unknown trainer {netCfg.TrainerCfg}");
                }
            }
            else if(IsPP)
            {
                //Parallel perceptron network
                //Check num of output values is 1
                if(_trainingBundle.OutputVectorCollection[0].Length != 1)
                {
                    throw new Exception("In case of parallel perceptron is allowed only single output value.");
                }
                ParallelPerceptronSettings netCfg = (ParallelPerceptronSettings)_networkSettings;
                ParallelPerceptron ppn = new ParallelPerceptron(_trainingBundle.InputVectorCollection[0].Length, netCfg);
                net = ppn;
                trainer = new PDeltaRuleTrainer(ppn, _trainingBundle.InputVectorCollection, _trainingBundle.OutputVectorCollection, netCfg.PDeltaRuleTrainerCfg, _rand);
            }
            else
            {
                throw new Exception("Unknown network settings");
            }
            net.RandomizeWeights(_rand);
            return;
        }

        /// <summary>
        /// Builds trained network
        /// </summary>
        /// <returns>Trained network</returns>
        public TrainedNetwork Build()
        {
            TrainedNetwork bestNetwork = null;
            int lastImprovementEpoch = 0;
            //Create network and trainer
            NewNetworkAndTrainer(out INonRecurrentNetwork net, out INonRecurrentNetworkTrainer trainer);
            //Iterate training cycles
            while (trainer.Iteration())
            {
                //Restart lastImprovementEpoch when new trainer's attempt started
                lastImprovementEpoch = trainer.AttemptEpoch == 1 ? 1 : lastImprovementEpoch;
                //Compute current error statistics after training iteration
                //Training data part
                TrainedNetwork currNetwork = new TrainedNetwork
                {
                    NetworkName = _networkName,
                    BinBorder = _binBorder,
                    Network = net,
                    TrainerInfoMessage = trainer.InfoMessage,
                    TrainingErrorStat = net.ComputeBatchErrorStat(_trainingBundle.InputVectorCollection, _trainingBundle.OutputVectorCollection, out List<double[]> trainingComputedOutputsCollection)
                };
                if (BinaryOutput)
                {
                    currNetwork.TrainingBinErrorStat = new BinErrStat(_binBorder, trainingComputedOutputsCollection, _trainingBundle.OutputVectorCollection);
                    currNetwork.CombinedBinaryError = currNetwork.TrainingBinErrorStat.TotalErrStat.Sum;
                }
                currNetwork.CombinedPrecisionError = currNetwork.TrainingErrorStat.ArithAvg;
                //Testing data part
                currNetwork.TestingErrorStat = net.ComputeBatchErrorStat(_testingBundle.InputVectorCollection, _testingBundle.OutputVectorCollection, out List<double[]> testingComputedOutputsCollection);
                currNetwork.CombinedPrecisionError = Math.Max(currNetwork.CombinedPrecisionError, currNetwork.TestingErrorStat.ArithAvg);
                if (BinaryOutput)
                {
                    currNetwork.TestingBinErrorStat = new BinErrStat(_binBorder, testingComputedOutputsCollection, _testingBundle.OutputVectorCollection);
                    currNetwork.CombinedBinaryError = Math.Max(currNetwork.CombinedBinaryError, currNetwork.TestingBinErrorStat.TotalErrStat.Sum);
                }
                //First initialization of the best network
                bestNetwork = bestNetwork ?? currNetwork.DeepClone();
                //RegrState instance
                BuildingState regrState = new BuildingState(_networkName, _binBorder, _foldNum, _numOfFolds, trainer.Attempt, trainer.MaxAttempt, trainer.AttemptEpoch, trainer.MaxAttemptEpoch, currNetwork, bestNetwork, lastImprovementEpoch);
                //Call controller
                BuildingInstr instructions = _controller(regrState);
                //Better?
                if (instructions.CurrentIsBetter)
                {
                    //Adopt current regression unit as a best one
                    bestNetwork = currNetwork.DeepClone();
                    regrState.BestNetwork = bestNetwork;
                    lastImprovementEpoch = trainer.AttemptEpoch;
                }
                //Raise notification event
                RegressionEpochDone(regrState, instructions.CurrentIsBetter);
                //Process instructions
                if (instructions.StopProcess)
                {
                    break;
                }
                else if (instructions.StopCurrentAttempt)
                {
                    if (!trainer.NextAttempt())
                    {
                        break;
                    }
                }
            }//while (iteration)
            //Create statistics of the best network weights
            bestNetwork.OutputWeightsStat = bestNetwork.Network.ComputeWeightsStat();
            return bestNetwork;
        }


        //Inner classes
        /// <summary>
        /// The class contains information needed to control network building (regression) process.
        /// This class is also used for progeress changed event.
        /// </summary>
        [Serializable]
        public class BuildingState
        {
            //Attribute properties
            /// <summary>
            /// Name of the network
            /// </summary>
            public string NetworkName { get; }
            /// <summary>
            /// If specified, it indicates that the whole network output is binary and specifies numeric border where GE network output is decided as a 1 and LT output as a 0.
            /// </summary>
            public double BinBorder { get; }
            /// <summary>
            /// Current fold number
            /// </summary>
            public int FoldNum { get; }
            /// <summary>
            /// Total number of the folds
            /// </summary>
            public int NumOfFolds { get; }
            /// <summary>
            /// Current regression attempt number 
            /// </summary>
            public int RegrAttemptNumber { get; }
            /// <summary>
            /// Maximum number of regression attempts
            /// </summary>
            public int RegrMaxAttempts { get; }
            /// <summary>
            /// Current epoch number
            /// </summary>
            public int Epoch { get; }
            /// <summary>
            /// Maximum nuber of epochs
            /// </summary>
            public int MaxEpochs { get; }
            /// <summary>
            /// Contains current network and related important error statistics.
            /// </summary>
            public TrainedNetwork CurrNetwork { get; }
            /// <summary>
            /// Contains the best network for now and related important error statistics.
            /// </summary>
            public TrainedNetwork BestNetwork { get; set; }
            /// <summary>
            /// Specifies when was lastly found an improvement
            /// </summary>
            public int LastImprovementEpoch { get; set; }

            /// <summary>
            /// Creates an initialized instance
            /// </summary>
            /// <param name="networkName">Name of the network</param>
            /// <param name="binBorder">If specified, it indicates that the whole network output is binary and specifies numeric border where GE network output is decided as a 1 and LT output as a 0.</param>
            /// <param name="foldNum">Current fold number</param>
            /// <param name="numOfFolds">Total number of the folds</param>
            /// <param name="regrAttemptNumber">Current regression attempt number</param>
            /// <param name="regrMaxAttempts">Maximum number of regression attempts</param>
            /// <param name="epoch">Current epoch number</param>
            /// <param name="maxEpochs">Maximum nuber of epochs</param>
            /// <param name="currNetwork">Contains current network and related important error statistics.</param>
            /// <param name="bestNetwork">Contains the best network for now and related important error statistics.</param>
            /// <param name="lastImprovementEpoch">Specifies when was lastly found an improvement.</param>
            public BuildingState(string networkName,
                                 double binBorder,
                                 int foldNum,
                                 int numOfFolds,
                                 int regrAttemptNumber,
                                 int regrMaxAttempts,
                                 int epoch,
                                 int maxEpochs,
                                 TrainedNetwork currNetwork,
                                 TrainedNetwork bestNetwork,
                                 int lastImprovementEpoch
                                 )
            {
                NetworkName = networkName;
                BinBorder = binBorder;
                FoldNum = foldNum;
                NumOfFolds = numOfFolds;
                RegrAttemptNumber = regrAttemptNumber;
                RegrMaxAttempts = regrMaxAttempts;
                Epoch = epoch;
                MaxEpochs = maxEpochs;
                CurrNetwork = currNetwork;
                BestNetwork = bestNetwork;
                LastImprovementEpoch = lastImprovementEpoch;
                return;
            }

            //Properties
            /// <summary>
            /// Indicates that the whole network output is binary
            /// </summary>
            public bool BinaryOutput { get { return !double.IsNaN(BinBorder); } }

            //Methods
            /// <summary>
            /// Builds string containing information about the regression progress.
            /// </summary>
            /// <param name="margin">Specifies how many spaces to be at the begining of the line.</param>
            /// <returns>Built text line</returns>
            public string GetProgressInfo(int margin = 0)
            {
                //Build progress text message
                StringBuilder progressText = new StringBuilder();
                progressText.Append(new string(' ', margin));
                progressText.Append("Regression: ");
                progressText.Append(NetworkName);
                progressText.Append(", Fold/Attempt/Epoch: ");
                progressText.Append(FoldNum.ToString().PadLeft(NumOfFolds.ToString().Length, '0') + "/");
                progressText.Append(RegrAttemptNumber.ToString().PadLeft(RegrMaxAttempts.ToString().Length, '0') + "/");
                progressText.Append(Epoch.ToString().PadLeft(MaxEpochs.ToString().Length, '0'));
                progressText.Append(", DSet-Sizes: (");
                progressText.Append(CurrNetwork.TrainingErrorStat.NumOfSamples.ToString() + ", ");
                progressText.Append(CurrNetwork.TestingErrorStat.NumOfSamples.ToString() + ")");
                progressText.Append(", Best-Train: ");
                progressText.Append(BestNetwork.TrainingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
                if (BinaryOutput)
                {
                    //Append binary errors
                    progressText.Append("/" + BestNetwork.TrainingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                    progressText.Append("/" + BestNetwork.TrainingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
                }
                progressText.Append(", Best-Test: ");
                progressText.Append(BestNetwork.TestingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
                if (BinaryOutput)
                {
                    //Append binary errors
                    progressText.Append("/" + BestNetwork.TestingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                    progressText.Append("/" + BestNetwork.TestingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
                }
                progressText.Append(", Curr-Train: ");
                progressText.Append(CurrNetwork.TrainingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
                if (BinaryOutput)
                {
                    //Append binary errors
                    progressText.Append("/" + CurrNetwork.TrainingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                    progressText.Append("/" + CurrNetwork.TrainingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
                }
                progressText.Append(", Curr-Test: ");
                progressText.Append(CurrNetwork.TestingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
                if (BinaryOutput)
                {
                    //Append binary errors
                    progressText.Append("/" + CurrNetwork.TestingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                    progressText.Append("/" + CurrNetwork.TestingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
                }
                progressText.Append($" [{BestNetwork.TrainerInfoMessage}]");
                return progressText.ToString();
            }


        }//BuildingState

        /// <summary>
        /// Contains instructions for the network building (regression) process
        /// </summary>
        public class BuildingInstr
        {
            //Attribute properties
            /// <summary>
            /// Indicates whether to terminate the current regression attempt
            /// </summary>
            public bool StopCurrentAttempt { get; set; } = false;
            /// <summary>
            /// Indicates whether to terminate the entire regression process
            /// </summary>
            public bool StopProcess { get; set; } = false;
            /// <summary>
            /// This is the most important switch indicating whether the CurrNetwork is better than
            /// the BestNetwork
            /// </summary>
            public bool CurrentIsBetter { get; set; } = false;

        }//BuildingInstr

    }//TrainedNetworkBuilder

}//Namespace