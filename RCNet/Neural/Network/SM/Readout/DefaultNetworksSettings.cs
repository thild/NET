﻿using RCNet.Neural.Network.NonRecurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace RCNet.Neural.Network.SM.Readout
{
    /// <summary>
    /// Task dependent networks settings to be applied when specific networks for readout unit are not specified
    /// </summary>
    [Serializable]
    public class DefaultNetworksSettings : RCNetBaseSettings
    {
        //Constants
        /// <summary>
        /// Name of the associated xsd type
        /// </summary>
        public const string XsdTypeName = "ROutLayerUnitDefaultNetworksType";

        //Attribute properties
        /// <summary>
        /// Classification networks settings
        /// </summary>
        public ClassificationNetworksSettings ClassificationNetworksCfg { get; }
        /// <summary>
        /// Forecast networks settings
        /// </summary>
        public ForecastNetworksSettings ForecastNetworksCfg { get; }

        //Constructors
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="classificationNetworksCfg">Default classification networks settings</param>
        /// <param name="forecastNetworksCfg">Default forecast networks settings</param>
        public DefaultNetworksSettings(ClassificationNetworksSettings classificationNetworksCfg = null,
                                       ForecastNetworksSettings forecastNetworksCfg = null
                                       )
        {
            ClassificationNetworksCfg = classificationNetworksCfg == null ? new ClassificationNetworksSettings() : (ClassificationNetworksSettings)classificationNetworksCfg.DeepClone();
            ForecastNetworksCfg = forecastNetworksCfg == null ? new ForecastNetworksSettings() : (ForecastNetworksSettings)forecastNetworksCfg.DeepClone();
            return;
        }

        /// <summary>
        /// The deep copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public DefaultNetworksSettings(DefaultNetworksSettings source)
            : this(source.ClassificationNetworksCfg, source.ForecastNetworksCfg)
        {
            return;
        }

        /// <summary>
        /// Creates an initialized instance.
        /// </summary>
        /// <param name="elem">Xml element containing the initialization settings</param>
        public DefaultNetworksSettings(XElement elem)
        {
            //Validation
            XElement settingsElem = Validate(elem, XsdTypeName);
            //Parsing
            XElement classificationNetworksSettingsElem = settingsElem.Elements("classification").FirstOrDefault();
            ClassificationNetworksCfg = classificationNetworksSettingsElem == null ? new ClassificationNetworksSettings() : new ClassificationNetworksSettings(classificationNetworksSettingsElem);
            XElement forecastNetworksSettingsElem = settingsElem.Elements("forecast").FirstOrDefault();
            ForecastNetworksCfg = forecastNetworksSettingsElem == null ? new ForecastNetworksSettings() : new ForecastNetworksSettings(forecastNetworksSettingsElem);
            return;
        }

        //Properties
        /// <summary>
        /// Identifies settings containing only default values
        /// </summary>
        public override bool ContainsOnlyDefaults
        {
            get
            {
                return ClassificationNetworksCfg.ContainsOnlyDefaults &&
                       ForecastNetworksCfg.ContainsOnlyDefaults;
            }
        }


        //Methods
        /// <summary>
        /// Checks consistency
        /// </summary>
        protected override void Check()
        {
            return;
        }

        /// <summary>
        /// Returns task dependent collection of networks settings
        /// </summary>
        /// <param name="task">Task type</param>
        public List<INonRecurrentNetworkSettings> GetTaskNetworksCfgs(ReadoutUnit.TaskType task)
        {
            return task == ReadoutUnit.TaskType.Classification ? ClassificationNetworksCfg.NetworkCfgCollection : ForecastNetworksCfg.NetworkCfgCollection;
        }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public override RCNetBaseSettings DeepClone()
        {
            return new DefaultNetworksSettings(this);
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
            if (!ClassificationNetworksCfg.ContainsOnlyDefaults)
            {
                rootElem.Add(ClassificationNetworksCfg.GetXml("classification", suppressDefaults));
            }
            if (!ForecastNetworksCfg.ContainsOnlyDefaults)
            {
                rootElem.Add(ForecastNetworksCfg.GetXml("forecast", suppressDefaults));
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
            return GetXml("defaultNetworks", suppressDefaults);
        }

    }//DefaultNetworksSettings

}//Namespace
