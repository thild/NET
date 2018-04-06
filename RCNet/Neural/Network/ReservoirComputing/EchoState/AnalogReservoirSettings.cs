﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Xml.Linq;
using System.IO;
using RCNet.XmlTools;
using RCNet.Neural.Activation;

namespace RCNet.Neural.Network.ReservoirComputing.EchoState
{
    /// <summary>
    /// The class contains analog reservoir configuration parameters and also contains
    /// internal logic so it is not just a container of parameters. Creating an proper instance by hand is not
    /// a trivial task.
    /// The easiest and safest way to create an instance is to use the xml constructor.
    /// </summary>
    [Serializable]
    public class AnalogReservoirSettings
    {
        //Constants
        /// <summary>
        /// Supported types of reservoir internal topology
        /// </summary>
        public enum ReservoirTopologyType
        {
            /// <summary>
            /// Random topology. Reservoir's neurons are connected randomly.
            /// </summary>
            Random,
            /// <summary>
            /// Ring topology. Reservoir's neurons are connected in a ring shape.
            /// </summary>
            Ring,
            /// <summary>
            /// Doubly twisted thoroidal topology.
            /// </summary>
            DTT
        };

        //Attribute properties
        /// <summary>
        /// Name of this configuration
        /// </summary>
        public string SettingsName { get; set; }
        /// <summary>
        /// Each input field will be connected by the random weight to the number of
        /// reservoir neurons = (Size * Density).
        /// Typical InputConnectionDensity = 1 (it means the full connectivity).
        /// </summary>
        public double InputConnectionDensity { get; set; }
        /// <summary>
        /// A weight of each input field to reservoir's neuron connection will be randomly selected
        /// from the open interval (-InputWeightScale, +InputWeightScale).
        /// </summary>
        public double InputWeightScale { get; set; }
        /// <summary>
        /// Number of the neurons in the reservoir.
        /// </summary>
        public int Size { get; set; }
        /// <summary>
        /// Activation function of the neurons in the reservoir.
        /// </summary>
        public ActivationFactory.ActivationType ReservoirNeuronActivation { get; set; }
        /// <summary>
        /// Each reservoir's neuron has its own constant input bias. Bias is always added to input signal of the neuron.
        /// A constant bias value will be for each neuron selected randomly from the range (-BiasScale;+BiasScale).
        /// To disable biasing specify 0.
        /// </summary>
        public double BiasScale { get; set; }
        /// <summary>
        /// Neurons in the reservoir are interconnected. The weight of the connection will be randomly selected
        /// from the open interval (-InternalWeightScale, +InternalWeightScale).
        /// </summary>
        public double InternalWeightScale { get; set; }
        /// <summary>
        /// One of the supported reservoir topologies of internal neural networking.
        /// See the enumeration ReservoirTopologyType.
        /// </summary>
        public ReservoirTopologyType TopologyType { get; set; }
        /// <summary>
        /// Parameters of the topology of internal neural networking.
        /// See classes RandomTopology, RingTopology and DTTTopology.
        /// </summary>
        public Object TopologySettings { get; set; }
        /// <summary>
        /// Indicates whether the retainment (leaky integrators) neurons feature is used.
        /// </summary>
        public bool RetainmentNeuronsFeature { get; set; }
        /// <summary>
        /// The parameter says how much of the reservoir's neurons will have the Retainment property set.
        /// Specific neurons will be selected randomly.
        /// Count = Size * Density
        /// </summary>
        public double RetainmentNeuronsDensity { get; set; }
        /// <summary>
        /// If the reservoir's neuron is selected to have Retainment property then its retainment rate will be randomly selected
        /// from the closed interval (RetainmentMinRate, RetainmentMaxRate).
        /// </summary>
        public double RetainmentMinRate { get; set; }
        /// <summary>
        /// If the reservoir's neuron is selected to have Retainment property then its retainment rate will be randomly selected
        /// from the closed interval (RetainmentMinRate, RetainmentMaxRate).
        /// </summary>
        public double RetainmentMaxRate { get; set; }
        /// <summary>
        /// Indicates whether the context neuron feature is used.
        /// Context neuron is a special neuron outside the reservoir, which mixes and processes the signal from all
        /// the neurons in the reservoir. The context neuron state thus represents the state of the entire reservoir
        /// and is then used as one of the inputs to the neurons in the reservoir.
        /// </summary>
        public bool ContextNeuronFeature { get; set; }
        /// <summary>
        /// Activation function of the context neuron.
        /// </summary>
        public ActivationFactory.ActivationType ContextNeuronActivation { get; set; }
        /// <summary>
        /// Each weight of the connection from the reservoir neuron to the contex neuron will have this value
        /// </summary>
        public double ContextNeuronInputWeight { get; set; }
        /// <summary>
        /// The parameter says how many neurons in the reservoir will receive the signal from the context neuron.
        /// Count = Size * Density
        /// </summary>
        public double ContextNeuronFeedbackDensity { get; set; }
        /// <summary>
        /// Weight of the feedback connection from the context neuron.
        /// </summary>
        public double ContextNeuronFeedbackWeight { get; set; }
        /// Indicates whether the feedback feature is used.
        public bool FeedbackFeature { get; set; }
        /// <summary>
        /// Each feedback field will be connected by the random weight to the number of
        /// reservoir neurons = (Size * Density).
        /// Typical FeedbackConnectionDensity = 1 (it means the full connectivity).
        /// </summary>
        public double FeedbackConnectionDensity { get; set; }
        /// <summary>
        /// A weight of each feedback field to reservoir's neuron connection will be randomly selected
        /// from the open interval (-FeedbackWeightScale, +FeedbackWeightScale).
        /// </summary>
        public double FeedbackWeightScale { get; set; }
        /// <summary>
        /// Collection of feedback field names.
        /// </summary>
        public List<string> FeedbackFieldNameCollection { get; set; }

