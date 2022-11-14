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
        private string _targetFramework = "net462";
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
            _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath, cachePath);

            browser = new ChromiumWebBrowser(_outOfProcessHost, "https://google.com");
            BrowserContentPresenter.Content = browser;
           // _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath, cachePath);

            //   browser = new ChromiumWebBrowser(_outOfProcessHost, "https://google.com");
            //  BrowserContentPresenter.Content = browser;
            browser.DevToolsContextAvailable += Browser_DevToolsContextAvailable;
        }

        private ChromiumWebBrowser browser;

        private void Browser_DevToolsContextAvailable(object sender, EventArgs e)
        {
            Task t = browser.DevToolsContext.ExposeFunctionAsync("Foo", Foo);
            t.Wait();

            Task tt = browser.DevToolsContext.EvaluateExpressionAsync("Foo()");
            tt.Wait();

            browser.DevToolsContext.GoToAsync("http://www.sz.de");
        }

        void Foo()
        {
            ;
        }


        private void ShowDevToolsClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
