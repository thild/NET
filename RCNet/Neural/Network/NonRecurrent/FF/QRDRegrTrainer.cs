﻿using RCNet.Extensions;
using RCNet.MathTools.MatrixMath;
using RCNet.MathTools.PS;
using RCNet.Neural.Activation;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RCNet.Neural.Network.NonRecurrent.FF
{
    /// <summary>
    /// Implements the QRD regression trainer.
    /// Principle is to add each iteration less and less piece of white-noise to predictors
    /// and then perform the standard QR decomposition (regression).
    /// This technique allows to find more stable (generalized) weight solution than just a QR decomposition
    /// of pure predictors.
    /// FF network has to have only output layer with the Identity activation.
    /// </summary>
    [Serializable]
    public class QRDRegrTrainer : INonRecurrentNetworkTrainer
    {
        //Constants
        private const double StopNoiseDifference = 1e-8;
        //Attribute properties
        /// <summary>
        /// Epoch error (MSE).
        /// </summary>
        public double MSE { get; private set; }
        /// <summary>
        /// Max attempt
        /// </summary>
        public int MaxAttempt { get; private set; }
        /// <summary>
        /// Current attempt
        /// </summary>
        public int Attempt { get; private set; }
        /// <summary>
        /// Max epoch
        /// </summary>
        public int MaxAttemptEpoch { get; private set; }
        /// <summary>
        /// Current epoch (incremented each call of Iteration)
        /// </summary>
        public int AttemptEpoch { get; private set; }
        /// <summary>
        /// Informative message from the trainer
        /// </summary>
        public string InfoMessage { get; private set; }

        //Attributes
        private readonly QRDRegrTrainerSettings _settings;
        private readonly FeedForwardNetwork _net;
        private readonly List<double[]> _inputVectorCollection;
        private readonly List<double[]> _outputVectorCollection;
        private readonly List<Matrix> _outputSingleColMatrixCollection;
        private readonly Random _rand;
        private double _currNoise;
        private ParamSeeker _noiseSeeker;

        //Constructor
        /// <summary>
        /// Constructs an initialized instance
        /// </summary>
        /// <param name="net">FF network to be trained</param>
        /// <param name="inputVectorCollection">Predictors (input)</param>
        /// <param name="outputVectorCollection">Ideal outputs (the same number of rows as number of inputs)</param>
        /// <param name="rand">Random object to be used for adding a white-noise to predictors</param>
        /// <param name="settings">Startup parameters of the trainer</param>
        public QRDRegrTrainer(FeedForwardNetwork net,
                              List<double[]> inputVectorCollection,
                              List<double[]> outputVectorCollection,
                              QRDRegrTrainerSettings settings,
                              Random rand
                              )
        {
            //Check network readyness
            if (!net.Finalized)
            {
                throw new InvalidOperationException($"Can´t create trainer. Network structure was not finalized.");
            }
            //Check network conditions
            if (net.LayerCollection.Count != 1 || !(net.LayerCollection[0].Activation is Identity))
            {
                throw new InvalidOperationException($"Can´t create trainer. Network structure is not complient (single layer having Identity activation).");
            }
            //Check samples conditions
            if (inputVectorCollection.Count < inputVectorCollection[0].Length + 1)
            {
                throw new InvalidOperationException($"Can´t create trainer. Insufficient number of training samples {inputVectorCollection.Count}. Minimum is {(inputVectorCollection[0].Length + 1)}.");
            }
            //Parameters
            _settings = settings;
            MaxAttempt = _settings.NumOfAttempts;
            MaxAttemptEpoch = _settings.NumOfAttemptEpochs;
            _net = net;
            _rand = rand;
            _inputVectorCollection = inputVectorCollection;
            _outputVectorCollection = outputVectorCollection;
            _outputSingleColMatrixCollection = new List<Matrix>(_net.NumOfOutputValues);
            for (int outputIdx = 0; outputIdx < _net.NumOfOutputValues; outputIdx++)
            {
                Matrix outputSingleColMatrix = new Matrix(_outputVectorCollection.Count, 1);
                for (int row = 0; row < _outputVectorCollection.Count; row++)
                {
                    //Output
                    outputSingleColMatrix.Data[row][0] = _outputVectorCollection[row][outputIdx];
                }
                _outputSingleColMatrixCollection.Add(outputSingleColMatrix);
            }
            //Start training attempt
            Attempt = 0;
            NextAttempt();
            return;
        }

        //Properties
        /// <summary>
        /// FF network beeing trained
        /// </summary>
        public INonRecurrentNetwork Net { get { return _net; } }

        //Methods
        private Matrix PreparePredictors(double noise)
        {
            Matrix predictors = new Matrix(_inputVectorCollection.Count, _net.NumOfInputValues + 1);
            for (int row = 0; row < _inputVectorCollection.Count; row++)
            {
                //Predictors
                for (int col = 0; col < _net.NumOfInputValues; col++)
                {
                    double predictor = _inputVectorCollection[row][col];
                    predictors.Data[row][col] = predictor * (1d + _rand.NextRangedUniformDouble(noise * _settings.NoiseZeroMargin, noise) * _rand.NextSign());
                }
                //Add constant bias to predictors
                predictors.Data[row][_net.NumOfInputValues] = 1;
            }
            return predictors;
        }


        /// <summary>
        /// Starts next training attempt
        /// </summary>
        public bool NextAttempt()
        {
            if (Attempt < MaxAttempt)
            {
                //Next attempt is allowed
                ++Attempt;
                //Reset
                _noiseSeeker = new ParamSeeker(_settings.NoiseSeekerCfg);
                _currNoise = 1e6;
                MSE = 0;
                AttemptEpoch = 0;
                return true;
            }
            else
            {
                //Max attempt reached -> do nothhing and return false
                return false;
            }
        }

        /// <summary>
        /// Performs training iteration.
        /// </summary>
        public bool Iteration()
        {
            //Fetch next noise intensity
            double newNoise = _noiseSeeker.Next;
            //Check continue conditions
            if (AttemptEpoch == MaxAttemptEpoch || Math.Abs(_currNoise - newNoise) < StopNoiseDifference)
            {
                //Try new attempt
                if (!NextAttempt())
                {
                    //Next attempt is not available
                    return false;
                }
                else
                {
                    newNoise = _noiseSeeker.Next;
                }
            }
            //Next epoch
            ++AttemptEpoch;
            _currNoise = newNoise;
            InfoMessage = $"noise={_currNoise.ToString(CultureInfo.InvariantCulture)}";
            //Adjusted predictors
            Matrix predictors = PreparePredictors(_currNoise);
            //Decomposition
            QRD decomposition = null;
            bool useableQRD = true;
            try
            {
                //Try to create QRD. Any exception signals numerical unstability
                decomposition = new QRD(predictors);
            }
            catch
            {
                //Creation of QRD object throws exception. QRD object is not ready for use.
                useableQRD = false;
                if (AttemptEpoch == 1)
                {
                    //No previous successful epoch so stop training
                    throw;
                }
            }
            if (useableQRD)
            {
                //QRD is ready for use (low probability of numerical unstability)
                //New weights
                double[] newWeights = new double[_net.NumOfWeights];
                //Regression for each output neuron
                for (int outputIdx = 0; outputIdx < _net.NumOfOutputValues; outputIdx++)
                {
                    //Regression
                    Matrix solution = decomposition.Solve(_outputSingleColMatrixCollection[outputIdx]);
                    //Store weights
                    //Input weights
                    for (int i = 0; i < solution.NumOfRows - 1; i++)
                    {
                        newWeights[outputIdx * _net.NumOfInputValues + i] = solution.Data[i][0];
                    }
                    //Bias weight
                    newWeights[_net.NumOfOutputValues * _net.NumOfInputValues + outputIdx] = solution.Data[solution.NumOfRows - 1][0];
                }
                //Set new weights and compute error
                _net.SetWeights(newWeights);
                MSE = _net.ComputeBatchErrorStat(_inputVectorCollection, _outputVectorCollection).MeanSquare;
            }
            _noiseSeeker.ProcessError(MSE);
            return true;
        }

    }//QRDRegrTrainer

}//Namespace

