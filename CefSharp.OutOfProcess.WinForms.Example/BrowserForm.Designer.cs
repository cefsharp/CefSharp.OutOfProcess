namespace CefSharp.OutOfProcess.WinForms.Example
{
    partial class BrowserForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowserForm));
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            newTabToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeTabToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            printToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            printToPdfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            takeScreenShotMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showDevToolsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeDevToolsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            undoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            redoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            findMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            cutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            selectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            copySourceToClipBoardAsyncMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            zoomLevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            zoomInToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            zoomOutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            currentZoomLevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openDataUrlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            httpbinorgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            browserTabControl = new System.Windows.Forms.TabControl();
            setMinFontSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, zoomLevelToolStripMenuItem, testToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            menuStrip1.Size = new System.Drawing.Size(852, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newTabToolStripMenuItem, closeTabToolStripMenuItem, printToolStripMenuItem, printToPdfToolStripMenuItem, aboutToolStripMenuItem, takeScreenShotMenuItem, showDevToolsMenuItem, closeDevToolsMenuItem, toolStripMenuItem3, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // newTabToolStripMenuItem
            // 
            newTabToolStripMenuItem.Name = "newTabToolStripMenuItem";
            newTabToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T;
            newTabToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            newTabToolStripMenuItem.Text = "&New Tab";
            newTabToolStripMenuItem.Click += NewTabToolStripMenuItemClick;
            // 
            // closeTabToolStripMenuItem
            // 
            closeTabToolStripMenuItem.Name = "closeTabToolStripMenuItem";
            closeTabToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W;
            closeTabToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            closeTabToolStripMenuItem.Text = "&Close Tab";
            closeTabToolStripMenuItem.Click += CloseTabToolStripMenuItemClick;
            // 
            // printToolStripMenuItem
            // 
            printToolStripMenuItem.Name = "printToolStripMenuItem";
            printToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            printToolStripMenuItem.Text = "&Print";
            printToolStripMenuItem.Click += PrintToolStripMenuItemClick;
            // 
            // printToPdfToolStripMenuItem
            // 
            printToPdfToolStripMenuItem.Name = "printToPdfToolStripMenuItem";
            printToPdfToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            printToPdfToolStripMenuItem.Text = "Print To Pdf";
            printToPdfToolStripMenuItem.Click += PrintToPdfToolStripMenuItemClick;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += AboutToolStripMenuItemClick;
            // 
            // takeScreenShotMenuItem
            // 
            takeScreenShotMenuItem.Name = "takeScreenShotMenuItem";
            takeScreenShotMenuItem.Size = new System.Drawing.Size(205, 22);
            takeScreenShotMenuItem.Text = "Take Screenshot";
            takeScreenShotMenuItem.Click += TakeScreenShotMenuItemClick;
            // 
            // showDevToolsMenuItem
            // 
            showDevToolsMenuItem.Name = "showDevToolsMenuItem";
            showDevToolsMenuItem.Size = new System.Drawing.Size(205, 22);
            showDevToolsMenuItem.Text = "Show Dev Tools (Default)";
            showDevToolsMenuItem.Click += ShowDevToolsMenuItemClick;
            // 
            // closeDevToolsMenuItem
            // 
            closeDevToolsMenuItem.Name = "closeDevToolsMenuItem";
            closeDevToolsMenuItem.Size = new System.Drawing.Size(205, 22);
            closeDevToolsMenuItem.Text = "Close Dev Tools";
            closeDevToolsMenuItem.Click += CloseDevToolsMenuItemClick;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new System.Drawing.Size(202, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += ExitMenuItemClick;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { undoMenuItem, redoMenuItem, findMenuItem, toolStripMenuItem2, cutMenuItem, copyMenuItem, pasteMenuItem, deleteMenuItem, selectAllMenuItem, toolStripSeparator1, copySourceToClipBoardAsyncMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            editToolStripMenuItem.Text = "Edit";
            // 
            // undoMenuItem
            // 
            undoMenuItem.Name = "undoMenuItem";
            undoMenuItem.Size = new System.Drawing.Size(251, 22);
            undoMenuItem.Text = "Undo";
            undoMenuItem.Click += UndoMenuItemClick;
            // 
            // redoMenuItem
            // 
            redoMenuItem.Name = "redoMenuItem";
            redoMenuItem.Size = new System.Drawing.Size(251, 22);
            redoMenuItem.Text = "Redo";
            redoMenuItem.Click += RedoMenuItemClick;
            // 
            // findMenuItem
            // 
            findMenuItem.Name = "findMenuItem";
            findMenuItem.Size = new System.Drawing.Size(251, 22);
            findMenuItem.Text = "Find";
            findMenuItem.Click += FindMenuItemClick;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(248, 6);
            // 
            // cutMenuItem
            // 
            cutMenuItem.Name = "cutMenuItem";
            cutMenuItem.Size = new System.Drawing.Size(251, 22);
            cutMenuItem.Text = "Cut";
            cutMenuItem.Click += CutMenuItemClick;
            // 
            // copyMenuItem
            // 
            copyMenuItem.Name = "copyMenuItem";
            copyMenuItem.Size = new System.Drawing.Size(251, 22);
            copyMenuItem.Text = "Copy";
            copyMenuItem.Click += CopyMenuItemClick;
            // 
            // pasteMenuItem
            // 
            pasteMenuItem.Name = "pasteMenuItem";
            pasteMenuItem.Size = new System.Drawing.Size(251, 22);
            pasteMenuItem.Text = "Paste";
            pasteMenuItem.Click += PasteMenuItemClick;
            // 
            // deleteMenuItem
            // 
            deleteMenuItem.Name = "deleteMenuItem";
            deleteMenuItem.Size = new System.Drawing.Size(251, 22);
            deleteMenuItem.Text = "Delete";
            deleteMenuItem.Click += DeleteMenuItemClick;
            // 
            // selectAllMenuItem
            // 
            selectAllMenuItem.Name = "selectAllMenuItem";
            selectAllMenuItem.Size = new System.Drawing.Size(251, 22);
            selectAllMenuItem.Text = "Select All";
            selectAllMenuItem.Click += SelectAllMenuItemClick;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(248, 6);
            // 
            // copySourceToClipBoardAsyncMenuItem
            // 
            copySourceToClipBoardAsyncMenuItem.Name = "copySourceToClipBoardAsyncMenuItem";
            copySourceToClipBoardAsyncMenuItem.Size = new System.Drawing.Size(251, 22);
            copySourceToClipBoardAsyncMenuItem.Text = "Copy Source to Clipboard (async)";
            copySourceToClipBoardAsyncMenuItem.Click += CopySourceToClipBoardAsyncClick;
            // 
            // zoomLevelToolStripMenuItem
            // 
            zoomLevelToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { zoomInToolStripMenuItem, zoomOutToolStripMenuItem, currentZoomLevelToolStripMenuItem });
            zoomLevelToolStripMenuItem.Name = "zoomLevelToolStripMenuItem";
            zoomLevelToolStripMenuItem.Size = new System.Drawing.Size(81, 20);
            zoomLevelToolStripMenuItem.Text = "Zoom Level";
            // 
            // zoomInToolStripMenuItem
            // 
            zoomInToolStripMenuItem.Name = "zoomInToolStripMenuItem";
            zoomInToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            zoomInToolStripMenuItem.Text = "Zoom In";
            zoomInToolStripMenuItem.Click += ZoomInToolStripMenuItemClick;
            // 
            // zoomOutToolStripMenuItem
            // 
            zoomOutToolStripMenuItem.Name = "zoomOutToolStripMenuItem";
            zoomOutToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            zoomOutToolStripMenuItem.Text = "Zoom Out";
            zoomOutToolStripMenuItem.Click += ZoomOutToolStripMenuItemClick;
            // 
            // currentZoomLevelToolStripMenuItem
            // 
            currentZoomLevelToolStripMenuItem.Name = "currentZoomLevelToolStripMenuItem";
            currentZoomLevelToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            currentZoomLevelToolStripMenuItem.Text = "Current Zoom Level";
            currentZoomLevelToolStripMenuItem.Click += CurrentZoomLevelToolStripMenuItemClick;
            // 
            // testToolStripMenuItem
            // 
            testToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openDataUrlToolStripMenuItem, httpbinorgToolStripMenuItem, setMinFontSizeToolStripMenuItem });
            testToolStripMenuItem.Name = "testToolStripMenuItem";
            testToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            testToolStripMenuItem.Text = "Test";
            // 
            // openDataUrlToolStripMenuItem
            // 
            openDataUrlToolStripMenuItem.Name = "openDataUrlToolStripMenuItem";
            openDataUrlToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            openDataUrlToolStripMenuItem.Text = "Open Data Url";
            openDataUrlToolStripMenuItem.Click += OpenDataUrlToolStripMenuItemClick;
            // 
            // httpbinorgToolStripMenuItem
            // 
            httpbinorgToolStripMenuItem.Name = "httpbinorgToolStripMenuItem";
            httpbinorgToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            httpbinorgToolStripMenuItem.Text = "httpbin.org";
            httpbinorgToolStripMenuItem.Click += OpenHttpBinOrgToolStripMenuItemClick;
            // 
            // browserTabControl
            // 
            browserTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            browserTabControl.Location = new System.Drawing.Point(0, 24);
            browserTabControl.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            browserTabControl.Name = "browserTabControl";
            browserTabControl.SelectedIndex = 0;
            browserTabControl.Size = new System.Drawing.Size(852, 541);
            browserTabControl.TabIndex = 2;
            // 
            // setMinFontSizeToolStripMenuItem
            // 
            setMinFontSizeToolStripMenuItem.Name = "setMinFontSizeToolStripMenuItem";
            setMinFontSizeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            setMinFontSizeToolStripMenuItem.Text = "Set Min Font Size";
            setMinFontSizeToolStripMenuItem.Click += SetMinFontSizeToolStripMenuItemClick;
            // 
            // BrowserForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(852, 565);
            Controls.Add(browserTabControl);
            Controls.Add(menuStrip1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "BrowserForm";
            Text = "BrowserForm";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectAllMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem showDevToolsMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem findMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem copySourceToClipBoardAsyncMenuItem;
        private System.Windows.Forms.TabControl browserTabControl;
        private System.Windows.Forms.ToolStripMenuItem newTabToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeTabToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeDevToolsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomLevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomInToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomOutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem currentZoomLevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem printToPdfToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDataUrlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem httpbinorgToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem takeScreenShotMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setMinFontSizeToolStripMenuItem;
    }
}
