﻿using RCNet.Extensions;
using System;
using System.Collections.Generic;

namespace RCNet.Neural.Data.Transformers
{
    /// <summary>
    /// Two input fields multiplication transformation
    /// </summary>
    [Serializable]
    public class MulTransformer : ITransformer
    {

        //Attributes
        private readonly int _xFieldIdx;
        private readonly int _yFieldIdx;
        private readonly MulTransformerSettings _settings;

        //Constructor
        /// <summary>
        /// Creates an initialized instance
        /// </summary>
        /// <param name="availableFieldNames">Collection of names of all available input fields</param>
        /// <param name="settings">Configuration</param>
        public MulTransformer(List<string> availableFieldNames, MulTransformerSettings settings)
        {
            _settings = (MulTransformerSettings)settings.DeepClone();
            _xFieldIdx = availableFieldNames.IndexOf(_settings.XInputFieldName);
            _yFieldIdx = availableFieldNames.IndexOf(_settings.YInputFieldName);
            return;
        }

        //Methods
        /// <summary>
        /// Resets generator to its initial state
        /// </summary>
        public void Reset()
        {
            return;
        }

        /// <summary>
        /// Computes transformed value
        /// </summary>
        /// <param name="data">Collection of natural values of the already known input fields</param>
        public double Next(double[] data)
        {
            if (double.IsNaN(data[_xFieldIdx]))
            {
                throw new InvalidOperationException($"Invalid data value at input field index {_xFieldIdx} (NaN).");
            }
            if (double.IsNaN(data[_yFieldIdx]))
            {
                throw new InvalidOperationException($"Invalid data value at input field index {_yFieldIdx} (NaN).");
            }
            return data[_xFieldIdx].Bound() * data[_yFieldIdx].Bound();
        }

    }//MulTransformer
}//Namespace
