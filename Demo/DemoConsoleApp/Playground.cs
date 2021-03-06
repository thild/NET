﻿using RCNet.Neural.Activation;
using RCNet.Neural.Data.Transformers;
using RCNet.Neural.Data.Generators;
using System;
using System.Collections.Generic;
using RCNet.CsvTools;
using System.Globalization;

namespace Demo.DemoConsoleApp
{
    /// <summary>
    /// This class is a free "playground", the place where it is possible to test new concepts or somewhat else
    /// </summary>
    class Playground
    {
        //Attributes
        private readonly Random _rand;

        //Constructor
        public Playground()
        {
            _rand = new Random();
            return;
        }

        //Methods
        private void TestActivation(IActivationFunction af, int simLength, double constCurrent, int from, int count)
        {
            for (int i = 1; i <= simLength; i++)
            {
                double signal;
                double input;
                if (i >= from && i < from + count)
                {
                    input = double.IsNaN(constCurrent) ? _rand.NextDouble() : constCurrent;
                }
                else
                {
                    input = 0d;
                }
                signal = af.Compute(input);
                Console.WriteLine($"{af.GetType().Name} step {i}, State {(af.TypeOfActivation == ActivationType.Spiking ? af.InternalState : signal)} signal {signal}");
            }
            Console.ReadLine();

            return;
        }

        private void TestSingleFieldTransformer(ITransformer transformer)
        {
            double[] inputValues = new double[1];
            inputValues[0] = double.MinValue;
            Console.WriteLine($"{transformer.GetType().Name} Input {inputValues[0]} Output {transformer.Next(inputValues)}");
            for (double input = -5d; input <= 5d; input += 0.1d)
            {
                input = Math.Round(input, 1);
                inputValues[0] = input;
                Console.WriteLine($"{transformer.GetType().Name} Input {input} Output {transformer.Next(inputValues)}");
            }
            inputValues[0] = double.MaxValue;
            Console.WriteLine($"{transformer.GetType().Name} Input {inputValues[0]} Output {transformer.Next(inputValues)}");
            Console.ReadLine();
            return;
        }

        private void TestTwoFieldsTransformer(ITransformer transformer)
        {
            double[] inputValues = new double[2];
            inputValues[0] = double.MinValue;
            inputValues[1] = double.MinValue;
            Console.WriteLine($"{transformer.GetType().Name} Inputs [{inputValues[0]}, {inputValues[1]}] Output {transformer.Next(inputValues)}");

            for (double input1 = -5d; input1 <= 5d; input1 += 0.5d)
            {
                input1 = Math.Round(input1, 1);
                for (double input2 = -5d; input2 <= 5d; input2 += 0.5d)
                {
                    input2 = Math.Round(input2, 1);
                    inputValues[0] = input1;
                    inputValues[1] = input2;
                    Console.WriteLine($"{transformer.GetType().Name} Inputs [{inputValues[0]}, {inputValues[1]}] Output {transformer.Next(inputValues)}");
                }
            }
            inputValues[0] = double.MaxValue;
            inputValues[1] = double.MaxValue;
            Console.WriteLine($"{transformer.GetType().Name} Inputs [{inputValues[0]}, {inputValues[1]}] Output {transformer.Next(inputValues)}");
            Console.ReadLine();
            return;
        }

