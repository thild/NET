﻿using RCNet.Extensions;
using RCNet.MathTools;
using System;

namespace RCNet.Neural.Activation
{
    /// <summary>
    /// Implements SoftExponential activation function
    /// </summary>
    [Serializable]
    public class SoftExponential : AnalogActivationFunction
    {
        //Constants

        //Attributes
        private static readonly Interval _outputRange = new Interval(double.NegativeInfinity.Bound(), double.PositiveInfinity.Bound());

        /// <summary>
        /// Alpha
        /// </summary>
        public double Alpha { get; }

        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="alpha">alpha</param>
        public SoftExponential(double alpha)
            : base()
        {
            Alpha = alpha.Bound();
            return;
        }

        //Properties
        /// <summary>
        /// Output range of the Compute method
        /// </summary>
        public override Interval OutputRange { get { return _outputRange; } }

        //Methods
        /// <summary>
        /// Computes output of the activation function
        /// </summary>
        /// <param name="x">Activation input</param>
        public override double Compute(double x)
        {
            x = x.Bound();
            if (Alpha < 0)
            {
                return (-(Math.Log(1 - Alpha * (x + Alpha)) / Alpha)).Bound();
            }
            else if (Alpha == 0)
            {
                return x;
            }
            else
            {
                return ((Math.Exp(Alpha * x) - 1) / Alpha + Alpha).Bound();
            }
        }

        /// <summary>
        /// Computes derivative (with respect to x)
        /// </summary>
        /// <param name="c">The result of the activation (Compute method)</param>
        /// <param name="x">Activation input (x argument of the Compute method)</param>
        public override double ComputeDerivative(double c, double x)
        {
            x = x.Bound();
            if (Alpha < 0)
            {
                return 1d / (1d - Alpha * (Alpha + x));
            }
            else
            {
                return Math.Exp(Alpha * x);
            }
        }

    }//SoftExponential

}//Namespace

