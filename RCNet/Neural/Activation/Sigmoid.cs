﻿using System;
using RCNet.Extensions;
using RCNet.MathTools;

namespace RCNet.Neural.Activation
{
    /// <summary>
    /// Implements Sigmoid activation function
    /// </summary>
    [Serializable]
    public class Sigmoid : AnalogActivationFunction
    {

        //Static members
        private static Interval _stimuliRange = new Interval(-3, 3);
        private static readonly Interval _outputRange = new Interval(0, 1);

        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        public Sigmoid()
            : base()
        {
            return;
        }

        //Properties
        /// <summary>
        /// Optimal strength of the stimulation
        /// </summary>
        public override double OptimalStimulationStrength { get { return 0; } }

        /// <summary>
        /// Range of reasonable incoming current
        /// </summary>
        public override Interval StimuliRange { get { return _stimuliRange; } }

        /// <summary>
        /// Output signal range
        /// </summary>
        public override Interval OutputRange { get { return _outputRange; } }

        //Methods
        /// <summary>
        /// Computes output of the activation function (changes internal state)
        /// </summary>
        /// <param name="x">Activation input</param>
        public override double Compute(double x)
        {
            x = x.Bound();
            return 1d / (1d + Math.Exp(-x));
        }

        /// <summary>
        /// Computes derivative of the activation input (does not change internal state)
        /// </summary>
        /// <param name="c">The result of the activation (Compute method)</param>
        /// <param name="x">Activation input (x argument of the Compute method)</param>
        public override double ComputeDerivative(double c, double x)
        {
            c = c.Bound();
            return c * (1d - c);
        }

    }//Sigmoid

}//Namespace