        //Constructors
        /// <summary>
        /// Creates an uninitialized instance
        /// </summary>
        public AnalogReservoirSettings()
        {
            SettingsName = string.Empty;
            InputConnectionDensity = 0;
            InputWeightScale = 0;
            Size = 0;
            ReservoirNeuronActivation = ActivationFactory.ActivationType.TanH;
            BiasScale = 0;
            InternalWeightScale = 0;
            TopologyType = ReservoirTopologyType.Random;
            TopologySettings = null;
            RetainmentNeuronsFeature = false;
            RetainmentNeuronsDensity = 0;
            RetainmentMinRate = 0;
            RetainmentMaxRate = 0;
            ContextNeuronFeature = false;
            ContextNeuronActivation = ReservoirNeuronActivation;
            ContextNeuronInputWeight = 0;
            ContextNeuronFeedbackDensity = 0;
            ContextNeuronFeedbackWeight = 0;
            FeedbackFeature = false;
            FeedbackConnectionDensity = 0;
            FeedbackWeightScale = 0;
            FeedbackFieldNameCollection = new List<string>();
            return;
        }

        /// <summary>
        /// The deep copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public AnalogReservoirSettings(AnalogReservoirSettings source)
        {
            SettingsName = source.SettingsName;
            InputConnectionDensity = source.InputConnectionDensity;
            InputWeightScale = source.InputWeightScale;
            Size = source.Size;
            ReservoirNeuronActivation = source.ReservoirNeuronActivation;
            BiasScale = source.BiasScale;
            InternalWeightScale = source.InternalWeightScale;
            TopologyType = source.TopologyType;
            if (source.TopologySettings != null)
            {
                if (source.TopologySettings.GetType() == typeof(RandomTopologySettings))
                {
                    TopologySettings = new RandomTopologySettings((RandomTopologySettings)source.TopologySettings);
                }
                if (source.TopologySettings.GetType() == typeof(RingTopologySettings))
                {
                    TopologySettings = new RingTopologySettings((RingTopologySettings)source.TopologySettings);
                }
                if (source.TopologySettings.GetType() == typeof(DTTTopologySettings))
                {
                    TopologySettings = new DTTTopologySettings((DTTTopologySettings)source.TopologySettings);
                }
            }
            RetainmentNeuronsFeature = source.RetainmentNeuronsFeature;
            RetainmentNeuronsDensity = source.RetainmentNeuronsDensity;
            RetainmentMinRate = source.RetainmentMinRate;
            RetainmentMaxRate = source.RetainmentMaxRate;
            ContextNeuronFeature = source.ContextNeuronFeature;
            ContextNeuronActivation = source.ContextNeuronActivation;
            ContextNeuronInputWeight = source.ContextNeuronInputWeight;
            ContextNeuronFeedbackDensity = source.ContextNeuronFeedbackDensity;
            ContextNeuronFeedbackWeight = source.ContextNeuronFeedbackWeight;
            FeedbackFeature = source.FeedbackFeature;
            FeedbackConnectionDensity = source.FeedbackConnectionDensity;
            FeedbackWeightScale = source.FeedbackWeightScale;
            FeedbackFieldNameCollection = new List<string>(source.FeedbackFieldNameCollection);
            return;
        }

