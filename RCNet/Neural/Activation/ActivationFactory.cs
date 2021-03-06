﻿using RCNet.Extensions;
using RCNet.MathTools;
using RCNet.MathTools.Differential;
using System;
using System.Xml.Linq;

namespace RCNet.Neural.Activation
{
    /// <summary>
    /// Class mediates operations with activation functions
    /// </summary>
    public static class ActivationFactory
    {
        //Constants
        //Default values
        /// <summary>
        /// Default value of refractory periods
        /// </summary>
        public const int DefaultRefractoryPeriods = 1;

        /// <summary>
        /// Default ODE numerical solver method
        /// </summary>
        public const ODENumSolver.Method DefaultSolverMethod = ODENumSolver.Method.Euler;

        /// <summary>
        /// Default ODE numerical solver computation steps
        /// </summary>
        public const int DefaultSolverCompSteps = 2;

        /// <summary>
        /// Default duration of spiking neuron stimulation in ms.
        /// </summary>
        public const double DefaultStimuliDuration = 1;


        //Methods
        /// <summary>
        /// Returns the instance of the activation function settings
        /// </summary>
        /// <param name="settingsElem">
        /// XML element containing specific activation settings
        /// </param>
        public static RCNetBaseSettings LoadSettings(XElement settingsElem)
        {
            switch (settingsElem.Name.LocalName)
            {
                case "activationAdExpIF":
                    return new AdExpIFSettings(settingsElem);
                case "activationSQNL":
                    return new SQNLSettings(settingsElem);
                case "activationBentIdentity":
                    return new BentIdentitySettings(settingsElem);
                case "activationElliot":
                    return new ElliotSettings(settingsElem);
                case "activationExpIF":
                    return new ExpIFSettings(settingsElem);
                case "activationIzhikevichIF":
                    return new IzhikevichIFSettings(settingsElem);
                case "activationAutoIzhikevichIF":
                    return new AutoIzhikevichIFSettings(settingsElem);
                case "activationGaussian":
                    return new GaussianSettings(settingsElem);
                case "activationIdentity":
                    return new IdentitySettings(settingsElem);
                case "activationISRU":
                    return new ISRUSettings(settingsElem);
                case "activationLeakyIF":
                    return new LeakyIFSettings(settingsElem);
                case "activationLeakyReLU":
                    return new LeakyReLUSettings(settingsElem);
                case "activationSigmoid":
                    return new SigmoidSettings(settingsElem);
                case "activationSimpleIF":
                    return new SimpleIFSettings(settingsElem);
                case "activationSinc":
                    return new SincSettings(settingsElem);
                case "activationSinusoid":
                    return new SinusoidSettings(settingsElem);
                case "activationSoftExponential":
                    return new SoftExponentialSettings(settingsElem);
                case "activationSoftPlus":
                    return new SoftPlusSettings(settingsElem);
                case "activationTanH":
                    return new TanHSettings(settingsElem);
                default:
                    throw new ArgumentException($"Unsupported activation function settings: {settingsElem.Name}", "settingsElem");
            }
        }

        /// <summary>
        /// Collects basic information about activation function corresponding to given configuration
        /// </summary>
        /// <param name="activationSettings">Activation function settings</param>
        /// <param name="stateless">Indicates whether the activation function is stateless</param>
        /// <param name="supportsDerivative">Indicates whether the activation function supports derivative</param>
        /// <returns>Output range of the activation function</returns>
        public static Interval GetInfo(RCNetBaseSettings activationSettings, out bool stateless, out bool supportsDerivative)
        {
            IActivationFunction af = Create(activationSettings, new Random());
            Interval outputRange = af.OutputRange.DeepClone();
            stateless = af.Stateless;
            supportsDerivative = af.SupportsDerivative;
            return outputRange;
        }

