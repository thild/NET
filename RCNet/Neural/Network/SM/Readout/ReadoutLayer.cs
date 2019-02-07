﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using RCNet.MathTools;
using RCNet.Neural.Activation;
using RCNet.Neural.Data;
using RCNet.Extensions;

namespace RCNet.Neural.Network.SM.Readout
{
    /// <summary>
    /// Class implements the common readout layer for the reservoir computing methods
    /// </summary>
    [Serializable]
    public class ReadoutLayer
    {
        //Constants
        /// <summary>
        /// Default reserve of normalizer range kept for ability to operate future unseen data
        /// </summary>
        public const double NormalizerDefaultReserve = 0.1d;

        //Static attributes
        /// <summary>
        /// Input and output data will be normalized to this range before the usage
        /// </summary>
        public static readonly Interval DataRange = new Interval(-1, 1);
        
        //Attributes
        /// <summary>
        /// Collection of normalizers of input predictors
        /// </summary>
        private Normalizer[] _predictorNormalizerCollection;
        /// <summary>
        /// Collection of normalizers of output values
        /// </summary>
        private Normalizer[] _outputNormalizerCollection;
        /// <summary>
        /// Mapping of specific predictors to readout units
        /// </summary>
        private PredictorsMapper _predictorsMapper;
        /// <summary>
        /// Maximum number of the folds
        /// </summary>
        public const int MaxNumOfFolds = 100;
        /// <summary>
        /// Maximum part of available samples useable for test purposes
        /// </summary>
        public const double MaxRatioOfTestData = 1d/3d;
        /// <summary>
        /// Minimum length of the test dataset
        /// </summary>
        public const int MinLengthOfTestDataset = 2;
        //Attributes
        /// <summary>
        /// Readout layer configuration
        /// </summary>
        private ReadoutLayerSettings _settings;
        /// <summary>
        /// Collection of clusters of trained readout units. One cluster of units per output field.
        /// </summary>
        private ReadoutUnit[][] _clusterCollection;
        /// <summary>
        /// Cluster overall error statistics collection
        /// </summary>
        private List<ClusterErrStatistics> _clusterErrStatisticsCollection;



        //Constructor
        /// <summary>
        /// Creates an uninitialized instance
        /// </summary>
        /// <param name="settings">Readout layer configuration</param>
        public ReadoutLayer(ReadoutLayerSettings settings)
        {
            _settings = settings.DeepClone();
            _predictorNormalizerCollection = null;
            _outputNormalizerCollection = null;
            _predictorsMapper = null;
            foreach (ReadoutLayerSettings.ReadoutUnitSettings rus in _settings.ReadoutUnitCfgCollection)
            {
                if (!rus.OutputRange.BelongsTo(DataRange.Min) || !rus.OutputRange.BelongsTo(DataRange.Max))
                {
                    throw new Exception($"Readout unit {rus.Name} does not support data range <{DataRange.Min}; {DataRange.Max}>.");
                }
            }
            //Clusters
            _clusterCollection = new ReadoutUnit[_settings.ReadoutUnitCfgCollection.Count][];
            _clusterErrStatisticsCollection = new List<ClusterErrStatistics>();
            return;
        }

