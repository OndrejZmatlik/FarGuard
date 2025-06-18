using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FarGuard.Windows.Components
{
    public partial class Chat : UserControl
    {
        public event Action<string>? MessageSent;
        public event Action? UserNameChanged;
        public Chat()
        {
            InitializeComponent();
        }

        public void AddMessage(string message, string username, DateTime time)
        {
            this.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                this.listBox1.Items.Add($"{time:HH:mm:ss} - {username}: {message}");
                this.listBox1.SelectedIndex = this.listBox1.Items.Count - 1;
            });
        }

        public void ClearInput()
        {
            this.Invoke(() =>
            {
                this.textBox1.Clear();
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageSent?.Invoke(this.textBox1.Text);
        }

        public void SetTitle(string title)
        {
            this.Invoke(() =>
            {
                this.infoText_lbl.Text = title;
            });
        }

        private void Chat_Load(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void userName_btn_Click(object sender, EventArgs e)
        {
            UserNameChanged?.Invoke();
        }
    }
}