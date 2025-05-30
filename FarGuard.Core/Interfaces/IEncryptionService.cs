using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Core.Interfaces;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] data, byte[] key, byte[] iv);
    byte[] Decrypt(byte[] data, byte[] key, byte[] iv);
}