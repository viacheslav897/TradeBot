using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TradeBot.Models;

namespace TradeBot.Services.Notifications;

public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly ITelegramBotClient _botClient;
    private readonly NotificationFormatter _formatter;
    private readonly ILogger<TelegramNotificationService> _logger;
    private readonly TelegramConfig _config;
    private readonly Channel<NotificationMessage> _messageQueue;

    public TelegramNotificationService(
        ITelegramBotClient botClient,
        NotificationFormatter formatter,
        IOptions<TelegramConfig> config,
        ILogger<TelegramNotificationService> logger)
    {
        _botClient = botClient;
        _formatter = formatter;
        _logger = logger;
        _config = config.Value;
        _messageQueue = Channel.CreateUnbounded<NotificationMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task SendTradingNotificationAsync(TradingNotification notification)
    {
        if (!ShouldSendNotification(notification))
            return;

        var message = _formatter.FormatNotification(notification);
        await QueueNotificationAsync(message, notification.Priority);
    }

    public async Task SendOrderNotificationAsync(OrderNotification notification)
    {
        if (!ShouldSendNotification(notification))
            return;

        var message = _formatter.FormatNotification(notification);
        await QueueNotificationAsync(message, notification.Priority);
    }

    public async Task SendPositionNotificationAsync(PositionNotification notification)
    {
        if (!ShouldSendNotification(notification))
            return;

        var message = _formatter.FormatNotification(notification);
        await QueueNotificationAsync(message, notification.Priority);
    }

    public async Task SendSystemNotificationAsync(SystemNotification notification)
    {
        if (!ShouldSendNotification(notification))
            return;

        var message = _formatter.FormatNotification(notification);
        await QueueNotificationAsync(message, notification.Priority);
    }

    public async Task SendCustomMessageAsync(long chatId, string message, NotificationPriority priority = NotificationPriority.Normal)
    {
        var notificationMessage = new NotificationMessage
        {
            ChatId = chatId,
            Message = message,
            Priority = priority,
            Timestamp = DateTime.UtcNow
        };

        await _messageQueue.Writer.WriteAsync(notificationMessage);
    }

    public async Task<bool> IsUserAuthorizedAsync(long chatId)
    {
        return _config.AuthorizedUsers.Contains(chatId);
    }

    private bool ShouldSendNotification(TradingNotification notification)
    {
        // Check if notifications are enabled for this type
        if (!IsNotificationTypeEnabled(notification.Type))
            return false;

        // Check quiet hours
        if (IsInQuietHours() && !IsCriticalNotification(notification))
            return false;

        return true;
    }

    private bool IsNotificationTypeEnabled(NotificationType type)
    {
        return type switch
        {
            NotificationType.OrderPlaced or NotificationType.OrderFilled or 
            NotificationType.OrderCancelled or NotificationType.OrderFailed => _config.NotificationSettings.EnableOrderNotifications,
            
            NotificationType.PositionOpened or NotificationType.PositionClosed or 
            NotificationType.StopLossTriggered or NotificationType.TakeProfitHit => _config.NotificationSettings.EnablePositionNotifications,
            
            NotificationType.MarketAnalysis or NotificationType.SidewaysDetected or 
            NotificationType.TrendChange => _config.NotificationSettings.EnableMarketAnalysis,
            
            NotificationType.SystemStart or NotificationType.SystemStop or 
            NotificationType.ConnectionLost or NotificationType.Error => _config.NotificationSettings.EnableSystemAlerts,
            
            _ => true
        };
    }

    private bool IsInQuietHours()
    {
        if (!_config.NotificationSettings.QuietHours.Enabled)
            return false;

        var now = DateTime.UtcNow;
        var startTime = TimeSpan.Parse(_config.NotificationSettings.QuietHours.StartTime);
        var endTime = TimeSpan.Parse(_config.NotificationSettings.QuietHours.EndTime);

        var currentTime = now.TimeOfDay;

        if (startTime <= endTime)
        {
            return currentTime >= startTime && currentTime <= endTime;
        }
        else // Crosses midnight
        {
            return currentTime >= startTime || currentTime <= endTime;
        }
    }

    private bool IsCriticalNotification(TradingNotification notification)
    {
        return notification.Priority == NotificationPriority.Critical && 
               _config.NotificationSettings.QuietHours.AllowCriticalOnly;
    }

    private async Task QueueNotificationAsync(string message, NotificationPriority priority)
    {
        var notificationMessage = new NotificationMessage
        {
            Message = message,
            Priority = priority,
            Timestamp = DateTime.UtcNow
        };

        await _messageQueue.Writer.WriteAsync(notificationMessage);
    }

    public async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
    {
        var batch = new List<NotificationMessage>();
        var batchSize = _config.MessageSettings.QueueBatchSize;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Collect messages for batch processing
                while (batch.Count < batchSize && _messageQueue.Reader.TryRead(out var message))
                {
                    batch.Add(message);
                }

                if (batch.Count == 0)
                {
                    // No messages, wait a bit
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                // Process batch
                foreach (var message in batch)
                {
                    await SendMessageToAllAuthorizedUsersAsync(message, cancellationToken);
                }

                batch.Clear();

                // Rate limiting
                await Task.Delay(_config.MessageSettings.RateLimitDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification queue");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task SendMessageToAllAuthorizedUsersAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        foreach (var chatId in _config.AuthorizedUsers)
        {
            try
            {
                await SendMessageWithRetryAsync(chatId, message.Message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to chat {ChatId}", chatId);
            }
        }
    }

    private async Task SendMessageWithRetryAsync(long chatId, string message, CancellationToken cancellationToken)
    {
        var maxRetries = _config.MessageSettings.MaxRetries;
        var retryDelay = TimeSpan.FromSeconds(_config.MessageSettings.RetryDelaySeconds);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Message sent successfully to chat {ChatId}", chatId);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Failed to send message to chat {ChatId}, attempt {Attempt}/{MaxRetries}", 
                    chatId, attempt, maxRetries);
                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to chat {ChatId} after {MaxRetries} attempts", 
                    chatId, maxRetries);
                throw;
            }
        }
    }
}

public class NotificationMessage
{
    public long? ChatId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public DateTime Timestamp { get; set; }
} 