        /// <summary>
        /// Builds readout layer.
        /// Prepares prediction clusters containing trained readout units.
        /// </summary>
        /// <param name="dataBundle">Collection of input predictors and associated desired output values</param>
        /// <param name="regressionController">Regression controller delegate</param>
        /// <param name="regressionControllerData">An user object</param>
        /// <param name="predictorsMapper">Optional specific mapping of predictors to readout units</param>
        /// <returns>Returned ResultComparativeBundle is something like a protocol.
        /// There is recorded fold by fold (unit by unit) predicted and corresponding ideal values.
        /// This is the pesimistic approach. Real results on unseen data could be better due to the clustering synergy.
        /// </returns>
        public ResultComparativeBundle Build(VectorBundle dataBundle,
                                             ReadoutUnit.RegressionCallbackDelegate regressionController,
                                             Object regressionControllerData,
                                             PredictorsMapper predictorsMapper = null
                                             )
        {
            //Basic checks
            int numOfPredictors = dataBundle.InputVectorCollection[0].Length;
            int numOfOutputs = dataBundle.OutputVectorCollection[0].Length;
            if (numOfPredictors == 0)
            {
                throw new Exception("Number of predictors must be greater tham 0.");
            }
            if (numOfOutputs != _settings.ReadoutUnitCfgCollection.Count)
            {
                throw new Exception("Incorrect number of ideal output values in the vector.");
            }

            //Normalization of predictors and output data collections
            //Allocation of normalizers
            _predictorNormalizerCollection = new Normalizer[numOfPredictors];
            for(int i = 0; i < numOfPredictors; i++)
            {
                _predictorNormalizerCollection[i] = new Normalizer(DataRange, NormalizerDefaultReserve, true, false);
            }
            _outputNormalizerCollection = new Normalizer[numOfOutputs];
            for (int i = 0; i < numOfOutputs; i++)
            {
                bool classificationTask = (_settings.ReadoutUnitCfgCollection[i].TaskType == CommonEnums.TaskType.Classification);
                _outputNormalizerCollection[i] = new Normalizer(DataRange,
                                                                classificationTask ? 0 : NormalizerDefaultReserve,
                                                                classificationTask ? false : true,
                                                                false
                                                                );
            }
            //Normalizers adjustment
            for(int pairIdx = 0; pairIdx < dataBundle.InputVectorCollection.Count; pairIdx++)
            {
                //Checks
                if(dataBundle.InputVectorCollection[pairIdx].Length != numOfPredictors)
                {
                    throw new Exception("Inconsistent number of predictors in the predictors collection.");
                }
                if(dataBundle.OutputVectorCollection[pairIdx].Length != numOfOutputs)
                {
                    throw new Exception("Inconsistent number of values in the ideal values collection.");
                }
                //Adjust predictors normalizers
                for (int i = 0; i < numOfPredictors; i++)
                {
                    _predictorNormalizerCollection[i].Adjust(dataBundle.InputVectorCollection[pairIdx][i]);
                }
                //Adjust outputs normalizers
                for (int i = 0; i < numOfOutputs; i++)
                {
                    _outputNormalizerCollection[i].Adjust(dataBundle.OutputVectorCollection[pairIdx][i]);
                }
            }
            //Data normalization
            //Allocation
            List<double[]> predictorsCollection = new List<double[]>(dataBundle.InputVectorCollection.Count);
            List<double[]> idealOutputsCollection = new List<double[]>(dataBundle.OutputVectorCollection.Count);
            //Normalization
            for (int pairIdx = 0; pairIdx < dataBundle.InputVectorCollection.Count; pairIdx++)
            {
                //Predictors
                double[] predictors = new double[numOfPredictors];
                for (int i = 0; i < numOfPredictors; i++)
                {
                    predictors[i] = _predictorNormalizerCollection[i].Normalize(dataBundle.InputVectorCollection[pairIdx][i]);
                }
                predictorsCollection.Add(predictors);
                //Outputs
                double[] outputs = new double[numOfOutputs];
                for (int i = 0; i < numOfOutputs; i++)
                {
                    outputs[i] = _outputNormalizerCollection[i].Normalize(dataBundle.OutputVectorCollection[pairIdx][i]);
                }
                idealOutputsCollection.Add(outputs);
            }
            //Data processing
            //Random object initialization
            Random rand = new Random(0);
            //Predictors mapper (specified or default)
            _predictorsMapper = predictorsMapper ?? new PredictorsMapper(numOfPredictors);
            //Allocation of computed and ideal vectors for result comparative bundle
            List<double[]> validationComputedVectorCollection = new List<double[]>(idealOutputsCollection.Count);
            List<double[]> validationIdealVectorCollection = new List<double[]>(idealOutputsCollection.Count);
            for (int i = 0; i < idealOutputsCollection.Count; i++)
            {
                validationComputedVectorCollection.Add(new double[numOfOutputs]);
                validationIdealVectorCollection.Add(new double[numOfOutputs]);
            }
            //Test dataset size
            if (_settings.TestDataRatio > MaxRatioOfTestData)
            {
                throw new ArgumentException($"Test dataset size is greater than {MaxRatioOfTestData.ToString(CultureInfo.InvariantCulture)}", "TestDataSetSize");
            }
            int testDataSetLength = (int)Math.Round(idealOutputsCollection.Count * _settings.TestDataRatio, 0);
            if (testDataSetLength < MinLengthOfTestDataset)
            {
                throw new ArgumentException($"Num of test samples is less than {MinLengthOfTestDataset.ToString(CultureInfo.InvariantCulture)}", "TestDataSetSize");
            }
            //Number of folds
            int numOfFolds = _settings.NumOfFolds;
            if (numOfFolds <= 0)
            {
                //Auto setup
                numOfFolds = idealOutputsCollection.Count / testDataSetLength;
                if (numOfFolds > MaxNumOfFolds)
                {
                    numOfFolds = MaxNumOfFolds;
                }
            }
            //Create shuffled copy of the data
            VectorBundle shuffledData = new VectorBundle(predictorsCollection, idealOutputsCollection);
            shuffledData.Shuffle(rand);
            //Data inspection, preparation of datasets and training of ReadoutUnits
            //Clusters of readout units (one cluster for each output field)
            for (int clusterIdx = 0; clusterIdx < _settings.ReadoutUnitCfgCollection.Count; clusterIdx++)
            {
                _clusterCollection[clusterIdx] = new ReadoutUnit[numOfFolds];
                List<double[]> idealValueCollection = new List<double[]>(idealOutputsCollection.Count);
                BinDistribution refBinDistr = null;
                if (_settings.ReadoutUnitCfgCollection[clusterIdx].TaskType == CommonEnums.TaskType.Classification)
                {
                    //Reference binary distribution is relevant only for classification task
                    refBinDistr = new BinDistribution(DataRange.Mid);
                }
                //Transformation to a single value vectors and data analysis
                foreach (double[] idealVector in shuffledData.OutputVectorCollection)
                {
                    double[] value = new double[1];
                    value[0] = idealVector[clusterIdx];
                    idealValueCollection.Add(value);
                    if (_settings.ReadoutUnitCfgCollection[clusterIdx].TaskType == CommonEnums.TaskType.Classification)
                    {
                        //Reference binary distribution is relevant only for classification task
                        refBinDistr.Update(value);
                    }
                }
                List<VectorBundle> subBundleCollection = null;
                List<double[]> readoutUnitInputVectorCollection = _predictorsMapper.CreateVectorCollection(_settings.ReadoutUnitCfgCollection[clusterIdx].Name, shuffledData.InputVectorCollection);
                //Datasets preparation is depending on the task type
                if (_settings.ReadoutUnitCfgCollection[clusterIdx].TaskType == CommonEnums.TaskType.Classification)
                {
                    //Classification task
                    subBundleCollection = DivideSamplesForClassificationTask(readoutUnitInputVectorCollection,
                                                                             idealValueCollection,
                                                                             refBinDistr,
                                                                             testDataSetLength
                                                                             );
                }
                else
                {
                    //Forecast task
                    subBundleCollection = DivideSamplesForForecastTask(readoutUnitInputVectorCollection,
                                                                       idealValueCollection,
                                                                       testDataSetLength
                                                                       );
                }
                //Find best unit per each fold in the cluster.
                ClusterErrStatistics ces = new ClusterErrStatistics(_settings.ReadoutUnitCfgCollection[clusterIdx].TaskType, numOfFolds, refBinDistr);
                int arrayPos = 0;
                for (int foldIdx = 0; foldIdx < numOfFolds; foldIdx++)
                {
                    //Build training samples
                    List<double[]> trainingPredictorsCollection = new List<double[]>();
                    List<double[]> trainingIdealValueCollection = new List<double[]>();
                    for (int bundleIdx = 0; bundleIdx < subBundleCollection.Count; bundleIdx++)
                    {
                        if (bundleIdx != foldIdx)
                        {
                            trainingPredictorsCollection.AddRange(subBundleCollection[bundleIdx].InputVectorCollection);
                            trainingIdealValueCollection.AddRange(subBundleCollection[bundleIdx].OutputVectorCollection);
                        }
                    }
                    //Call training regression to get the best fold's readout unit.
                    //The best unit becomes to be the predicting cluster member.
                    _clusterCollection[clusterIdx][foldIdx] = ReadoutUnit.CreateTrained(_settings.ReadoutUnitCfgCollection[clusterIdx].TaskType,
                                                                                        clusterIdx,
                                                                                        foldIdx + 1,
                                                                                        numOfFolds,
                                                                                        refBinDistr,
                                                                                        trainingPredictorsCollection,
                                                                                        trainingIdealValueCollection,
                                                                                        subBundleCollection[foldIdx].InputVectorCollection,
                                                                                        subBundleCollection[foldIdx].OutputVectorCollection,
                                                                                        rand,
                                                                                        _settings.ReadoutUnitCfgCollection[clusterIdx],
                                                                                        regressionController,
                                                                                        regressionControllerData
                                                                                        );
                    //Cluster error statistics & data for validation bundle (pesimistic approach)
                    for (int sampleIdx = 0; sampleIdx < subBundleCollection[foldIdx].OutputVectorCollection.Count; sampleIdx++)
                    {
                        
                        double nrmComputedValue = _clusterCollection[clusterIdx][foldIdx].Network.Compute(subBundleCollection[foldIdx].InputVectorCollection[sampleIdx])[0];
                        double natComputedValue = _outputNormalizerCollection[clusterIdx].Naturalize(nrmComputedValue);
                        double natIdealValue = _outputNormalizerCollection[clusterIdx].Naturalize(subBundleCollection[foldIdx].OutputVectorCollection[sampleIdx][0]);
                        ces.Update(nrmComputedValue,
                                   subBundleCollection[foldIdx].OutputVectorCollection[sampleIdx][0],
                                   natComputedValue,
                                   natIdealValue);
                        validationIdealVectorCollection[arrayPos][clusterIdx] = natIdealValue;
                        validationComputedVectorCollection[arrayPos][clusterIdx] = natComputedValue;
                        ++arrayPos;
                    }

                }//foldIdx
                _clusterErrStatisticsCollection.Add(ces);

            }//clusterIdx
            //Validation bundle is returned. 
            return new ResultComparativeBundle(validationComputedVectorCollection, validationIdealVectorCollection);
        }

