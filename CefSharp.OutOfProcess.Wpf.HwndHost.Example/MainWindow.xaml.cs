using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CefSharp.OutOfProcess.Wpf.HwndHost.Example
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
            _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath, false, cachePath);
            BrowserContentPresenter.Content = new ChromiumWebBrowser(_outOfProcessHost, "https://google.com");
        }


        private void ShowDevToolsClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