        /// <summary>
        /// Creates an instance of the activation function according to given settings.
        /// </summary>
        /// <param name="settings">Specific activation function settings </param>
        /// <param name="rand">Random object to be used for randomly generated parameters</param>
        public static IActivationFunction Create(RCNetBaseSettings settings, Random rand)
        {
            IActivationFunction af;
            Type settingsType = settings.GetType();
            if (settingsType == typeof(AdExpIFSettings))
            {
                AdExpIFSettings afs = (AdExpIFSettings)settings;
                af = new AdExpIF(rand.NextDouble(afs.TimeScale),
                                 rand.NextDouble(afs.Resistance),
                                 rand.NextDouble(afs.RestV),
                                 rand.NextDouble(afs.ResetV),
                                 rand.NextDouble(afs.RheobaseV),
                                 rand.NextDouble(afs.FiringThresholdV),
                                 rand.NextDouble(afs.SharpnessDeltaT),
                                 rand.NextDouble(afs.AdaptationVoltageCoupling),
                                 rand.NextDouble(afs.AdaptationTimeConstant),
                                 rand.NextDouble(afs.AdaptationSpikeTriggeredIncrement),
                                 afs.SolverMethod,
                                 afs.SolverCompSteps,
                                 afs.StimuliDuration
                                 );
            }
            else if (settingsType == typeof(BentIdentitySettings))
            {
                af = new BentIdentity();
            }
            else if (settingsType == typeof(ElliotSettings))
            {
                ElliotSettings afs = (ElliotSettings)settings;
                af = new Elliot(rand.NextDouble(afs.Slope));
            }
            else if (settingsType == typeof(ExpIFSettings))
            {
                ExpIFSettings afs = (ExpIFSettings)settings;
                af = new ExpIF(rand.NextDouble(afs.TimeScale),
                               rand.NextDouble(afs.Resistance),
                               rand.NextDouble(afs.RestV),
                               rand.NextDouble(afs.ResetV),
                               rand.NextDouble(afs.RheobaseV),
                               rand.NextDouble(afs.FiringThresholdV),
                               rand.NextDouble(afs.SharpnessDeltaT),
                               afs.RefractoryPeriods,
                               afs.SolverMethod,
                               afs.SolverCompSteps,
                               afs.StimuliDuration
                               );
            }
            else if (settingsType == typeof(GaussianSettings))
            {
                af = new Gaussian();
            }
            else if (settingsType == typeof(IdentitySettings))
            {
                af = new Identity();
            }
            else if (settingsType == typeof(ISRUSettings))
            {
                ISRUSettings afs = (ISRUSettings)settings;
                af = new ISRU(rand.NextDouble(afs.Alpha));
            }
            else if (settingsType == typeof(IzhikevichIFSettings))
            {
                IzhikevichIFSettings afs = (IzhikevichIFSettings)settings;
                af = new IzhikevichIF(rand.NextDouble(afs.RecoveryTimeScale),
                                      rand.NextDouble(afs.RecoverySensitivity),
                                      rand.NextDouble(afs.RecoveryReset),
                                      rand.NextDouble(afs.RestV),
                                      rand.NextDouble(afs.ResetV),
                                      rand.NextDouble(afs.FiringThresholdV),
                                      afs.RefractoryPeriods,
                                      afs.SolverMethod,
                                      afs.SolverCompSteps,
                                      afs.StimuliDuration
                                      );

            }
            else if (settingsType == typeof(AutoIzhikevichIFSettings))
            {
                double randomValue = rand.NextDouble().Power(2);
                AutoIzhikevichIFSettings afs = (AutoIzhikevichIFSettings)settings;
                //Ranges
                af = new IzhikevichIF(0.02,
                                      0.2,
                                      8 + (-6 * randomValue),
                                      -70,
                                      -65 + (15 * randomValue),
                                      30,
                                      afs.RefractoryPeriods,
                                      afs.SolverMethod,
                                      afs.SolverCompSteps,
                                      afs.StimuliDuration
                                      );
            }
            else if (settingsType == typeof(LeakyIFSettings))
            {
                LeakyIFSettings afs = (LeakyIFSettings)settings;
                af = new LeakyIF(rand.NextDouble(afs.TimeScale),
                                 rand.NextDouble(afs.Resistance),
                                 rand.NextDouble(afs.RestV),
                                 rand.NextDouble(afs.ResetV),
                                 rand.NextDouble(afs.FiringThresholdV),
                                 afs.RefractoryPeriods,
                                 afs.SolverMethod,
                                 afs.SolverCompSteps,
                                 afs.StimuliDuration
                                 );
            }
            else if (settingsType == typeof(LeakyReLUSettings))
            {
                LeakyReLUSettings afs = (LeakyReLUSettings)settings;
                af = new LeakyReLU(rand.NextDouble(afs.NegSlope));
            }
            else if (settingsType == typeof(SigmoidSettings))
            {
                af = new Sigmoid();
            }
            else if (settingsType == typeof(SimpleIFSettings))
            {
                SimpleIFSettings afs = (SimpleIFSettings)settings;
                af = new SimpleIF(rand.NextDouble(afs.Resistance),
                                  rand.NextDouble(afs.DecayRate),
                                  rand.NextDouble(afs.ResetV),
                                  rand.NextDouble(afs.FiringThresholdV),
                                  afs.RefractoryPeriods
                                  );
            }
            else if (settingsType == typeof(SincSettings))
            {
                af = new Sinc();
            }
            else if (settingsType == typeof(SinusoidSettings))
            {
                af = new Sinusoid();
            }
            else if (settingsType == typeof(SoftExponentialSettings))
            {
                SoftExponentialSettings afs = (SoftExponentialSettings)settings;
                af = new SoftExponential(rand.NextDouble(afs.Alpha));
            }
            else if (settingsType == typeof(SoftPlusSettings))
            {
                af = new SoftPlus();
            }
            else if (settingsType == typeof(SQNLSettings))
            {
                af = new SQNL();
            }
            else if (settingsType == typeof(TanHSettings))
            {
                af = new TanH();
            }
            else
            {
                throw new ArgumentException($"Unsupported activation function settings: {settingsType.Name}");
            }
            //*
            //Set random initial membrane potential for spiking activation
            if(!af.Stateless && af.TypeOfActivation == ActivationType.Spiking)
            {
                af.SetInitialInternalState(rand.NextRangedUniformDouble(0.05, 0.95));
            }
            //*/
            return af;
        }

