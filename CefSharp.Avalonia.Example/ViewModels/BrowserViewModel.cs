using ReactiveUI;
using System.Runtime.Serialization;

namespace CefSharp.Avalonia.Example.ViewModels
{
	public class BrowserViewModel : ViewModelBase
	{
		private string _header;

		[DataMember]
		public string Header
		{
			get => _header;
			set => this.RaiseAndSetIfChanged(ref _header, value);
		}
	}
}
