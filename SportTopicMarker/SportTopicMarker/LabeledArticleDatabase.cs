using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SportTopicMarker
{
    public class LabeledArticleDatabase
    {
        private readonly List<LabeledArticle> _articles;

        public LabeledArticleDatabase(List<LabeledArticle> articles)
        {
            _articles = articles;
        }

        public List<LabeledArticle> Articles
        {
            get { return _articles; }
        }

        public static LabeledArticleDatabase LoadFromFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<LabeledArticle>));
            if (!File.Exists(path))
            {
                StreamWriter writer = File.CreateText(path);
                writer.Close();
            }

            FileStream stream = File.OpenRead(path);
            List<LabeledArticle> dataset;
            try
            {
                dataset = (List<LabeledArticle>)serializer.Deserialize(stream);
            }
            catch (Exception)
            {
                dataset = new List<LabeledArticle>();
                Console.WriteLine("Cannot deserialize dataset - creating new");
            }
            stream.Close();

            return new LabeledArticleDatabase(dataset);
        }

        public void Save(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<LabeledArticle>));
            serializer.Serialize(File.OpenWrite(path), _articles);
        }
    }
}
