namespace SportTopicMarkerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            var model = new MainViewModel();
            DataContext = model;
            
        }
    }
}
