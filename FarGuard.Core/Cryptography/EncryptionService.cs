using FarGuard.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Cryptography;

public class EncryptionService : IEncryptionService
{
    public byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
    {
        throw new NotImplementedException();
    }

    public byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
    {
        throw new NotImplementedException();
    }
}