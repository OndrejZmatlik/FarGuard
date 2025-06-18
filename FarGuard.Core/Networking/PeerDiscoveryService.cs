using Blake2Fast;
using Elliptic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FarGuard.Core.Networking;

public class PeerDiscoveryService : IDisposable
{
    private readonly UdpClient _udpClient = new();
    private CancellationTokenSource _token = new();
    public LocalPeerIdentity LocalIdentity = new();
    private TcpListener? _tcpListener;
    private TcpClient _tcpClient = new();
    private NetworkStream? _networkStream;
    public event Action<PeerInfo>? PeerDiscovered;
    public event Action<string>? MessageReceived;
    public event Action? PeerConnected;
    public event Action<PeerInfo>? PeerRequestReceived;
    public event Action? PeerDisconnected;
    public PeerInfo PeerInfo { get; set; } = new();
    private CancellationTokenSource ReceiveMessageToken = new();
    private CancellationTokenSource AliveToken = new();
    public void Start()
    {
        LocalIdentity = LocalIdentity.Generate();
        _tcpListener = new TcpListener(IPAddress.Any, LocalIdentity.Port);
        _tcpListener.Start();
        StartTcpListener();
        StartBroadcastServices();

        this.PeerConnected += () =>
        {
            ReceiveMessageToken = new();
            AliveToken = new();
            _token.Cancel();
            _ = Task.Run(() => ReceiveMessageAsync(ReceiveMessageToken.Token));
            StartPeerCheck();
        };

        this.PeerDisconnected += () =>
        {
            ReceiveMessageToken.Cancel();
            _token = new();
            AliveToken.Cancel();
            StartTcpListener();
            StartBroadcastServices();
        };
    }

    private void StartTcpListener()
    {
        if (_tcpListener is null) return;
        _ = Task.Run(() => AcceptTcpClientAsync(_tcpListener));
    }

    private void StartBroadcastServices()
    {
        _ = Task.Run(() => ListenForPeersAsync(_token.Token));
        _ = Task.Run(() => BroadcastPresenceAsync(_token.Token));
    }

    public void StartPeerCheck()
    {
        _ = Task.Run(() => CheckPeerLive(AliveToken.Token));
    }

