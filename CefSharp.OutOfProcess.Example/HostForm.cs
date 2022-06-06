using System;
using System.Diagnostics;
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
            var host = new ChromiumHostControl();
            host.CreateControl();
            var hostHwnd = host.Handle;

            splitContainer.Panel2.Controls.Add(host);

            var currentProcess = Process.GetCurrentProcess();

            var args = $"--parentProcessId={currentProcess.Id} --hostHwnd={hostHwnd.ToInt32()}";

            var browserProcess = Process.Start("CefSharp.OutOfProcess.BrowserProcess.exe", args);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }
    }
}
