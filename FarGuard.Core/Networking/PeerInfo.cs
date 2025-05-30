using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class PeerInfo
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public IPAddress IPAddress { get; init; } = IPAddress.Any;
    public int Port { get; init; }
    public byte[] PublicKey { get; init; } = [];
    public byte[] PreSharedKey { get; init; } = [];
    public DateTime LastSeen { get; set; }
}