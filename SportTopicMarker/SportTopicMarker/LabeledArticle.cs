using System;

namespace SportTopicMarker
{
    [Serializable]
    public class LabeledArticle
    {
        public string Article { get; set; }
        public bool IsAboutSport { get; set; }
        public SportCategory Category { get; set; }

        public LabeledArticle(string article, bool isAboutSport, SportCategory category)
        {
            Article = article;
            IsAboutSport = isAboutSport;
            Category = category;
        }

        public LabeledArticle()
        {
        }
    }

    [Serializable]
    public enum SportCategory
    {
        NoSport, 
        AmericanFootball, 
        Football, 
        Tennis, 
        Golf, 
        Biathlon, 
        Athletics, 
        Diving, 
        Swimming, 
        SynchronizedSwimming, 
        WaterPolo, 
        CanoeCayakSprint, 
        CanoeCayakSvalom, 
        Bmx, 
        MountainBiking, 
        RoadCycling, 
        TrackCycling, 
        IceHockey,
        FigureSkating,
        Softball,
        Baseball,
        Rugby,
        Triathlon,
        TableTennis,
        Handball,
        Basketball,
        Bedminton,
        Volleyball
    }
}
