using Microsoft.EntityFrameworkCore;
using PocRdsDatabaseSwap.API.Models;

namespace PocRdsDatabaseSwap.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public virtual DbSet<LogEntity> Logs { get; set; } = null!;
}