        private void TestTransformers()
        {
            List<string> singleFieldList = new List<string>() { "f1" };
            List<string> twoFieldsList = new List<string>() { "f1", "f2" };
            ITransformer transformer;
            //Difference transformer
            transformer = new DiffTransformer(singleFieldList, new DiffTransformerSettings(singleFieldList[0], 2));
            TestSingleFieldTransformer(transformer);
            //CDiv transformer
            transformer = new CDivTransformer(singleFieldList, new CDivTransformerSettings(singleFieldList[0], 1d));
            TestSingleFieldTransformer(transformer);
            //Log transformer
            transformer = new LogTransformer(singleFieldList, new LogTransformerSettings(singleFieldList[0], 10));
            TestSingleFieldTransformer(transformer);
            //Exp transformer
            transformer = new ExpTransformer(singleFieldList, new ExpTransformerSettings(singleFieldList[0]));
            TestSingleFieldTransformer(transformer);
            //Power transformer
            transformer = new PowerTransformer(singleFieldList, new PowerTransformerSettings(singleFieldList[0], 0.5d, true));
            TestSingleFieldTransformer(transformer);
            //YeoJohnson transformer
            transformer = new YeoJohnsonTransformer(singleFieldList, new YeoJohnsonTransformerSettings(singleFieldList[0], 0.5d));
            TestSingleFieldTransformer(transformer);
            //MWStat transformer
            transformer = new MWStatTransformer(singleFieldList, new MWStatTransformerSettings(singleFieldList[0], 5, MWStatTransformer.OutputValue.RootMeanSquare));
            TestSingleFieldTransformer(transformer);
            //Mul transformer
            transformer = new MulTransformer(twoFieldsList, new MulTransformerSettings(twoFieldsList[0], twoFieldsList[1]));
            TestTwoFieldsTransformer(transformer);
            //Div transformer
            transformer = new DivTransformer(twoFieldsList, new DivTransformerSettings(twoFieldsList[0], twoFieldsList[1]));
            TestTwoFieldsTransformer(transformer);
            //Linear transformer
            transformer = new LinearTransformer(twoFieldsList, new LinearTransformerSettings(twoFieldsList[0], twoFieldsList[1], 0.03, 0.2));
            TestTwoFieldsTransformer(transformer);
            return;
        }

        private void GenSteadyPatternedMGData(int minTau, int maxTau, int tauSamples, int patternLength, double verifyRatio, string path)
        {
            CsvDataHolder trainingData = new CsvDataHolder(DelimitedStringValues.DefaultDelimiter);
            CsvDataHolder verificationData = new CsvDataHolder(DelimitedStringValues.DefaultDelimiter);
            int verifyBorderIdx = (int)(tauSamples * verifyRatio);
            for (int tau = minTau; tau <= maxTau; tau++)
            {
                MackeyGlassGenerator mgg = new MackeyGlassGenerator(new MackeyGlassGeneratorSettings(tau));
                int neededDataLength = 1 + patternLength + (tauSamples - 1);
                double[] mggData = new double[neededDataLength];
                for(int i = 0; i < neededDataLength; i++)
                {
                    mggData[i] = mgg.Next();
                }
                for(int i = 0; i < tauSamples; i++)
                {
                    DelimitedStringValues patternData = new DelimitedStringValues();
                    //Steady data
                    patternData.AddValue(tau.ToString(CultureInfo.InvariantCulture));
                    //Varying data
                    for (int j = 0; j < patternLength; j++)
                    {
                        patternData.AddValue(mggData[i + j].ToString(CultureInfo.InvariantCulture));
                    }
                    //Desired data 1
                    patternData.AddValue(mggData[i + patternLength].ToString(CultureInfo.InvariantCulture));
                    //Desired data 2
                    patternData.AddValue(mggData[i + patternLength].ToString(CultureInfo.InvariantCulture));
                    //Add to a collections
                    if (i < verifyBorderIdx)
                    {
                        trainingData.DataRowCollection.Add(patternData);
                    }
                    else
                    {
                        verificationData.DataRowCollection.Add(patternData);
                    }
                }
            }
            //Save files
            trainingData.Save(path + "\\" + "SteadyMG_train.csv");
            verificationData.Save(path + "\\" + "SteadyMG_verify.csv");

            return;
        }

        /// <summary>
        /// Playground's entry point
        /// </summary>
        public void Run()
        {
            //TODO - place your code here
            /*
            TestActivation(ActivationFactory.Create(new SimpleIFSettings(), _rand), 200, 0.25, 50, 100);
            TestTransformers();
            GenSteadyPatternedMGData(10, 18, 200, 200, 0.5d, "C:\\Users\\Okozelsk\\Development\\DotNet\\Projects\\NET\\Demo\\DemoConsoleApp\\Data");
            */


            return;
        }


    }//Playground
}
