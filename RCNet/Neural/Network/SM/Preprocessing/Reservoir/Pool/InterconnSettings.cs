﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO;
using RCNet.Extensions;
using RCNet.MathTools.Probability;
using RCNet.XmlTools;
using RCNet.RandomValue;

namespace RCNet.Neural.Network.SM.Preprocessing.Reservoir.Pool
{
    /// <summary>
    /// Collection of pool's neurons interconnection schemas settings
    /// </summary>
    [Serializable]
    public class InterconnSettings :RCNetBaseSettings
    {
        //Constants
        /// <summary>
        /// Name of the associated xsd type
        /// </summary>
        public const string XsdTypeName = "PoolInterconnectionType";

        //Attribute properties
        /// <summary>
        /// Collection of interconnection schemas to be applied
        /// </summary>
        public List<IInterconnSchemaSettings> SchemaCfgCollection { get; }

        //Constructors
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        private InterconnSettings()
        {
            SchemaCfgCollection = new List<IInterconnSchemaSettings>();
            return;
        }

        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="schemaCfgCollection">Schema settings</param>
        public InterconnSettings(IEnumerable<IInterconnSchemaSettings> schemaCfgCollection)
            : this()
        {
            AddSchemas(schemaCfgCollection);
            Check();
            return;
        }

        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="schemaCfgCollection">Schema settings</param>
        public InterconnSettings(params IInterconnSchemaSettings[] schemaCfgCollection)
            : this()
        {
            AddSchemas(schemaCfgCollection);
            Check();
            return;
        }

        /// <summary>
        /// The deep copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public InterconnSettings(InterconnSettings source)
            : this()
        {
            AddSchemas(source.SchemaCfgCollection);
            return;
        }

        /// <summary>
        /// Creates the instance and initialize it from given xml element.
        /// </summary>
        /// <param name="elem">Xml data containing settings.</param>
        public InterconnSettings(XElement elem)
        {
            //Validation
            XElement settingsElem = Validate(elem, XsdTypeName);
            //Parsing
            SchemaCfgCollection = new List<IInterconnSchemaSettings>();
            foreach (XElement schemaElem in settingsElem.Descendants())
            {
                if (schemaElem.Name.LocalName == "randomSchema")
                {
                    SchemaCfgCollection.Add(new RandomSchemaSettings(schemaElem));
                }
                else if (schemaElem.Name.LocalName == "chainSchema")
                {
                    SchemaCfgCollection.Add(new ChainSchemaSettings(schemaElem));
                }
                else
                {
                    //Ignore
                    ;
                }
            }
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
        /// Checks validity
        /// </summary>
        private void Check()
        {
            if (SchemaCfgCollection.Count == 0)
            {
                throw new Exception($"At least one interconnection schema must be specified.");
            }
            return;
        }

        /// <summary>
        /// Adds cloned schemas from given collection into the internal collection
        /// </summary>
        /// <param name="schemaCfgCollection"></param>
        private void AddSchemas(IEnumerable<IInterconnSchemaSettings> schemaCfgCollection)
        {
            foreach (IInterconnSchemaSettings schemaCfg in schemaCfgCollection)
            {
                SchemaCfgCollection.Add((IInterconnSchemaSettings)schemaCfg.DeepClone());
            }
            return;
        }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public override RCNetBaseSettings DeepClone()
        {
            return new InterconnSettings(this);
        }

        /// <summary>
        /// Generates xml element containing the settings.
        /// </summary>
        /// <param name="rootElemName">Name to be used as a name of the root element.</param>
        /// <param name="suppressDefaults">Specifies if to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(string rootElemName, bool suppressDefaults)
        {
            XElement rootElem = new XElement(rootElemName);
            foreach (IInterconnSchemaSettings schemaCfg in SchemaCfgCollection)
            {
                rootElem.Add(schemaCfg.GetXml(suppressDefaults));
            }
            Validate(rootElem, XsdTypeName);
            return rootElem;
        }

        /// <summary>
        /// Generates default named xml element containing the settings.
        /// </summary>
        /// <param name="suppressDefaults">Specifies if to ommit optional nodes having set default values</param>
        /// <returns>XElement containing the settings</returns>
        public override XElement GetXml(bool suppressDefaults)
        {
            return GetXml("interconnection", suppressDefaults);
        }

    }//PoolInterconnectionSettings

}//Namespace