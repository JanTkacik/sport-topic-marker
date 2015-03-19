using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Catel.Data;
using Catel.MVVM;
using SportTopicMarker;

namespace SportTopicMarkerWPF
{
    public class MainViewModel : ViewModelBase
    {
        private const string DataSetPath = @"C:\Users\jantk_000\Documents\GitHub\sport-topic-marker\Dataset\dataset.xml";
        private const string ModelsPath = @"C:\Users\jantk_000\Documents\GitHub\sport-topic-marker\Model\";
        
        public static readonly PropertyData TrainingStatusProperty = RegisterProperty("TrainingStatus", typeof(string));
        public static readonly PropertyData TestingArticleProperty = RegisterProperty("TestingArticle", typeof(string));
        public static readonly PropertyData MarkedTopicProperty = RegisterProperty("MarkedTopic", typeof(string));
        public static readonly PropertyData SelectedRealTopicProperty = RegisterProperty("SelectedRealTopic", typeof(SportCategory));

        private readonly LabeledArticleDatabase _database;
        private Marker _marker;
        private NLPProcessor _processor;

        public ObservableCollection<LabeledArticle> DataSet
        {
            get { return _database.ArticlesObservable; }
        }
        public AsynchronousCommand Train { get; private set; }
        public Command ResetTrainedStatus { get; private set; }

        public string TrainingStatus
        {
            get { return GetValue<string>(TrainingStatusProperty); } 
            set { SetValue(TrainingStatusProperty, value); }
        }

        public string TestingArticle
        {
            get { return GetValue<string>(TestingArticleProperty); }
            set { SetValue(TestingArticleProperty, value); }
        }

        public AsynchronousCommand Test { get; private set; }

        public Command EnhanceDatabase { get; private set; }

        public string MarkedTopic
        {
            get { return GetValue<string>(MarkedTopicProperty); }
            set { SetValue(MarkedTopicProperty, value); }
        }

        public List<SportCategory> Topics
        {
            get
            {
                Array list = Enum.GetValues(typeof (SportCategory));
                List<SportCategory> categories = new List<SportCategory>();
                foreach (SportCategory category in list)
                {
                    categories.Add(category);
                }
                return categories;
            }
        }

        public SportCategory SelectedRealTopic
        {
            get { return GetValue<SportCategory>(SelectedRealTopicProperty); }
            set { SetValue(SelectedRealTopicProperty, value); }
        }

        public MainViewModel()
        {
            _database = LabeledArticleDatabase.LoadFromFile(DataSetPath);
            Train = new AsynchronousCommand(TrainAction, () => !Train.IsExecuting);
            ResetTrainedStatus = new Command(ResetStatusAction);
            Test = new AsynchronousCommand(TestAction, () => !Test.IsExecuting && !string.IsNullOrWhiteSpace(TestingArticle));
            EnhanceDatabase = new Command(EnhanceDatabaseAction, () => !string.IsNullOrWhiteSpace(TestingArticle));
            TrainingStatus = "Not started yet";
        }

        private void ResetStatusAction()
        {
            foreach (LabeledArticle labeledArticle in _database.ArticlesObservable)
            {
                labeledArticle.IsProcessed = false;
            }
            if (_marker == null)
            {
                LoadMarker();
            }

            _marker.Reset();
        }

        private void EnhanceDatabaseAction()
        {
            LabeledArticle article = new LabeledArticle(TestingArticle, SelectedRealTopic);
            _database.ArticlesObservable.Add(article);
            _database.Save(DataSetPath);
        }

        private void LoadMarker()
        {
            _processor = new NLPProcessor();
            _marker = new Marker(_processor, ModelsPath);
            _marker.Load();
        }

        private void TestAction()
        {
            if (_marker == null)
            {
                Test.ReportProgress(() => MarkedTopic = "Loading marker...");
                LoadMarker();
            }
            if (_marker != null)
            {
                LabeledArticle labeledArticle = _marker.LabelArticle(TestingArticle);
                Test.ReportProgress(() => MarkedTopic = labeledArticle.Category.ToString());
            }
        }

        private void TrainAction()
        {
            if (_marker == null)
            {
                Train.ReportProgress(() => TrainingStatus = "Loading marker...");
                LoadMarker();
            }
            if (_marker != null)
            {
                List<LabeledArticle> articles = _database.Articles.FindAll(article => !article.IsProcessed);
                int total = articles.Count;
                int i = 1;

                foreach (LabeledArticle article in articles)
                {
                    int i1 = i;
                    Train.ReportProgress(() => TrainingStatus = string.Format("Extending database with article {0}/{1}", i1, total));
                    _marker.ExtendDatabaseWithArticle(article);
                    i++;
                }

                Train.ReportProgress(() => TrainingStatus = "Teaching classifier ...");

                double averageError = _marker.TrainClassifierWithArticles(_database.Articles);

                Train.ReportProgress(() => TrainingStatus = "Saving...");

                foreach (LabeledArticle article in _database.Articles)
                {
                    article.IsProcessed = true;
                }

                _database.Save(DataSetPath);
                _marker.Save();

                Train.ReportProgress(() => TrainingStatus = "Training completed with average error on validation set " + averageError);
            }
        }
    }
}
