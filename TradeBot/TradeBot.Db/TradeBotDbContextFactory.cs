using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TradeBot.Db;

public class TradeBotDbContextFactory: IDesignTimeDbContextFactory<TradeBotDbContext>
{
    public TradeBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TradeBotDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1435;Database=TradeBotDb;User Id=sa;Password=TradeBot123!;TrustServerCertificate=true;MultipleActiveResultSets=true");

        return new TradeBotDbContext(optionsBuilder.Options);
    }
}