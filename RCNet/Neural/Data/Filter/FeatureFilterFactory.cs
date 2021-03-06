﻿using RCNet.MathTools;
using System;
using System.Xml.Linq;

namespace RCNet.Neural.Data.Filter
{
    /// <summary>
    /// Provides proper load of settings and instantiation of feature filters
    /// </summary>
    public static class FeatureFilterFactory
    {
        /// <summary>
        /// Based on element name loads proper type of feature filter settings
        /// </summary>
        /// <param name="elem">Element containing settings</param>
        /// <returns></returns>
        public static IFeatureFilterSettings LoadSettings(XElement elem)
        {
            switch (elem.Name.LocalName)
            {
                case "binFeature":
                    return new BinFeatureFilterSettings(elem);
                case "enumFeature":
                    return new EnumFeatureFilterSettings(elem);
                case "realFeature":
                    return new RealFeatureFilterSettings(elem);
                default:
                    throw new ArgumentException($"Unexpected element name {elem.Name.LocalName}", "elem");
            }
        }

        /// <summary>
        /// Instantiates feature filter of proper type according to settings
        /// </summary>
        /// <param name="outputRange">Output range of feature filter</param>
        /// <param name="settings">Settings of feature filter</param>
        public static FeatureFilterBase Create(Interval outputRange, IFeatureFilterSettings settings)
        {
            switch (settings.Type)
            {
                case FeatureFilterBase.FeatureType.Binary:
                    return new BinFeatureFilter(outputRange, (BinFeatureFilterSettings)settings);
                case FeatureFilterBase.FeatureType.Enum:
                    return new EnumFeatureFilter(outputRange, (EnumFeatureFilterSettings)settings);
                case FeatureFilterBase.FeatureType.Real:
                    return new RealFeatureFilter(outputRange, (RealFeatureFilterSettings)settings);
                default:
                    throw new ArgumentException($"Unexpected feature type {settings.Type}", "settings");
            }
        }

        /// <summary>
        /// Creates deep copy of given settings
        /// </summary>
        /// <param name="settings">Settings of feature filter</param>
        public static IFeatureFilterSettings DeepClone(IFeatureFilterSettings settings)
        {
            switch (settings.Type)
            {
                case FeatureFilterBase.FeatureType.Binary:
                    return new BinFeatureFilterSettings((BinFeatureFilterSettings)settings);
                case FeatureFilterBase.FeatureType.Enum:
                    return new EnumFeatureFilterSettings((EnumFeatureFilterSettings)settings);
                case FeatureFilterBase.FeatureType.Real:
                    return new RealFeatureFilterSettings((RealFeatureFilterSettings)settings);
                default:
                    throw new ArgumentException($"Unexpected feature type {settings.Type}", "settings");
            }
        }

    }//FeatureFilterFactory
}
