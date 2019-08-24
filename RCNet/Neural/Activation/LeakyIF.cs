﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.Extensions;
using RCNet.MathTools;
using RCNet.MathTools.Differential;
using RCNet.MathTools.VectorMath;

namespace RCNet.Neural.Activation
{
    /// <summary>
    /// Implements Leaky Integrate and Fire neuron model.
    /// For more information visit http://neuronaldynamics.epfl.ch/online/Ch1.S3.html
    /// </summary>
    [Serializable]
    public class LeakyIF : ODESpikingMembrane
    {
        //Constants

        //Static members
        //protected static Interval _stimuliRange = new Interval(-3.629, 12.887);
        protected static Interval _stimuliRange = new Interval(-3.629/5, 12.887/5);

        //Attributes
        //Parameters
        private readonly double _timeScale;
        private readonly double _resistance;

        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="timeScale">Membrane time scale (ms)</param>
        /// <param name="resistance">Membrane resistance (Mohm)</param>
        /// <param name="restV">Membrane rest potential (mV)</param>
        /// <param name="resetV">Membrane reset potential (mV)</param>
        /// <param name="firingThresholdV">Membrane firing threshold (mV)</param>
        /// <param name="refractoryPeriods">Number of after spike computation cycles while an input stimuli is ignored (ms)</param>
        /// <param name="solverMethod">ODE numerical solver method</param>
        /// <param name="solverCompSteps">ODE numerical solver computation steps of the time step</param>
        public LeakyIF(double timeScale,
                       double resistance,
                       double restV,
                       double resetV,
                       double firingThresholdV,
                       int refractoryPeriods,
                       ODENumSolver.Method solverMethod,
                       int solverCompSteps
                       )
            : base(restV,
                   resetV,
                   firingThresholdV,
                   refractoryPeriods,
                   solverMethod,
                   1,
                   solverCompSteps,
                   1,
                   100,
                   1
                   )
        {
            _timeScale = timeScale;
            _resistance = resistance;
            return;
        }


        //Properties
        /// <summary>
        /// Optimal strength of the stimulation
        /// </summary>
        public override double OptimalStimulationStrength { get { return 2.725; } }

        /// <summary>
        /// Range of reasonable incoming current
        /// </summary>
        public override Interval StimuliRange { get { return _stimuliRange; } }

        //Methods
        /// <summary>
        /// Triggered when membrane is firing a spike
        /// </summary>
        protected override void OnFiring()
        {
            //Does nothing
            return;
        }

        /// <summary>
        /// LeakyIF autonomous ordinary differential equation.
        /// </summary>
        /// <param name="t">Time. Not used in autonomous ODE.</param>
        /// <param name="v">Membrane potential</param>
        /// <returns>dvdt</returns>
        protected override Vector MembraneDiffEq(double t, Vector v)
        {
            Vector dvdt = new Vector(1);
            dvdt[VarMembraneVIdx] = (-(v[VarMembraneVIdx] - _restV) + _resistance * _stimuli) / _timeScale;
            return dvdt;
        }

    }//LeakyIF

}//Namespace