    private void CheckPeerLive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_tcpClient.Connected) continue;
            PeerDisconnected?.Invoke();
            return;
        }
    }

    public async Task RequestConnectionToPeerAsync(PeerInfo peerInfo)
    {
        if (peerInfo is null) return;

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(peerInfo.IPAddress, peerInfo.Port);
        _networkStream = _tcpClient.GetStream();

        var requestMessage = new PeerRequestMessage(LocalIdentity.Id, LocalIdentity.Username, LocalIdentity.PublicKey, LocalIdentity.PresharedKey);
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(requestMessage).Concat("\n"u8.ToArray()).ToArray();

        ushort dataLength = (ushort)requestBytes.Length;
        var lengthBytes = BitConverter.GetBytes(dataLength);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);

        await _networkStream.WriteAsync(lengthBytes.AsMemory(0, 2));
        await _networkStream.WriteAsync(requestBytes);
        await _networkStream.FlushAsync();
        var peerPublicKeyBuffer = new byte[32];
        await _networkStream.ReadExactlyAsync(peerPublicKeyBuffer);

        var sharedSecret = Curve25519.GetSharedSecret(LocalIdentity.PrivateKey, peerPublicKeyBuffer);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(LocalIdentity.PresharedKey).ToArray());

        PeerInfo.IPAddress = peerInfo.IPAddress;
        PeerInfo.Port = peerInfo.Port;
        PeerInfo.Username = peerInfo.Username;
        PeerInfo.Id = peerInfo.Id;
        PeerInfo.PublicKey = peerPublicKeyBuffer;
        PeerInfo.PresharedKey = LocalIdentity.PresharedKey;
        PeerInfo.LastSeen = DateTime.Now;

        PeerConnected?.Invoke();
    }



    private async Task AcceptTcpClientAsync(TcpListener listener)
    {
        _tcpClient = await listener.AcceptTcpClientAsync();
        _networkStream = _tcpClient.GetStream();

        var lengthBuffer = new byte[2];
        await _networkStream.ReadExactlyAsync(lengthBuffer);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBuffer);
        ushort length = BitConverter.ToUInt16(lengthBuffer);

        var dataBuffer = new byte[length];
        await _networkStream.ReadExactlyAsync(dataBuffer);

        var result = JsonSerializer.Deserialize<PeerRequestMessage>(dataBuffer);
        if (result is null) return;

        await _networkStream.WriteAsync(LocalIdentity.PublicKey);
        await _networkStream.FlushAsync();
        var sharedSecret = Curve25519.GetSharedSecret(LocalIdentity.PrivateKey, result.PublicKey);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(LocalIdentity.PresharedKey).ToArray());

        PeerInfo = new PeerInfo
        {
            Id = result.Id,
            Username = result.Username,
            IPAddress = ((IPEndPoint)_tcpClient.Client.RemoteEndPoint!).Address,
            Port = LocalIdentity.Port,
            PublicKey = result.PublicKey,
            PresharedKey = result.PresharedKey,
            LastSeen = DateTime.Now
        };
        LocalIdentity.PresharedKey = result.PresharedKey;

        PeerConnected?.Invoke();
    }

    public async Task SendMessageAsync(byte[] message)
    {
        if (LocalIdentity is null || PeerInfo is null) return;

        if (_tcpClient is null || !_tcpClient.Connected || _networkStream is null || !_networkStream.CanWrite)
        {
            PeerDisconnected?.Invoke();
            return;
        }

        try
        {
            await this.SendEncryptedDataAsync(message);
        }
        catch
        {
            _tcpClient?.Dispose();
            _networkStream = null;
            PeerDisconnected?.Invoke();
        }
    }

    public void Disconnect()
    {
        _tcpClient.Close();
        if (_tcpListener is not null && _tcpListener.Server.IsBound)
            _tcpListener.Stop();
        _networkStream?.Dispose();
        _networkStream = null;
        PeerDisconnected?.Invoke();
    }

    private async Task SendEncryptedDataAsync(byte[] data)
    {
        if (_networkStream is null || !_networkStream.CanWrite || _tcpClient is null || !_tcpClient.Connected)
        {
            PeerDisconnected?.Invoke();
            return;
        }
        var sharedSecret = Curve25519.GetSharedSecret(LocalIdentity.PrivateKey, PeerInfo.PublicKey);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(LocalIdentity.PresharedKey).ToArray());

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var chacha20 = new ChaCha20Poly1305(aeadKey);
        var cipherText = new byte[data.Length];
        var tag = new byte[16];

        chacha20.Encrypt(nonce, data, cipherText, tag);

        ushort totalLength = (ushort)(cipherText.Length + tag.Length);

        var lengthBytes = BitConverter.GetBytes(totalLength);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);

        await _networkStream.WriteAsync(lengthBytes.AsMemory(0, 2));
        await _networkStream.WriteAsync(nonce);
        await _networkStream.WriteAsync(tag);
        await _networkStream.WriteAsync(cipherText);
        await _networkStream.FlushAsync();
    }

    public async Task ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_tcpClient is null || !_tcpClient.Connected || _networkStream is null)
            {
                PeerDisconnected?.Invoke();
                await Task.Delay(100, cancellationToken);
                continue;
            }

            try
            {
                var lengthBytes = new byte[2];
                await ReadFullAsync(_networkStream, lengthBytes, 2);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                ushort messageLength = BitConverter.ToUInt16(lengthBytes);

                if (messageLength < 16)
                {
                    PeerDisconnected?.Invoke();
                    continue;
                }

                var nonce = new byte[12];
                await ReadFullAsync(_networkStream, nonce, 12);

                var tag = new byte[16];
                await ReadFullAsync(_networkStream, tag, 16);

                var cipherTextLength = messageLength - 16;
                var cipherText = new byte[cipherTextLength];

                await ReadFullAsync(_networkStream, cipherText, cipherTextLength);

                var sharedSecret = Curve25519.GetSharedSecret(LocalIdentity.PrivateKey, PeerInfo.PublicKey);
                var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(LocalIdentity.PresharedKey).ToArray());

                var chacha20 = new ChaCha20Poly1305(aeadKey);

                var plainText = new byte[cipherTextLength];

                chacha20.Decrypt(nonce, cipherText, tag, plainText);

                if (plainText.Length == 0) continue;

                var messageString = Encoding.UTF8.GetString(plainText);

                MessageReceived?.Invoke(messageString);
            }
            catch
            {
                PeerDisconnected?.Invoke();
                await Task.Delay(100, cancellationToken);
                continue;
            }
        }
    }




    private async Task ReadFullAsync(NetworkStream stream, byte[] buffer, int totalBytes)
    {
        int offset = 0;
        while (offset < totalBytes)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(offset, totalBytes - offset));
            if (bytesRead == 0)
            {
                PeerDisconnected?.Invoke();
            }
            offset += bytesRead;
        }
    }

    private async Task BroadcastPresenceAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var message = new PeerBroadcastMessage(
                Id: LocalIdentity.Id,
                Username: LocalIdentity.Username,
                TcpPort: LocalIdentity.Port
            );

            var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
            var broadcastAddresses = GetLanBroadcastAddresses();

            foreach (var broadcast in broadcastAddresses)
            {
                await _udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(broadcast, StaticData.DiscoveryPort));
            }

            await Task.Delay(3000, token);
        }
    }

    private async Task ListenForPeersAsync(CancellationToken token)
    {
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, StaticData.DiscoveryPort));

        while (!token.IsCancellationRequested)
        {
            var result = await _udpClient.ReceiveAsync(token);
            var peerMessage = JsonSerializer.Deserialize<PeerBroadcastMessage>(result.Buffer);
            if (peerMessage is null)
                continue;
            if (peerMessage.Id == LocalIdentity.Id)
                continue;
            var peerInfo = new PeerInfo
            {
                Id = peerMessage.Id,
                Username = peerMessage.Username,
                IPAddress = result.RemoteEndPoint.Address,
                Port = peerMessage.TcpPort,
                LastSeen = DateTime.Now,
            };

            PeerDiscovered?.Invoke(peerInfo);
        }
    }

    private static IEnumerable<IPAddress> GetLanBroadcastAddresses()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            var ipProps = ni.GetIPProperties();
            foreach (var ua in ipProps.UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                var ipBytes = ua.Address.GetAddressBytes();
                var maskBytes = ua.IPv4Mask.GetAddressBytes();
                var broadcastBytes = new byte[4];

                for (int i = 0; i < 4; i++)
                    broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

                yield return new IPAddress(broadcastBytes);
            }
        }
    }
    public void Dispose()
    {
        _token.Cancel();
        _tcpListener?.Stop();
        _udpClient.Dispose();
        _tcpClient.Dispose();
        _networkStream?.Dispose();
        AliveToken.Cancel();
        ReceiveMessageToken.Cancel();
    }

    private record PeerBroadcastMessage(Guid Id, string Username, int TcpPort);
    private record PeerRequestMessage(Guid Id, string Username, byte[] PublicKey, byte[] PresharedKey);
}