        /// <summary>
        /// Creates the instance and initialize it from given xml element.
        /// This is the preferred way to instantiate reservoir settings.
        /// </summary>
        /// <param name="reservoirSettingsElem">
        /// Xml data containing reservoir settings.
        /// Content of xml element is always validated against the xml schema.
        /// </param>
        public AnalogReservoirSettings(XElement reservoirSettingsElem)
        {
            //Validation
            //A very ugly validation. Xml schema does not support validation of the xml fragment against specific type.
            XmlValidator validator = new XmlValidator();
            Assembly assemblyRCNet = Assembly.GetExecutingAssembly();
            using (Stream schemaStream = assemblyRCNet.GetManifestResourceStream("RCNet.Neural.Network.ReservoirComputing.EchoState.AnalogReservoirSettings.xsd"))
            {
                validator.AddSchema(schemaStream);
            }
            using (Stream schemaStream = assemblyRCNet.GetManifestResourceStream("RCNet.NeuralSettingsTypes.xsd"))
            {
                validator.AddSchema(schemaStream);
            }
            validator.LoadXDocFromString(reservoirSettingsElem.ToString());
            //Parsing
            //Settings name
            SettingsName = reservoirSettingsElem.Attribute("name").Value;
            //Input
            XElement inputElem = reservoirSettingsElem.Descendants("input").First();
            InputConnectionDensity = double.Parse(inputElem.Attribute("connectionDensity").Value, CultureInfo.InvariantCulture);
            InputWeightScale = double.Parse(inputElem.Attribute("weightScale").Value, CultureInfo.InvariantCulture);
            //Internal
            XElement internalElem = reservoirSettingsElem.Descendants("internal").First();
            Size = int.Parse(internalElem.Attribute("size").Value);
            ReservoirNeuronActivation = ActivationFactory.ParseActivation(internalElem.Attribute("activation").Value);
            BiasScale = double.Parse(internalElem.Attribute("biasScale").Value, CultureInfo.InvariantCulture);
            InternalWeightScale = double.Parse(internalElem.Attribute("weightScale").Value, CultureInfo.InvariantCulture);
            //Topology
            List<XElement> topologyElems = new List<XElement>();
            topologyElems.AddRange(internalElem.Descendants("topologyRandom"));
            topologyElems.AddRange(internalElem.Descendants("topologyRing"));
            topologyElems.AddRange(internalElem.Descendants("topologyDTT"));
            if(topologyElems.Count != 1)
            {
                throw new Exception("Only one reservoir topology can be specified in reservoir settings.");
            }
            if (topologyElems.Count == 0)
            {
                throw new Exception("Reservoir topology is not specified in reservoir settings.");
            }
            XElement topologyElem = topologyElems[0];
            //Random?
            if (topologyElem.Name == "topologyRandom")
            {
                TopologyType = ReservoirTopologyType.Random;
                TopologySettings = new RandomTopologySettings(topologyElem);
            }
            //Ring?
            else if (topologyElem.Name == "topologyRing")
            {
                TopologyType = ReservoirTopologyType.Ring;
                TopologySettings = new RingTopologySettings(topologyElem);
            }
            else
            {
                //DTT
                TopologyType = ReservoirTopologyType.DTT;
                TopologySettings = new DTTTopologySettings(topologyElem);
            }
            //Retainment neurons
            XElement retainmentElem = internalElem.Descendants("retainmentNeurons").FirstOrDefault();
            RetainmentNeuronsFeature = (retainmentElem != null);
            if (RetainmentNeuronsFeature)
            {
                RetainmentNeuronsDensity = double.Parse(retainmentElem.Attribute("density").Value, CultureInfo.InvariantCulture);
                RetainmentMinRate = double.Parse(retainmentElem.Attribute("retainmentMinRate").Value, CultureInfo.InvariantCulture);
                RetainmentMaxRate = double.Parse(retainmentElem.Attribute("retainmentMaxRate").Value, CultureInfo.InvariantCulture);
                RetainmentNeuronsFeature = (RetainmentNeuronsDensity > 0 &&
                                            RetainmentMaxRate > 0
                                            );
            }
            else
            {
                RetainmentNeuronsDensity = 0;
                RetainmentMinRate = 0;
                RetainmentMaxRate = 0;
            }
            //Context neuron
            XElement ctxNeuronElem = internalElem.Descendants("contextNeuron").FirstOrDefault();
            ContextNeuronFeature = (ctxNeuronElem != null);
            if (ContextNeuronFeature)
            {
                ContextNeuronActivation = ActivationFactory.ParseActivation(ctxNeuronElem.Attribute("activation").Value);
                ContextNeuronInputWeight = double.Parse(ctxNeuronElem.Attribute("inputWeight").Value, CultureInfo.InvariantCulture);
                ContextNeuronFeedbackDensity = double.Parse(ctxNeuronElem.Attribute("feedbackDensity").Value, CultureInfo.InvariantCulture);
                ContextNeuronFeedbackWeight = double.Parse(ctxNeuronElem.Attribute("feedbackWeight").Value, CultureInfo.InvariantCulture);
                ContextNeuronFeature = (ContextNeuronFeedbackDensity > 0 &&
                                        ContextNeuronInputWeight > 0 &&
                                        ContextNeuronFeedbackWeight > 0
                                        );
            }
            else
            {
                ContextNeuronActivation = ReservoirNeuronActivation;
                ContextNeuronInputWeight = 0;
                ContextNeuronFeedbackDensity = 0;
                ContextNeuronFeedbackWeight = 0;
            }
            //Feedback
            XElement feedbackElem = reservoirSettingsElem.Descendants("feedback").FirstOrDefault();
            FeedbackFeature = (feedbackElem != null);
            FeedbackFieldNameCollection = new List<string>();
            if (FeedbackFeature)
            {
                FeedbackConnectionDensity = double.Parse(feedbackElem.Attribute("density").Value, CultureInfo.InvariantCulture);
                FeedbackWeightScale = double.Parse(feedbackElem.Attribute("weightScale").Value, CultureInfo.InvariantCulture);
                foreach (XElement feedbackFieldElem in feedbackElem.Descendants("field"))
                {
                    FeedbackFieldNameCollection.Add(feedbackFieldElem.Attribute("name").Value);
                }
                FeedbackFeature = (FeedbackFieldNameCollection.Count > 0);
            }
            else
            {
                FeedbackConnectionDensity = 0;
                FeedbackWeightScale = 0;
            }
            return;
        }

