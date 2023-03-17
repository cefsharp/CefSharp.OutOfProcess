using System.Collections.ObjectModel;

namespace CefSharp.Avalonia.Example.ViewModels
{
	internal class MainWindowViewModel : ViewModelBase
	{
		public ObservableCollection<BrowserViewModel> Tabs { get; } = new();

		public MainWindowViewModel()
		{
			AddTab();
		}

		public void AddTab()
		{
			Tabs.Add(new BrowserViewModel { Header = "New Tab" });
		}
	}
}