        /// <summary>
        /// Returns the deep clone of the activation function settings
        /// </summary>
        /// <param name="settings">
        /// Specific activation function settings
        /// </param>
        public static RCNetBaseSettings DeepCloneActivationSettings(RCNetBaseSettings settings)
        {
            Type settingsType = settings.GetType();
            if (settingsType == typeof(AdExpIFSettings))
            {
                return ((AdExpIFSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(SQNLSettings))
            {
                return ((SQNLSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(BentIdentitySettings))
            {
                return ((BentIdentitySettings)settings).DeepClone();
            }
            else if (settingsType == typeof(ElliotSettings))
            {
                return ((ElliotSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(ExpIFSettings))
            {
                return ((ExpIFSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(GaussianSettings))
            {
                return ((GaussianSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(IdentitySettings))
            {
                return ((IdentitySettings)settings).DeepClone();
            }
            else if (settingsType == typeof(ISRUSettings))
            {
                return ((ISRUSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(IzhikevichIFSettings))
            {
                return ((IzhikevichIFSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(AutoIzhikevichIFSettings))
            {
                return ((AutoIzhikevichIFSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(LeakyIFSettings))
            {
                return ((LeakyIFSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(LeakyReLUSettings))
            {
                return ((LeakyReLUSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(LeakyReLUSettings))
            {
                return ((LeakyReLUSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(SigmoidSettings))
            {
                return ((SigmoidSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(SimpleIFSettings))
            {
                return ((SimpleIFSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(SincSettings))
            {
                return ((SincSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(SinusoidSettings))
            {
                return ((SinusoidSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(SoftExponentialSettings))
            {
                return ((SoftExponentialSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(SoftPlusSettings))
            {
                return ((SoftPlusSettings)settings).DeepClone();
            }
            else if (settingsType == typeof(TanHSettings))
            {
                return ((TanHSettings)settings).DeepClone();
            }
            else
            {
                throw new ArgumentException($"Unsupported activation function settings: {settingsType.Name}");
            }
        }

    }//ActivationFactory

}//Namespace