        //Methods
        //Static methods
        /// <summary>
        /// Parses string code to ReservoirTopologyType.
        /// </summary>
        /// <param name="code">Topology code</param>
        /// <returns></returns>
        public static ReservoirTopologyType ParseReservoirTopology(string code)
        {
            switch (code.ToUpper())
            {
                case "RANDOM": return ReservoirTopologyType.Random;
                case "RING": return ReservoirTopologyType.Ring;
                case "DTT": return ReservoirTopologyType.DTT;
                default:
                    throw new ArgumentException($"Unknown reservoir's topology code {code}");
            }
        }

        //Instance methods
        /// <summary>
        /// See the base.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            AnalogReservoirSettings cmpSettings = obj as AnalogReservoirSettings;
            if (SettingsName != cmpSettings.SettingsName ||
                InputConnectionDensity != cmpSettings.InputConnectionDensity ||
                InputWeightScale != cmpSettings.InputWeightScale ||
                Size != cmpSettings.Size ||
                ReservoirNeuronActivation != cmpSettings.ReservoirNeuronActivation ||
                InternalWeightScale != cmpSettings.InternalWeightScale ||
                BiasScale != cmpSettings.BiasScale ||
                TopologyType != cmpSettings.TopologyType ||
                RetainmentNeuronsFeature != cmpSettings.RetainmentNeuronsFeature ||
                RetainmentNeuronsDensity != cmpSettings.RetainmentNeuronsDensity ||
                RetainmentMinRate != cmpSettings.RetainmentMinRate ||
                RetainmentMaxRate != cmpSettings.RetainmentMaxRate ||
                ContextNeuronFeature != cmpSettings.ContextNeuronFeature ||
                ContextNeuronFeedbackDensity != cmpSettings.ContextNeuronFeedbackDensity ||
                ContextNeuronActivation != cmpSettings.ContextNeuronActivation ||
                ContextNeuronInputWeight != cmpSettings.ContextNeuronInputWeight ||
                ContextNeuronFeedbackWeight != cmpSettings.ContextNeuronFeedbackWeight ||
                FeedbackFeature != cmpSettings.FeedbackFeature ||
                FeedbackConnectionDensity != cmpSettings.FeedbackConnectionDensity ||
                FeedbackWeightScale != cmpSettings.FeedbackWeightScale
                )
            {
                return false;
            }
            switch (TopologyType)
            {
                case ReservoirTopologyType.Random:
                    if (!((RandomTopologySettings)TopologySettings).Equals((RandomTopologySettings)cmpSettings.TopologySettings)) return false;
                    break;
                case ReservoirTopologyType.Ring:
                    if (!((RingTopologySettings)TopologySettings).Equals((RingTopologySettings)cmpSettings.TopologySettings)) return false;
                    break;
                case ReservoirTopologyType.DTT:
                    if (!((DTTTopologySettings)TopologySettings).Equals((DTTTopologySettings)cmpSettings.TopologySettings)) return false;
                    break;
            }
            return true;
        }

