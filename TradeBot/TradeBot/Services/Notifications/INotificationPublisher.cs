using TradeBot.Models;

namespace TradeBot.Services.Notifications;

public interface INotificationPublisher
{
    Task PublishOrderEventAsync(OrderEvent orderEvent);
    Task PublishPositionEventAsync(PositionEvent positionEvent);
    Task PublishTradingEventAsync(TradingEvent tradingEvent);
    Task PublishSystemEventAsync(SystemEvent systemEvent);
} 