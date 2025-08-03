using Binance.Net.Enums;

namespace TradeBot.Models;

public enum NotificationType
{
    OrderPlaced, OrderFilled, OrderCancelled, OrderFailed,
    PositionOpened, PositionClosed, StopLossTriggered, TakeProfitHit,
    MarketAnalysis, SidewaysDetected, TrendChange,
    NewsAnalysis, SignalGenerated, SentimentAnalysis,
    SystemStart, SystemStop, ConnectionLost, Error
}

public enum NotificationPriority
{
    Low,      // Market analysis, general info
    Normal,   // Order confirmations, position updates
    High,     // Stop loss, take profit, errors
    Critical  // System failures, connection issues
}

public class TradingNotification
{
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? PnL { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class OrderNotification : TradingNotification
{
    public OrderInfo? OrderInfo { get; set; }
    public OrderSide Side { get; set; }
    public OrderType OrderType { get; set; }
}

public class PositionNotification : TradingNotification
{
    public Position? Position { get; set; }
    public decimal? EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
}

public class SystemNotification : TradingNotification
{
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
}

// Event models for publisher pattern
public class OrderEvent
{
    public NotificationType Type { get; set; }
    public OrderInfo? OrderInfo { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PositionEvent
{
    public NotificationType Type { get; set; }
    public Position? Position { get; set; }
    public decimal? PnL { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TradingEvent
{
    public NotificationType Type { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SystemEvent
{
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 