        /// <summary>
        /// See the base.
        /// </summary>
        public override int GetHashCode()
        {
            return SettingsName.GetHashCode();
        }

        /// <summary>
        /// Creates the deep copy instance of this instance
        /// </summary>
        public AnalogReservoirSettings DeepClone()
        {
            AnalogReservoirSettings clone = new AnalogReservoirSettings(this);
            return clone;
        }


        //Inner classes
        /// <summary>
        /// Additional setup parameters for Random reservoir topology
        /// </summary>
        [Serializable]
        public class RandomTopologySettings
        {
            //Attributes
            /// <summary>
            /// The parameter says how many interconnections from all possible interconnections will be used.
            /// Count = Size * Size * Density
            /// </summary>
            public double ConnectionsDensity { get; set; }

            //Constructors
            /// <summary>
            /// Creates an unitialized instance
            /// </summary>
            public RandomTopologySettings()
            {
                ConnectionsDensity = 0;
                return;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="source">Source instance</param>
            public RandomTopologySettings(RandomTopologySettings source)
            {
                ConnectionsDensity = source.ConnectionsDensity;
                return;
            }

            /// <summary>
            /// Creates the instance itialized from xml
            /// </summary>
            public RandomTopologySettings(XElement randomTopologyElem)
            {
                ConnectionsDensity = double.Parse(randomTopologyElem.Attribute("connectionsDensity").Value, CultureInfo.InvariantCulture);
                return;
            }

            //Methods
            /// <summary>
            /// See the base.
            /// </summary>
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                RandomTopologySettings cmpSettings = obj as RandomTopologySettings;
                if (ConnectionsDensity != cmpSettings.ConnectionsDensity)
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// See the base.
            /// </summary>
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        }//RandomTopologySettings

