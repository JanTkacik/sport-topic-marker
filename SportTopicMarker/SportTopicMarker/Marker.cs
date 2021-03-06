﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AForge.Neuro;
using AForge.Neuro.Learning;
using edu.stanford.nlp.pipeline;

namespace SportTopicMarker
{
    public class Marker
    {
        private readonly NLPProcessor _processor;
        private readonly string _modelPath;
        public WordOccurenceDatabase PersonOccurenceDatabase;
        public WordOccurenceDatabase OrganizationOccurenceDatabase;
        public WordOccurenceDatabase LocationsOccurenceDatabase;
        public WordOccurenceDatabase SportSpecificWordsOccurenceDatabase;
        private readonly HashSet<string> _sportSpecificWords;
        private ActivationNetwork _classifier;
        private BackPropagationLearning _teacher;
        private readonly int _categoriesCount;
        private readonly Dictionary<SportCategory, int> _categoryIndex;
        private readonly Dictionary<int, SportCategory> _indexCategory; 

        public Marker(NLPProcessor processor, string modelPath)
        {
            _processor = processor;
            _modelPath = modelPath;

            Annotation sportSpecificWords = _processor.Annotate(File.ReadAllText(modelPath + "specificwords.txt"));
            _sportSpecificWords = NLPCoreHelper.GetLemmas(sportSpecificWords);
            File.Delete(modelPath + "specificwords.txt");
            StringBuilder builder = new StringBuilder();
            foreach (string word in _sportSpecificWords)
            {
                builder.AppendLine(word);
            }
            File.WriteAllText(modelPath + "specificwords.txt", builder.ToString());

            PersonOccurenceDatabase = new WordOccurenceDatabase();
            OrganizationOccurenceDatabase = new WordOccurenceDatabase();
            LocationsOccurenceDatabase = new WordOccurenceDatabase();
            SportSpecificWordsOccurenceDatabase = new WordOccurenceDatabase();
            _categoriesCount = Enum.GetValues(typeof(SportCategory)).Length;
            int hiddenLayerCount = (10 * _categoriesCount) / 3;
            _classifier = new ActivationNetwork(new SigmoidFunction(), 4 * _categoriesCount, hiddenLayerCount, _categoriesCount);
            _teacher = new BackPropagationLearning(_classifier);

            Array values = Enum.GetValues(typeof(SportCategory));
            _categoryIndex = new Dictionary<SportCategory, int>();
            _indexCategory = new Dictionary<int, SportCategory>();
            for (int index = 0; index < values.Length; index++)
            {
                SportCategory category = (SportCategory)values.GetValue(index);
                _categoryIndex.Add(category, index);
                _indexCategory.Add(index, category);
            }
        }

        public LabeledArticle LabelArticle(string article)
        {
            double[] input = GetRawFeatures(article);
            double[] output = _classifier.Compute(input);
            var maxIndex = GetMaxIndex(output);
            return new LabeledArticle(article, _indexCategory[maxIndex]);
        }

        private static int GetMaxIndex(double[] output)
        {
            double max = 0;
            int maxIndex = -1;
            for (int index = 0; index < output.Length; index++)
            {
                if (output[index] > max)
                {
                    max = output[index];
                    maxIndex = index;
                }
            }
            return maxIndex;
        }

        public void ExtendDatabaseWithArticle(LabeledArticle article)
        {
            Annotation annotation = _processor.Annotate(article.Article);
            HashSet<string> persons = NLPCoreHelper.GetPersons(annotation);
            HashSet<string> organizations = NLPCoreHelper.GetOrganizations(annotation);
            HashSet<string> locations = NLPCoreHelper.GetLocation(annotation);
            HashSet<string> sportSpecificWords = NLPCoreHelper.GetOccurence(annotation, _sportSpecificWords);

            foreach (string person in persons)
            {
                PersonOccurenceDatabase.AddWord(person, article.Category);
            }

            foreach (string organization in organizations)
            {
                OrganizationOccurenceDatabase.AddWord(organization, article.Category);
            }

            foreach (string location in locations)
            {
                LocationsOccurenceDatabase.AddWord(location, article.Category);
            }

            foreach (string sportSpecificWord in sportSpecificWords)
            {
                SportSpecificWordsOccurenceDatabase.AddWord(sportSpecificWord, article.Category);
            }
        }

