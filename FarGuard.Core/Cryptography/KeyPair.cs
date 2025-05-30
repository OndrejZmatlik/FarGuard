using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Cryptography;

public class KeyPair
{
    public required byte[] PublicKey { get; init; }
    public required byte[] PrivateKey { get; init; }
    public required byte[] PresharedKey { get; init; }
}