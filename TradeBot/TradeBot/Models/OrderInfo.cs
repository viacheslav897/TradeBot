namespace TradeBot.Models;

public class OrderInfo
{
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
}

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderType
{
    Market,
    Limit,
    StopLoss,
    TakeProfit
}

public enum OrderStatus
{
    New,
    PartiallyFilled,
    Filled,
    Canceled,
    Rejected,
    Expired
}
