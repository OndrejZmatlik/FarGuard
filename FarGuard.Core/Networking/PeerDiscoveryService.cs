using Blake2Fast;
using Elliptic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FarGuard.Core.Networking;

public class PeerDiscoveryService : IDisposable
{
    private readonly UdpClient _udpClient = new();
    private readonly CancellationTokenSource _cts = new();
    private LocalPeerIdentity _identity = new();
    private TcpListener? _tcpListener;

    private const int DiscoveryPort = 49999;
    private readonly int TcpPortRangeStart = 40000;
    private readonly int TcpPortRangeCount = 100;

    public event Action<PeerInfo>? PeerDiscovered;
    public event Action<string>? MessageReceived;
    public event Action? PeerConnected;

    public PeerInfo PeerInfo { get; set; } = new();

    public void SetIdentity(LocalPeerIdentity identity)
    {
        _identity = identity;
    }

    public void SetUserName(string username)
    {
        _identity.Username = username;
    }

    public void Start()
    {
        var tcpPort = TcpPortRangeStart + RandomNumberGenerator.GetInt32(TcpPortRangeCount);
        _identity = LocalPeerIdentity.Generate();
        _identity.ListeningPort = tcpPort;

        _tcpListener = new TcpListener(IPAddress.Any, tcpPort);
        _tcpListener.Start();
        _ = Task.Run(() => AcceptTcpClientsAsync(_tcpListener, _cts.Token));
        _ = Task.Run(() => BroadcastPresenceAsync(_cts.Token));
        _ = Task.Run(() => ListenForPeersAsync(_cts.Token));
    }

    private LocalPeerIdentity LocalPeerIdentity = new();
    private async Task AcceptTcpClientsAsync(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await listener.AcceptTcpClientAsync(token);
                _ = Task.Run(async () =>
                {
                    using var networkStream = tcpClient.GetStream();
                    var clientPublicKeyBuffer = new byte[32];
                    await networkStream.ReadExactlyAsync(clientPublicKeyBuffer, token);
                    var clientPublicKey = clientPublicKeyBuffer;
                    await networkStream.WriteAsync(LocalPeerIdentity.PublicKey, token);
                    var sharedSecret = Curve25519.GetSharedSecret(LocalPeerIdentity.PrivateKey, clientPublicKey);
                    var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(LocalPeerIdentity.PresharedKey).ToArray());
                    var nonce = new byte[12];
                    var cipherText = new byte[1024];
                    PeerInfo = new PeerInfo
                    {
                        Id = LocalPeerIdentity.Id,
                        Username = LocalPeerIdentity.Username,
                        IPAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint!).Address,
                        Port = LocalPeerIdentity.ListeningPort,
                        PublicKey = LocalPeerIdentity.PublicKey,
                        PresharedKey = LocalPeerIdentity.PresharedKey,
                        LastSeen = DateTime.Now
                    };
                    _ = Task.Run(() => ConnectToPeer(tcpClient));
                    PeerConnected?.Invoke();

                }, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EEEEEE: {ex.Message}");
            }
        }
    }

    public async Task SendMessageAsync(byte[] message, CancellationToken token)
    {
        if (_identity is null) return;
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(PeerInfo.IPAddress, PeerInfo.Port, token);
        using var networkStream = tcpClient.GetStream();
        var sharedSecret = Curve25519.GetSharedSecret(_identity.PrivateKey, PeerInfo.PublicKey);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(_identity.PresharedKey).ToArray());
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        await networkStream.WriteAsync(nonce, token);
        var chacha20 = new ChaCha20Poly1305(aeadKey);
        var cipherText = new byte[message.Length];
        var tag = new byte[16];
        chacha20.Encrypt(nonce, message, cipherText, tag);
        await networkStream.WriteAsync(cipherText, token);
    }

    public async Task ConnectToPeer(TcpClient tcpClient)
    {
        using var networkStream = tcpClient.GetStream();
        var clientPublicKeyBuffer = new byte[32];
        await networkStream.ReadExactlyAsync(clientPublicKeyBuffer, CancellationToken.None);
        var clientPublicKey = clientPublicKeyBuffer;
        await networkStream.WriteAsync(_identity.PublicKey, CancellationToken.None);
        var sharedSecret = Curve25519.GetSharedSecret(_identity.PrivateKey, clientPublicKey);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(_identity.PresharedKey).ToArray());
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        while (tcpClient.Connected)
        {
            try
            {
                var cipherText = new byte[1024];
                var bytesRead = await networkStream.ReadAsync(cipherText, CancellationToken.None);
                if (bytesRead == 0) break;
                var chacha20 = new ChaCha20Poly1305(aeadKey);
                var message = new byte[bytesRead - 16];
                var tag = cipherText.Skip(bytesRead - 16).Take(16).ToArray();
                chacha20.Decrypt(nonce, cipherText.Take(bytesRead - 16).ToArray(), message, tag);
                MessageReceived?.Invoke(Encoding.UTF8.GetString(message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in peer connection: {ex.Message}");
                break;
            }
        }
    }
    public async Task ConnectToPeer(PeerInfo peerInfo)
    {
        if (peerInfo is null) return;
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(peerInfo.IPAddress, peerInfo.Port);
        using var networkStream = tcpClient.GetStream();
        var sharedSecret = Curve25519.GetSharedSecret(_identity.PrivateKey, peerInfo.PublicKey);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(_identity.PresharedKey).ToArray());
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        await networkStream.WriteAsync(nonce, CancellationToken.None);
        var cipherText = new byte[1024];
        var tag = new byte[16];
        PeerInfo = peerInfo;
        _ = Task.Run(() => ConnectToPeer(tcpClient), _cts.Token);
    }
    private async Task BroadcastPresenceAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var message = new PeerBroadcastMessage(
                Id: _identity.Id,
                Username: _identity.Username,
                IPAddress: GetLocalIpAddress(),
                TcpPort: _identity.ListeningPort,
                publicKey: _identity.PublicKey
            );

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

            var ip = IPAddress.TryParse(peerMessage.IPAddress, out var parsedIp) ? parsedIp : IPAddress.None;
            var peerInfo = new PeerInfo
            {
                Id = peerMessage.Id,
                Username = peerMessage.Username,
                IPAddress = ip,
                Port = peerMessage.TcpPort,
                LastSeen = DateTime.Now,
                PublicKey = peerMessage.publicKey,
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

    private record PeerBroadcastMessage(Guid Id, string Username, string IPAddress, int TcpPort, byte[] publicKey);
}
