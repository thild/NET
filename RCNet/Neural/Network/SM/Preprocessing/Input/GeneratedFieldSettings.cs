﻿using RCNet.Neural.Data.Filter;
using RCNet.Neural.Data.Generators;
using RCNet.RandomValue;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace RCNet.Neural.Network.SM.Preprocessing.Input
{
    /// <summary>
    /// Configuration of a generated input field
    /// </summary>
    [Serializable]
    public class GeneratedFieldSettings : RCNetBaseSettings
    {
        //Constants
        /// <summary>
        /// Name of the associated xsd type
        /// </summary>
        public const string XsdTypeName = "NPGeneratedInpFieldType";
        //Default values
        /// <summary>
        /// Default value of parameter specifying if to route generated field to readout layer together with other predictors 
        /// </summary>
        public const bool DefaultRouteToReadout = false;

        //Attribute properties
        /// <summary>
        /// Generated field name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Configuration of associated generator
        /// </summary>
        public RCNetBaseSettings GeneratorCfg { get; }

        /// <summary>
        /// Specifies whether to route generated field to readout layer together with other predictors
        /// </summary>
        public bool RouteToReadout { get; }

        /// <summary>
        /// Configuration of real feature filter
        /// </summary>
        public RealFeatureFilterSettings FeatureFilterCfg { get; }

        /// <summary>
        /// Configuration of spiking coding neurons
        /// </summary>
        public SpikeCodeSettings SpikingCodingCfg { get; }


        //Constructors
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="name">Generated field name</param>
        /// <param name="generatorCfg">Configuration of associated generator</param>
        /// <param name="featureFilterCfg">Configuration of real feature filter</param>
        /// <param name="spikingCodingCfg">Configuration of spiking coding neurons</param>
        /// <param name="routeToReadout">Specifies whether to route generated field to readout layer together with other predictors</param>
        public GeneratedFieldSettings(string name,
                                      RCNetBaseSettings generatorCfg,
                                      bool routeToReadout = DefaultRouteToReadout,
                                      RealFeatureFilterSettings featureFilterCfg = null,
                                      SpikeCodeSettings spikingCodingCfg = null
                                      )
        {
            Name = name;
            GeneratorCfg = generatorCfg.DeepClone();
            RouteToReadout = routeToReadout;
            FeatureFilterCfg = featureFilterCfg == null ? null : (RealFeatureFilterSettings)featureFilterCfg.DeepClone();
            SpikingCodingCfg = spikingCodingCfg == null ? null : (SpikeCodeSettings)spikingCodingCfg.DeepClone();
            Check();
            return;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public GeneratedFieldSettings(GeneratedFieldSettings source)
            : this(source.Name, source.GeneratorCfg, source.RouteToReadout, source.FeatureFilterCfg, source.SpikingCodingCfg)
        {
            return;
        }

        /// <summary>
        /// Creates an initialized instance.
        /// </summary>
        /// <param name="elem">Xml data containing the settings.</param>
        public GeneratedFieldSettings(XElement elem)
        {
            //Validation
            XElement settingsElem = Validate(elem, XsdTypeName);
            //Parsing
            Name = settingsElem.Attribute("name").Value;
            RouteToReadout = bool.Parse(settingsElem.Attribute("routeToReadout").Value);
            XElement genElem = settingsElem.Elements().First();
            GeneratorCfg = GeneratorFactory.LoadSettings(genElem);
            XElement realFeatureFilterElem = settingsElem.Elements("realFeatureFilter").FirstOrDefault();
            FeatureFilterCfg = realFeatureFilterElem == null ? new RealFeatureFilterSettings() : new RealFeatureFilterSettings(realFeatureFilterElem);
            XElement spikingCodingElem = settingsElem.Elements("spikingCoding").FirstOrDefault();
            SpikingCodingCfg = spikingCodingElem == null ? new SpikeCodeSettings() : new SpikeCodeSettings(spikingCodingElem);
            Check();
            return;
        }

        //Properties
        /// <summary>
        /// Checks if settings are default
        /// </summary>
        public bool IsDefaultRouteToReadout { get { return (RouteToReadout == DefaultRouteToReadout); } }

        /// <summary>
        /// Identifies settings containing only default values
        /// </summary>
        public override bool ContainsOnlyDefaults { get { return false; } }

        //Methods
        /// <summary>
        /// Checks consistency
        /// </summary>
        protected override void Check()
        {
            if (Name.Length == 0)
            {
                throw new ArgumentException($"Name can not be empty.", "Name");
            }
            Type genType = GeneratorCfg.GetType();
            if (genType != typeof(PulseGeneratorSettings) &&
               genType != typeof(RandomValueSettings) &&
               genType != typeof(SinusoidalGeneratorSettings) &&
               genType != typeof(MackeyGlassGeneratorSettings))
            {
                throw new ArgumentException($"Unsupported generator configuration {genType}.", "GeneratorCfg");
            }
            return;
        }

        /// <summary>
        /// Creates the deep copy instance of this instance.
        /// </summary>
        public override RCNetBaseSettings DeepClone()
        {
            return new GeneratedFieldSettings(this);
        }

        /// <summary>
        /// Generates xml element containing the settings.
        /// </summary>
        /// <param name="rootElemName">Name to be used as a name of the root element.</param>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(string rootElemName, bool suppressDefaults)
        {
            XElement rootElem = new XElement(rootElemName,
                                             new XAttribute("name", Name),
                                             GeneratorCfg.GetXml(suppressDefaults)
                                             );
            if (!suppressDefaults || !IsDefaultRouteToReadout)
            {
                rootElem.Add(new XAttribute("routeToReadout", RouteToReadout.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            }
            if (!suppressDefaults || !FeatureFilterCfg.ContainsOnlyDefaults)
            {
                rootElem.Add(FeatureFilterCfg.GetXml(suppressDefaults));
            }
            if (!suppressDefaults || !SpikingCodingCfg.ContainsOnlyDefaults)
            {
                rootElem.Add(SpikingCodingCfg.GetXml(suppressDefaults));
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
            return GetXml("field", suppressDefaults);
        }

    }//GeneratedFieldSettings

}//Namespace
