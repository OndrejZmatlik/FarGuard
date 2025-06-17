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
    private readonly CancellationTokenSource _token = new();
    public LocalPeerIdentity _identity = new();
    private TcpListener? _tcpListener;
    private TcpClient _tcpClient = new();
    private NetworkStream? _networkStream;
    private const int _discoveryPort = 49999;
    public event Action<PeerInfo>? PeerDiscovered;
    public event Action<string>? MessageReceived;
    public event Action? PeerConnected;
    public event Action<PeerInfo>? PeerRequestReceived;
    public event Action? PeerDisconnected;
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
        _identity = LocalPeerIdentity.Generate();
        _tcpListener = new TcpListener(IPAddress.Any, _identity.ListeningPort);
        _tcpListener.Start();
        _ = Task.Run(() => AcceptTcpClientAsync(_tcpListener));
        _ = Task.Run(() => ListenForPeersAsync(_token.Token));
        _ = Task.Run(() => BroadcastPresenceAsync(_token.Token));
        _ = Task.Run(() => CheckPeerLive());
        this.PeerConnected += () =>
        {
            _ = Task.Run(() => ReceiveMessageAsync());
        };
    }

    private void CheckPeerLive()
    {
        while (true)
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

        var requestMessage = new PeerRequestMessage(_identity.Id, _identity.Username, _identity.PublicKey, _identity.PresharedKey);
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

        var sharedSecret = Curve25519.GetSharedSecret(_identity.PrivateKey, peerPublicKeyBuffer);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(_identity.PresharedKey).ToArray());

        PeerInfo.IPAddress = peerInfo.IPAddress;
        PeerInfo.Port = peerInfo.Port;
        PeerInfo.Username = peerInfo.Username;
        PeerInfo.Id = peerInfo.Id;
        PeerInfo.PublicKey = peerPublicKeyBuffer;
        PeerInfo.PresharedKey = _identity.PresharedKey;
        PeerInfo.LastSeen = DateTime.Now;
        PeerInfo.AeadKey = aeadKey;

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

        await _networkStream.WriteAsync(_identity.PublicKey);
        await _networkStream.FlushAsync();
        var sharedSecret = Curve25519.GetSharedSecret(_identity.PrivateKey, result.PublicKey);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(_identity.PresharedKey).ToArray());

        PeerInfo = new PeerInfo
        {
            Id = result.Id,
            Username = result.Username,
            IPAddress = ((IPEndPoint)_tcpClient.Client.RemoteEndPoint!).Address,
            Port = _identity.ListeningPort,
            PublicKey = result.PublicKey,
            PresharedKey = result.PresharedKey,
            LastSeen = DateTime.Now,
            AeadKey = aeadKey
        };
        _identity.PresharedKey = result.PresharedKey;

        PeerConnected?.Invoke();
    }





    public async Task SendMessageAsync(byte[] message)
{
    if (_identity is null || PeerInfo is null) return;

    if (_tcpClient is null || !_tcpClient.Connected || _networkStream is null || !_networkStream.CanWrite)
    {
        PeerDisconnected?.Invoke();
        return;
    }

    try
    {
        var sharedSecret = Curve25519.GetSharedSecret(_identity.PrivateKey, PeerInfo.PublicKey);
        var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(_identity.PresharedKey).ToArray());

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var chacha20 = new ChaCha20Poly1305(aeadKey);

        var cipherText = new byte[message.Length];
        var tag = new byte[16];

        chacha20.Encrypt(nonce, message, cipherText, tag);

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
    catch
    {
        _tcpClient?.Dispose();
        _networkStream = null;
        PeerDisconnected?.Invoke();
    }
}

public async Task ReceiveMessageAsync()
{
    while (true)
    {
        if (_tcpClient is null || !_tcpClient.Connected || _networkStream is null)
        {
            Debug.WriteLine("TCP client not connected or network stream is null.");
            PeerDisconnected?.Invoke();
            await Task.Delay(100);
            continue;
        }

        try
        {
            Debug.WriteLine("Waiting for message...");

            var lengthBytes = new byte[2];
            await ReadFullAsync(_networkStream, lengthBytes, 2);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            ushort messageLength = BitConverter.ToUInt16(lengthBytes);

            if (messageLength < 16)
            {
                Debug.WriteLine("Invalid message length.");
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

            var sharedSecret = Curve25519.GetSharedSecret(_identity.PrivateKey, PeerInfo.PublicKey);
            var aeadKey = Blake2s.ComputeHash(32, sharedSecret.Concat(_identity.PresharedKey).ToArray());

            var chacha20 = new ChaCha20Poly1305(aeadKey);

            var plainText = new byte[cipherTextLength];

            chacha20.Decrypt(nonce, cipherText, tag, plainText);

            if (plainText.Length == 0)
            {
                Debug.WriteLine("Received empty message.");
                continue;
            }

            var messageString = Encoding.UTF8.GetString(plainText);

            Debug.WriteLine($"Received message: {messageString}");

            MessageReceived?.Invoke(messageString);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Receive failed: {ex.Message}");
            PeerDisconnected?.Invoke();
            await Task.Delay(100);
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
                Id: _identity.Id,
                Username: _identity.Username,
                TcpPort: _identity.ListeningPort
            );

            var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
            var broadcastAddresses = GetLanBroadcastAddresses();

            foreach (var broadcast in broadcastAddresses)
            {
                await _udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(broadcast, _discoveryPort));
            }

            await Task.Delay(3000, token);
        }
    }

    private async Task ListenForPeersAsync(CancellationToken token)
    {
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _discoveryPort));

        while (!token.IsCancellationRequested)
        {
            var result = await _udpClient.ReceiveAsync(token);
            var peerMessage = JsonSerializer.Deserialize<PeerBroadcastMessage>(result.Buffer);
            if (peerMessage is null)
                continue;
            if (peerMessage.Id == _identity.Id)
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
    }

    private record PeerBroadcastMessage(Guid Id, string Username, int TcpPort);
    private record PeerRequestMessage(Guid Id, string Username, byte[] PublicKey, byte[] PresharedKey);
}
