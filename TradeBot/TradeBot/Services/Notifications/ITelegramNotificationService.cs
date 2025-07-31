using TradeBot.Models;

namespace TradeBot.Services.Notifications;

public interface ITelegramNotificationService
{
    Task SendTradingNotificationAsync(TradingNotification notification);
    Task SendOrderNotificationAsync(OrderNotification notification);
    Task SendPositionNotificationAsync(PositionNotification notification);
    Task SendSystemNotificationAsync(SystemNotification notification);
    Task SendCustomMessageAsync(long chatId, string message, NotificationPriority priority = NotificationPriority.Normal);
    Task<bool> IsUserAuthorizedAsync(long chatId);
} 