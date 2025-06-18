using FarGuard.Windows.Data;
using FarGuard.Windows.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Services;

public class ChatService : IChatService
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    public ChatService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task AddMessageAsync(ChatHistory chatHistory)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.ChatHistories.AddAsync(chatHistory);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ChatHistory>> GetChatHistoryAsync(Guid clientId)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.ChatHistories
            .Where(ch => ch.ClientId == clientId || ch.SenderId == clientId)
            .Include(x => x.Client)
            .Include(x => x.Sender)
            .OrderByDescending(ch => ch.CreatedAt)
            .ToListAsync();
    }
}
