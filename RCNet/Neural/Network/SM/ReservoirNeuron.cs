﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.Extensions;
using RCNet.MathTools;
using RCNet.Neural.Activation;

namespace RCNet.Neural.Network.SM
{
    /// <summary>
    /// Reservoir neuron is the main type of the neuron.
    /// Reservoir neuron processes input stimuli and produces output signal.
    /// </summary>
    [Serializable]
    public class ReservoirNeuron : INeuron
    {
        //Attributes
        /// <summary>
        /// Rescalled state value range (allways between -1 and 1)
        /// </summary>
        private static readonly Interval _rescalledStateRange = new Interval(-1, 1);

        /// <summary>
        /// Neuron's activation function (the heart of the neuron)
        /// </summary>
        private IActivationFunction _activation;

        /// <summary>
        /// Relevant for analog neuron (time independent activation functions) only.
        /// If specified, neuron is the leaky intgrator
        /// </summary>
        private double _retainmentRatio;

        /// <summary>
        /// Number of passed neuron computations
        /// </summary>
        private int _numOfComputationCycles;

        /// <summary>
        /// Current state of the neuron in activation function natural form
        /// </summary>
        private double _state;

        /// <summary>
        /// Current state of the neuron rescaled to uniform range
        /// </summary>
        private double _rescaledState;

        /// <summary>
        /// Current signal of the neuron
        /// </summary>
        private double _signal;

        /// <summary>
        /// Transmission signal
        /// </summary>
        private double _transmissionSignal;


        //Attribute properties
        /// <summary>
        /// Home pool identificator and neuron placement within the pool
        /// </summary>
        public NeuronPlacement Placement { get; }

        /// <summary>
        /// Constant bias of the neuron
        /// </summary>
        public double Bias { get; }

        /// <summary>
        /// Statistics of incoming stimulations (input values)
        /// </summary>
        public BasicStat StimuliStat { get; }
        
        /// <summary>
        /// Statistics of neuron rescalled state values
        /// </summary>
        public BasicStat StatesStat { get; }

        /// <summary>
        /// Statistics of neuron output signals
        /// </summary>
        public BasicStat TransmissinSignalStat { get; }





        /// <summary>
        /// Statistics of neuron output positive signals
        /// </summary>
        public BasicStat PositiveTransmissinSignalStat { get; }

        /// <summary>
        /// Statistics of neuron output negative signals
        /// </summary>
        public BasicStat NegativeTransmissinSignalStat { get; }



        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="placement">Home pool identificator and neuron placement within the pool.</param>
        /// <param name="activation">Instantiated activation function.</param>
        /// <param name="retainmentRatio">Retainment ratio. Useable for time independent activations only.</param>
        public ReservoirNeuron(NeuronPlacement placement,
                               IActivationFunction activation,
                               double bias,
                               double retainmentRatio = 0
                               )
        {
            Placement = placement;
            Bias = bias;
            //Check whether activation function input range meets the requirements
            if (activation.InputRange.Min != double.NegativeInfinity.Bound() ||
                activation.InputRange.Max != double.PositiveInfinity.Bound()
               )
            {
                throw new ArgumentException("Input range of the activation function does not meet ReservoirNeuron conditions.", "activation");
            }
            //Check whether activation function output range meets the requirements
            if (activation.OutputRange.Min != -1 || activation.OutputRange.Max != 1)
            {
                throw new ArgumentException("Output range of the activation function does not meet ReservoirNeuron conditions.", "activation");
            }
            //Check retainment ratio and type of activation
            if(activation.TimeDependent && retainmentRatio != 0)
            {
                throw new ArgumentException("For the time dependent activations must be retainmentRatio = 0.", "retainmentRatio");
            }
            //Check retainment ratio
            if (retainmentRatio < 0)
            {
                throw new ArgumentOutOfRangeException("retainmentRatio", "Retainment ratio must be GE 0.");
            }
            _activation = activation;
            _retainmentRatio = retainmentRatio;
            StimuliStat = new BasicStat();
            StatesStat = new BasicStat();
            TransmissinSignalStat = new BasicStat();
            PositiveTransmissinSignalStat = new BasicStat();
            NegativeTransmissinSignalStat = new BasicStat();
            Reset(false);
            return;
        }

