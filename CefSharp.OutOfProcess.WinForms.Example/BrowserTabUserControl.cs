// Copyright © 2010 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefSharp.OutOfProcess.WinForms.Example
{
    public partial class BrowserTabUserControl : UserControl
    {
        public IChromiumWebBrowser Browser { get; private set; }

        public BrowserTabUserControl(OutOfProcessHost outOfProcessHost, string url)
        {
            InitializeComponent();

            var browser = new ChromiumWebBrowser(outOfProcessHost, url)
            {
                Dock = DockStyle.Fill,
                Bounds = this.Bounds
            };

            browserPanel.Controls.Add(browser);

            Browser = browser;

            browser.LoadingStateChanged += OnBrowserLoadingStateChanged;
            browser.ConsoleMessage += OnBrowserConsoleMessage;
            browser.TitleChanged += OnBrowserTitleChanged;
            browser.AddressChanged += OnBrowserAddressChanged;
            browser.StatusMessage += OnBrowserStatusMessage;
            browser.NetworkRequestFailed += OnBrowserNetworkRequestFailed;
            //browser.LoadError += OnLoadError;

            var version = string.Format("Chromium: {0}, CEF: {1}, CefSharp: {2}", outOfProcessHost.ChromiumVersion, outOfProcessHost.CefVersion, outOfProcessHost.CefSharpVersion);
            //Set label directly, don't use DisplayOutput as call would be a NOOP (no valid handle yet).
            outputLabel.Text = version;
        }

        private  async void OnBrowserNetworkRequestFailed(object sender, Puppeteer.RequestEventArgs args)
        {
            var request = args.Request;

            var errorHtml = string.Format("<html><body><h2>Failed to load URL {0} with error {1} ({2}).</h2></body></html>",
                                              request.Url, request.Failure);

            await Browser.MainFrame.SetContentAsync(errorHtml);

            //AddressChanged isn't called for failed Urls so we need to manually update the Url TextBox
            this.InvokeOnUiThreadIfRequired(() => urlTextBox.Text = request.Url);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                components = null;
            }
            base.Dispose(disposing);
        }

        private void OnBrowserConsoleMessage(object sender, Puppeteer.ConsoleEventArgs args)
        {
            
            DisplayOutput(string.Format("Line: {0}, Source: {1}, Message: {2}", args.Message.Location.LineNumber, args.Message.Location.URL, args.Message.Text));
        }

        private void OnBrowserStatusMessage(object sender, StatusMessageEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => statusLabel.Text = args.Value);
        }

        private void OnBrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {
            SetCanGoBack(args.CanGoBack);
            SetCanGoForward(args.CanGoForward);

            this.InvokeOnUiThreadIfRequired(() => SetIsLoading(args.IsLoading));
        }

        private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => Parent.Text = args.Title);
        }

        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => urlTextBox.Text = args.Address);
        }

        private void SetCanGoBack(bool canGoBack)
        {
            this.InvokeOnUiThreadIfRequired(() => backButton.Enabled = canGoBack);
        }

        private void SetCanGoForward(bool canGoForward)
        {
            this.InvokeOnUiThreadIfRequired(() => forwardButton.Enabled = canGoForward);
        }

        private void SetIsLoading(bool isLoading)
        {
            goButton.Text = isLoading ?
                "Stop" :
                "Go";
            goButton.Image = isLoading ?
                Properties.Resources.nav_plain_red :
                Properties.Resources.nav_plain_green;

            HandleToolStripLayout();
        }

        private void DisplayOutput(string output)
        {
            outputLabel.InvokeOnUiThreadIfRequired(() => outputLabel.Text = output);
        }

        private void HandleToolStripLayout(object sender, LayoutEventArgs e)
        {
            HandleToolStripLayout();
        }

        private void HandleToolStripLayout()
        {
            var width = toolStrip1.Width;
            foreach (ToolStripItem item in toolStrip1.Items)
            {
                if (item != urlTextBox)
                {
                    width -= item.Width - item.Margin.Horizontal;
                }
            }
            urlTextBox.Width = Math.Max(0, width - urlTextBox.Margin.Horizontal - 18);
        }

        private void GoButtonClick(object sender, EventArgs e)
        {
            LoadUrl(urlTextBox.Text);
        }

        private async void BackButtonClick(object sender, EventArgs e)
        {
            _ = await Browser.GoBackAsync();
        }

        private async void ForwardButtonClick(object sender, EventArgs e)
        {
            _ = await Browser.GoForwardAsync();
        }

        private void UrlTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            LoadUrl(urlTextBox.Text);
        }

        private void LoadUrl(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                Browser.LoadUrl(url);
            }
            else
            {
                var searchUrl = "https://www.google.com/search?q=" + Uri.EscapeDataString(url);

                Browser.LoadUrl(searchUrl);
            }

        }

        
        public async Task CopySourceToClipBoardAsync()
        {
            var htmlSource = await Browser.MainFrame.GetContentAsync();

            Clipboard.SetText(htmlSource);
            DisplayOutput("HTML Source copied to clipboard");
        }

        private void ToggleBottomToolStrip()
        {
            if (toolStrip2.Visible)
            {
                //Browser.StopFinding(true);
                toolStrip2.Visible = false;
            }
            else
            {
                toolStrip2.Visible = true;
                findTextBox.Focus();
            }
        }

        private void FindNextButtonClick(object sender, EventArgs e)
        {
            Find(true);
        }

        private void FindPreviousButtonClick(object sender, EventArgs e)
        {
            Find(false);
        }

        private void Find(bool next)
        {
            if (!string.IsNullOrEmpty(findTextBox.Text))
            {
                //Browser.Find(findTextBox.Text, next, false, false);
            }
        }

        private void FindTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            Find(true);
        }

        public void ShowFind()
        {
            ToggleBottomToolStrip();
        }

        private void FindCloseButtonClick(object sender, EventArgs e)
        {
            ToggleBottomToolStrip();
        }
    }
}
