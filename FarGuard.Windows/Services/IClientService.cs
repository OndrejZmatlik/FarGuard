using FarGuard.Windows.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Services;

public interface IClientService
{
    Task AddClientAsync(Client client);
    Task<Client?> GetClientAsync(Guid clientId);
    Task UpdateClientAsync(Client client);
}