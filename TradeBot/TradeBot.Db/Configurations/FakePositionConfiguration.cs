using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeBot.Db.Models;

namespace TradeBot.Db.Configurations;

public class FakePositionConfiguration : IEntityTypeConfiguration<FakePosition>
{
    public void Configure(EntityTypeBuilder<FakePosition> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Symbol)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(e => e.Quantity)
            .HasPrecision(18, 8);
            
        builder.Property(e => e.EntryPrice)
            .HasPrecision(18, 8);
    }
} 