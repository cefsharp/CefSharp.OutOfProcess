using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CefSharp.Avalonia.Example.ViewModels;
using CefSharp.Avalonia.Example.Views;
using ReactiveUI;
using Splat;

namespace CefSharp.Avalonia.Example;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);

		Locator.CurrentMutable.Register(() => new BrowserView(), typeof(IViewFor<BrowserViewModel>));
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var mainWindow = new MainWindow();

			desktop.MainWindow = mainWindow;
			desktop.ShutdownRequested += (s, e) =>
			{
				mainWindow.Dispose();
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
