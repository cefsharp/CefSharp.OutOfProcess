using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;

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


        private string browserTargetFramework = "netcoreapp3.1";


      //  private OutOfProcessHost _outOfProcessHost;

        public MainWindow()
        {
            var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}\\{browserTargetFramework}");
            ChromiumWebBrowser2.Path = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);
            ChromiumWebBrowser2.CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\OutOfProcessCache");

            InitializeComponent();

            txtBoxAddress.TextChanged += TxtBoxAddress_TextChanged;
        }

        private void TxtBoxAddress_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            tcbrowser.Address = txtBoxAddress.Text;
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            contenthost.Child = new TcWebBrowser() { Address = "http://trumpf.com" };
        }
        
    }
}
