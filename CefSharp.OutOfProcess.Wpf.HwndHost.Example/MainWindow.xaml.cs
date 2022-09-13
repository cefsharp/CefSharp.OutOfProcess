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

#if NETCOREAPP3_1_OR_GREATER
        private string _targetFramework = "netcoreapp3.1";
#else
        private string _targetFramework = "net462";
#endif

      //  private OutOfProcessHost _outOfProcessHost;

        public MainWindow()
        {
            var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}\\{_targetFramework}");
            ChromiumWebBrowser2.Path = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);
            ChromiumWebBrowser2.CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\OutOfProcessCache");

            InitializeComponent();

         //   Loaded += OnMainWindowLoaded;
            txtBoxAddress.TextChanged += TxtBoxAddress_TextChanged;
        }

        private void TxtBoxAddress_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            tcbrowser.Address = txtBoxAddress.Text;
           // ((ChromiumWebBrowser2)BrowserContentPresenter.Content).Address = txtBoxAddress.Text;
        }

        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
         //   _outOfProcessHost = await OutOfProcessHost.CreateAsync(ChromiumWebBrowser2.Path, ChromiumWebBrowser2.CachePath);

         //   BrowserContentPresenter.Content = new ChromiumWebBrowser2(_outOfProcessHost, "https://google.com");
        }
    }
}
