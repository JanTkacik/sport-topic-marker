using System;
using System.Text;
using SportTopicMarker;

namespace SportTopicDatasetMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Dataset maker");

            if (args.Length < 1)
            {
                Console.WriteLine("Invalid number of arguments, first argument should be path to dataset");
                return;
            }

            string pathToDataset = args[0];

            LabeledArticleDatabase database = LabeledArticleDatabase.LoadFromFile(pathToDataset);

            Console.WriteLine("Currently dataset holds {0} articles", database.Articles.Count);
            
            while (true)
            {
                Console.WriteLine("Press 'A' to add new article");
                Console.WriteLine("Press 'ESC' to quit");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.A)
                {
                    StringBuilder article = new StringBuilder();
                    Console.WriteLine("Please enter article. Type '---' after end of article.");
                    while (true)
                    {
                        string line = Console.ReadLine();
                        if ("---".Equals(line))
                        {
                            break;
                        }
                        article.AppendLine(line);
                    }
                    bool isAboutSport;
                    Console.WriteLine("Is this article about sport (Y/N)?");
                    while (true)
                    {
                        ConsoleKeyInfo keyPressed = Console.ReadKey();
                        if (keyPressed.Key == ConsoleKey.Y)
                        {
                            isAboutSport = true;
                            break;
                        }
                        if (keyPressed.Key == ConsoleKey.N)
                        {
                            isAboutSport = false;
                            break;
                        }
                    }

                    if (isAboutSport)
                    {
                        SportCategory category;
                        Console.WriteLine("What sport ?");
                        while (true)
                        {
                            string sport = Console.ReadLine();
                            bool ok = Enum.TryParse(sport, out category);
                            if (ok)
                            {
                                break;
                            }
                            Console.WriteLine("Invalid sport - choose one from:");
                            string[] names = Enum.GetNames(typeof (SportCategory));
                            foreach (string name in names)
                            {
                                Console.WriteLine(name);
                            }
                        }
                        database.Articles.Add(new LabeledArticle(article.ToString(), category));
                    }
                    else
                    {
                        database.Articles.Add(new LabeledArticle(article.ToString(), SportCategory.NoSport));
                    }
                }
                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            database.Save(pathToDataset);
        }
    }
}
