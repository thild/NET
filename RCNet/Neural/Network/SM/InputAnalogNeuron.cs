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
    /// Input neuron is the special type of very simple neuron. Its purpose is only to mediate
    /// external input for a synapse.
    /// </summary>
    [Serializable]
    public class InputAnalogNeuron : INeuron
    {
        //Attribute properties
        /// <summary>
        /// Home pool identificator and neuron placement within the pool
        /// Note that Input neuron home PoolID is always -1, because Input neurons do not belong to a physical pool.
        /// </summary>
        public NeuronPlacement Placement { get; }

        /// <summary>
        /// Neuron's key statistics
        /// </summary>
        public NeuronStatistics Statistics { get; }

        /// <summary>
        /// Neuron's role within the reservoir (excitatory or inhibitory)
        /// Note that Input neuron is always excitatory.
        /// </summary>
        public CommonEnums.NeuronRole Role { get { return CommonEnums.NeuronRole.Excitatory; } }

        /// <summary>
        /// Specifies whether to use neuron's secondary predictor.
        /// Input neuron never generates secondary predictor
        /// </summary>
        public bool UseSecondaryPredictor { get { return false; } }

        /// <summary>
        /// Type of the output signal (spike or analog)
        /// This is an analog neuron.
        /// </summary>
        public ActivationFactory.FunctionOutputSignalType OutputType { get { return ActivationFactory.FunctionOutputSignalType.Analog; } }

        /// <summary>
        /// Output signal range
        /// </summary>
        public Interval OutputRange { get; }

        /// <summary>
        /// Constant bias.
        /// Note that Input neuron has bias always 0.
        /// </summary>
        public double Bias { get { return 0; } }

        /// <summary>
        /// Output signal
        /// </summary>
        public double OutputSignal { get; private set; }

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
        private double _stimuli;

        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="inputFieldIdx">Index of the corresponding reservoir's input field.</param>
        /// <param name="inputRange">
        /// Range of input value.
        /// It is very recommended to have input values normalized and standardized before
        /// they are passed as an input.
        /// </param>
        public InputAnalogNeuron(int inputFieldIdx, Interval inputRange)
        {
            Placement = new NeuronPlacement(inputFieldIdx, - 1, inputFieldIdx, 0, inputFieldIdx, 0, 0);
            Statistics = new NeuronStatistics();
            OutputRange = inputRange.DeepClone();
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
            _stimuli = 0;
            if(statistics)
            {
                Statistics.Reset();
            }
            return;
        }

        /// <summary>
        /// Stores new incoming stimulation.
        /// </summary>
        /// <param name="stimuli">Input stimulation</param>
        public void NewStimuli(double stimuli)
        {
            _stimuli = stimuli.Bound();
            return;
        }

        /// <summary>
        /// Computes neuron's new output signal and updates statistics
        /// </summary>
        /// <param name="collectStatistics">Specifies whether to update internal statistics</param>
        public void NewState(bool collectStatistics)
        {
            OutputSignal = _stimuli;
            if (collectStatistics)
            {
                Statistics.Update(_stimuli, NeuronStatistics.NormalizedStateRange.Rescale(_stimuli, OutputRange), _stimuli);
            }
            return;
        }
 

    }//InputAnalogNeuron

}//Namespace
