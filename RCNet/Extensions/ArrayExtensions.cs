﻿using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RCNet.Extensions
{
    /// <summary>
    /// Useful extensions of Array class
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Shifts all array elements to the right and sets the first element value to newValue
        /// </summary>
        /// <param name="newValue">New value of the first array element</param>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftRight<T>(this T[] array, T newValue)
        {
            for (int i = array.Length - 1; i >= 1; i--)
            {
                array[i] = array[i - 1];
            }
            array[0] = newValue;
            return;
        }

        /// <summary>
        /// Shifts all array elements to the left and sets the last element value to newValue
        /// </summary>
        /// <param name="newValue">New value of the last array element</param>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftLeft<T>(this T[] array, T newValue)
        {
            for (int i = 0; i <= array.Length - 2; i++)
            {
                array[i] = array[i + 1];
            }
            array[array.Length - 1] = newValue;
            return;
        }

        /// <summary>
        /// Compares the values in this array with the values in the given array.
        /// Uses Equals method co compare items.
        /// </summary>
        /// <param name="cmpArray">Array of values to be compared</param>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsEqualValues<T>(this T[] array, T[] cmpArray)
        {
            if ((array == null && cmpArray != null) ||
               (array != null && cmpArray == null)
               )
            {
                return false;
            }
            if (array == null && cmpArray == null) return true;
            if (array.Length != cmpArray.Length) return false;
            for (int i = 0; i < array.Length; i++)
            {
                if (!array[i].Equals(cmpArray[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Compares the values in this array with the values in the given array starting at specified position in this array.
        /// Uses Equals method co compare items.
        /// </summary>
        /// <param name="startIdx">Start position in this array</param>
        /// <param name="cmpArray">Array of values to be compared</param>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqualSequence<T>(this T[] array, int startIdx, T[] cmpArray)
        {
            if (array == null || cmpArray == null)
            {
                return false;
            }
            if ((startIdx + cmpArray.Length) >= array.Length) return false;
            for (int i = 0; i < cmpArray.Length; i++)
            {
                if (!array[startIdx + i].Equals(cmpArray[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Fills the array with a specified value
        /// </summary>
        /// <param name="value">Value to be used</param>
        /// <param name="start">Starting index</param>
        /// <param name="count">Count</param>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Populate<T>(this T[] array, T value, int start = -1, int count = -1)
        {
            if (start < 0) start = 0;
            if (count < 0) count = array.Length;
            for (int i = start; i < (start + count); i++)
            {
                array[i] = value;
            }
            return;
        }

        /// <summary>
        /// Fills the 2D array with a specified value
        /// </summary>
        /// <param name="value">Value to be used</param>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Populate<T>(this T[,] array, T value)
        {
            int lastIdx0 = array.GetUpperBound(0);
            int lastIdx1 = array.GetUpperBound(1);
            for (int i = 0; i <= lastIdx0; i++)
            {
                for (int j = 0; j <= lastIdx1; j++)
                {
                    array[i, j] = value;
                }
            }
            return;
        }

        /// <summary>
        /// Fills the array of arrays with a specified value
        /// </summary>
        /// <param name="value">Value to be used</param>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Populate<T>(this T[][] array, T value)
        {
            int vLength = array.GetUpperBound(0) + 1;
            Parallel.For(0, vLength, i =>
            {
                int hLength = array[i].GetUpperBound(0) + 1;
                for (int j = 0; j < hLength; j++)
                {
                    array[i][j] = value;
                }
            });
            return;
        }

        /// <summary>
        /// Clones the array of arrays
        /// </summary>
        /// <param name="array"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[][] Clone2D<T>(this T[][] array)
        {
            int vLength = array.GetUpperBound(0) + 1;
            T[][] clone = new T[vLength][];
            Parallel.For(0, vLength, i =>
            {
                if (array[i] == null)
                {
                    clone[i] = null;
                }
                else
                {
                    clone[i] = (T[])array[i].Clone();
                }
            });
            return clone;
        }

    }//ArrayExtensions

}//Namespace

