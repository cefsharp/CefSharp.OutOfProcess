using CefSharp.OutOfProcess;
using System;
using System.Windows.Forms;

namespace CefSharp.OutOfProcess.WinForms.Example
{
    public partial class HostForm : Form
    {
        private OutOfProcessHost _outOfProcessHost;

        public HostForm()
        {
            InitializeComponent();

            Load += HostFormOnLoad;
            Resize += HostFormOResize;
        }

        private void HostFormOResize(object sender, EventArgs e)
        {
            var hostControl = splitContainer.Panel2.Controls[0];

            hostControl.Size = splitContainer.Panel2.ClientSize;
        }

        private async void HostFormOnLoad(object sender, EventArgs e)
        {
            _outOfProcessHost = await OutOfProcessHost.CreateAsync();

            var browser = new ChromiumWebBrowser(_outOfProcessHost, "https://github.com");
            browser.Dock = DockStyle.Fill;

            splitContainer.Panel2.Controls.Add(browser);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }

        private void CloseBrowserToolStripMenuItemClick(object sender, EventArgs e)
        {
            var ctrl = splitContainer.Panel2.Controls[0];

            splitContainer.Panel2.Controls.Remove(ctrl);

            ctrl.Dispose();
        }
    }
}
