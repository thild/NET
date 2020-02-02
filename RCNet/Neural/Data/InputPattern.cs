﻿using RCNet.Extensions;
using RCNet.MathTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCNet.Neural.Data
{
    /// <summary>
    /// Implements an input pattern.
    /// Pattern can be both univariate or multivariate.
    /// Supports data resampling (including simple detection of signal begin/end) and amplitude unification.
    /// </summary>
    [Serializable]
    public class InputPattern
    {
        //Enums
        /// <summary>
        /// Type of variables' time-order data organization in the 1D input data array
        /// </summary>
        public enum TimeOrderVarDataOrganization
        {
            /// <summary>
            /// Variables' data are groupped in time-order:
            /// [v1(t1),v2(t1),v1(t2),v2(t2),v1(t3),v2(t3)]
            /// </summary>
            Groupped,
            /// <summary>
            /// Variables' data are sequential in time-order:
            /// [v1(t1),v1(t2),v1(t3),v2(t1),v2(t2),v2(t3)]
            /// </summary>
            Sequential
        }

        /// <summary>
        /// Collection of member variables data.
        /// Each pattern member variable has time-ordered data in its own double[] array.
        /// </summary>
        public List<double[]> VarDataCollection { get; }

        //Constructors
        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="source">Source input pattern</param>
        public InputPattern(InputPattern source)
        {
            VarDataCollection = new List<double[]>(source.VarDataCollection.Count);
            foreach(double[] vector in source.VarDataCollection)
            {
                VarDataCollection.Add((double[])vector.Clone());
            }
            return;
        }

        /// <summary>
        /// Instantiates an uninitialized instance
        /// </summary>
        /// <param name="numOfVariables">Number of pattern variables</param>
        public InputPattern(int numOfVariables = 1)
        {
            VarDataCollection = new List<double[]>(numOfVariables);
            return;
        }

        /// <summary>
        /// Instantiates an initialized instance.
        /// Supports data resampling (including simple detection of signal begin/end) and amplitude unification.
        /// </summary>
        /// <param name="inputData">1D array containing pattern input data</param>
        /// <param name="dataStartIndex">Specifies the zero-based starting index of pattern input data in the given 1D input data array</param>
        /// <param name="dataLength">Specifies the length of pattern input data in the given 1D input data array</param>
        /// <param name="numOfVariables">Number of pattern variables</param>
        /// <param name="varDataOrganization">Variables' time-order data organization in the given 1D input data array</param>
        /// <param name="detrend">Specifies if to remove trend from the variables' data</param>
        /// <param name="unifyAmplitudes">Specifies if to unify amplitude of variable's data over the time dimension</param>
        /// <param name="thresholdOfSignalBeginDetection">If specified (GT 0), signal begin will be decided at timepoint Tx where (abs(s(Tx) - s(T0)) / s(max) - s(min)) >= given threshold (x in order 0..last)</param>
        /// <param name="thresholdOfSignalEndDetection">If specified (GT 0), signal end will be decided at timepoint Tx where (abs(s(Tx) - s(T last)) / s(max) - s(min)) >= given threshold (x in order last..0)</param>
        /// <param name="keepCommonTimeScale">If false then each variable will have its own time dimension</param>
        /// <param name="targetTimePoints">If specified, resulting parttern variable's data will be upsampled and/or downsampled to have specified fixed length (time points)</param>
        public InputPattern(double[] inputData,
                            int dataStartIndex,
                            int dataLength,
                            int numOfVariables,
                            TimeOrderVarDataOrganization varDataOrganization,
                            bool detrend = false,
                            bool unifyAmplitudes = false,
                            double thresholdOfSignalBeginDetection = 0d,
                            double thresholdOfSignalEndDetection = 0d,
                            bool keepCommonTimeScale = true,
                            int targetTimePoints = -1
                            )
            : this(numOfVariables)
        {
            List<double[]> patternRawData = PatternDataFromArray(inputData, dataStartIndex, dataLength, numOfVariables, varDataOrganization);
            int rawTimePoints = patternRawData[0].Length;
            //Remove trend?
            if(detrend)
            {
                //Trend removal -> convert data to differences
                for (int varIdx = 0; varIdx < numOfVariables; varIdx++)
                {
                    double[] detrended = new double[patternRawData[varIdx].Length];
                    detrended[0] = 0;
                    for (int i = 1; i < patternRawData[varIdx].Length; i++)
                    {
                        detrended[i] = patternRawData[varIdx][i] - patternRawData[varIdx][i - 1];
                    }
                    patternRawData[varIdx] = detrended;
                }
            }
            //Initially set begin and end signal indexes to full range
            int[] signalBeginIdxs = new int[numOfVariables];
            signalBeginIdxs.Populate(0);
            int[] signalEndIdxs = new int[numOfVariables];
            signalEndIdxs.Populate(rawTimePoints - 1);
            //Detection of signal begin?
            if (thresholdOfSignalBeginDetection > 0d)
            {
                int minSignalBeginIdx = -1;
                for (int varIdx = 0; varIdx < numOfVariables; varIdx++)
                {
                    signalBeginIdxs[varIdx] = DetectSignalBegin(patternRawData[varIdx], thresholdOfSignalBeginDetection);
                    if(minSignalBeginIdx == -1 || minSignalBeginIdx > signalBeginIdxs[varIdx])
                    {
                        minSignalBeginIdx = signalBeginIdxs[varIdx];
                    }
                }
                if(keepCommonTimeScale)
                {
                    signalBeginIdxs.Populate(minSignalBeginIdx);
                }
            }
            //Detection of signal end?
            if (thresholdOfSignalEndDetection > 0d)
            {
                int maxSignalEndIdx = -1;
                for (int varIdx = 0; varIdx < numOfVariables; varIdx++)
                {
                    signalEndIdxs[varIdx] = DetectSignalEnd(patternRawData[varIdx], thresholdOfSignalEndDetection);
                    if (maxSignalEndIdx == -1 || maxSignalEndIdx < signalEndIdxs[varIdx])
                    {
                        maxSignalEndIdx = signalEndIdxs[varIdx];
                    }
                }
                if (keepCommonTimeScale)
                {
                    signalEndIdxs.Populate(maxSignalEndIdx);
                }
            }
            //Correct begin/end indexes
            for (int varIdx = 0; varIdx < numOfVariables; varIdx++)
            {
                if(signalEndIdxs[varIdx] <= signalBeginIdxs[varIdx])
                {
                    signalBeginIdxs[varIdx] = 0;
                    signalEndIdxs[varIdx] = rawTimePoints - 1;
                }
            }

            //Resampling
            targetTimePoints = Math.Max(2, targetTimePoints == -1 ? rawTimePoints : targetTimePoints);
            for (int varIdx = 0; varIdx < numOfVariables; varIdx++)
            {
                int signalLength = (signalEndIdxs[varIdx] - signalBeginIdxs[varIdx]) + 1;
                if (signalLength != rawTimePoints || targetTimePoints != rawTimePoints)
                {
                    //Perform resampling
                    int lcm = Discrete.LCM(signalLength, targetTimePoints, out _);
                    //Upsample
                    double[] upsampledData = Upsample(patternRawData[varIdx], signalBeginIdxs[varIdx], signalEndIdxs[varIdx], lcm);
                    //Downsample
                    double[] downsampledData = Downsample(upsampledData, targetTimePoints);
                    VarDataCollection.Add(downsampledData);
                }
                else
                {
                    //No resampling is necessary so simply use the unchanged raw data
                    double[] signalData = new double[signalLength];
                    for (int i = 0; i < signalLength; i++)
                    {
                        signalData[i] = patternRawData[varIdx][signalBeginIdxs[varIdx] + i];
                    }
                    VarDataCollection.Add(signalData);
                }
            }

            //Unify amplitudes
            if(unifyAmplitudes)
            {
                UnifyAmplitudes();
            }
            return;
        }

        //Static methods
        /// <summary>
        /// Parses type of variables' time-order data organization in the 1D input data array
        /// </summary>
        /// <param name="code">Keyword</param>
        public static TimeOrderVarDataOrganization ParseTimeOrderVarDataOrganization(string code)
        {
            switch (code.ToUpper())
            {
                case "SEQUENTIAL":
                    return TimeOrderVarDataOrganization.Sequential;
                case "GROUPPED":
                    return TimeOrderVarDataOrganization.Groupped;
                default:
                    throw new Exception($"Unknown type of variables' time-order data organization in the 1D input data array: {code}");
            }
        }

        /// <summary>
        /// Creates an initialized instance of InputPattern.
        /// Supports data resampling (including simple detection of signal begin/end), detrending and amplitude unification.
        /// </summary>
        /// <param name="inputData">1D array containing pattern input data</param>
        /// <param name="dataStartIndex">Specifies the zero-based starting index of pattern input data in the given 1D input data array</param>
        /// <param name="dataLength">Specifies the length of pattern input data in the given 1D input data array</param>
        /// <param name="numOfVariables">Number of pattern variables</param>
        /// <param name="varDataOrganization">Variables' time-order data organization in the given 1D input data array</param>
        /// <param name="detrend">Specifies if to remove trend from the variables' data</param>
        /// <param name="unifyAmplitudes">Specifies if to unify amplitude of variable's data over the time dimension</param>
        /// <param name="thresholdOfSignalBeginDetection">If specified (GT 0), signal begin will be decided at timepoint Tx where (abs(s(Tx) - s(T0)) / s(max) - s(min)) >= given threshold (x in order 0..last)</param>
        /// <param name="thresholdOfSignalEndDetection">If specified (GT 0), signal end will be decided at timepoint Tx where (abs(s(Tx) - s(T last)) / s(max) - s(min)) >= given threshold (x in order last..0)</param>
        /// <param name="keepCommonTimeScale">If false then each variable will have its own time dimension</param>
        /// <param name="targetTimePoins">If specified, resulting parttern variable's data will be upsampled and/or downsampled to have specified fixed length (time points)</param>
        public InputPattern FromVector(double[] inputData,
                                       int dataStartIndex,
                                       int dataLength,
                                       int numOfVariables,
                                       TimeOrderVarDataOrganization varDataOrganization,
                                       bool detrend = false,
                                       bool unifyAmplitudes = false,
                                       double thresholdOfSignalBeginDetection = 0d,
                                       double thresholdOfSignalEndDetection = 0d,
                                       bool keepCommonTimeScale = true,
                                       int targetTimePoins = -1
                                       )
        {
            return new InputPattern(inputData,
                                    dataStartIndex,
                                    dataLength,
                                    numOfVariables,
                                    varDataOrganization,
                                    detrend,
                                    unifyAmplitudes,
                                    thresholdOfSignalBeginDetection,
                                    thresholdOfSignalEndDetection,
                                    keepCommonTimeScale,
                                    targetTimePoins
                                    );
        }

        private static int DetectSignalBegin(double[] varData, double thresholdOfSignalDetection)
        {
            //Detection of signal begin
            if (thresholdOfSignalDetection > 0d)
            {
                Interval varDataInterval = new Interval(varData);
                for (int i = 1; i < varData.Length; i++)
                {
                    if (Math.Abs(varData[i] - varData[0]) / varDataInterval.Span >= thresholdOfSignalDetection)
                    {
                        return i - 1; ;
                    }
                }
            }
            return 0;
        }

        private static int DetectSignalEnd(double[] varData, double thresholdOfSignalDetection)
        {
            //Detection of signal end
            if (thresholdOfSignalDetection > 0d)
            {
                Interval varDataInterval = new Interval(varData);
                for (int i = varData.Length - 2; i >= 0; i--)
                {
                    if (Math.Abs(varData[i] - varData[varData.Length - 1]) / varDataInterval.Span >= thresholdOfSignalDetection)
                    {
                        return i + 1;
                    }
                }
            }
            return varData.Length - 1;
        }

        private static double[] Upsample(double[] varData, int signalBeginIdx, int signalEndIdx, int targetLength)
        {
            int signalLength = (signalEndIdx - signalBeginIdx) + 1;
            int upsamplingPoints = targetLength / signalLength - 1;
            double[] upsampledData = new double[targetLength];
            int upsampledDataIdx = 0;
            for (int i = signalBeginIdx + 1; i <= signalEndIdx; i++)
            {
                upsampledData[upsampledDataIdx++] = varData[i - 1];
                if (upsamplingPoints > 0)
                {
                    //Add interpolated points
                    double step = (varData[i] - varData[i - 1]) / (upsamplingPoints + 1);
                    for (int j = 0; j < upsamplingPoints; j++, upsampledDataIdx++)
                    {
                        upsampledData[upsampledDataIdx] = upsampledData[upsampledDataIdx - 1] + step;
                    }
                }
            }
            //Fill remaining data points by the last value
            while (upsampledDataIdx < targetLength)
            {
                upsampledData[upsampledDataIdx++] = varData[signalEndIdx];
            }
            return upsampledData;
        }

        private static double[] Downsample(double[] varData, int targetLength)
        {
            int downsamplingPoints = varData.Length / targetLength;
            if (downsamplingPoints > 1)
            {
                //Downsampling is necessary
                double[] downsampledData = new double[targetLength];
                for (int downsampledDataIdx = 0, varDataIdx = downsamplingPoints - 1; downsampledDataIdx < targetLength; downsampledDataIdx++, varDataIdx += downsamplingPoints)
                {
                    downsampledData[downsampledDataIdx] = varData[varDataIdx];
                }
                return downsampledData;
            }
            else
            {
                //No downsampling is necessary so simply return original data
                return varData;
            }
        }
        /*
        private static double[] Downsample(double[] varData, int targetLength)
        {
            int downsamplingPoints = varData.Length / targetLength;
            if (downsamplingPoints > 1)
            {
                //Downsampling is necessary
                double[] downsampledData = new double[targetLength];
                for (int downsampledDataIdx = 0; downsampledDataIdx < targetLength; downsampledDataIdx++)
                {
                    double sum = 0d;
                    for (int i = 0; i < downsamplingPoints; i++)
                    {
                        sum += varData[downsampledDataIdx * downsamplingPoints + i];
                    }
                    downsampledData[downsampledDataIdx] = sum / downsamplingPoints;
                }
                return downsampledData;
            }
            else
            {
                //No downsampling is necessary so simply return original data
                return varData;
            }
        }
        */

        //Methods
        /// <summary>
        /// Gets variable's data at specified time point
        /// </summary>
        /// <param name="timePointIndex">Zero based index of time point</param>
        /// <returns>Variable's data at specified time point</returns>
        public double[] GetDataAtTimePoint(int timePointIndex)
        {
            double[] data = new double[VarDataCollection.Count];
            for(int i = 0; i < VarDataCollection.Count; i++)
            {
                data[i] = VarDataCollection[i][timePointIndex];
            }
            return data;
        }

        /// <summary>
        /// Extracts variables' data from 1D array
        /// </summary>
        /// <param name="inputData">1D array containing pattern input data</param>
        /// <param name="dataStartIndex">Specifies the zero-based starting index of pattern input data in the given 1D input data array</param>
        /// <param name="dataLength">Specifies the length of pattern input data in the given 1D input data array</param>
        /// <param name="numOfVariables">Number of pattern variables</param>
        /// <param name="varDataOrganization">Variables' time-order data organization in the given 1D input data array</param>
        public List<double[]> PatternDataFromArray(double[] inputData, int dataStartIndex, int dataLength, int numOfVariables, TimeOrderVarDataOrganization varDataOrganization)
        {
            //Check data length
            if (dataLength < numOfVariables || (dataLength % numOfVariables) != 0)
            {
                throw new FormatException("Incorrect length of input data.");
            }
            //Pattern data
            int timePoints = dataLength / numOfVariables;
            List<double[]> patternData = new List<double[]>(numOfVariables);
            for(int i = 0; i < numOfVariables; i++)
            {
                patternData.Add(new double[timePoints]);
            }
            for (int timeIdx = 0; timeIdx < timePoints; timeIdx++)
            {
                for (int i = 0; i < numOfVariables; i++)
                {
                    double varValue = varDataOrganization == TimeOrderVarDataOrganization.Groupped ? inputData[dataStartIndex + timeIdx * numOfVariables + i] : inputData[dataStartIndex + i * timePoints + timeIdx];
                    patternData[i][timeIdx] = varValue;
                }
            }//timeIdx
            return patternData;
        }

        /// <summary>
        /// Rescales data of each variable between 0 and 1
        /// </summary>
        public void UnifyAmplitudes()
        {
            foreach (double[] timeData in VarDataCollection)
            {
                Interval dataRange = new Interval(timeData);
                if (dataRange.Max > dataRange.Min)
                {
                    for (int i = 0; i < timeData.Length; i++)
                    {
                        timeData[i] = (timeData[i] - dataRange.Min) / (dataRange.Span);
                    }
                }
            }
            return;
        }


    }//Pattern

}//Namespace
