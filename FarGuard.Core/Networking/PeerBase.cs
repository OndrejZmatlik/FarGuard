using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class PeerBase
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Port { get; set; }
    public byte[] PublicKey { get; set; } = [];
    public byte[] PresharedKey { get; set; } = [];
}