        /// <summary>
        /// Additional setup parameters for Ring reservoir topology
        /// </summary>
        [Serializable]
        public class RingTopologySettings
        {
            //Attributes
            /// <summary>
            /// The parameter specifies whether the ring interconnection will be bidirectional.
            /// </summary>
            public bool Bidirectional { get; set; }
            /// <summary>
            /// The parameter says how many neurons in the reservoir will receive the signal from itself.
            /// Count = Size * Density
            /// </summary>
            public double SelfConnectionsDensity { get; set; }
            /// <summary>
            /// The parameter says how many additional interconnections from all possible interconnections will be used.
            /// Count = Size * Size * Density
            /// </summary>
            public double InterConnectionsDensity { get; set; }

            //Constructors
            /// <summary>
            /// Creates an unitialized instance
            /// </summary>
            public RingTopologySettings()
            {
                Bidirectional = false;
                SelfConnectionsDensity = 0;
                InterConnectionsDensity = 0;
                return;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="source">Source instance</param>
            public RingTopologySettings(RingTopologySettings source)
            {
                Bidirectional = source.Bidirectional;
                SelfConnectionsDensity = source.SelfConnectionsDensity;
                InterConnectionsDensity = source.InterConnectionsDensity;
                return;
            }

            /// <summary>
            /// Creates the instance itialized from xml
            /// </summary>
            public RingTopologySettings(XElement ringTopologyElem)
            {
                Bidirectional = bool.Parse(ringTopologyElem.Attribute("bidirectional").Value);
                SelfConnectionsDensity = double.Parse(ringTopologyElem.Attribute("selfConnectionsDensity").Value, CultureInfo.InvariantCulture);
                InterConnectionsDensity = double.Parse(ringTopologyElem.Attribute("interConnectionsDensity").Value, CultureInfo.InvariantCulture);
                return;
            }

            //Methods
            /// <summary>
            /// See the base.
            /// </summary>
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                RingTopologySettings cmpSettings = obj as RingTopologySettings;
                if (Bidirectional != cmpSettings.Bidirectional ||
                   SelfConnectionsDensity != cmpSettings.SelfConnectionsDensity ||
                   InterConnectionsDensity != cmpSettings.InterConnectionsDensity
                   )
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// See the base.
            /// </summary>
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        }//RingTopologySettings

        /// <summary>
        /// Additional setup parameters for DTT reservoir topology
        /// </summary>
        [Serializable]
        public class DTTTopologySettings
        {
            //Attributes
            /// <summary>
            /// The parameter says how many neurons in the reservoir will receive the signal from itself.
            /// Count = Size * Density
            /// </summary>
            public double SelfConnectionsDensity { get; set; }

            //Constructors
            /// <summary>
            /// Creates an unitialized instance
            /// </summary>
            public DTTTopologySettings()
            {
                SelfConnectionsDensity = 0;
                return;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="source">Source instance</param>
            public DTTTopologySettings(DTTTopologySettings source)
            {
                SelfConnectionsDensity = source.SelfConnectionsDensity;
                return;
            }

            /// <summary>
            /// Creates the instance itialized from xml
            /// </summary>
            public DTTTopologySettings(XElement dttTopologyElem)
            {
                SelfConnectionsDensity = double.Parse(dttTopologyElem.Attribute("selfConnectionsDensity").Value, CultureInfo.InvariantCulture);
                return;
            }

            //Methods
            /// <summary>
            /// See the base.
            /// </summary>
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                DTTTopologySettings cmpSettings = obj as DTTTopologySettings;
                if (SelfConnectionsDensity != cmpSettings.SelfConnectionsDensity)
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// See the base.
            /// </summary>
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        }//DTTTopologySettings

    }//AnalogReservoirSettings

}//Namespace

