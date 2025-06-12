using FarGuard.Core.Networking;
using FarGuard.Windows.Components;
using System.ComponentModel;
using System.Text;

namespace FarGuard.Windows
{
    public partial class Form1 : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static PeerDiscoveryService PeerDiscoveryService { get; private set; } = new();
        private ICollection<PeerInfo> _peers = [];
        private CancellationTokenSource _cancellationTokenSource = new();
        private Chat Chat = new();
        public Form1()
        {
            InitializeComponent();
            PeerDiscoveryService.PeerDiscovered += PeerDiscovered;
            PeerDiscoveryService.MessageReceived += MessageReceived;
            PeerDiscoveryService.PeerConnected += PeerConnected;
            PeerDiscoveryService.Start();
            networkScan.CreateChat += CreateChat;
            networkScan.UserNameChange += ChangeUserName;
            networkScan.PeerSelected += PeerSelected;
            Chat.MessageSent += MessageSent;
        }

        private void PeerConnected()
        {
            MessageBox.Show("Connected to peer!", "Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Invoke(new Action(() =>
            {
                this.Controls.Remove(networkScan);
                this.Controls.Add(Chat);
                Chat.Focus();
            }));
        }

        private void PeerSelected(PeerInfo info)
        {
            if (info is null) return;
            PeerDiscoveryService.PeerInfo = info;
            PeerDiscoveryService.ConnectToPeer(info).Wait();
            CreateChat();
        }

        private void MessageReceived(string obj)
        {
            Chat.AddMessage(obj);
        }

        private void CreateChat()
        {
            this.Controls.Remove(networkScan);
            this.Controls.Add(Chat);
        }

        private void MessageSent(string obj)
        {
            PeerDiscoveryService.SendMessageAsync(Encoding.UTF8.GetBytes(obj), _cancellationTokenSource.Token).Wait();
        }

        private void ChangeUserName()
        {
            var username = new UserName();
            username.Show();
            username.UserNameChanged += PeerDiscoveryService.SetUserName;
        }

        private void PeerDiscovered(PeerInfo info)
        {
            if (this._peers.Any(x => x.Id == info.Id))
            {
                var existingPeer = this._peers.FirstOrDefault(x => x.Id == info.Id);
                if (existingPeer is null) return;
                existingPeer.LastSeen = info.LastSeen;
                return;
            }
            this._peers.Add(info);
            this.networkScan.UpdateList(_peers);
        }
    }
}
