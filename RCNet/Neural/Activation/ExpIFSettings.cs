﻿using RCNet.MathTools.Differential;
using RCNet.RandomValue;
using System;
using System.Globalization;
using System.Xml.Linq;

namespace RCNet.Neural.Activation
{
    /// <summary>
    /// Class encaptulates arguments of the ExpIF activation function.
    /// Arguments are in RandomValue form to allow their dynamic random initialization within the specified ranges.
    /// </summary>
    [Serializable]
    public class ExpIFSettings : RCNetBaseSettings
    {
        //Constants
        /// <summary>
        /// Name of the associated xsd type
        /// </summary>
        public const string XsdTypeName = "ActivationExpIFType";

        //Typical values
        /// <summary>
        /// Typical value of time scale
        /// </summary>
        public const double TypicalTimeScale = 12;
        /// <summary>
        /// Typical value of resistance
        /// </summary>
        public const double TypicalResistance = 20;
        /// <summary>
        /// Typical value of resting voltage
        /// </summary>
        public const double TypicalRestV = -65;
        /// <summary>
        /// Typical value of reset voltage
        /// </summary>
        public const double TypicalResetV = -60;
        /// <summary>
        /// Typical value of rheobase
        /// </summary>
        public const double TypicalRheobaseV = -55;
        /// <summary>
        /// Typical value of firing voltage
        /// </summary>
        public const double TypicalFiringThresholdV = -30;
        /// <summary>
        /// Typical value of sharpness delta
        /// </summary>
        public const double TypicalSharpnessDeltaT = 2;

        //Attribute properties
        /// <summary>
        /// Membrane time scale (ms)
        /// </summary>
        public URandomValueSettings TimeScale { get; }
        /// <summary>
        /// Membrane resistance (Mohm)
        /// </summary>
        public URandomValueSettings Resistance { get; }
        /// <summary>
        /// Membrane rest potential (mV)
        /// </summary>
        public RandomValueSettings RestV { get; }
        /// <summary>
        /// Membrane reset potential (mV)
        /// </summary>
        public RandomValueSettings ResetV { get; }
        /// <summary>
        /// Membrane rheobase threshold (mV)
        /// </summary>
        public RandomValueSettings RheobaseV { get; }
        /// <summary>
        /// Membrane firing threshold (mV)
        /// </summary>
        public RandomValueSettings FiringThresholdV { get; }
        /// <summary>
        /// Sharpness of membrane potential change (mV)
        /// </summary>
        public URandomValueSettings SharpnessDeltaT { get; }
        /// <summary>
        /// Number of after spike computation cycles while an input stimuli is ignored (ms)
        /// </summary>
        public int RefractoryPeriods { get; }
        /// <summary>
        /// ODE numerical solver method
        /// </summary>
        public ODENumSolver.Method SolverMethod { get; }
        /// <summary>
        /// ODE numerical solver computation steps of the time step 
        /// </summary>
        public int SolverCompSteps { get; }
        /// <summary>
        /// Duration of the stimulation
        /// </summary>
        public double StimuliDuration { get; }


