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

        public void AddPeerMessage(string message, string userName)
        {
            this.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                this.listBox1.Items.Add($"{DateTime.Now:HH:mm:ss} - {userName}: {message}");
                this.listBox1.SelectedIndex = this.listBox1.Items.Count - 1;
            });
        }

        public void AddMessage(string message)
        {
            this.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                this.listBox1.Items.Add($"{DateTime.Now:HH:mm:ss} - You: {message}");
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

        public void SetAeadLabel(string l)
        {
            this.Invoke(() =>
 {
     this.label1.Text = l;
 });
        }

        public void SetMyPrivatKeyLabel(string l)
        {
            this.Invoke(() =>
            {
                this.label2.Text = "MyPrivate: " + l;
            });
        }

        public void SetMyPublicKeyLabel(string l)
        {
            this.Invoke(() =>
            {
                this.label3.Text = "MyPublic: " + l;
            });
        }

        public void SetPeerPublicKeyLabel(string l)
        {
            this.Invoke(() =>
            {
                this.label4.Text = "PeerPublic: " + l;
            });
        }

        public void SetPreSharedKeyLabel(string l)
        {
            this.Invoke(() =>
            {
                this.label5.Text = "PreShared: " + l;
            });
        }
    }
}