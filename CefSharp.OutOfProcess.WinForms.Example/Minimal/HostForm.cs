using System;
using System.IO;
using System.Windows.Forms;

namespace CefSharp.OutOfProcess.WinForms.Example
{
    public partial class HostForm : Form
    {
#if DEBUG
        private string _buildType = "Debug";
#else
        private string _buildType = "Release";
#endif
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
            var outOfProcessHostPath = Path.GetFullPath($"..\\..\\..\\..\\..\\CefSharp.OutOfProcess.BrowserProcess\\bin\\{_buildType}");
            outOfProcessHostPath = Path.Combine(outOfProcessHostPath, OutOfProcessHost.HostExeName);
            _outOfProcessHost = await OutOfProcessHost.CreateAsync(outOfProcessHostPath);

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
