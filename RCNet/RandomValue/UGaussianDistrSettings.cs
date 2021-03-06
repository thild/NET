﻿using System;
using System.Globalization;
using System.Xml.Linq;

namespace RCNet.RandomValue
{
    /// <summary>
    /// Gaussian distribution parameters (unsigned)
    /// </summary>
    [Serializable]
    public class UGaussianDistrSettings : RCNetBaseSettings, IDistrSettings
    {
        //Constants
        /// <summary>
        /// Name of the associated xsd type
        /// </summary>
        public const string XsdTypeName = "UGaussianDistrType";
        //Default values
        /// <summary>
        /// Default value of Mean
        /// </summary>
        public const double DefaultMeanValue = 0.5d;
        /// <summary>
        /// Default value of StdDev
        /// </summary>
        public const double DefaultStdDevValue = 1d;

        //Attributes
        /// <summary>
        /// Mean
        /// </summary>
        public double Mean { get; }

        /// <summary>
        /// Standard deviation
        /// </summary>
        public double StdDev { get; }

        //Constructors
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="mean">Mean</param>
        /// <param name="stdDev">Standard deviation</param>
        public UGaussianDistrSettings(double mean = DefaultMeanValue, double stdDev = DefaultStdDevValue)
        {
            Mean = mean;
            StdDev = stdDev;
            Check();
            return;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public UGaussianDistrSettings(UGaussianDistrSettings source)
        {
            Mean = source.Mean;
            StdDev = source.StdDev;
            return;
        }

        /// <summary>
        /// Creates an instance and initializes it from given xml element.
        /// </summary>
        /// <param name="elem"> Xml element containing the initialization settings.</param>
        public UGaussianDistrSettings(XElement elem)
        {
            //Validation
            XElement settingsElem = Validate(elem, XsdTypeName);
            //Parsing
            Mean = double.Parse(settingsElem.Attribute("mean").Value, CultureInfo.InvariantCulture);
            StdDev = double.Parse(settingsElem.Attribute("stdDev").Value, CultureInfo.InvariantCulture);
            Check();
            return;
        }

        //Properties
        /// <summary>
        /// Identifies settings containing only default values
        /// </summary>
        public override bool ContainsOnlyDefaults { get { return (Mean == DefaultMeanValue && StdDev == DefaultStdDevValue); } }

        /// <summary>
        /// Type of random distribution
        /// </summary>
        public RandomCommon.DistributionType Type { get { return RandomCommon.DistributionType.Gaussian; } }


        //Methods
        /// <summary>
        /// Checks consistency
        /// </summary>
        protected override void Check()
        {
            if (Mean <= 0)
            {
                throw new ArgumentException($"Incorrect Mean ({Mean.ToString(CultureInfo.InvariantCulture)}) value. Mean must be GT 0.", "Mean");
            }
            if (StdDev <= 0)
            {
                throw new ArgumentException($"Incorrect StdDev ({StdDev.ToString(CultureInfo.InvariantCulture)}) value. StdDev must be GT 0.", "StdDev");
            }
            return;
        }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public override RCNetBaseSettings DeepClone()
        {
            return new UGaussianDistrSettings(this);
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
            if (!suppressDefaults || Mean != DefaultMeanValue)
            {
                rootElem.Add(new XAttribute("mean", Mean.ToString(CultureInfo.InvariantCulture)));
            }
            if (!suppressDefaults || StdDev != DefaultStdDevValue)
            {
                rootElem.Add(new XAttribute("stdDev", StdDev.ToString(CultureInfo.InvariantCulture)));
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
            return GetXml(RandomCommon.GetDistrElemName(Type), suppressDefaults);
        }

    }//UGaussianDistrSettings

}//Namespace
