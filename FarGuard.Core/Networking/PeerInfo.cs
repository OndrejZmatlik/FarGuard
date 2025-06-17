using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class PeerInfo
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public IPAddress IPAddress { get; set; } = IPAddress.None;
    public int Port { get; set; }
    public byte[] PublicKey { get; set; } = [];
    public byte[] PresharedKey { get; set; } = [];
    public byte[] AeadKey { get; set; } = [];
    public DateTime LastSeen { get; set; }

    public override string ToString()
    {
        return $"{Username} ({Id}) - {IPAddress}:{Port}, {LastSeen}";
    }
}