        //Properties
        /// <summary>
        /// Cluster overall error statistics collection
        /// </summary>
        public List<ClusterErrStatistics> ClusterErrStatisticsCollection
        {
            get
            {
                //Create and return the deep clone
                List<ClusterErrStatistics> clonedStatisticsCollection = new List<ClusterErrStatistics>(_clusterErrStatisticsCollection.Count);
                foreach(ClusterErrStatistics ces in _clusterErrStatisticsCollection)
                {
                    clonedStatisticsCollection.Add(ces.DeepClone());
                }
                return clonedStatisticsCollection;
            }
        }

        //Static methods
        /// <summary>
        /// Builds report string containing information about the regression progress.
        /// It is usually called from the RegressionControl user implementation.
        /// </summary>
        /// <param name="inArgs">>Contains all the necessary information to control the regression.</param>
        /// <param name="bestReadoutUnit">Currently the best readout unit.</param>
        /// <param name="margin">Specifies how many spaces to be at the begining of the row.</param>
        /// <returns>Built text report</returns>
        public static string GetProgressReport(ReadoutUnit.RegressionControlInArgs inArgs,
                                               ReadoutUnit bestReadoutUnit,
                                               int margin = 0
                                               )
        {
            //Build progress text message
            StringBuilder progressText = new StringBuilder();
            progressText.Append(new string(' ', margin));
            progressText.Append("OutputField: ");
            progressText.Append(inArgs.OutputFieldName);
            progressText.Append(", Fold/Attempt/Epoch: ");
            progressText.Append(inArgs.FoldNum.ToString().PadLeft(inArgs.NumOfFolds.ToString().Length, '0') + "/");
            progressText.Append(inArgs.RegrAttemptNumber.ToString().PadLeft(inArgs.RegrMaxAttempts.ToString().Length, '0') + "/");
            progressText.Append(inArgs.Epoch.ToString().PadLeft(inArgs.MaxEpochs.ToString().Length, '0'));
            progressText.Append(", DSet-Sizes: (");
            progressText.Append(inArgs.CurrReadoutUnit.TrainingErrorStat.NumOfSamples.ToString() + ", ");
            progressText.Append(inArgs.CurrReadoutUnit.TestingErrorStat.NumOfSamples.ToString() + ")");
            progressText.Append(", Best-Train: ");
            progressText.Append(bestReadoutUnit.TrainingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
            if (inArgs.TaskType == CommonEnums.TaskType.Classification)
            {
                //Append binary errors
                progressText.Append("/" + bestReadoutUnit.TrainingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                progressText.Append("/" + bestReadoutUnit.TrainingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
            }
            progressText.Append(", Best-Test: ");
            progressText.Append(bestReadoutUnit.TestingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
            if (inArgs.TaskType == CommonEnums.TaskType.Classification)
            {
                //Append binary errors
                progressText.Append("/" + bestReadoutUnit.TestingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                progressText.Append("/" + bestReadoutUnit.TestingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
            }
            progressText.Append(", Curr-Train: ");
            progressText.Append(inArgs.CurrReadoutUnit.TrainingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
            if (inArgs.TaskType == CommonEnums.TaskType.Classification)
            {
                //Append binary errors
                progressText.Append("/" + inArgs.CurrReadoutUnit.TrainingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                progressText.Append("/" + inArgs.CurrReadoutUnit.TrainingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
            }
            progressText.Append(", Curr-Test: ");
            progressText.Append(inArgs.CurrReadoutUnit.TestingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture));
            if (inArgs.TaskType == CommonEnums.TaskType.Classification)
            {
                //Append binary errors
                progressText.Append("/" + inArgs.CurrReadoutUnit.TestingBinErrorStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture));
                progressText.Append("/" + inArgs.CurrReadoutUnit.TestingBinErrorStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture));
            }
            progressText.Append($" [{bestReadoutUnit.TrainerInfoMessage}]");
            return progressText.ToString();
        }

        //Methods
        /// <summary>
        /// Returns results of the readout units training
        /// </summary>
        /// <param name="margin">Specifies how many spaces should be at the begining of each row.</param>
        /// <returns>Built text report</returns>
        public string GetTrainingResultsReport(int margin)
        {
            string leftMargin = margin == 0 ? string.Empty : new string(' ', margin);
            StringBuilder sb = new StringBuilder();
            //Training results
            for (int outputIdx = 0; outputIdx < _settings.ReadoutUnitCfgCollection.Count; outputIdx++)
            {
                ReadoutLayer.ClusterErrStatistics ces = _clusterErrStatisticsCollection[outputIdx];
                sb.Append(leftMargin + $"Output field [{_settings.ReadoutUnitCfgCollection[outputIdx].Name}]" + Environment.NewLine);
                if (_settings.ReadoutUnitCfgCollection[outputIdx].TaskType == CommonEnums.TaskType.Classification)
                {
                    //Classification task report
                    sb.Append(leftMargin + $"  Classification of negative samples" + Environment.NewLine);
                    sb.Append(leftMargin + $"    Number of samples: {ces.BinaryErrStat.BinValErrStat[0].NumOfSamples}" + Environment.NewLine);
                    sb.Append(leftMargin + $"     Number of errors: {ces.BinaryErrStat.BinValErrStat[0].Sum.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"           Error rate: {ces.BinaryErrStat.BinValErrStat[0].ArithAvg.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"             Accuracy: {(1 - ces.BinaryErrStat.BinValErrStat[0].ArithAvg).ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"  Classification of positive samples" + Environment.NewLine);
                    sb.Append(leftMargin + $"    Number of samples: {ces.BinaryErrStat.BinValErrStat[1].NumOfSamples}" + Environment.NewLine);
                    sb.Append(leftMargin + $"     Number of errors: {ces.BinaryErrStat.BinValErrStat[1].Sum.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"           Error rate: {ces.BinaryErrStat.BinValErrStat[1].ArithAvg.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"             Accuracy: {(1 - ces.BinaryErrStat.BinValErrStat[1].ArithAvg).ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"  Overall classification results" + Environment.NewLine);
                    sb.Append(leftMargin + $"    Number of samples: {ces.BinaryErrStat.TotalErrStat.NumOfSamples}" + Environment.NewLine);
                    sb.Append(leftMargin + $"     Number of errors: {ces.BinaryErrStat.TotalErrStat.Sum.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"           Error rate: {ces.BinaryErrStat.TotalErrStat.ArithAvg.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"             Accuracy: {(1 - ces.BinaryErrStat.TotalErrStat.ArithAvg).ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                }
                else
                {
                    //Forecast task report
                    sb.Append(leftMargin + $"  Number of samples: {ces.NatPrecissionErrStat.NumOfSamples}" + Environment.NewLine);
                    sb.Append(leftMargin + $"      Biggest error: {ces.NatPrecissionErrStat.Max.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"     Smallest error: {ces.NatPrecissionErrStat.Min.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                    sb.Append(leftMargin + $"      Average error: {ces.NatPrecissionErrStat.ArithAvg.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns results of the readout units training
        /// </summary>
        /// <param name="predictedValues">Vector of computed values.</param>
        /// <param name="margin">Specifies how many spaces should be at the begining of each row.</param>
        /// <returns>Built text report</returns>
        public string GetForecastReport(double[] predictedValues, int margin)
        {
            string leftMargin = margin == 0 ? string.Empty : new string(' ', margin);
            StringBuilder sb = new StringBuilder();
            //Results
            for (int outputIdx = 0; outputIdx < _settings.ReadoutUnitCfgCollection.Count; outputIdx++)
            {
                sb.Append(leftMargin + $"Output field [{_settings.ReadoutUnitCfgCollection[outputIdx].Name}]: {predictedValues[outputIdx].ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine);
            }
            return sb.ToString();
        }


        private double Compute(double[] predictors, int clusterIdx)
        {
            WeightedAvg wAvg = new WeightedAvg();
            string readoutUnitName = _settings.ReadoutUnitCfgCollection[clusterIdx].Name;
            for (int readoutUnitIdx = 0; readoutUnitIdx < _clusterCollection[clusterIdx].Length; readoutUnitIdx++)
            {
                double[] outputValue = _clusterCollection[clusterIdx][readoutUnitIdx].Network.Compute(_predictorsMapper.CreateVector(readoutUnitName, predictors));
                double weight = _clusterCollection[clusterIdx][readoutUnitIdx].TrainingErrorStat.NumOfSamples;
                if(_clusterCollection[clusterIdx][readoutUnitIdx].TestingErrorStat != null)
                {
                    weight += _clusterCollection[clusterIdx][readoutUnitIdx].TestingErrorStat.NumOfSamples;
                }
                wAvg.AddSampleValue(outputValue[0], weight);
                // Or flat weight
                //wAvg.AddSampleValue(outputValue[0], 1);
            }
            return wAvg.Avg;
        }

        /// <summary>
        /// Normalizes predictors vector
        /// </summary>
        /// <param name="predictors">Predictors vector</param>
        private double[] NormalizePredictors(double[] predictors)
        {
            //Check
            if (predictors.Length != _predictorNormalizerCollection.Length)
            {
                throw new Exception("Incorrect length of predictors vector.");
            }
            double[] nrmPredictors = new double[predictors.Length];
            for (int i = 0; i < predictors.Length; i++)
            {
                nrmPredictors[i] = _predictorNormalizerCollection[i].Normalize(predictors[i]);
            }
            return nrmPredictors;
        }

        /// <summary>
        /// Naturalizes output values vector
        /// </summary>
        /// <param name="outputs">Output values vector</param>
        private double[] NaturalizeOutputs(double[] outputs)
        {
            double[] natOutputs = new double[outputs.Length];
            for (int i = 0; i < outputs.Length; i++)
            {
                natOutputs[i] = _outputNormalizerCollection[i].Naturalize(outputs[i]);
            }
            return natOutputs;
        }

        /// <summary>
        /// Computes output fields
        /// </summary>
        /// <param name="predictors">The predictors</param>
        public double[] Compute(double[] predictors)
        {
            //Check readyness
            if(_predictorNormalizerCollection == null || _outputNormalizerCollection == null)
            {
                throw new Exception("Readout layer is not trained. Build function has to be called before Compute function can be used.");
            }
            double[] nrmPredictors = NormalizePredictors(predictors);
            double[] outputVector = new double[_clusterCollection.Length];
            for(int clusterIdx = 0; clusterIdx < _clusterCollection.Length; clusterIdx++)
            {
                outputVector[clusterIdx] = Compute(nrmPredictors, clusterIdx);
            }
            double[] natOuputVector = NaturalizeOutputs(outputVector);
            return natOuputVector;
        }
        
        private List<VectorBundle> DivideSamplesForClassificationTask(List<double[]> predictorsCollection,
                                                                      List<double[]> idealValueCollection,
                                                                      BinDistribution refBinDistr,
                                                                      int bundleSize
                                                                      )
        {
            int numOfBundles = idealValueCollection.Count / bundleSize;
            List<VectorBundle> bundleCollection = new List<VectorBundle>(numOfBundles);
            //Scan
            int[] bin0SampleIdxs = new int[refBinDistr.NumOf[0]];
            int bin0SamplesPos = 0;
            int[] bin1SampleIdxs = new int[refBinDistr.NumOf[1]];
            int bin1SamplesPos = 0;
            for (int i = 0; i < idealValueCollection.Count; i++)
            {
                if(idealValueCollection[i][0] >= refBinDistr.BinBorder)
                {
                    bin1SampleIdxs[bin1SamplesPos++] = i;
                }
                else
                {
                    bin0SampleIdxs[bin0SamplesPos++] = i;
                }
            }
            //Division
            int bundleBin0Count = Math.Max(1, refBinDistr.NumOf[0] / numOfBundles);
            int bundleBin1Count = Math.Max(1, refBinDistr.NumOf[1] / numOfBundles);
            if(bundleBin0Count * numOfBundles > bin0SampleIdxs.Length)
            {
                throw new Exception("Insufficient bin 0 samples");
            }
            if (bundleBin1Count * numOfBundles > bin1SampleIdxs.Length)
            {
                throw new Exception("Insufficient bin 1 samples");
            }
            //Bundles creation
            bin0SamplesPos = 0;
            bin1SamplesPos = 0;
            for(int bundleNum = 0; bundleNum < numOfBundles; bundleNum++)
            {
                VectorBundle bundle = new VectorBundle();
                //Bin 0
                for (int i = 0; i < bundleBin0Count; i++)
                {
                    bundle.InputVectorCollection.Add(predictorsCollection[bin0SampleIdxs[bin0SamplesPos]]);
                    bundle.OutputVectorCollection.Add(idealValueCollection[bin0SampleIdxs[bin0SamplesPos]]);
                    ++bin0SamplesPos;
                }
                //Bin 1
                for (int i = 0; i < bundleBin1Count; i++)
                {
                    bundle.InputVectorCollection.Add(predictorsCollection[bin1SampleIdxs[bin1SamplesPos]]);
                    bundle.OutputVectorCollection.Add(idealValueCollection[bin1SampleIdxs[bin1SamplesPos]]);
                    ++bin1SamplesPos;
                }
                bundleCollection.Add(bundle);
            }
            //Remaining samples
            for(int i = 0; i < bin0SampleIdxs.Length - bin0SamplesPos; i++)
            {
                int bundleIdx = i % bundleCollection.Count;
                bundleCollection[bundleIdx].InputVectorCollection.Add(predictorsCollection[bin0SampleIdxs[bin0SamplesPos + i]]);
                bundleCollection[bundleIdx].OutputVectorCollection.Add(idealValueCollection[bin0SampleIdxs[bin0SamplesPos + i]]);
            }
            for (int i = 0; i < bin1SampleIdxs.Length - bin1SamplesPos; i++)
            {
                int bundleIdx = i % bundleCollection.Count;
                bundleCollection[bundleIdx].InputVectorCollection.Add(predictorsCollection[bin1SampleIdxs[bin1SamplesPos + i]]);
                bundleCollection[bundleIdx].OutputVectorCollection.Add(idealValueCollection[bin1SampleIdxs[bin1SamplesPos + i]]);
            }
            return bundleCollection;
        }

        private List<VectorBundle> DivideSamplesForForecastTask(List<double[]> predictorsCollection,
                                                                List<double[]> idealValueCollection,
                                                                int bundleSize
                                                                )
        {
            int numOfBundles = idealValueCollection.Count / bundleSize;
            List<VectorBundle> bundleCollection = new List<VectorBundle>(numOfBundles);
            //Bundles creation
            int samplesPos = 0;
            for (int bundleNum = 0; bundleNum < numOfBundles; bundleNum++)
            {
                VectorBundle bundle = new VectorBundle();
                for (int i = 0; i < bundleSize && samplesPos < idealValueCollection.Count; i++)
                {
                    bundle.InputVectorCollection.Add(predictorsCollection[samplesPos]);
                    bundle.OutputVectorCollection.Add(idealValueCollection[samplesPos]);
                    ++samplesPos;
                }
                bundleCollection.Add(bundle);
            }
            //Remaining samples
            for (int i = 0; i < idealValueCollection.Count - samplesPos; i++)
            {
                int bundleIdx = i % bundleCollection.Count;
                bundleCollection[bundleIdx].InputVectorCollection.Add(predictorsCollection[samplesPos + i]);
                bundleCollection[bundleIdx].OutputVectorCollection.Add(idealValueCollection[samplesPos + i]);
            }
            return bundleCollection;
        }

        //Inner classes
        /// <summary>
        /// Maps specific predictors to readout units
        /// </summary>
        [Serializable]
        public class PredictorsMapper
        {
            /// <summary>
            /// Number of all available predictors
            /// </summary>
            private readonly int _numOfPredictors;
            /// <summary>
            /// Mapping of readout unit to switches determining what predictors are assigned to.
            /// </summary>
            private readonly Dictionary<string, ReadoutUnitMap> _mapCollection;

            /// <summary>
            /// Creates uninitialized instance
            /// </summary>
            /// <param name="numOfPredictors">Total number of available predictors</param>
            public PredictorsMapper(int numOfPredictors)
            {
                _numOfPredictors = numOfPredictors;
                _mapCollection = new Dictionary<string, ReadoutUnitMap>();
                return;
            }

            /// <summary>
            /// Adds new mapping for ReadoutUntit
            /// </summary>
            /// <param name="readoutUnitName"></param>
            /// <param name="map">Boolean switches indicating if to use available prdictor for the ReadoutUnit</param>
            public void Add(string readoutUnitName, bool[] map)
            {
                if(map.Length != _numOfPredictors)
                {
                    throw new ArgumentException("Incorrect number of switches in the map", "map");
                }
                if (readoutUnitName.Length == 0)
                {
                    throw new ArgumentException("ReadoutUnit name can not be empty", "readoutUnitName");
                }
                if (_mapCollection.ContainsKey(readoutUnitName))
                {
                    throw new ArgumentException($"Mapping already contains mapping for ReadoutUnit {readoutUnitName}", "readoutUnitName");
                }
                ReadoutUnitMap rum = new ReadoutUnitMap(map);
                if(rum.VectorLength == 0)
                {
                    throw new ArgumentException("Map does not contain mapped predictors", "map");
                }
                _mapCollection.Add(readoutUnitName, rum);
                return;
            }

            private double[] CreateVector(double[] predictors, bool[] map, int vectorLength)
            {
                if (predictors.Length != map.Length)
                {
                    throw new ArgumentException("Incorrect number of predictors", "predictors");
                }
                double[] vector = new double[vectorLength];
                for(int i = 0, vIdx = 0; i < predictors.Length; i++)
                {
                    if(map[i])
                    {
                        vector[vIdx] = predictors[i];
                        ++vIdx;
                    }
                }
                return vector;
            }

            /// <summary>
            /// Creates input vector containing specific subset of predictors for the ReadoutUnit.
            /// </summary>
            /// <param name="readoutUnitName">ReadoutUnit name</param>
            /// <param name="predictors">Available predictors</param>
            public double[] CreateVector(string readoutUnitName, double[] predictors)
            {
                if (_mapCollection.ContainsKey(readoutUnitName))
                {
                    ReadoutUnitMap rum = _mapCollection[readoutUnitName];
                    return CreateVector(predictors, rum.Map, rum.VectorLength);
                }
                else
                {
                    if (predictors.Length != _numOfPredictors)
                    {
                        throw new ArgumentException("Incorrect number of predictors", "predictors");
                    }
                    return (double[])predictors.Clone();
                }
            }

            /// <summary>
            /// Creates input vector collection where each vector containing specific subset of predictors for the ReadoutUnit.
            /// </summary>
            /// <param name="readoutUnitName">ReadoutUnit name</param>
            /// <param name="predictorsCollection">Collection of available predictors</param>
            public List<double[]> CreateVectorCollection(string readoutUnitName, List<double[]> predictorsCollection)
            {
                List<double[]> vectorCollection = new List<double[]>(predictorsCollection.Count);
                ReadoutUnitMap rum = null;
                if (_mapCollection.ContainsKey(readoutUnitName))
                {
                    rum = _mapCollection[readoutUnitName];
                }
                foreach(double[] predictors in predictorsCollection)
                {
                    if(rum == null)
                    {
                        vectorCollection.Add((double[])predictors.Clone());
                    }
                    else
                    {
                        vectorCollection.Add(CreateVector(predictors, rum.Map, rum.VectorLength));
                    }
                }
                return vectorCollection;
            }

            //Inner classes
            /// <summary>
            /// Maps specific predictors to readout unit
            /// </summary>
            [Serializable]
            private class ReadoutUnitMap
            {
                //Attribute properties
                /// <summary>
                /// Boolean switches indicating if to use available prdictor for this ReadoutUnit
                /// </summary>
                public bool[] Map { get; set; }
                /// <summary>
                /// Resulting length of ReadoutUnit's input vector (number of true switches in the Map)
                /// </summary>
                public int VectorLength { get; private set; }

                /// <summary>
                /// Creates initialized instance
                /// </summary>
                /// <param name="map">Boolean switches indicating if to use available prdictor for this ReadoutUnit.</param>
                public ReadoutUnitMap(bool[] map)
                {
                    Map = (bool[])map.Clone();
                    VectorLength = 0;
                    foreach (bool bSwitch in Map)
                    {
                        if (bSwitch) ++VectorLength;
                    }
                    return;
                }

            }//ReadoutUnitMap
        }

        /// <summary>
        /// Overall error statistics of the cluster of readout units
        /// </summary>
        [Serializable]
        public class ClusterErrStatistics
        {
            //Property attributes
            /// <summary>
            /// Type of the solved neural task
            /// </summary>
            public CommonEnums.TaskType TaskType { get; }
            /// <summary>
            /// Number of readout units within the cluster
            /// </summary>
            public int NumOfReadoutUnits { get; }
            /// <summary>
            /// Error statistics of the distance between computed and ideal valus in natural form
            /// </summary>
            public BasicStat NatPrecissionErrStat { get; }
            /// <summary>
            /// Error statistics of the distance between computed and ideal valus in normalized form
            /// </summary>
            public BasicStat NrmPrecissionErrStat { get; }
            /// <summary>
            /// Statistics of the binary errors.
            /// Relevant only for the classification task type.
            /// </summary>
            public BinErrStat BinaryErrStat { get; }

            /// <summary>
            /// Constructs an instance prepared for initialization (updates)
            /// </summary>
            /// <param name="taskType"></param>
            /// <param name="numOfReadoutUnits"></param>
            /// <param name="refBinDistr"></param>
            public ClusterErrStatistics(CommonEnums.TaskType taskType, int numOfReadoutUnits, BinDistribution refBinDistr)
            {
                TaskType = taskType;
                NumOfReadoutUnits = numOfReadoutUnits;
                NatPrecissionErrStat = new BasicStat();
                NrmPrecissionErrStat = new BasicStat();
                BinaryErrStat = null;
                if (TaskType == CommonEnums.TaskType.Classification)
                {
                    BinaryErrStat = new BinErrStat(refBinDistr);
                }
                return;
            }

            /// <summary>
            /// A deep copy constructor
            /// </summary>
            /// <param name="source">Source instance</param>
            public ClusterErrStatistics(ClusterErrStatistics source)
            {
                TaskType = source.TaskType;
                NumOfReadoutUnits = source.NumOfReadoutUnits;
                NatPrecissionErrStat = new BasicStat(source.NatPrecissionErrStat);
                NrmPrecissionErrStat = new BasicStat(source.NrmPrecissionErrStat);
                BinaryErrStat = null;
                if (TaskType == CommonEnums.TaskType.Classification)
                {
                    BinaryErrStat = new BinErrStat(source.BinaryErrStat);
                }
                return;
            }

            /// <summary>
            /// Updates cluster statistics
            /// </summary>
            /// <param name="nrmComputedValue">Normalized value computed by the cluster</param>
            /// <param name="nrmIdealValue">Normalized ideal value</param>
            /// <param name="natComputedValue">Naturalized value computed by the cluster</param>
            /// <param name="natIdealValue">Naturalized ideal value</param>
            public void Update(double nrmComputedValue, double nrmIdealValue, double natComputedValue, double natIdealValue)
            {
                NatPrecissionErrStat.AddSampleValue(Math.Abs(natComputedValue - natIdealValue));
                NrmPrecissionErrStat.AddSampleValue(Math.Abs(nrmComputedValue - nrmIdealValue));
                if (TaskType == CommonEnums.TaskType.Classification)
                {
                    BinaryErrStat.Update(nrmComputedValue, nrmIdealValue);
                }
                return;
            }

            /// <summary>
            /// Creates a deep copy instance of this instance
            /// </summary>
            public ClusterErrStatistics DeepClone()
            {
                return new ClusterErrStatistics(this);
            }

        }//ClusterErrStatistics

    }//ReadoutLayer

}//Namespace