        //Properties
        /// <summary>
        /// Stored output signal for transmission purposes
        /// </summary>
        public double TransmissinSignal { get { return _transmissionSignal; } }

        /// <summary>
        /// Value to be passed to readout layer as a predictor value
        /// </summary>
        public double ReadoutPredictorValue
        {
            get
            {
                if(_activation.TimeDependent)
                {
                    //Spiking neuron
                    //This is tricky a bit.
                    return _rescaledState;
                }
                else
                {
                    //Analog neuron
                    return _transmissionSignal;
                }
            }
        }

        //Methods
        /// <summary>
        /// Resets the neuron to its initial state
        /// </summary>
        /// <param name="statistics">Specifies whether to reset also internal statistics</param>
        public void Reset(bool statistics)
        {
            _activation.Reset();
            _numOfComputationCycles = 0;
            _state = 0;
            _rescaledState = 0;
            _signal = 0;
            _transmissionSignal = 0;
            if (statistics)
            {
                StimuliStat.Reset();
                StatesStat.Reset();
                TransmissinSignalStat.Reset();
                PositiveTransmissinSignalStat.Reset();
                NegativeTransmissinSignalStat.Reset();
            }
            return;
        }

        /// <summary>
        /// Prepares and stores transmission signal
        /// </summary>
        /// <param name="collectStatistics">Specifies whether to update internal statistics</param>
        public void PrepareTransmissionSignal(bool collectStatistics)
        {
            _transmissionSignal = _signal;
            if (collectStatistics)
            {
                TransmissinSignalStat.AddSampleValue(_transmissionSignal);
            }
            NegativeTransmissinSignalStat.AddSampleValue(_transmissionSignal < 0 ? _transmissionSignal : 0);
            PositiveTransmissinSignalStat.AddSampleValue(_transmissionSignal > 0 ? _transmissionSignal : 0);
            return;
        }

        /// <summary>
        /// Computes neuron state and output signal.
        /// </summary>
        /// <param name="stimuli">Input stimulation</param>
        /// <param name="collectStatistics">Specifies whether to update internal statistics</param>
        public void Compute(double stimuli, bool collectStatistics)
        {
            stimuli = (stimuli + Bias).Bound();
            if (collectStatistics)
            {
                StimuliStat.AddSampleValue(stimuli);
            }
            if (_retainmentRatio == 0)
            {
                //Spiking activation or analog activation without leaky integration
                //Compute signal and neuron state
                _signal = _activation.Compute(stimuli);
                //Store state
                _state = _activation.InternalState;
                //Compute rescaled state
                _rescaledState = _rescalledStateRange.Min + (((_state - _activation.InternalStateRange.Min) / _activation.InternalStateRange.Span) * _rescalledStateRange.Span);
                _rescaledState.Bound(_rescalledStateRange.Min, _rescalledStateRange.Max);
            }
            else
            {
                //Analog leaky integrator
                if (_numOfComputationCycles == 0)
                {
                    //In case of the first computation, retainment is not applied
                    _state = _activation.Compute(stimuli);
                }
                else
                {
                    //Apply retainment
                    _state = (_retainmentRatio * _state) + (1d - _retainmentRatio) * _activation.Compute(stimuli);
                }
                //Signal equals to state
                _signal = _state;
                //Rescaled state equals to state
                _rescaledState = _state;
            }
            //Statistics
            if (collectStatistics)
            {
                StatesStat.AddSampleValue(_rescaledState);
            }
            //Cycles counter
            ++_numOfComputationCycles;
            return;
        }


    }//InputNeuron

}//Namespace
