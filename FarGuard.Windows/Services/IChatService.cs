using FarGuard.Windows.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Services;

public interface IChatService
{
    Task AddMessageAsync(ChatHistory chatHistory);
    Task<IEnumerable<ChatHistory>> GetChatHistoryAsync(Guid clientId);
}