using FarGuard.Windows.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Services;

public interface IAppService
{
    Task<App?> GetAppSettingsAsync();
    Task UpdateAppSettingsAsync(App app);
    Task CreateAppSettingsAsync(App app);
}