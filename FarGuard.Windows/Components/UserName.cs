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
    public partial class UserName : Form
    {
        public event Action<string>? UserNameChanged;
        public UserName()
        {
            InitializeComponent();
            if (string.IsNullOrWhiteSpace(this.textBox1.Text))
            {
                this.textBox1.Text = Environment.UserName;
            }
        }

        public void SetUserName(string userName)
        {
            this.Invoke(() =>
            {
                this.textBox1.Text = userName;
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Username cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            this.UserNameChanged?.Invoke(textBox1.Text);
            this.Close();
        }
    }
}
