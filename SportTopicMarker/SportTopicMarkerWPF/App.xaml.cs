using System.Windows;
using Catel.Logging;

namespace SportTopicMarkerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LogManager.IgnoreCatelLogging = true;
            LogManager.AddDebugListener(true);
        }
    }
}
