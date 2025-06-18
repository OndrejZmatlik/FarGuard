using FarGuard.Windows.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarGuard.Windows.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ChatHistory> ChatHistories => Set<ChatHistory>();
    public DbSet<App> App => Set<App>();

}