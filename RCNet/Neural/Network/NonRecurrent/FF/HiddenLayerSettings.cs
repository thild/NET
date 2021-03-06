﻿using RCNet.Neural.Activation;
using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace RCNet.Neural.Network.NonRecurrent.FF
{
    /// <summary>
    /// Feed forward network hidden layer settings
    /// </summary>
    [Serializable]
    public class HiddenLayerSettings : RCNetBaseSettings
    {
        /// <summary>
        /// Name of the associated xsd type
        /// </summary>
        public const string XsdTypeName = "FFNetHiddenLayerType";

        //Attributes
        /// <summary>
        /// Number of hidden layer neurons
        /// </summary>
        public int NumOfNeurons { get; }
        /// <summary>
        /// Layer activation configuration
        /// </summary>
        public RCNetBaseSettings ActivationCfg { get; }

        //Constructors
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="numOfNeurons">Number of hidden layer neurons</param>
        /// <param name="activationCfg">Layer activation configuration</param>
        public HiddenLayerSettings(int numOfNeurons, RCNetBaseSettings activationCfg)
        {
            NumOfNeurons = numOfNeurons;
            ActivationCfg = ActivationFactory.DeepCloneActivationSettings(activationCfg);
            Check();
            return;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public HiddenLayerSettings(HiddenLayerSettings source)
        {
            NumOfNeurons = source.NumOfNeurons;
            ActivationCfg = ActivationFactory.DeepCloneActivationSettings(source.ActivationCfg);
            return;
        }

        /// <summary>
        /// Creates an initialized instance from given xml element.
        /// </summary>
        /// <param name="elem">Xml element containing the settings.</param>
        public HiddenLayerSettings(XElement elem)
        {
            //Validation
            XElement settingsElem = Validate(elem, XsdTypeName);
            //Parsing
            NumOfNeurons = int.Parse(settingsElem.Attribute("neurons").Value);
            ActivationCfg = ActivationFactory.LoadSettings(settingsElem.Elements().First());
            Check();
            return;
        }

        //Properties
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
            if (NumOfNeurons < 1)
            {
                throw new ArgumentException($"Invalid NumOfNeurons {NumOfNeurons.ToString(CultureInfo.InvariantCulture)}. NumOfNeurons must be GT 0.", "NumOfNeurons");
            }
            if (!FeedForwardNetworkSettings.IsAllowedActivation(ActivationCfg, out _))
            {
                throw new ArgumentException($"Specified ActivationCfg can't be used in the hidden layer of a FF network. Activation function has to be stateless and has to support derivative calculation.", "ActivationCfg");
            }
            return;
        }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public override RCNetBaseSettings DeepClone()
        {
            return new HiddenLayerSettings(this);
        }

        /// <summary>
        /// Generates xml element containing the settings.
        /// </summary>
        /// <param name="rootElemName">Name to be used as a name of the root element.</param>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(string rootElemName, bool suppressDefaults)
        {
            return Validate(new XElement(rootElemName, new XAttribute("neurons", NumOfNeurons.ToString(CultureInfo.InvariantCulture)),
                                                       ActivationCfg.GetXml(suppressDefaults)),
                                                       XsdTypeName);
        }

        /// <summary>
        /// Generates default named xml element containing the settings.
        /// </summary>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(bool suppressDefaults)
        {
            return GetXml("layer", suppressDefaults);
        }

    }//HiddenLayerSettings

}//Namespace
