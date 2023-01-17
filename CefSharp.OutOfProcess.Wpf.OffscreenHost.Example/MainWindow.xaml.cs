using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace CefSharp.OutOfProcess.Wpf.OffscreenHost.Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if DEBUG
        private string _buildType = "Debug";
#else
        private string _buildType = "Release";
#endif

        #if NETCOREAPP3_1_OR_GREATER
        private string _targetFramework = "netcoreapp3.1";
        #else
        private string _targetFramework = "net472";
        #endif

        private OutOfProcessHost _outOfProcessHost;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnMainWindowLoaded;
        }

        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {

            var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}\\{_targetFramework}");
            outOfProcessHostPath = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);
            var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\OutOfProcessCache");

            _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath, true, cachePath);

            var browser = new OffscreenChromiumWebBrowser(_outOfProcessHost, "https://www.w3schools.com/tags/tryit.asp?filename=tryhtml_select");
            BrowserContentPresenter.Content = browser;

        }

        private void ShowDevToolsClick(object sender, RoutedEventArgs e)
        {
            ;
        }
    }
}
