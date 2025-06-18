using FarGuard.Core.Networking;
using FarGuard.Windows.Components;
using FarGuard.Windows.Data;
using FarGuard.Windows.Data.Entities;
using FarGuard.Windows.Services;
using Microsoft.EntityFrameworkCore;
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
        private readonly IAppService appService;
        private readonly IChatService chatService;
        private readonly IClientService clientService;

        public Form1(IAppService appService, IChatService chatService, IClientService clientService)
        {
            InitializeComponent();
            PeerDiscoveryService.PeerDiscovered += PeerDiscovered;
            PeerDiscoveryService.MessageReceived += MessageReceived;
            PeerDiscoveryService.PeerConnected += PeerConnected;
            PeerDiscoveryService.PeerDisconnected += PeerDisconnected;
            networkScan.UserNameChange += ChangeUserName;
            networkScan.PeerSelected += PeerSelected;
            Chat.MessageSent += MessageSent;
            this.appService = appService;
            this.chatService = chatService;
            this.clientService = clientService;
            var app = appService.GetAppSettingsAsync().GetAwaiter().GetResult();
            if (app is null)
            {
                app = new App
                {
                    UserId = Guid.NewGuid(),
                    UserName = Environment.UserName
                };
                appService.CreateAppSettingsAsync(app).GetAwaiter().GetResult();
            }
            var localClient = clientService.GetClientAsync(app.UserId).GetAwaiter().GetResult();
            if (localClient is null)
            {
                clientService.AddClientAsync(new Client
                {
                    Id = app.UserId,
                    UserName = app.UserName
                }).GetAwaiter().GetResult();
            }
            PeerDiscoveryService.LocalIdentity.Username = app.UserName ?? Environment.UserName;
            PeerDiscoveryService.LocalIdentity.Id = app.UserId;
            PeerDiscoveryService.Start();
        }

        private void PeerDisconnected()
        {
            this.Invoke(new Action(() =>
            {
                this.Controls.Remove(Chat);
                this.Controls.Add(networkScan);
            }));
        }


        private void PeerConnected()
        {
            this.Invoke(new Action(() =>
            {
                this.Controls.Remove(networkScan);
                Chat.Dock = DockStyle.Fill;
                Chat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                Chat.Load += (_, _) =>
                {
                    Chat.SetTitle($"Listening at 0.0.0.0:{PeerDiscoveryService.LocalIdentity.ListeningPort}");
                    var client = clientService.GetClientAsync(PeerDiscoveryService.PeerInfo.Id).GetAwaiter().GetResult();
                    if (client is null)
                    {
                        clientService.AddClientAsync(new Client
                        {
                            Id = PeerDiscoveryService.PeerInfo.Id,
                            UserName = PeerDiscoveryService.PeerInfo.Username
                        }).GetAwaiter().GetResult();
                    }
                    var chatHistory = chatService.GetChatHistoryAsync(PeerDiscoveryService.PeerInfo.Id).GetAwaiter().GetResult();
                    if (chatHistory is not null && chatHistory.Any())
                    {
                        foreach (var message in chatHistory)
                        {
                            Chat.AddMessage(message.Message, message.SenderId == PeerDiscoveryService.LocalIdentity.Id ? "You" : PeerDiscoveryService.PeerInfo.Username, message.CreatedAt);
                        }
                    }
                    PeerDiscoveryService.StartPeerCheck();
                };
                Chat.UserNameChanged += ChangeUserName;
                this.Controls.Add(Chat);
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
            this.Chat.AddMessage(obj, PeerDiscoveryService.PeerInfo.Username, DateTime.Now);
            this.AddMessageToDb(obj, true).GetAwaiter().GetResult();
        }

        private async Task AddMessageToDb(string message, bool received)
        {
            var chatHistory = new ChatHistory
            {
                Message = message,
                ClientId = received ? PeerDiscoveryService.LocalIdentity.Id : PeerDiscoveryService.PeerInfo.Id,
                SenderId = received ? PeerDiscoveryService.PeerInfo.Id : PeerDiscoveryService.LocalIdentity.Id
            };
            await chatService.AddMessageAsync(chatHistory);
        }

        private void MessageSent(string obj)
        {
            PeerDiscoveryService.SendMessageAsync(Encoding.UTF8.GetBytes(obj)).Wait();
            Chat.AddMessage(obj, "You", DateTime.Now);
            Chat.ClearInput();
            this.AddMessageToDb(obj, false).GetAwaiter().GetResult();
        }

        private void ChangeUserName()
        {
            var username = new UserName();
            username.Show();
            username.SetUserName(PeerDiscoveryService.LocalIdentity.Username);
            username.UserNameChanged += (e) =>
            {
                PeerDiscoveryService.LocalIdentity.Username = e;
                var app = appService.GetAppSettingsAsync().GetAwaiter().GetResult();
                if (app is null) return;
                app.UserName = e;
                appService.UpdateAppSettingsAsync(app).GetAwaiter().GetResult();
            };
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