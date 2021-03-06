﻿using RCNet.MathTools;
using System.Xml.Linq;

namespace RCNet.Neural.Network.NonRecurrent
{
    /// <summary>
    /// Common interface of non-recurrent network settings
    /// </summary>
    public interface INonRecurrentNetworkSettings
    {
        //Properties
        /// <summary>
        /// Output range
        /// </summary>
        Interval OutputRange { get; }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        RCNetBaseSettings DeepClone();

        /// <summary>
        /// Generates default named xml element containing the settings.
        /// </summary>
        /// <param name="suppressDefaults">Specifies whether to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        XElement GetXml(bool suppressDefaults);


    }//INonRecurrentNetworkSettings

}//Namespace