        //Constructors
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="timeScale">Membrane time scale (ms)</param>
        /// <param name="resistance">Membrane resistance (Mohm)</param>
        /// <param name="restV">Membrane rest potential (mV)</param>
        /// <param name="resetV">Membrane reset potential (mV)</param>
        /// <param name="rheobaseV">Membrane rheobase threshold (mV)</param>
        /// <param name="firingThresholdV">Membrane firing threshold (mV)</param>
        /// <param name="sharpnessDeltaT">Sharpness of membrane potential change (mV)</param>
        /// <param name="refractoryPeriods">Number of after spike computation cycles while an input stimuli is ignored (ms)</param>
        /// <param name="solverMethod">ODE numerical solver method</param>
        /// <param name="solverCompSteps">ODE numerical solver computation steps of the time step</param>
        /// <param name="stimuliDuration">Duration of the stimulation</param>
        public ExpIFSettings(URandomValueSettings timeScale = null,
                             URandomValueSettings resistance = null,
                             RandomValueSettings restV = null,
                             RandomValueSettings resetV = null,
                             RandomValueSettings rheobaseV = null,
                             RandomValueSettings firingThresholdV = null,
                             URandomValueSettings sharpnessDeltaT = null,
                             int refractoryPeriods = ActivationFactory.DefaultRefractoryPeriods,
                             ODENumSolver.Method solverMethod = ActivationFactory.DefaultSolverMethod,
                             int solverCompSteps = ActivationFactory.DefaultSolverCompSteps,
                             double stimuliDuration = ActivationFactory.DefaultStimuliDuration
                             )
        {
            TimeScale = URandomValueSettings.CloneOrDefault(timeScale, TypicalTimeScale);
            Resistance = URandomValueSettings.CloneOrDefault(resistance, TypicalResistance);
            RestV = RandomValueSettings.CloneOrDefault(restV, TypicalRestV);
            ResetV = RandomValueSettings.CloneOrDefault(resetV, TypicalResetV);
            RheobaseV = RandomValueSettings.CloneOrDefault(rheobaseV, TypicalRheobaseV);
            FiringThresholdV = RandomValueSettings.CloneOrDefault(firingThresholdV, TypicalFiringThresholdV);
            SharpnessDeltaT = URandomValueSettings.CloneOrDefault(sharpnessDeltaT, TypicalSharpnessDeltaT);
            RefractoryPeriods = refractoryPeriods;
            SolverMethod = solverMethod;
            SolverCompSteps = solverCompSteps;
            StimuliDuration = stimuliDuration;
            Check();
            return;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public ExpIFSettings(ExpIFSettings source)
        {
            TimeScale = (URandomValueSettings)source.TimeScale.DeepClone();
            Resistance = (URandomValueSettings)source.Resistance.DeepClone();
            RestV = (RandomValueSettings)source.RestV.DeepClone();
            ResetV = (RandomValueSettings)source.ResetV.DeepClone();
            RheobaseV = (RandomValueSettings)source.RheobaseV.DeepClone();
            FiringThresholdV = (RandomValueSettings)source.FiringThresholdV.DeepClone();
            SharpnessDeltaT = (URandomValueSettings)source.SharpnessDeltaT.DeepClone();
            RefractoryPeriods = source.RefractoryPeriods;
            SolverMethod = source.SolverMethod;
            SolverCompSteps = source.SolverCompSteps;
            StimuliDuration = source.StimuliDuration;
            return;
        }

        /// <summary>
        /// Creates an instance and initializes it from given xml element.
        /// </summary>
        /// <param name="elem">Xml element containing the initialization settings</param>
        public ExpIFSettings(XElement elem)
        {
            //Validation
            XElement activationSettingsElem = Validate(elem, XsdTypeName);
            //Parsing
            TimeScale = URandomValueSettings.LoadOrDefault(activationSettingsElem, "timeScale", TypicalTimeScale);
            Resistance = URandomValueSettings.LoadOrDefault(activationSettingsElem, "resistance", TypicalResistance);
            RestV = RandomValueSettings.LoadOrDefault(activationSettingsElem, "restV", TypicalRestV);
            ResetV = RandomValueSettings.LoadOrDefault(activationSettingsElem, "resetV", TypicalResetV);
            RheobaseV = RandomValueSettings.LoadOrDefault(activationSettingsElem, "rheobaseV", TypicalRheobaseV);
            FiringThresholdV = RandomValueSettings.LoadOrDefault(activationSettingsElem, "firingThresholdV", TypicalFiringThresholdV);
            SharpnessDeltaT = URandomValueSettings.LoadOrDefault(activationSettingsElem, "sharpnessDeltaT", TypicalSharpnessDeltaT);
            RefractoryPeriods = int.Parse(activationSettingsElem.Attribute("refractoryPeriods").Value, CultureInfo.InvariantCulture);
            SolverMethod = (ODENumSolver.Method)Enum.Parse(typeof(ODENumSolver.Method), activationSettingsElem.Attribute("solverMethod").Value, true);
            SolverCompSteps = int.Parse(activationSettingsElem.Attribute("solverCompSteps").Value, CultureInfo.InvariantCulture);
            StimuliDuration = double.Parse(activationSettingsElem.Attribute("stimuliDuration").Value, CultureInfo.InvariantCulture);
            Check();
            return;
        }

        //Properties
        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultTimeScale { get { return (TimeScale.Min == TypicalTimeScale && TimeScale.Max == TypicalTimeScale && TimeScale.DistrType == RandomCommon.DistributionType.Uniform); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultResistance { get { return (Resistance.Min == TypicalResistance && Resistance.Max == TypicalResistance && Resistance.DistrType == RandomCommon.DistributionType.Uniform); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultRestV { get { return (RestV.Min == TypicalRestV && RestV.Max == TypicalRestV && !RestV.RandomSign && RestV.DistrType == RandomCommon.DistributionType.Uniform); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultResetV { get { return (ResetV.Min == TypicalResetV && ResetV.Max == TypicalResetV && !ResetV.RandomSign && ResetV.DistrType == RandomCommon.DistributionType.Uniform); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultRheobaseV { get { return (RheobaseV.Min == TypicalRheobaseV && RheobaseV.Max == TypicalRheobaseV && !RheobaseV.RandomSign && RheobaseV.DistrType == RandomCommon.DistributionType.Uniform); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultFiringThresholdV { get { return (FiringThresholdV.Min == TypicalFiringThresholdV && FiringThresholdV.Max == TypicalFiringThresholdV && !FiringThresholdV.RandomSign && FiringThresholdV.DistrType == RandomCommon.DistributionType.Uniform); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultSharpnessDeltaT { get { return (SharpnessDeltaT.Min == TypicalSharpnessDeltaT && SharpnessDeltaT.Max == TypicalSharpnessDeltaT && SharpnessDeltaT.DistrType == RandomCommon.DistributionType.Uniform); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultRefractoryPeriods { get { return (RefractoryPeriods == ActivationFactory.DefaultRefractoryPeriods); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultSolverMethod { get { return (SolverMethod == ActivationFactory.DefaultSolverMethod); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultSolverCompSteps { get { return (SolverCompSteps == ActivationFactory.DefaultSolverCompSteps); } }

        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultStimuliDuration { get { return (StimuliDuration == ActivationFactory.DefaultStimuliDuration); } }

        /// <summary>
        /// Identifies settings containing only default values
        /// </summary>
        public override bool ContainsOnlyDefaults
        {
            get
            {
                return IsDefaultTimeScale &&
                       IsDefaultResistance &&
                       IsDefaultRestV &&
                       IsDefaultResetV &&
                       IsDefaultRheobaseV &&
                       IsDefaultFiringThresholdV &&
                       IsDefaultSharpnessDeltaT &&
                       IsDefaultRefractoryPeriods &&
                       IsDefaultSolverMethod &&
                       IsDefaultSolverCompSteps &&
                       IsDefaultStimuliDuration;
            }
        }

        //Methods
        /// <summary>
        /// Checks consistency
        /// </summary>
        protected override void Check()
        {
            if (RefractoryPeriods < 0)
            {
                throw new ArgumentException($"Invalid RefractoryPeriods {RefractoryPeriods.ToString(CultureInfo.InvariantCulture)}. RefractoryPeriods must be GE to 0.", "RefractoryPeriods");
            }
            if (SolverCompSteps < 1)
            {
                throw new ArgumentException($"Invalid SolverCompSteps {SolverCompSteps.ToString(CultureInfo.InvariantCulture)}. SolverCompSteps must be GE to 1.", "SolverCompSteps");
            }
            if (StimuliDuration <= 0)
            {
                throw new ArgumentException($"Invalid StimuliDuration {StimuliDuration.ToString(CultureInfo.InvariantCulture)}. StimuliDuration must be GT 0.", "StimuliDuration");
            }
            return;
        }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public override RCNetBaseSettings DeepClone()
        {
            return new ExpIFSettings(this);
        }

        /// <summary>
        /// Generates xml element containing the settings.
        /// </summary>
        /// <param name="rootElemName">Name to be used as a name of the root element.</param>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(string rootElemName, bool suppressDefaults)
        {
            XElement rootElem = new XElement(rootElemName);
            if (!suppressDefaults || !IsDefaultRefractoryPeriods)
            {
                rootElem.Add(new XAttribute("refractoryPeriods", RefractoryPeriods.ToString(CultureInfo.InvariantCulture)));
            }
            if (!suppressDefaults || !IsDefaultSolverMethod)
            {
                rootElem.Add(new XAttribute("solverMethod", SolverMethod.ToString()));
            }
            if (!suppressDefaults || !IsDefaultSolverCompSteps)
            {
                rootElem.Add(new XAttribute("solverCompSteps", SolverCompSteps.ToString(CultureInfo.InvariantCulture)));
            }
            if (!suppressDefaults || !IsDefaultStimuliDuration)
            {
                rootElem.Add(new XAttribute("stimuliDuration", StimuliDuration.ToString(CultureInfo.InvariantCulture)));
            }
            if (!suppressDefaults || !IsDefaultTimeScale)
            {
                rootElem.Add(TimeScale.GetXml("timeScale", suppressDefaults));
            }
            if (!suppressDefaults || !IsDefaultResistance)
            {
                rootElem.Add(Resistance.GetXml("resistance", suppressDefaults));
            }
            if (!suppressDefaults || !IsDefaultRestV)
            {
                rootElem.Add(RestV.GetXml("restV", suppressDefaults));
            }
            if (!suppressDefaults || !IsDefaultResetV)
            {
                rootElem.Add(ResetV.GetXml("resetV", suppressDefaults));
            }
            if (!suppressDefaults || !IsDefaultRheobaseV)
            {
                rootElem.Add(RheobaseV.GetXml("rheobaseV", suppressDefaults));
            }
            if (!suppressDefaults || !IsDefaultFiringThresholdV)
            {
                rootElem.Add(FiringThresholdV.GetXml("firingThresholdV", suppressDefaults));
            }
            if (!suppressDefaults || !IsDefaultSharpnessDeltaT)
            {
                rootElem.Add(SharpnessDeltaT.GetXml("sharpnessDeltaT", suppressDefaults));
            }

            Validate(rootElem, XsdTypeName);
            return rootElem;
        }

        /// <summary>
        /// Generates default named xml element containing the settings.
        /// </summary>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(bool suppressDefaults)
        {
            return GetXml("activationExpIF", suppressDefaults);
        }

    }//ExpIFSettings

}//Namespace