        public double TrainClassifierWithArticle(LabeledArticle article)
        {
            double[] input = GetRawFeatures(article.Article);
            double[] output = GetOutput(article);

            double error = _teacher.Run(input, output);
            Console.WriteLine(error);
            return error;
        }

        public double TrainClassifierWithArticles(List<LabeledArticle> articles)
        {
            List<LabeledArticle> trainingSet = new List<LabeledArticle>();
            List<LabeledArticle> validationSet = new List<LabeledArticle>();

            Array values = Enum.GetValues(typeof (SportCategory));
            foreach (SportCategory category in values)
            {
                List<LabeledArticle> articlesForCategory = articles.FindAll(article => article.Category == category);
                int half = articlesForCategory.Count/2;
                for (int i = 0; i < articlesForCategory.Count; i++)
                {
                    if (i < half)
                    {
                        validationSet.Add(articlesForCategory[i]);
                    }
                    else
                    {
                        trainingSet.Add(articlesForCategory[i]);
                    }
                }
            }

            List<Tuple<double[], double[]>> trainingSetConverted = new List<Tuple<double[], double[]>>();
            List<Tuple<double[], double[]>> validationSetConverted = new List<Tuple<double[], double[]>>();

            foreach (LabeledArticle labeledArticle in trainingSet)
            {
                trainingSetConverted.Add(new Tuple<double[], double[]>(GetRawFeatures(labeledArticle.Article), GetOutput(labeledArticle)));
            }
            foreach (LabeledArticle labeledArticle in validationSet)
            {
                validationSetConverted.Add(new Tuple<double[], double[]>(GetRawFeatures(labeledArticle.Article), GetOutput(labeledArticle)));
            }

            double lastAverageError = double.MaxValue;
            while (true)
            {
                foreach (Tuple<double[], double[]> article in trainingSetConverted)
                {
                    TrainClassifierWithArticleFast(article.Item1, article.Item2);
                }
                double averageError = CalculateAverageErrorFast(validationSetConverted);
                if (averageError > lastAverageError)
                {
                    break;
                }
                if ((lastAverageError - averageError) < 0.000001)
                {
                    break;
                }
                if (lastAverageError < 0.0001)
                {
                    break;
                }
                lastAverageError = averageError;
            }

            return lastAverageError;
        }

        private double CalculateAverageErrorFast(List<Tuple<double[], double[]>> validationSetConverted)
        {
            double errorSum = 0;
            foreach (Tuple<double[], double[]> article in validationSetConverted)
            {
                errorSum += CalculateErrorFast(article);
            }
            return errorSum / validationSetConverted.Count;
        }

        private double CalculateErrorFast(Tuple<double[], double[]> article)
        {
            double[] output = _classifier.Compute(article.Item1);
            double sum = output.Sum();
            for (int i = 0; i < output.Length; i++)
            {
                output[i] /= sum;
            }
            double error = 0;
            for (int i = 0; i < output.Length; i++)
            {
                error += Math.Abs(output[i] - article.Item2[i]);
            }
            return error;
        }

        private void TrainClassifierWithArticleFast(double[] input, double[] output)
        {
            _teacher.Run(input, output);
        }

        private double[] GetOutput(LabeledArticle article)
        {
            double[] output = new double[_categoriesCount];
            output[_categoryIndex[article.Category]] = 1;
            return output;
        }

