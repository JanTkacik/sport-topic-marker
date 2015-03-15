using System;
using SportTopicMarker;

namespace SportTopicMarkerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Invalid number of arguments, first argument should be path to dataset");
                Console.WriteLine("Second argument is action - teach means that database will be upgraded - process means that provided article will be labeled");
                return;
            }

            string pathToDataset = args[0];
            string action = args[1];

            LabeledArticleDatabase database = LabeledArticleDatabase.LoadFromFile(pathToDataset);

            NLPProcessor processor = new NLPProcessor();
            Marker marker = new Marker(processor, @"C:\Users\jantk_000\Documents\GitHub\sport-topic-marker\Model\");
            var articleCount = database.Articles.Count;

            if ("teach".Equals(action))
            {
                for (int i = 0; i < articleCount; i++)
                {
                    Console.WriteLine("Processing article {0}/{1}", i + 1, articleCount);
                    LabeledArticle article = database.Articles[i];
                    marker.ExtendDatabaseWithArticle(article);
                }

                for (int i = 0; i < articleCount; i++)
                {
                    Console.WriteLine("Teaching network with article {0}/{1}", i + 1, articleCount);
                    marker.TrainClassifierWithArticle(database.Articles[i]);
                }

                for (int i = 0; i < articleCount; i++)
                {
                    LabeledArticle labeled = marker.LabelArticle(database.Articles[i].Article);
                    Console.WriteLine("Article marked as: {0} should be: {1}", labeled.Category, database.Articles[i].Category);
                }

                marker.Save();
            }
            else if ("process".Equals(action))
            {
                marker.Load();

                for (int i = 0; i < articleCount; i++)
                {
                    LabeledArticle labeled = marker.LabelArticle(database.Articles[i].Article);
                    Console.WriteLine("Article marked as: {0} should be: {1}", labeled.Category, database.Articles[i].Category);
                }
            }

            Console.ReadLine();
        }
    }
}
