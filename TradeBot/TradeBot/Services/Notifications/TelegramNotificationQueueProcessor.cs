using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradeBot.Services.Notifications;

public class TelegramNotificationQueueProcessor : BackgroundService
{
    private readonly ITelegramNotificationService _notificationService;
    private readonly ILogger<TelegramNotificationQueueProcessor> _logger;

    public TelegramNotificationQueueProcessor(
        ITelegramNotificationService notificationService,
        ILogger<TelegramNotificationQueueProcessor> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram notification queue processor");

        try
        {
            if (_notificationService is TelegramNotificationService telegramService)
            {
                await telegramService.ProcessMessageQueueAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Telegram notification queue processor stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Telegram notification queue processor");
        }
    }
} 