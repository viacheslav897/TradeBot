using Binance.Net.Enums;
using TradeBot.Models;

namespace TradeBot.Services.Notifications;

public class NotificationFormatter
{
    private readonly Dictionary<NotificationType, Func<TradingNotification, string>> _templates;

    public NotificationFormatter()
    {
        _templates = new Dictionary<NotificationType, Func<TradingNotification, string>>
        {
            // Order notifications
            { NotificationType.OrderPlaced, FormatOrderPlaced },
            { NotificationType.OrderFilled, FormatOrderFilled },
            { NotificationType.OrderCancelled, FormatOrderCancelled },
            { NotificationType.OrderFailed, FormatOrderFailed },
            
            // Position notifications
            { NotificationType.PositionOpened, FormatPositionOpened },
            { NotificationType.PositionClosed, FormatPositionClosed },
            { NotificationType.StopLossTriggered, FormatStopLossTriggered },
            { NotificationType.TakeProfitHit, FormatTakeProfitHit },
            
            // Trading notifications
            { NotificationType.MarketAnalysis, FormatMarketAnalysis },
            { NotificationType.SidewaysDetected, FormatSidewaysDetected },
            { NotificationType.TrendChange, FormatTrendChange },
            
            // System notifications
            { NotificationType.SystemStart, FormatSystemStart },
            { NotificationType.SystemStop, FormatSystemStop },
            { NotificationType.ConnectionLost, FormatConnectionLost },
            { NotificationType.Error, FormatError }
        };
    }

    public string FormatNotification(TradingNotification notification)
    {
        if (_templates.TryGetValue(notification.Type, out var formatter))
        {
            return formatter(notification);
        }
        
        return FormatDefault(notification);
    }

    private string FormatOrderPlaced(TradingNotification notification)
    {
        if (notification is OrderNotification orderNotif && orderNotif.OrderInfo != null)
        {
            var side = orderNotif.Side == OrderSide.Buy ? "🟢 BUY" : "🔴 SELL";
            var type = orderNotif.OrderType == OrderType.Market ? "MARKET" : "LIMIT";
            return $"{side} {type} Order Placed\n" +
                   $"Symbol: {orderNotif.Symbol}\n" +
                   $"Quantity: {orderNotif.Quantity:F8}\n" +
                   $"Price: ${orderNotif.Price:F2}\n" +
                   $"Order ID: {orderNotif.OrderInfo.OrderId}";
        }
        return "Order placed";
    }

    private string FormatOrderFilled(TradingNotification notification)
    {
        if (notification is OrderNotification orderNotif && orderNotif.OrderInfo != null)
        {
            var side = orderNotif.Side == OrderSide.Buy ? "🟢 BUY" : "🔴 SELL";
            return $"{side} Order Filled ✅\n" +
                   $"Symbol: {orderNotif.Symbol}\n" +
                   $"Quantity: {orderNotif.Quantity:F8}\n" +
                   $"Price: ${orderNotif.Price:F2}\n" +
                   $"Order ID: {orderNotif.OrderInfo.OrderId}";
        }
        return "Order filled";
    }

    private string FormatOrderCancelled(TradingNotification notification)
    {
        if (notification is OrderNotification orderNotif && orderNotif.OrderInfo != null)
        {
            return $"⚠️ Order Cancelled\n" +
                   $"Symbol: {orderNotif.Symbol}\n" +
                   $"Order ID: {orderNotif.OrderInfo.OrderId}";
        }
        return "Order cancelled";
    }

    private string FormatOrderFailed(TradingNotification notification)
    {
        if (notification is OrderNotification orderNotif)
        {
            return $"❌ Order Failed\n" +
                   $"Symbol: {orderNotif.Symbol}\n" +
                   $"Side: {orderNotif.Side}\n" +
                   $"Quantity: {orderNotif.Quantity:F8}";
        }
        return "Order failed";
    }

    private string FormatPositionOpened(TradingNotification notification)
    {
        if (notification is PositionNotification posNotif && posNotif.Position != null)
        {
            var side = posNotif.Position.Side == OrderSide.Buy ? "🟢 LONG" : "🔴 SHORT";
            return $"{side} Position Opened\n" +
                   $"Symbol: {posNotif.Symbol}\n" +
                   $"Quantity: {posNotif.Quantity:F8}\n" +
                   $"Entry Price: ${posNotif.EntryPrice:F2}\n" +
                   $"Entry Time: {posNotif.Position.EntryTime:HH:mm:ss}";
        }
        return "Position opened";
    }

