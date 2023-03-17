using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CefSharp.Avalonia.Example.ViewModels;
using CefSharp.Avalonia.Example.Views;
using CefSharp.OutOfProcess;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CefSharp.Avalonia.Example;

public partial class MainWindow : Window, IDisposable
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

		_ = InitializeComponentAsync();
    }

    private async Task InitializeComponentAsync()
    {
        var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}");
        outOfProcessHostPath = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);
        var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\OutOfProcessCache");

        var settings = Settings.WithCachePath(cachePath);
        _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath, settings);

        ChromiumWebBrowser.SetDefaultOutOfProcessHost(_outOfProcessHost);

		DataContext = new MainWindowViewModel();
	}

    private BrowserView ActiveBrowserView => (BrowserView) this.FindControl<TabControl>("tabControl").SelectedContent;

	private void OnFileExitMenuItemClick(object sender, RoutedEventArgs e)
	{
        Close();
	}

	public void Dispose()
	{
        _outOfProcessHost?.Dispose();
	}
}
