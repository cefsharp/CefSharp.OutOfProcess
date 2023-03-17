using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CefSharp.Avalonia.Example.ViewModels;
using CefSharp.OutOfProcess;
using ReactiveUI;
using Tmds.DBus;

namespace CefSharp.Avalonia.Example.Views;

public partial class BrowserView : UserControl, IViewFor<BrowserViewModel>
{
	public BrowserView()
    {
		InitializeComponent();

		Browser.Address = "https://www.google.com";
		Browser.AddressChanged += OnAddressChanged;
		Browser.TitleChanged += OnTitleChanged;
	}

	public BrowserViewModel? ViewModel { get; set; }

	object? IViewFor.ViewModel
	{
		get => ViewModel;
		set => ViewModel = (BrowserViewModel?)value;
	}

	private void OnTitleChanged(object sender, TitleChangedEventArgs e)
	{
		Dispatcher.UIThread.Post(() =>
		{
			ViewModel.Header = e.Title;
		});
	}

	private void OnAddressChanged(object sender, AddressChangedEventArgs e)
	{
		Dispatcher.UIThread.Post(() =>
		{
			var addressTextBox = this.FindControl<TextBox>("addressTextBox");

			addressTextBox.Text = e.Address;
		});
	}

    private void OnAddressTextBoxKeyDown(object sender, global::Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Browser.Address = ((TextBox)sender).Text;
        }
    }
    
    public void Dispose()
    {
        //browser.Dispose();
    }
}