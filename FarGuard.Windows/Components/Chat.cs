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
        public Chat()
        {
            InitializeComponent();
        }

        public void AddMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            this.textBox1.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
            this.textBox1.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageSent?.Invoke(this.textBox1.Text);
        }
    }
}
