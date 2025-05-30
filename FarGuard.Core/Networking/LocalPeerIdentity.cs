using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class LocalPeerIdentity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Username { get; init; } = Environment.UserName;
    public int ListeningPort { get; set; }
    public byte[] PublicKey { get; init; } = [];
    public byte[] PrivateKey { get; init; } = [];
    public byte[] PreSharedKey { get; init; } = [];
}