﻿using System;
using System.Globalization;
using System.Xml.Linq;

namespace RCNet.Neural.Data.Filter
{
    /// <summary>
    /// Startup parameters for the enumeration feature filter
    /// </summary>
    [Serializable]
    public class EnumFeatureFilterSettings : RCNetBaseSettings, IFeatureFilterSettings
    {
        //Constants
        /// <summary>
        /// Name of the associated xsd type
        /// </summary>
        public const string XsdTypeName = "EnumFeatureFilterType";

        //Attribute properties
        /// <summary>
        /// Number of enum elements
        /// </summary>
        public int NumOfElements { get; }

        //Constructors
        /// <summary>
        /// Constructs an initialized instance
        /// </summary>
        /// <param name="numOfElements">Number of feature's enumerated elements</param>
        public EnumFeatureFilterSettings(int numOfElements)
        {
            NumOfElements = numOfElements;
            Check();
            return;
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public EnumFeatureFilterSettings(EnumFeatureFilterSettings source)
        {
            NumOfElements = source.NumOfElements;
            return;
        }

        /// <summary>
        /// Creates an initialized instance.
        /// </summary>
        /// <param name="elem">Xml element containing the initialization settings</param>
        public EnumFeatureFilterSettings(XElement elem)
        {
            //Validation
            XElement settingsElem = Validate(elem, XsdTypeName);
            //Parsing
            NumOfElements = int.Parse(settingsElem.Attribute("numOfElements").Value, CultureInfo.InvariantCulture);
            Check();
            return;
        }

        //Properties
        /// <summary>
        /// Feature type
        /// </summary>
        public FeatureFilterBase.FeatureType Type { get { return FeatureFilterBase.FeatureType.Enum; } }

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
            if (NumOfElements < 2)
            {
                throw new ArgumentException($"Invalid NumOfElements {NumOfElements.ToString(CultureInfo.InvariantCulture)}. NumOfElements must be GE to 2.", "NumOfElements");
            }
            return;
        }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public override RCNetBaseSettings DeepClone()
        {
            return new EnumFeatureFilterSettings(this);
        }

        /// <summary>
        /// Generates xml element containing the settings.
        /// </summary>
        /// <param name="rootElemName">Name to be used as a name of the root element.</param>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(string rootElemName, bool suppressDefaults)
        {
            return Validate(new XElement(rootElemName, new XAttribute("numOfElements", NumOfElements.ToString(CultureInfo.InvariantCulture))),
                                                       XsdTypeName);
        }

        /// <summary>
        /// Generates default named xml element containing the settings.
        /// </summary>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(bool suppressDefaults)
        {
            return GetXml("enumFeature", suppressDefaults);
        }

    }//EnumFeatureFilterSettings

}//Namespace
