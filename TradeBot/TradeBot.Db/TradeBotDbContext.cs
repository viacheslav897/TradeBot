using Microsoft.EntityFrameworkCore;
using TradeBot.Db.Models;

namespace TradeBot.Db;

public class TradeBotDbContext : DbContext
{
    public TradeBotDbContext(DbContextOptions<TradeBotDbContext> options) : base(options)
    {
    }

    public DbSet<FakeOrder> FakeOrders { get; set; }
    public DbSet<FakePosition> FakePositions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from separate files
        modelBuilder.ApplyConfiguration(new Configurations.FakeOrderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.FakePositionConfiguration());
    }
}