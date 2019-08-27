﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.Extensions;
using RCNet.MathTools;
using RCNet.Neural.Activation;

namespace RCNet.Neural.Network.SM.Neuron
{
    /// <summary>
    /// Input neuron is the special type of very simple neuron. Its purpose is only to mediate
    /// external input for a synapse.
    /// </summary>
    [Serializable]
    public class InputNeuron : INeuron
    {
        //Static attributes
        private static Interval _spikingTargetRange = new Interval(0, 1);

        //Attribute properties
        /// <summary>
        /// Home pool identifierr and neuron placement within the reservoir
        /// Note that Input neuron home PoolID is always -1, because Input neurons do not belong to a physical pool.
        /// </summary>
        public NeuronPlacement Placement { get; }

        /// <summary>
        /// Neuron's key statistics
        /// </summary>
        public NeuronStatistics Statistics { get; }

        /// <summary>
        /// Neuron's role within the reservoir (input, excitatory or inhibitory)
        /// </summary>
        public CommonEnums.NeuronRole Role { get { return CommonEnums.NeuronRole.Input; } }

        /// <summary>
        /// Type of the activation function
        /// </summary>
        public CommonEnums.ActivationType ActivationType { get { return CommonEnums.ActivationType.Analog; } }

        /// <summary>
        /// Output signaling restriction
        /// </summary>
        public CommonEnums.NeuronSignalingRestrictionType SignalingRestriction { get { return CommonEnums.NeuronSignalingRestrictionType.AnalogOnly; } }

        /// <summary>
        /// Constant bias.
        /// </summary>
        public double Bias { get { return 0; } }

        /// <summary>
        /// Computation cycles gone from the last emitted spike
        /// </summary>
        public int SpikeLeak { get { throw new NotImplementedException("SpikeLeak is unsupported for InputNeuron"); } }

        /// <summary>
        /// Specifies, if neuron has already emitted output signal before current signal
        /// </summary>
        public bool AfterFirstSpike { get { return false; } }

        /// <summary>
        /// Value to be passed to readout layer as a primary predictor.
        /// Predictor value does not make sense in case of Input neuron.
        /// </summary>
        public double PrimaryPredictor { get { return double.NaN; } }

        /// <summary>
        /// Value to be passed to readout layer as an augmented predictor.
        /// Augmented predictor value does not make sense in case of Input neuron.
        /// </summary>
        public double SecondaryPredictor { get { return double.NaN; } }

        //Attributes
        private readonly Interval _inputRange;
        private double _iStimuli;
        private double _rStimuli;
        private double _tStimuli;
        private double _analogTargetSignal;
        private double _spikingTargetSignal;

        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="inputEntryPoint">Input entry point coordinates within the reservoir.</param>
        /// <param name="inputFieldIdx">Index of the corresponding reservoir's input field.</param>
        /// <param name="inputRange">
        /// Range of input value.
        /// It is very recommended to have input values normalized and standardized before
        /// they are passed as an input.
        /// </param>
        public InputNeuron(int[] inputEntryPoint, int inputFieldIdx, Interval inputRange)
        {
            Placement = new NeuronPlacement(-1, inputFieldIdx, - 1, inputFieldIdx, 0, inputEntryPoint[0], inputEntryPoint[1], inputEntryPoint[2]);
            _inputRange = inputRange.DeepClone();
            Statistics = new NeuronStatistics();
            Reset(false);
            return;
        }

        //Methods
        /// <summary>
        /// Resets neuron to its initial state
        /// </summary>
        /// <param name="statistics">Specifies whether to reset internal statistics</param>
        public void Reset(bool statistics)
        {
            _iStimuli = 0;
            _rStimuli = 0;
            _tStimuli = 0;
            _analogTargetSignal = 0;
            _spikingTargetSignal = 0;
            if (statistics)
            {
                Statistics.Reset();
            }
            return;
        }

        /// <summary>
        /// Stores new incoming stimulation.
        /// </summary>
        /// <param name="iStimuli">External input analog signal</param>
        /// <param name="rStimuli">Parameter is ignored. Stimulation comming from reservoir neurons is irrelevant. </param>
        public void NewStimulation(double iStimuli, double rStimuli)
        {
            _iStimuli = iStimuli;
            _rStimuli = 0;
            _tStimuli = (_iStimuli + _rStimuli + Bias).Bound();
            return;
        }

        /// <summary>
        /// Prepares new output signal (input for hidden neurons).
        /// </summary>
        /// <param name="collectStatistics">Specifies whether to update internal statistics</param>
        public void ComputeSignal(bool collectStatistics)
        {
            _analogTargetSignal = _tStimuli;
            _spikingTargetSignal = _spikingTargetRange.Rescale(_tStimuli, _inputRange);
            if (collectStatistics)
            {
                Statistics.Update(_iStimuli, _rStimuli, _tStimuli, _tStimuli, _analogTargetSignal, _spikingTargetSignal);
            }
            return;
        }

        /// <summary>
        /// Neuron returns input for neuron having activation of specified type.
        /// </summary>
        /// <param name="targetActivationType">Specifies what type of the signal is required.</param>
        public double GetSignal(CommonEnums.ActivationType targetActivationType)
        {
            if (targetActivationType == CommonEnums.ActivationType.Spiking)
            {
                return _spikingTargetSignal;
            }
            else
            {
                return _analogTargetSignal;
            }
        }


    }//InputNeuron

}//Namespace
