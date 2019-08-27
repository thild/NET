﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.MathTools;

namespace RCNet.Neural.Network.SM.Neuron
{
    /// <summary>
    /// Key statistics of the neuron
    /// </summary>
    [Serializable]
    public class NeuronStatistics
    {
        //Constants
        //Attribute properties
        /// <summary>
        /// Statistics of neuron's input stimulation component
        /// </summary>
        public BasicStat InputStimuliStat { get; }

        /// <summary>
        /// Statistics of neuron's stimulation component incoming from the reservoir
        /// </summary>
        public BasicStat ReservoirStimuliStat { get; }

        /// <summary>
        /// Statistics of neuron's total stimulation (all components together: Bias + Input + Reservoir)
        /// </summary>
        public BasicStat TotalStimuliStat { get; }

        /// <summary>
        /// Statistics of activations
        /// </summary>
        public BasicStat ActivationStat { get; }

        /// <summary>
        /// Statistics of neuron's amitted analog signal
        /// </summary>
        public BasicStat AnalogSignalStat { get; }

        /// <summary>
        /// Statistics of neuron's amitted spiking signal
        /// </summary>
        public BasicStat SpikingSignalStat { get; }


        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        public NeuronStatistics()
        {
            InputStimuliStat = new BasicStat();
            ReservoirStimuliStat = new BasicStat();
            TotalStimuliStat = new BasicStat();
            ActivationStat = new BasicStat();
            AnalogSignalStat = new BasicStat();
            SpikingSignalStat = new BasicStat();
            return;
        }

        //Methods
        /// <summary>
        /// Resets all statistics
        /// </summary>
        public void Reset()
        {
            InputStimuliStat.Reset();
            ReservoirStimuliStat.Reset();
            TotalStimuliStat.Reset();
            ActivationStat.Reset();
            AnalogSignalStat.Reset();
            SpikingSignalStat.Reset();
            return;
        }

        /// <summary>
        /// Updates statistics
        /// </summary>
        /// <param name="iStimuli">Incoming stimulation related to part coming from connected Input neurons</param>
        /// <param name="rStimuli">Incoming stimulation related to part coming from connected reservoir's neurons</param>
        /// <param name="tStimuli">Incoming stimulation (all components together including Bias)</param>
        /// <param name="activationState">Neuron's activation state</param>
        /// <param name="outputSignal">Neuron's output signal</param>
        public void Update(double iStimuli, double rStimuli, double tStimuli, double activationState, double analogSignal, double spikingSignal)
        {
            InputStimuliStat.AddSampleValue(iStimuli);
            ReservoirStimuliStat.AddSampleValue(rStimuli);
            TotalStimuliStat.AddSampleValue(tStimuli);
            ActivationStat.AddSampleValue(activationState);
            AnalogSignalStat.AddSampleValue(analogSignal);
            SpikingSignalStat.AddSampleValue(spikingSignal);
            return;
        }

    }//NeuronStatistics
}//Namespace
