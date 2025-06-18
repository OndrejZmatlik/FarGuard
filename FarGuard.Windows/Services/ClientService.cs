using FarGuard.Windows.Data;
using FarGuard.Windows.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Services;

public class ClientService : IClientService
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    public ClientService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task AddClientAsync(Client client)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Clients.AddAsync(client);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Client?> GetClientAsync(Guid clientId)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
    }

    public async Task UpdateClientAsync(Client client)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var result = await dbContext.Clients.FindAsync(client.Id);
        if (result is null)
        {
            await dbContext.Clients.AddAsync(client);
        }
        else
        {
            result.Id = client.Id;
            dbContext.Clients.Update(result);
        }
        await dbContext.SaveChangesAsync();
    }
}