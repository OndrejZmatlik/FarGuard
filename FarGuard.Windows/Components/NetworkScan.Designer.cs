namespace FarGuard.Windows.Components
{
    partial class NetworkScan
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            NetworkScan_lbl = new Label();
            NetworkScan_listBox = new ListBox();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(NetworkScan_lbl, 0, 0);
            tableLayoutPanel1.Controls.Add(NetworkScan_listBox, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(510, 427);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // NetworkScan_lbl
            // 
            NetworkScan_lbl.AutoSize = true;
            NetworkScan_lbl.Dock = DockStyle.Fill;
            NetworkScan_lbl.Location = new Point(3, 0);
            NetworkScan_lbl.Name = "NetworkScan_lbl";
            NetworkScan_lbl.Size = new Size(504, 213);
            NetworkScan_lbl.TabIndex = 0;
            NetworkScan_lbl.Text = "Device in network";
            NetworkScan_lbl.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // NetworkScan_listBox
            // 
            NetworkScan_listBox.Dock = DockStyle.Fill;
            NetworkScan_listBox.FormattingEnabled = true;
            NetworkScan_listBox.Location = new Point(3, 216);
            NetworkScan_listBox.Name = "NetworkScan_listBox";
            NetworkScan_listBox.Size = new Size(504, 208);
            NetworkScan_listBox.TabIndex = 1;
            // 
            // NetworkScan
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel1);
            Name = "NetworkScan";
            Size = new Size(510, 427);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private Label NetworkScan_lbl;
        private ListBox NetworkScan_listBox;
    }
}
