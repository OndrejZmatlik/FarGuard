using Blake2Fast;
using Elliptic;
using Isopoh.Cryptography.Blake2b;
using NaCl.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class LocalPeerIdentity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Username { get; set; } = Environment.UserName;
    public int ListeningPort { get; set; }
    public byte[] PublicKey { get; init; } = [];
    public byte[] PrivateKey { get; init; } = [];
    public byte[] PresharedKey { get; set; } = [];

    private const int _tcpPortRangeStart = 40000;
    private const int _tcpPortRangeCount = 100;

    public static LocalPeerIdentity Generate()
    {
        var privateKey = Curve25519.CreateRandomPrivateKey();
        var publicKey = Curve25519.GetPublicKey(privateKey);

        var randomSeed = new byte[32];
        RandomNumberGenerator.Fill(randomSeed);
        var psk = Blake2s.ComputeHash(32, randomSeed);
        var port = _tcpPortRangeStart + RandomNumberGenerator.GetInt32(0, _tcpPortRangeCount);
        return new LocalPeerIdentity
        {
            PrivateKey = privateKey,
            PublicKey = publicKey,
            PresharedKey = psk,
            ListeningPort = port
        };
    }

}