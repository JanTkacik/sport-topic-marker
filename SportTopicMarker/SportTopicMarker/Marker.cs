using System;
using System.Collections.Generic;
using AForge.Neuro;
using AForge.Neuro.Learning;
using edu.stanford.nlp.pipeline;

namespace SportTopicMarker
{
    public class Marker
    {
        private readonly NLPProcessor _processor;
        private readonly string _modelPath;
        private readonly WordOccurenceDatabase _personOccurenceDatabase;
        private readonly WordOccurenceDatabase _organizationOccurenceDatabase;
        private readonly WordOccurenceDatabase _locationsOccurenceDatabase;
        private readonly WordOccurenceDatabase _sportSpecificWordsOccurenceDatabase;
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
            _sportSpecificWords = new HashSet<string>();
            _personOccurenceDatabase = new WordOccurenceDatabase();
            _organizationOccurenceDatabase = new WordOccurenceDatabase();
            _locationsOccurenceDatabase = new WordOccurenceDatabase();
            _sportSpecificWordsOccurenceDatabase = new WordOccurenceDatabase();
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
            return new LabeledArticle(article, maxIndex == 0, _indexCategory[maxIndex]);
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
                _personOccurenceDatabase.AddWord(person, article.Category);
            }

            foreach (string organization in organizations)
            {
                _organizationOccurenceDatabase.AddWord(organization, article.Category);
            }

            foreach (string location in locations)
            {
                _locationsOccurenceDatabase.AddWord(location, article.Category);
            }

            foreach (string sportSpecificWord in sportSpecificWords)
            {
                _sportSpecificWordsOccurenceDatabase.AddWord(sportSpecificWord, article.Category);
            }
        }

        public double TrainClassifierWithArticle(LabeledArticle article)
        {
            double[] input = GetRawFeatures(article.Article);
            double[] output = GetOutput(article);

            SportCategory category;
            double error;
            int i = 0;
            do
            {
                i++;
                error = _teacher.Run(input, output);
                double[] calculated = _classifier.Compute(input);
                category = _indexCategory[GetMaxIndex(calculated)];
            } while ((article.Category != category) && (i < 10));

            return error;
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

            double[] personFeatures = _personOccurenceDatabase.GetFeatures(persons);
            double[] organizationFeatures = _organizationOccurenceDatabase.GetFeatures(organizations);
            double[] locationFeatures = _locationsOccurenceDatabase.GetFeatures(locations);
            double[] specificFeatures = _sportSpecificWordsOccurenceDatabase.GetFeatures(sportSpecificWords);

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
            _personOccurenceDatabase.Save(_modelPath + "persons.csv");
            _organizationOccurenceDatabase.Save(_modelPath + "organizations.csv");
            _locationsOccurenceDatabase.Save(_modelPath + "locations.csv");
            _sportSpecificWordsOccurenceDatabase.Save(_modelPath + "specificwords.csv");
            _sportSpecificWords.Save(_modelPath + "specificwords.txt");
            _classifier.Save(_modelPath + "classifier");
        }

        public void Load()
        {
            _personOccurenceDatabase.Load(_modelPath + "persons.csv");
            _organizationOccurenceDatabase.Load(_modelPath + "organizations.csv");
            _locationsOccurenceDatabase.Load(_modelPath + "locations.csv");
            _sportSpecificWordsOccurenceDatabase.Load(_modelPath + "specificwords.csv");
            _sportSpecificWords.Load(_modelPath + "specificwords.txt");
            ActivationNetwork network = (ActivationNetwork)Network.Load(_modelPath + "classifier");
            _classifier = network;
            _teacher = new BackPropagationLearning(network);
        }
    }
}
