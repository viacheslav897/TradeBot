using Binance.Net.Enums;

namespace TradeBot.Db.Models;

public class FakeOrder
{
    public int Id { get; set; }
    public long OrderId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? StopPrice { get; set; }
    public DateTime CreateTime { get; set; }
    public OrderStatus Status { get; set; }
    public string ClientOrderId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
} 