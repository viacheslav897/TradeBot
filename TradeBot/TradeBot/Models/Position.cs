using Binance.Net.Enums;

namespace TradeBot.Models;

public class Position
{
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime EntryTime { get; set; }
    public bool IsActive { get; set; } = true;
}