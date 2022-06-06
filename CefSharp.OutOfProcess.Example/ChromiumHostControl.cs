using System;
using System.Windows.Forms;

namespace CefSharp.OutOfProcess.Example
{
    public class ChromiumHostControl : Control
    {
        public ChromiumHostControl()
        {
            Dock = DockStyle.Fill;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
        }
    }
}
