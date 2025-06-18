using FarGuard.Windows.Data;
using FarGuard.Windows.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FarGuard.Windows
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();

            services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseSqlite("Data Source=FarGuard.db");
            });

            services.AddScoped<IAppService, AppService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IClientService, ClientService>();

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                using var db = dbFactory.CreateDbContext();
                db.Database.EnsureCreated();
            }

            ApplicationConfiguration.Initialize();

            var appService = serviceProvider.GetRequiredService<IAppService>();
            var chatService = serviceProvider.GetRequiredService<IChatService>();
            var clientService = serviceProvider.GetRequiredService<IClientService>();

            Application.Run(new Form1(appService, chatService, clientService));
        }
    }
}
