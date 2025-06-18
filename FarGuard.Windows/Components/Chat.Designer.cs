namespace FarGuard.Windows.Components
{
    partial class Chat
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
            listBox1 = new ListBox();
            send_btn = new Button();
            userName_btn = new Button();
            textBox1 = new TextBox();
            infoText_lbl = new Label();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(21, 40);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(523, 384);
            listBox1.TabIndex = 0;
            // 
            // send_btn
            // 
            send_btn.Location = new Point(450, 428);
            send_btn.Name = "send_btn";
            send_btn.Size = new Size(94, 29);
            send_btn.TabIndex = 1;
            send_btn.Text = "Send";
            send_btn.UseVisualStyleBackColor = true;
            send_btn.Click += button1_Click;
            // 
            // userName_btn
            // 
            userName_btn.Location = new Point(383, 5);
            userName_btn.Name = "userName_btn";
            userName_btn.Size = new Size(161, 29);
            userName_btn.TabIndex = 2;
            userName_btn.Text = "Change username";
            userName_btn.UseVisualStyleBackColor = true;
            userName_btn.Click += userName_btn_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(21, 430);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(422, 27);
            textBox1.TabIndex = 3;
            // 
            // infoText_lbl
            // 
            infoText_lbl.AutoSize = true;
            infoText_lbl.Location = new Point(21, 14);
            infoText_lbl.Name = "infoText_lbl";
            infoText_lbl.Size = new Size(35, 20);
            infoText_lbl.TabIndex = 4;
            infoText_lbl.Text = "Info";
            // 
            // panel1
            // 
            panel1.Controls.Add(infoText_lbl);
            panel1.Controls.Add(send_btn);
            panel1.Controls.Add(textBox1);
            panel1.Controls.Add(userName_btn);
            panel1.Controls.Add(listBox1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(570, 497);
            panel1.TabIndex = 5;
            // 
            // Chat
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel1);
            Name = "Chat";
            Size = new Size(570, 497);
            Load += Chat_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ListBox listBox1;
        private Button send_btn;
        private Button userName_btn;
        private TextBox textBox1;
        private Label infoText_lbl;
        private Panel panel1;
    }
}
