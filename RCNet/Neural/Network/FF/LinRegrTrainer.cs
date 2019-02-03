﻿using System;
using System.Collections.Generic;
using RCNet.Extensions;
using RCNet.Neural.Activation;
using RCNet.MathTools.MatrixMath;
using System.Globalization;

namespace RCNet.Neural.Network.FF
{
    /// <summary>
    /// Implements the linear regression trainer.
    /// Principle is to add each iteration less and less piece of white-noise to predictors
    /// and then perform the standard linear regression.
    /// This technique allows to find more stable weight solution than just a linear regression
    /// of pure predictors.
    /// FF network has to have only output layer with the Identity activation.
    /// </summary>
    [Serializable]
    public class LinRegrTrainer : INonRecurrentNetworkTrainer
    {
        //Attribute Properties
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
        private LinRegrTrainerSettings _settings;
        private FeedForwardNetwork _net;
        private List<double[]> _inputVectorCollection;
        private List<double[]> _outputVectorCollection;
        private List<Matrix> _outputSingleColMatrixCollection;
        private Random _rand;
        private readonly double[] _alphas;

        //Constructor
        /// <summary>
        /// Constructs new instance of linear regression trainer
        /// </summary>
        /// <param name="net">FF network to be trained</param>
        /// <param name="inputVectorCollection">Predictors (input)</param>
        /// <param name="outputVectorCollection">Ideal outputs (the same number of rows as number of inputs)</param>
        /// <param name="settings">Startup parameters of the trainer</param>
        /// <param name="rand">Random object to be used for adding a white-noise to predictors</param>
        public LinRegrTrainer(FeedForwardNetwork net,
                              List<double[]> inputVectorCollection,
                              List<double[]> outputVectorCollection,
                              LinRegrTrainerSettings settings,
                              Random rand
                              )
        {
            //Check network readyness
            if (!net.Finalized)
            {
                throw new Exception("Can´t create LinRegr trainer. Network structure was not finalized.");
            }
            //Check network conditions
            if (net.LayerCollection.Count != 1 || !(net.LayerCollection[0].Activation is Identity))
            {
                throw new Exception("Can´t create LinRegr trainer. Network structure is not complient (single layer having Identity activation).");
            }
            //Check samples conditions
            if(inputVectorCollection.Count < inputVectorCollection[0].Length + 1)
            {
                throw new Exception("Can´t create LinRegr trainer. Insufficient number of training samples. Minimum is " + (inputVectorCollection[0].Length + 1).ToString() + ".");
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
            _alphas = new double[MaxAttemptEpoch];
            //Plan the iterations alphas
            double coeff = (MaxAttemptEpoch > 1) ? _settings.MaxStretch / (MaxAttemptEpoch - 1) : 0;
            for (int i = 0; i < MaxAttemptEpoch; i++)
            {
                _alphas[i] = _settings.HiNoiseIntensity - _settings.HiNoiseIntensity * Math.Tanh(i* coeff);
                _alphas[i] = Math.Max(0, _alphas[i]);
            }
            //Ensure the last alpha is zero
            _alphas[MaxAttemptEpoch - 1] = 0;
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
        private Matrix PreparePredictors(double noiseIntensity)
        {
            Matrix predictors = new Matrix(_inputVectorCollection.Count, _net.NumOfInputValues + 1);
            for (int row = 0; row < _inputVectorCollection.Count; row++)
            {
                //Predictors
                for(int col = 0; col < _net.NumOfInputValues; col++)
                {
                    double predictor = _inputVectorCollection[row][col];
                    predictors.Data[row][col] = predictor * (1d + _rand.NextDouble(noiseIntensity * _settings.ZeroMargin, noiseIntensity, true, RandomClassExtensions.DistributionType.Uniform));
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
            if (AttemptEpoch == MaxAttemptEpoch)
            {
                //Max epoch reached, try new attempt
                if (!NextAttempt())
                {
                    //Next attempt is not available
                    return false;
                }
            }
            //Next epoch
            ++AttemptEpoch;
            //Noise intensity
            double intensity = _alphas[Math.Min(MaxAttemptEpoch, AttemptEpoch) - 1];
            InfoMessage = $"noiseIntensity={intensity.ToString(CultureInfo.InvariantCulture)}";
            //Adjusted predictors
            Matrix predictors = PreparePredictors((double)intensity);
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
            return true;
        }

    }//LinRegrTrainer

}//Namespace

