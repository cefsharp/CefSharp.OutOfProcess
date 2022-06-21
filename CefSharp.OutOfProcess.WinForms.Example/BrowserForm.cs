// Copyright © 2022 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CefSharp.OutOfProcess.WinForms.Example
{
    public partial class BrowserForm : Form
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

        private const string DefaultUrlForAddedTabs = "https://www.google.com";
        private OutOfProcessHost _outOfProcessHost;

        public BrowserForm()
        {
            InitializeComponent();

            var bitness = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            Text = "CefSharp.OutOfProcess.WinForms.Example - " + bitness;
            WindowState = FormWindowState.Maximized;

            Load += BrowserFormLoad;

            //Only perform layout when control has completly finished resizing
            ResizeBegin += (s, e) => SuspendLayout();
            ResizeEnd += (s, e) => ResumeLayout(true);
        }

        public IContainer Components
        {
            get
            {
                if (components == null)
                {
                    components = new Container();
                }

                return components;
            }
        }

        private async void BrowserFormLoad(object sender, EventArgs e)
        {
            var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}\\{_targetFramework}");
            outOfProcessHostPath = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);
            _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath);

            AddTab(DefaultUrlForAddedTabs);
        }

        private void AddTab(string url, int? insertIndex = null)
        {
            browserTabControl.SuspendLayout();

            var browser = new BrowserTabUserControl(_outOfProcessHost, url)
            {
                Dock = DockStyle.Fill,
                Bounds = browserTabControl.Bounds
            };

            var tabPage = new TabPage(url)
            {
                Dock = DockStyle.Fill
            };

            tabPage.Controls.Add(browser);

            if (insertIndex == null)
            {
                browserTabControl.TabPages.Add(tabPage);
            }
            else
            {
                browserTabControl.TabPages.Insert(insertIndex.Value, tabPage);
            }

            //Make newly created tab active
            browserTabControl.SelectedTab = tabPage;

            browserTabControl.ResumeLayout(true);
        }

        private void ExitMenuItemClick(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void ExitApplication()
        {
            Close();
        }

        private void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            new AboutBox(_outOfProcessHost.CefSharpVersion, _outOfProcessHost.CefVersion, _outOfProcessHost.ChromiumVersion).ShowDialog();
        }

        private void FindMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.ShowFind();
            }
        }

        private void CopySourceToClipBoardAsyncClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                _ = control.CopySourceToClipBoardAsync();
            }
        }

        private BrowserTabUserControl GetCurrentTabControl()
        {
            if (browserTabControl.SelectedIndex == -1)
            {
                return null;
            }

            var tabPage = browserTabControl.Controls[browserTabControl.SelectedIndex];
            var control = tabPage.Controls[0] as BrowserTabUserControl;

            return control;
        }

        private void NewTabToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddTab(DefaultUrlForAddedTabs);
        }

        private void CloseTabToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (browserTabControl.TabPages.Count == 0)
            {
                return;
            }

            var currentIndex = browserTabControl.SelectedIndex;

            var tabPage = browserTabControl.TabPages[currentIndex];

            var control = GetCurrentTabControl();
            if (control != null && !control.IsDisposed)
            {
                control.Dispose();
            }

            browserTabControl.TabPages.Remove(tabPage);

            tabPage.Dispose();

            browserTabControl.SelectedIndex = currentIndex - 1;

            if (browserTabControl.TabPages.Count == 0)
            {
                ExitApplication();
            }
        }

        private void UndoMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //control.Browser.Undo();
            }
        }

        private void RedoMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //control.Browser.Redo();
            }
        }

        private void CutMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //control.Browser.Cut();
            }
        }

        private void CopyMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //control.Browser.Copy();
            }
        }

        private void PasteMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //control.Browser.Paste();
            }
        }

        private void DeleteMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //control.Browser.Delete();
            }
        }

        private async void SelectAllMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var devtoolsContext = control.Browser.DevToolsContext;

                _ = await devtoolsContext.EvaluateExpressionAsync("document.execCommand('selectAll');");
            }
        }

        private async void PrintToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var devtoolsContext = control.Browser.DevToolsContext;

                _ = await devtoolsContext.EvaluateExpressionAsync("window.print()");
            }
        }

        private async void ShowDevToolsMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //var isDevToolsOpen = await control.CheckIfDevToolsIsOpenAsync();
                //if (!isDevToolsOpen)
                //{
                //    control.Browser.ShowDevTools();
                //}
            }
        }

        private async void CloseDevToolsMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                throw new NotImplementedException();
                //Check if DevTools is open before closing, this isn't strictly required
                //If DevTools isn't open and you call CloseDevTools it's a No-Op, so prefectly
                //safe to call without checking
                //var isDevToolsOpen = await control.CheckIfDevToolsIsOpenAsync();
                //if (isDevToolsOpen)
                //{
                //control.Browser.CloseDevTools();
                //}
            }
        }

        private async void ZoomInToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var devtoolsContext = control.Browser.DevToolsContext;

                var currentZoomLevel = await devtoolsContext.EvaluateExpressionAsync<double>("window.devicePixelRatio");

                await devtoolsContext.SetPageScaleFactorAsync(currentZoomLevel + 0.25);
            }
        }

        private async void ZoomOutToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var devtoolsContext = control.Browser.DevToolsContext;

                var currentZoomLevel = await devtoolsContext.EvaluateExpressionAsync<double>("window.devicePixelRatio");

                await devtoolsContext.SetPageScaleFactorAsync(currentZoomLevel - 0.25);
            }
        }

        private async void CurrentZoomLevelToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var devtoolsContext = control.Browser.DevToolsContext;

                var currentZoomLevel = await devtoolsContext.EvaluateExpressionAsync<double>("window.devicePixelRatio");

                MessageBox.Show("Current ZoomLevel: " + currentZoomLevel.ToString());
            }
        }

        private async void PrintToPdfToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                var dialog = new SaveFileDialog
                {
                    DefaultExt = ".pdf",
                    Filter = "Pdf documents (.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var devtoolsContext = control.Browser.DevToolsContext;

                    await devtoolsContext.PdfAsync(dialog.FileName);

                    if (File.Exists(dialog.FileName))
                    {
                        MessageBox.Show("Pdf was saved to " + dialog.FileName);
                    }
                    else
                    {
                        MessageBox.Show("Unable to save Pdf, check you have write permissions to " + dialog.FileName);
                    }

                }

            }
        }

        private void OpenDataUrlToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                const string html = "<html><head><title>Test</title></head><body><h1>Html Encoded in URL!</h1></body></html>";
                _ = control.Browser.MainFrame.SetContentAsync(html);
            }
        }

        private void OpenHttpBinOrgToolStripMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();
            if (control != null)
            {
                control.Browser.LoadUrl("https://httpbin.org/");
            }
        }

        private async void TakeScreenShotMenuItemClick(object sender, EventArgs e)
        {
            var control = GetCurrentTabControl();

            if(control == null)
            {
                return;
            }

            var devtoolsContext = control.Browser.DevToolsContext;

            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot" + DateTime.Now.Ticks + ".png");

            await devtoolsContext.ScreenshotAsync(screenshotPath);

            if (File.Exists(screenshotPath))
            {
                // Tell Windows to launch the saved image.
                Process.Start(new ProcessStartInfo(screenshotPath)
                {
                    // UseShellExecute is false by default on .NET Core.
                    UseShellExecute = true
                });
            }
        }
    }
}
