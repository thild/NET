﻿using System;
using System.Collections.Generic;

namespace RCNet.MathTools
{
    /// <summary>
    /// Error statistics of binary 0/1 values
    /// </summary>
    [Serializable]
    public class BinErrStat
    {
        //Attribute properties
        /// <summary>
        /// Binary 0/1 border. Double value LT this border is considered as 0 and GE as 1.
        /// </summary>
        public double BinBorder { get; }
        /// <summary>
        /// Statistics of errors on individual 0/1 values
        /// </summary>
        public BasicStat[] BinValErrStat { get; }
        /// <summary>
        /// Total error statistics
        /// </summary>
        public BasicStat TotalErrStat { get; }

        //Constructors
        /// <summary>
        /// Creates an uninitialized instance
        /// </summary>
        /// <param name="binBorder">Double value LT this border is considered as 0 and GE as 1</param>
        public BinErrStat(double binBorder)
        {
            BinBorder = binBorder;
            BinValErrStat = new BasicStat[2];
            BinValErrStat[0] = new BasicStat();
            BinValErrStat[1] = new BasicStat();
            TotalErrStat = new BasicStat();
            return;
        }

        /// <summary>
        /// Construct an initialized instance
        /// </summary>
        /// <param name="binBorder">Double value LT this border is considered as 0 and GE as 1</param>
        /// <param name="computedVectorCollection">Collection of computed vectors</param>
        /// <param name="idealVectorCollection">Collection of ideal vectors</param>
        public BinErrStat(double binBorder, IEnumerable<double[]> computedVectorCollection, IEnumerable<double[]> idealVectorCollection)
            : this(binBorder)
        {
            Update(computedVectorCollection, idealVectorCollection);
            return;
        }

        /// <summary>
        /// Construct an initialized instance
        /// </summary>
        /// <param name="binBorder">Double value LT this border is considered as 0 and GE as 1</param>
        /// <param name="computedValueCollection">Collection of computed vectors</param>
        /// <param name="idealValueCollection">Collection of ideal vectors</param>
        public BinErrStat(double binBorder, IEnumerable<double> computedValueCollection, IEnumerable<double> idealValueCollection)
            : this(binBorder)
        {
            Update(computedValueCollection, idealValueCollection);
            return;
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="source">Source instance</param>
        public BinErrStat(BinErrStat source)
        {
            BinBorder = source.BinBorder;
            BinValErrStat = new BasicStat[2];
            BinValErrStat[0] = new BasicStat(source.BinValErrStat[0]);
            BinValErrStat[1] = new BasicStat(source.BinValErrStat[1]);
            TotalErrStat = new BasicStat(source.TotalErrStat);
            return;
        }

        //Methods
        /// <summary>
        /// Decides if two values represent the same binary value
        /// </summary>
        /// <param name="computedValue">Computed value</param>
        /// <param name="idealValue">Ideal value</param>
        private bool BinMatch(double computedValue, double idealValue)
        {
            if (computedValue >= BinBorder && idealValue >= BinBorder) return true;
            if (computedValue < BinBorder && idealValue < BinBorder) return true;
            return false;
        }

        /// <summary>
        /// Updates the statistics
        /// </summary>
        /// <param name="computedValue">Computed value</param>
        /// <param name="idealValue">Ideal value</param>
        public void Update(double computedValue, double idealValue)
        {
            int idealBinVal = (idealValue >= BinBorder) ? 1 : 0;
            int errValue = BinMatch(computedValue, idealValue) ? 0 : 1;
            BinValErrStat[idealBinVal].AddSampleValue(errValue);
            TotalErrStat.AddSampleValue(errValue);
            return;
        }

        /// <summary>
        /// Updates the statistics
        /// </summary>
        /// <param name="computedValueCollection">Collection of computed binary values</param>
        /// <param name="idealValueCollection">Collection of ideal binary values</param>
        public void Update(IEnumerable<double> computedValueCollection, IEnumerable<double> idealValueCollection)
        {
            IEnumerator<double> idealValueEnumerator = idealValueCollection.GetEnumerator();
            foreach (double computedValue in computedValueCollection)
            {
                idealValueEnumerator.MoveNext();
                double idealValue = idealValueEnumerator.Current;
                Update(computedValue, idealValue);
            }
            return;
        }

        /// <summary>
        /// Updates the statistics
        /// </summary>
        /// <param name="computedValuesCollection">Collection of computed binary values</param>
        /// <param name="idealValuesCollection">Collection of ideal binary values</param>
        public void Update(IEnumerable<double[]> computedValuesCollection, IEnumerable<double[]> idealValuesCollection)
        {
            IEnumerator<double[]> idealValueEnumerator = idealValuesCollection.GetEnumerator();
            foreach (double[] computedValues in computedValuesCollection)
            {
                idealValueEnumerator.MoveNext();
                double[] idealValues = idealValueEnumerator.Current;
                for (int i = 0; i < idealValues.Length; i++)
                {
                    Update(computedValues[i], idealValues[i]);
                }
            }
            return;
        }

        /// <summary>
        /// Creates the deep copy of this instance
        /// </summary>
        public BinErrStat DeepClone()
        {
            return new BinErrStat(this);
        }

    }//BinErrStat

}//Namespace
