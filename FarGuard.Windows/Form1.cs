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
        private Chat Chat = new();
        public Form1()
        {
            InitializeComponent();
            PeerDiscoveryService.PeerDiscovered += PeerDiscovered;
            PeerDiscoveryService.MessageReceived += MessageReceived;
            PeerDiscoveryService.PeerConnected += PeerConnected;
            PeerDiscoveryService.PeerRequestReceived += PeerRequestReceived;
            PeerDiscoveryService.PeerDisconnected += PeerDisconnected;
            PeerDiscoveryService.Start();
            networkScan.CreateChat += CreateChat;
            networkScan.UserNameChange += ChangeUserName;
            networkScan.PeerSelected += PeerSelected;
            Chat.MessageSent += MessageSent;
            
        }

        private void PeerDisconnected()
        {
            this.Invoke(new Action(() =>
            {
                this.Controls.Remove(Chat);
                this.Controls.Add(networkScan);
                networkScan.Focus();
            }));
        }

        private void PeerRequestReceived(PeerInfo info)
        {
            this.Invoke(new Action(() =>
            {
                var result = MessageBox.Show($"Connection request from {info.Username}. Do you want to accept?", "Connection Request", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    //PeerDiscoveryService.AcceptConnectionRequest(info);
                    //CreateChat();
                }
                else
                {
                    //PeerDiscoveryService.RejectConnectionRequest(info);
                }
            }));
        }

        private void PeerConnected()
        {
            this.Invoke(new Action(() =>
            {
                this.Controls.Remove(networkScan);
                this.Controls.Add(Chat);
                Chat.Focus();
                Chat.Dock = DockStyle.Fill;
                Chat.SetAeadLabel(Convert.ToBase64String(PeerDiscoveryService.PeerInfo.AeadKey));
                Chat.SetMyPrivatKeyLabel(Convert.ToBase64String(PeerDiscoveryService._identity.PrivateKey));
                Chat.SetMyPublicKeyLabel(Convert.ToBase64String(PeerDiscoveryService._identity.PublicKey));
                Chat.SetPeerPublicKeyLabel(Convert.ToBase64String(PeerDiscoveryService.PeerInfo.PublicKey));
                Chat.SetPreSharedKeyLabel(Convert.ToBase64String(PeerDiscoveryService._identity.PresharedKey));
            }));
        }

        private void PeerSelected(PeerInfo info)
        {
            if (info is null) return;
            PeerDiscoveryService.PeerInfo = info;
            _ = Task.Run(() => PeerDiscoveryService.RequestConnectionToPeerAsync(info));
        }

        private void MessageReceived(string obj)
        {
            Chat.AddPeerMessage(obj, PeerDiscoveryService.PeerInfo.Username);
        }

        private void CreateChat()
        {
            this.Controls.Remove(networkScan);
            this.Controls.Add(Chat);
        }

        private void MessageSent(string obj)
        {
            PeerDiscoveryService.SendMessageAsync(Encoding.UTF8.GetBytes(obj)).Wait();
            Chat.AddMessage(obj);
            Chat.ClearInput();
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
