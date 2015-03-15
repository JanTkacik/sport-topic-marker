using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SportTopicMarker
{
    public class WordOccurenceDatabase
    {
        private readonly Dictionary<string, Dictionary<SportCategory, int>> _wordOccurenceDatabase;

        public WordOccurenceDatabase()
        {
            _wordOccurenceDatabase = new Dictionary<string, Dictionary<SportCategory, int>>();
        }

        public Dictionary<string, Dictionary<SportCategory, int>> OccurenceDatabase
        {
            get { return _wordOccurenceDatabase; }
        }

        public void AddWord(string lemma, SportCategory category)
        {
            string key = lemma.ToLowerInvariant();

            if (!_wordOccurenceDatabase.ContainsKey(key))
            {
                _wordOccurenceDatabase.Add(key, new Dictionary<SportCategory, int>());
            }

            Dictionary<SportCategory, int> counts = _wordOccurenceDatabase[key];
            if (counts.ContainsKey(category))
            {
                counts[category]++;
            }
            else
            {
                counts.Add(category, 1);
            }
        }

        public void Save(string path)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Word");
            string[] names = Enum.GetNames(typeof (SportCategory));
            foreach (string name in names)
            {
                builder.Append("," + name);
            }

            foreach (KeyValuePair<string, Dictionary<SportCategory, int>> occurence in _wordOccurenceDatabase)
            {
                builder.Append(Environment.NewLine);
                builder.Append(occurence.Key);
                foreach (string name in names)
                {
                    SportCategory category = (SportCategory)Enum.Parse(typeof(SportCategory), name);
                    if (occurence.Value.ContainsKey(category))
                    {
                        builder.Append("," + occurence.Value[category]);
                    }
                    else
                    {
                        builder.Append(",0");
                    }
                }
            }

            File.WriteAllText(path, builder.ToString());
        }

        public void Load(string path)
        {
            var lines = File.ReadAllLines(path);
            string[] header = lines[0].Split(',');
            Dictionary<int, SportCategory> categoryIndex = new Dictionary<int, SportCategory>();
            for (int i = 0; i < header.Length; i++)
            {
                string cat = header[i];
                SportCategory category;
                if (Enum.TryParse(cat, out category))
                {
                    categoryIndex.Add(i, category);
                }
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string wordLine = lines[i];
                string[] data = wordLine.Split(',');
                string word = data[0];
                _wordOccurenceDatabase.Add(word, new Dictionary<SportCategory, int>());
                Dictionary<SportCategory, int> dict = _wordOccurenceDatabase[word];
                for (int j = 1; j < data.Length; j++)
                {
                    dict.Add(categoryIndex[j], int.Parse(data[j]));
                }
            }
        }

        public double[] GetFeatures(HashSet<string> data)
        {
            Array array = Enum.GetValues(typeof (SportCategory));
            double[] features = new double[array.Length];

            foreach (string s in data)
            {
                if (_wordOccurenceDatabase.ContainsKey(s))
                {
                    Dictionary<SportCategory, int> partial = _wordOccurenceDatabase[s];
                    for (int i = 0; i < array.Length; i++)
                    {
                        SportCategory category = (SportCategory)array.GetValue(i);
                        if (partial.ContainsKey(category))
                        {
                            features[i] += partial[category];
                        }
                    }
                }
            }

            double sum = features.Sum();
            if (Math.Abs(sum) < 0.00001)
            {
                double partial = (double)1/((features.Length - 1)*2);
                features[0] = partial*(features.Length - 1);
                for (int i = 1; i < features.Length; i++)
                {
                    features[i] = partial;
                }
            }
            else
            {
                for (int i = 0; i < features.Length; i++)
                {
                    features[i] /= sum;
                }
            }

            return features;
        }
    }
}
