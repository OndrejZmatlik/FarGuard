using FarGuard.Core.Networking;
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
    public partial class NetworkScan : UserControl
    {
        public event Action<PeerInfo>? PeerSelected;
        public event Action? CreateChat;
        public event Action? UserNameChange;
        public NetworkScan()
        {
            InitializeComponent();
        }

        public void UpdateList(IEnumerable<PeerInfo> devices)
        {
            Invoke(new Action(() =>
            {
                NetworkScan_listBox.Items.Clear();
                foreach (var item in devices)
                {
                    NetworkScan_listBox.Items.Add(item);
                }
            }));
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            CreateChat?.Invoke();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UserNameChange?.Invoke();
        }

        private void NetworkScan_listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (NetworkScan_listBox.SelectedItem is PeerInfo peerInfo)
            {
                PeerSelected?.Invoke(peerInfo);
            }
        }
    }
}