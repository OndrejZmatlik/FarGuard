namespace FarGuard.Windows
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            networkScan = new FarGuard.Windows.Components.NetworkScan();
            SuspendLayout();
            // 
            // networkScan
            // 
            networkScan.Dock = DockStyle.Fill;
            networkScan.Location = new Point(0, 0);
            networkScan.Name = "networkScan";
            networkScan.Size = new Size(572, 475);
            networkScan.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(572, 475);
            Controls.Add(networkScan);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "FarGuard";
            ResumeLayout(false);
        }

        #endregion

        private Components.NetworkScan networkScan;
    }
}
