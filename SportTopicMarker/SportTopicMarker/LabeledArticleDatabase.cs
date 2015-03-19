using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SportTopicMarker
{
    public class LabeledArticleDatabase
    {
        private readonly ObservableCollection<LabeledArticle> _articles;

        public LabeledArticleDatabase(List<LabeledArticle> articles)
        {
            _articles = new ObservableCollection<LabeledArticle>(articles);
        }

        public List<LabeledArticle> Articles
        {
            get { return _articles.ToList(); }
        }

        public ObservableCollection<LabeledArticle> ArticlesObservable
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
            catch (Exception ex)
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
            File.Delete(path);
            FileStream fileStream = File.OpenWrite(path);
            serializer.Serialize(fileStream, Articles);
            fileStream.Close();
        }
    }
}