    private string FormatPositionClosed(TradingNotification notification)
    {
        if (notification is PositionNotification posNotif)
        {
            var pnl = posNotif.PnL ?? 0;
            var pnlEmoji = pnl >= 0 ? "💰" : "📉";
            var pnlColor = pnl >= 0 ? "🟢" : "🔴";
            
            return $"{pnlEmoji} Position Closed\n" +
                   $"Symbol: {posNotif.Symbol}\n" +
                   $"Exit Price: ${posNotif.ExitPrice:F2}\n" +
                   $"{pnlColor} P&L: ${pnl:F2}";
        }
        return "Position closed";
    }

    private string FormatStopLossTriggered(TradingNotification notification)
    {
        if (notification is PositionNotification posNotif)
        {
            var pnl = posNotif.PnL ?? 0;
            return $"🛑 Stop Loss Triggered\n" +
                   $"Symbol: {posNotif.Symbol}\n" +
                   $"Exit Price: ${posNotif.ExitPrice:F2}\n" +
                   $"🔴 Loss: ${pnl:F2}";
        }
        return "Stop loss triggered";
    }

    private string FormatTakeProfitHit(TradingNotification notification)
    {
        if (notification is PositionNotification posNotif)
        {
            var pnl = posNotif.PnL ?? 0;
            return $"🎯 Take Profit Hit\n" +
                   $"Symbol: {posNotif.Symbol}\n" +
                   $"Exit Price: ${posNotif.ExitPrice:F2}\n" +
                   $"💰 Profit: ${pnl:F2}";
        }
        return "Take profit hit";
    }

    private string FormatMarketAnalysis(TradingNotification notification)
    {
        return $"📊 Market Analysis\n" +
               $"Symbol: {notification.Symbol}\n" +
               $"Current Price: ${notification.Price:F2}\n" +
               $"Time: {notification.Timestamp:HH:mm:ss}";
    }

    private string FormatSidewaysDetected(TradingNotification notification)
    {
        return $"📈 Sideways Trend Detected\n" +
               $"Symbol: {notification.Symbol}\n" +
               $"Price Range: ${notification.AdditionalData.GetValueOrDefault("Support", 0):F2} - ${notification.AdditionalData.GetValueOrDefault("Resistance", 0):F2}\n" +
               $"Current Price: ${notification.Price:F2}";
    }

    private string FormatTrendChange(TradingNotification notification)
    {
        return $"🔄 Trend Change Detected\n" +
               $"Symbol: {notification.Symbol}\n" +
               $"New Direction: {notification.AdditionalData.GetValueOrDefault("Trend", "Unknown")}\n" +
               $"Price: ${notification.Price:F2}";
    }

    private string FormatSystemStart(TradingNotification notification)
    {
        if (notification is SystemNotification sysNotif)
        {
            return $"🚀 {sysNotif.Message}\n" +
                   $"Started at: {notification.Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
        return "System started";
    }

    private string FormatSystemStop(TradingNotification notification)
    {
        if (notification is SystemNotification sysNotif)
        {
            return $"⏹️ {sysNotif.Message}\n" +
                   $"Stopped at: {notification.Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
        return "System stopped";
    }

    private string FormatConnectionLost(TradingNotification notification)
    {
        if (notification is SystemNotification sysNotif)
        {
            return $"🔌 Connection Lost\n" +
                   $"{sysNotif.Message}\n" +
                   $"Time: {notification.Timestamp:HH:mm:ss}";
        }
        return "Connection lost";
    }

    private string FormatError(TradingNotification notification)
    {
        if (notification is SystemNotification sysNotif)
        {
            return $"❌ Error\n" +
                   $"{sysNotif.Message}\n" +
                   $"Time: {notification.Timestamp:HH:mm:ss}";
        }
        return "Error occurred";
    }

    private string FormatDefault(TradingNotification notification)
    {
        return $"📢 {notification.Type}\n" +
               $"Symbol: {notification.Symbol}\n" +
               $"Time: {notification.Timestamp:HH:mm:ss}";
    }
} 