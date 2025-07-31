using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Trader;

namespace TradeBot.Services;

public class NotificationDecoratedBinanceTradingService : IBinanceTradingService
{
    private readonly IBinanceTradingService _inner;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<NotificationDecoratedBinanceTradingService> _logger;

    public NotificationDecoratedBinanceTradingService(
        IBinanceTradingService inner,
        INotificationPublisher notificationPublisher,
        ILogger<NotificationDecoratedBinanceTradingService> logger)
    {
        _inner = inner;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var result = await _inner.TestConnectionAsync();
            
            if (result)
            {
                await _notificationPublisher.PublishSystemEventAsync(new SystemEvent
                {
                    Type = NotificationType.SystemStart,
                    Message = "Connection to Binance established successfully"
                });
            }
            else
            {
                await _notificationPublisher.PublishSystemEventAsync(new SystemEvent
                {
                    Type = NotificationType.ConnectionLost,
                    Message = "Failed to connect to Binance"
                });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _notificationPublisher.PublishSystemEventAsync(new SystemEvent
            {
                Type = NotificationType.Error,
                Message = "Connection test failed",
                ErrorDetails = ex.Message
            });
            throw;
        }
    }

    public async Task<IEnumerable<IBinanceKline>?> GetKlinesAsync(string symbol, KlineInterval interval, int limit = TradingConstants.Defaults.DefaultKlineLimit)
    {
        return await _inner.GetKlinesAsync(symbol, interval, limit);
    }

    public async Task AnalyzeMarketAsync()
    {
        try
        {
            await _inner.AnalyzeMarketAsync();
            
            // Publish market analysis event
            await _notificationPublisher.PublishTradingEventAsync(new TradingEvent
            {
                Type = NotificationType.MarketAnalysis,
                Symbol = "BTCUSDT", // This should come from config
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await _notificationPublisher.PublishSystemEventAsync(new SystemEvent
            {
                Type = NotificationType.Error,
                Message = "Market analysis failed",
                ErrorDetails = ex.Message
            });
            throw;
        }
    }

    public void Dispose()
    {
        _inner.Dispose();
    }
} 