        private double[] GetRawFeatures(string article)
        {
            Annotation annotation = _processor.Annotate(article);
            HashSet<string> persons = NLPCoreHelper.GetPersons(annotation);
            HashSet<string> organizations = NLPCoreHelper.GetOrganizations(annotation);
            HashSet<string> locations = NLPCoreHelper.GetLocation(annotation);
            HashSet<string> sportSpecificWords = NLPCoreHelper.GetOccurence(annotation, _sportSpecificWords);

            double[] personFeatures = PersonOccurenceDatabase.GetFeatures(persons);
            double[] organizationFeatures = OrganizationOccurenceDatabase.GetFeatures(organizations);
            double[] locationFeatures = LocationsOccurenceDatabase.GetFeatures(locations);
            double[] specificFeatures = SportSpecificWordsOccurenceDatabase.GetFeatures(sportSpecificWords);

            double[] features = new double[personFeatures.Length + organizationFeatures.Length + locationFeatures.Length + specificFeatures.Length];
            Array.Copy(personFeatures,0,features,0,personFeatures.Length);
            Array.Copy(organizationFeatures, 0, features, personFeatures.Length, organizationFeatures.Length);
            Array.Copy(locationFeatures, 0, features, personFeatures.Length + organizationFeatures.Length, locationFeatures.Length);
            Array.Copy(specificFeatures, 0, features, personFeatures.Length + organizationFeatures.Length + locationFeatures.Length, specificFeatures.Length);

            for (int i = 0; i < features.Length; i++)
            {
                features[i] -= 0.5;
            }

            return features;
        }

        public void Save()
        {
            PersonOccurenceDatabase.Save(_modelPath + "persons.csv");
            OrganizationOccurenceDatabase.Save(_modelPath + "organizations.csv");
            LocationsOccurenceDatabase.Save(_modelPath + "locations.csv");
            SportSpecificWordsOccurenceDatabase.Save(_modelPath + "specificwords.csv");
            _sportSpecificWords.Save(_modelPath + "specificwords.txt");
            _classifier.Save(_modelPath + "classifier");
        }

        public void Load()
        {
            PersonOccurenceDatabase.Load(_modelPath + "persons.csv");
            OrganizationOccurenceDatabase.Load(_modelPath + "organizations.csv");
            LocationsOccurenceDatabase.Load(_modelPath + "locations.csv");
            SportSpecificWordsOccurenceDatabase.Load(_modelPath + "specificwords.csv");
            _sportSpecificWords.Load(_modelPath + "specificwords.txt");
            if (File.Exists(_modelPath + "classifier"))
            {
                ActivationNetwork network = (ActivationNetwork) Network.Load(_modelPath + "classifier");
                _classifier = network;
                _teacher = new BackPropagationLearning(network);
            }
        }

        public void Reset()
        {
            PersonOccurenceDatabase = new WordOccurenceDatabase();
            OrganizationOccurenceDatabase = new WordOccurenceDatabase();
            LocationsOccurenceDatabase = new WordOccurenceDatabase();
            SportSpecificWordsOccurenceDatabase = new WordOccurenceDatabase();
            int hiddenLayerCount = (10 * _categoriesCount) / 3;
            _classifier = new ActivationNetwork(new SigmoidFunction(), 4 * _categoriesCount, hiddenLayerCount, _categoriesCount);
            _teacher = new BackPropagationLearning(_classifier);
        }

        public double CalculateError(LabeledArticle article)
        {
            double[] input = GetRawFeatures(article.Article);
            double[] output = _classifier.Compute(input);
            double sum = output.Sum();
            for (int i = 0; i < output.Length; i++)
            {
                output[i] /= sum;
            }
            double[] correct = GetOutput(article);
            double error = 0;
            for (int i = 0; i < output.Length; i++)
            {
                error += Math.Abs(output[i] - correct[i]);
            }
            return error;
        }

        public double CalculateAverageError(List<LabeledArticle> articles)
        {
            double errorSum = 0;
            foreach (LabeledArticle article in articles)
            {
                errorSum += CalculateError(article);
            }
            return errorSum / articles.Count;
        }
    }
}
