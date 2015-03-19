using System;

namespace SportTopicMarker
{
    [Serializable]
    public class LabeledArticle
    {
        public string Article { get; set; }
        public SportCategory Category { get; set; }
        public bool IsProcessed { get; set; }

        public string ArticleStub 
        {
            get { return Article.Substring(0, 100).Replace("\n", " "); }
        }

        public LabeledArticle(string article, SportCategory category)
        {
            Article = article;
            Category = category;
            IsProcessed = false;
        }

        public LabeledArticle()
        {
        }
    }

    [Serializable]
    public enum SportCategory
    {
        NoSport,  
        Football, 
        Tennis,      
        IceHockey,
        Basketball
    }
}
