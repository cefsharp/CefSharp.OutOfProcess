using ReactiveUI;
using System.Reactive.Disposables;

namespace CefSharp.Avalonia.Example.ViewModels
{
	public class ViewModelBase : ReactiveObject, IActivatableViewModel
	{
		public ViewModelActivator Activator { get; }

		public ViewModelBase()
		{
			Activator = new ViewModelActivator();
			this.WhenActivated((CompositeDisposable disposables) =>
			{
				/* handle activation */
				Disposable
					.Create(() => { /* handle deactivation */ })
					.DisposeWith(disposables);
			});
		}
	}
}
