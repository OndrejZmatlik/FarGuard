using Blake2Fast;
using Elliptic;
using Isopoh.Cryptography.Blake2b;
using NaCl.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class LocalPeerIdentity : PeerBase
{
    public byte[] PrivateKey { get; set; } = [];
    public LocalPeerIdentity Generate()
    {
        var privateKey = Curve25519.CreateRandomPrivateKey();
        var publicKey = Curve25519.GetPublicKey(privateKey);

        var randomSeed = new byte[32];
        RandomNumberGenerator.Fill(randomSeed);
        var psk = Blake2s.ComputeHash(32, randomSeed);
        var port = StaticData.TcpPortRangeStart + RandomNumberGenerator.GetInt32(0, StaticData.TcpPortRangeCount);
        return new LocalPeerIdentity
        {
            Id = this.Id,
            Username = this.Username,
            PrivateKey = privateKey,
            PublicKey = publicKey,
            PresharedKey = psk,
            Port = port
        };
    }

}