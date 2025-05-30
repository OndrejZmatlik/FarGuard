using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class PeerDiscoveryService : IDisposable
{
    private readonly UdpClient _udpClient = new();
    private readonly CancellationTokenSource _cts = new();
    private LocalPeerIdentity? _identity;
    private TcpListener? _tcpListener;

    private const int DiscoveryPort = 49999;
    private readonly int TcpPortRangeStart = 40000;
    private readonly int TcpPortRangeCount = 100;

    public event Action<PeerInfo>? PeerDiscovered;

    public void SetIdentity(LocalPeerIdentity identity)
    {
        _identity = identity;
    }

    public void Start()
    {
        if (_identity is null) return;
        var tcpPort = TcpPortRangeStart + RandomNumberGenerator.GetInt32(TcpPortRangeCount);
        _identity.ListeningPort = tcpPort;

        _tcpListener = new TcpListener(IPAddress.Any, tcpPort);
        _tcpListener.Start();

        _ = Task.Run(() => AcceptTcpClientsAsync(_tcpListener, _cts.Token));
        _ = Task.Run(() => BroadcastPresenceAsync(_cts.Token));
        _ = Task.Run(() => ListenForPeersAsync(_cts.Token));
    }

    private async Task AcceptTcpClientsAsync(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(token);
        }
    }

    private async Task BroadcastPresenceAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_identity is null)
                return;

            var message = new
            {
                _identity.Id,
                _identity.Username,
                IpAddress = GetLocalIpAddress(),
                TcpPort = _identity.ListeningPort
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(message);

            await _udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
            await Task.Delay(3000, token);
        }
    }

    private async Task ListenForPeersAsync(CancellationToken token)
    {
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

        while (!token.IsCancellationRequested)
        {
            var result = await _udpClient.ReceiveAsync(token);
            var peerMessage = JsonSerializer.Deserialize<PeerBroadcastMessage>(result.Buffer);
            if (peerMessage is null)
                continue;

            var peerInfo = new PeerInfo
            {
                Id = peerMessage.Id,
                Username = peerMessage.Username,
                IPAddress = peerMessage.IPAddress,
                Port = peerMessage.TcpPort,
                LastSeen = DateTime.UtcNow,
            };

            PeerDiscovered?.Invoke(peerInfo);
        }
    }

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
    }

    public void Dispose()
    {
        _cts.Cancel();
        _tcpListener?.Stop();
        _udpClient.Dispose();
    }

    private record PeerBroadcastMessage(Guid Id, string Username, IPAddress IPAddress, int TcpPort);
}
