using System;
using System.Windows.Forms;

namespace CefSharp.OutOfProcess.Example
{
    public partial class HostForm : Form
    {
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

        private void HostFormOnLoad(object sender, EventArgs e)
        {
            var browser = new ChromiumWebBrowser();
            browser.Dock = DockStyle.Fill;

            splitContainer.Panel2.Controls.Add(browser);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }
    }
}
