﻿using RCNet.MathTools;
using System;

namespace RCNet.Neural.Network.NonRecurrent
{
    /// <summary>
    /// Contains trained network and related important error statistics from training/testing.
    /// Expected is only single output network.
    /// </summary>
    [Serializable]
    public class TrainedNetwork
    {
        //Attribute properties
        /// <summary>
        /// Name of the trained network
        /// </summary>
        public string NetworkName { get; set; }
        /// <summary>
        /// If specified, it indicates that the network ideal output is binary and specifies numeric border where GE network output is decided as a 1 and LT output as a 0.
        /// </summary>
        public double BinBorder { get; set; }
        /// <summary>
        /// Trained network
        /// </summary>
        public INonRecurrentNetwork Network { get; set; }
        /// <summary>
        /// Informative message from trainer
        /// </summary>
        public string TrainerInfoMessage { get; set; }
        /// <summary>
        /// Training error statistics
        /// </summary>
        public BasicStat TrainingErrorStat { get; set; }
        /// <summary>
        /// Training binary error statistics.
        /// Relevant only when network ideal output is binary.
        /// </summary>
        public BinErrStat TrainingBinErrorStat { get; set; }
        /// <summary>
        /// Testing error statistics
        /// </summary>
        public BasicStat TestingErrorStat { get; set; }
        /// <summary>
        /// Testing binary error statistics.
        /// Relevant only when network ideal output is binary.
        /// </summary>
        public BinErrStat TestingBinErrorStat { get; set; }
        /// <summary>
        /// Statistics of the network weights
        /// </summary>
        public BasicStat OutputWeightsStat { get; set; }
        /// <summary>
        /// Achieved training/testing combined precision error
        /// </summary>
        public double CombinedPrecisionError { get; set; }
        /// <summary>
        /// Achieved training/testing combined binary error.
        /// Relevant only when network ideal output is binary.
        /// </summary>
        public double CombinedBinaryError { get; set; }
        /// <summary>
        /// Expected normalized precision accuracy of this network (0...1).
        /// </summary>
        public double ExpectedPrecisionAccuracy { get; set; }
        /// <summary>
        /// Expected normalized binary accuracy of this network (0...1).
        /// Relevant only when network ideal output is binary.
        /// </summary>
        public double ExpectedBinaryAccuracy { get; set; }

        //Constructors
        /// <summary>
        /// Creates an unitialized instance
        /// </summary>
        public TrainedNetwork()
        {
            NetworkName = string.Empty;
            BinBorder = double.NaN;
            Network = null;
            TrainingErrorStat = null;
            TrainingBinErrorStat = null;
            TestingErrorStat = null;
            TestingBinErrorStat = null;
            OutputWeightsStat = null;
            CombinedPrecisionError = -1d;
            CombinedBinaryError = -1d;
            ExpectedPrecisionAccuracy = -1d;
            ExpectedBinaryAccuracy = -1d;
            return;
        }

        /// <summary>
        /// The deep copy constructor.
        /// </summary>
        /// <param name="source">Source instance</param>
        public TrainedNetwork(TrainedNetwork source)
        {
            NetworkName = source.NetworkName;
            BinBorder = source.BinBorder;
            Network = null;
            if (source.Network != null)
            {
                Network = source.Network.DeepClone();
            }
            TrainerInfoMessage = source.TrainerInfoMessage;
            TrainingErrorStat = source.TrainingErrorStat?.DeepClone();
            TrainingBinErrorStat = source.TrainingBinErrorStat?.DeepClone();
            TestingErrorStat = source.TestingErrorStat?.DeepClone();
            TestingBinErrorStat = source.TestingBinErrorStat?.DeepClone();
            OutputWeightsStat = source.OutputWeightsStat?.DeepClone();
            CombinedPrecisionError = source.CombinedPrecisionError;
            CombinedBinaryError = source.CombinedBinaryError;
            ExpectedPrecisionAccuracy = source.ExpectedPrecisionAccuracy;
            ExpectedBinaryAccuracy = source.ExpectedBinaryAccuracy;
            return;
        }

        //Properties
        /// <summary>
        /// Indicates that the network ideal output is binary
        /// </summary>
        public bool BinaryOutput { get { return !double.IsNaN(BinBorder); } }

        //Methods
        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public TrainedNetwork DeepClone()
        {
            return new TrainedNetwork(this);
        }

    }//TrainedNetwork
}//Namespace
