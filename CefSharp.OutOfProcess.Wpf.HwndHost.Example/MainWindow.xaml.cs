using System;
using System.IO;
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

        private OutOfProcessHost _outOfProcessHost;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnMainWindowLoaded;
        }

        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}");
            outOfProcessHostPath = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);
            var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\OutOfProcessCache");

            var settings = Settings.WithCachePath(cachePath);

            _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath, settings);

            BrowserContentPresenter.Content = new ChromiumWebBrowser(_outOfProcessHost, "https://google.com");
        }

        private void ShowDevToolsClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
