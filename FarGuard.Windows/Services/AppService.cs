using FarGuard.Windows.Data;
using FarGuard.Windows.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Services;

public class AppService : IAppService
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    public AppService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }
    public async Task CreateAppSettingsAsync(App app)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.App.AddAsync(app);
        await dbContext.SaveChangesAsync();
    }

    public Task<App?> GetAppSettingsAsync()
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return dbContext.App.FirstOrDefaultAsync();
    }

    public async Task UpdateAppSettingsAsync(App app)
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var result = await dbContext.App.FirstOrDefaultAsync();
        if (result is null)
        {
            await dbContext.App.AddAsync(app);
        }
        else
        {
            result.UserId = app.UserId;
            result.UserName = app.UserName;
            dbContext.App.Update(result);
        }
        await dbContext.SaveChangesAsync();
    }
}
