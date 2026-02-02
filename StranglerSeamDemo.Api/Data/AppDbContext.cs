using StranglerSeamDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace StranglerSeamDemo.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<CaseRecord> Cases => Set<CaseRecord>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
