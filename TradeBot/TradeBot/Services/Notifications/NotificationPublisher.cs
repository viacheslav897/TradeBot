using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeBot.Models;

namespace TradeBot.Services.Notifications;

public class NotificationPublisher : INotificationPublisher
{
    private readonly ITelegramNotificationService _notificationService;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
        ITelegramNotificationService notificationService,
        ILogger<NotificationPublisher> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task PublishOrderEventAsync(OrderEvent orderEvent)
    {
        try
        {
            var notification = new OrderNotification
            {
                Type = orderEvent.Type,
                Priority = GetPriorityForOrderEvent(orderEvent.Type),
                Symbol = orderEvent.OrderInfo?.Symbol ?? string.Empty,
                Price = orderEvent.OrderInfo?.Price,
                Quantity = orderEvent.OrderInfo?.Quantity,
                Timestamp = orderEvent.Timestamp,
                OrderInfo = orderEvent.OrderInfo,
                Side = orderEvent.OrderInfo?.Side ?? OrderSide.Buy,
                OrderType = orderEvent.OrderInfo?.Type ?? OrderType.Market
            };

            await _notificationService.SendOrderNotificationAsync(notification);
            _logger.LogDebug("Published order event: {Type} for {Symbol}", orderEvent.Type, notification.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish order event: {Type}", orderEvent.Type);
        }
    }

    public async Task PublishPositionEventAsync(PositionEvent positionEvent)
    {
        try
        {
            var notification = new PositionNotification
            {
                Type = positionEvent.Type,
                Priority = GetPriorityForPositionEvent(positionEvent.Type),
                Symbol = positionEvent.Position?.Symbol ?? string.Empty,
                Price = positionEvent.Position?.EntryPrice, // Using EntryPrice as current price
                Quantity = positionEvent.Position?.Quantity,
                PnL = positionEvent.PnL,
                Timestamp = positionEvent.Timestamp,
                Position = positionEvent.Position,
                EntryPrice = positionEvent.Position?.EntryPrice,
                ExitPrice = null // Position model doesn't have ExitPrice
            };

            await _notificationService.SendPositionNotificationAsync(notification);
            _logger.LogDebug("Published position event: {Type} for {Symbol}", positionEvent.Type, notification.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish position event: {Type}", positionEvent.Type);
        }
    }

    public async Task PublishTradingEventAsync(TradingEvent tradingEvent)
    {
        try
        {
            var notification = new TradingNotification
            {
                Type = tradingEvent.Type,
                Priority = GetPriorityForTradingEvent(tradingEvent.Type),
                Symbol = tradingEvent.Symbol,
                Price = tradingEvent.Price,
                Timestamp = tradingEvent.Timestamp,
                AdditionalData = tradingEvent.Data
            };

            await _notificationService.SendTradingNotificationAsync(notification);
            _logger.LogDebug("Published trading event: {Type} for {Symbol}", tradingEvent.Type, tradingEvent.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish trading event: {Type}", tradingEvent.Type);
        }
    }

    public async Task PublishSystemEventAsync(SystemEvent systemEvent)
    {
        try
        {
            var notification = new SystemNotification
            {
                Type = systemEvent.Type,
                Priority = GetPriorityForSystemEvent(systemEvent.Type),
                Timestamp = systemEvent.Timestamp,
                Message = systemEvent.Message,
                ErrorDetails = systemEvent.ErrorDetails
            };

            await _notificationService.SendSystemNotificationAsync(notification);
            _logger.LogDebug("Published system event: {Type}", systemEvent.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish system event: {Type}", systemEvent.Type);
        }
    }

    private NotificationPriority GetPriorityForOrderEvent(NotificationType type)
    {
        return type switch
        {
            NotificationType.OrderFailed => NotificationPriority.High,
            NotificationType.OrderPlaced or NotificationType.OrderFilled => NotificationPriority.Normal,
            NotificationType.OrderCancelled => NotificationPriority.Low,
            _ => NotificationPriority.Normal
        };
    }

    private NotificationPriority GetPriorityForPositionEvent(NotificationType type)
    {
        return type switch
        {
            NotificationType.StopLossTriggered or NotificationType.TakeProfitHit => NotificationPriority.High,
            NotificationType.PositionOpened or NotificationType.PositionClosed => NotificationPriority.Normal,
            _ => NotificationPriority.Normal
        };
    }

    private NotificationPriority GetPriorityForTradingEvent(NotificationType type)
    {
        return type switch
        {
            NotificationType.SidewaysDetected or NotificationType.TrendChange => NotificationPriority.Normal,
            NotificationType.MarketAnalysis => NotificationPriority.Low,
            _ => NotificationPriority.Normal
        };
    }

    private NotificationPriority GetPriorityForSystemEvent(NotificationType type)
    {
        return type switch
        {
            NotificationType.ConnectionLost or NotificationType.Error => NotificationPriority.Critical,
            NotificationType.SystemStart or NotificationType.SystemStop => NotificationPriority.High,
            _ => NotificationPriority.Normal
        };
    }
} 