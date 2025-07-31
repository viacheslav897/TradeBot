using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeBot.Db.Models;

namespace TradeBot.Db.Configurations;

public class FakeOrderConfiguration : IEntityTypeConfiguration<FakeOrder>
{
    public void Configure(EntityTypeBuilder<FakeOrder> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.OrderId)
            .IsRequired();
            
        builder.Property(e => e.Symbol)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(e => e.Quantity)
            .HasPrecision(18, 8);
            
        builder.Property(e => e.Price)
            .HasPrecision(18, 8);
            
        builder.Property(e => e.StopPrice)
            .HasPrecision(18, 8);
            
        builder.Property(e => e.ClientOrderId)
            .HasMaxLength(100);
    }
} 