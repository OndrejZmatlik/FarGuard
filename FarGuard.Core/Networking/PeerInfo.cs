using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Networking;

public class PeerInfo : PeerBase
{
    public IPAddress IPAddress { get; set; } = IPAddress.None;
    public DateTime LastSeen { get; set; }

    public override string ToString()
    {
        return $"{Username} ({Id}) - {IPAddress}:{Port}, {LastSeen}";
    }
}