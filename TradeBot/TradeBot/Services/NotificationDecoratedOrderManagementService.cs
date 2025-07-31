using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Trader;

namespace TradeBot.Services;

public class NotificationDecoratedOrderManagementService : IOrderManagementService
{
    private readonly IOrderManagementService _inner;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<NotificationDecoratedOrderManagementService> _logger;

    public NotificationDecoratedOrderManagementService(
        IOrderManagementService inner,
        INotificationPublisher notificationPublisher,
        ILogger<NotificationDecoratedOrderManagementService> logger)
    {
        _inner = inner;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<OrderInfo?> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity)
    {
        try
        {
            var result = await _inner.PlaceMarketOrderAsync(symbol, side, quantity);
            
            if (result != null)
            {
                await _notificationPublisher.PublishOrderEventAsync(new OrderEvent
                {
                    Type = NotificationType.OrderPlaced,
                    OrderInfo = result
                });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _notificationPublisher.PublishOrderEventAsync(new OrderEvent
            {
                Type = NotificationType.OrderFailed,
                OrderInfo = new OrderInfo
                {
                    Symbol = symbol,
                    Side = side,
                    Quantity = quantity,
                    Type = OrderType.Market
                }
            });
            throw;
        }
    }

    public async Task<OrderInfo?> PlaceLimitOrderAsync(string symbol, OrderSide side, decimal quantity, decimal price)
    {
        try
        {
            var result = await _inner.PlaceLimitOrderAsync(symbol, side, quantity, price);
            
            if (result != null)
            {
                await _notificationPublisher.PublishOrderEventAsync(new OrderEvent
                {
                    Type = NotificationType.OrderPlaced,
                    OrderInfo = result
                });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _notificationPublisher.PublishOrderEventAsync(new OrderEvent
            {
                Type = NotificationType.OrderFailed,
                OrderInfo = new OrderInfo
                {
                    Symbol = symbol,
                    Side = side,
                    Quantity = quantity,
                    Price = price,
                    Type = OrderType.Limit
                }
            });
            throw;
        }
    }

    public async Task<bool> CancelOrderAsync(string symbol, long orderId)
    {
        try
        {
            var result = await _inner.CancelOrderAsync(symbol, orderId);
            
            if (result)
            {
                await _notificationPublisher.PublishOrderEventAsync(new OrderEvent
                {
                    Type = NotificationType.OrderCancelled,
                    OrderInfo = new OrderInfo
                    {
                        Symbol = symbol,
                        OrderId = orderId
                    }
                });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId} for {Symbol}", orderId, symbol);
            throw;
        }
    }

    public async Task<OrderInfo?> GetOrderAsync(string symbol, long orderId)
    {
        return await _inner.GetOrderAsync(symbol, orderId);
    }

    public async Task<List<OrderInfo>> GetOpenOrdersAsync(string symbol)
    {
        return await _inner.GetOpenOrdersAsync(symbol);
    }

    public async Task<decimal> GetAccountBalanceAsync(string asset = TradingConstants.Defaults.DefaultAsset)
    {
        return await _inner.GetAccountBalanceAsync(asset);
    }

    public async Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal orderSize)
    {
        return await _inner.CalculateOrderQuantityAsync(symbol, orderSize);
    }

    public async Task<decimal> GetPositionPnLAsync(string symbol)
    {
        return await _inner.GetPositionPnLAsync(symbol);
    }

    public async Task<Position?> CreatePositionAsync(string symbol, OrderSide side, decimal quantity, decimal entryPrice)
    {
        var result = await _inner.CreatePositionAsync(symbol, side, quantity, entryPrice);
        
        if (result != null)
        {
            await _notificationPublisher.PublishPositionEventAsync(new PositionEvent
            {
                Type = NotificationType.PositionOpened,
                Position = result
            });
        }
        
        return result;
    }

    public async Task<bool> ClosePositionAsync(string symbol)
    {
        var result = await _inner.ClosePositionAsync(symbol);
        
        if (result)
        {
            var position = _inner.GetActivePosition(symbol);
            if (position != null)
            {
                var pnl = await _inner.GetPositionPnLAsync(symbol);
                await _notificationPublisher.PublishPositionEventAsync(new PositionEvent
                {
                    Type = NotificationType.PositionClosed,
                    Position = position,
                    PnL = pnl
                });
            }
        }
        
        return result;
    }

    public Position? GetActivePosition(string symbol)
    {
        return _inner.GetActivePosition(symbol);
    }

    public List<Position> GetAllActivePositions()
    {
        return _inner.GetAllActivePositions();
    }

    public List<OrderInfo> GetAllActiveOrders()
    {
        return _inner.GetAllActiveOrders();
    }

    public async Task MonitorPositionsAsync()
    {
        await _inner.MonitorPositionsAsync();
        
        // Notify about active positions
        var activePositions = _inner.GetAllActivePositions();
        foreach (var position in activePositions)
        {
            var pnl = await _inner.GetPositionPnLAsync(position.Symbol);
            await _notificationPublisher.PublishPositionEventAsync(new PositionEvent
            {
                Type = NotificationType.PositionOpened,
                Position = position,
                PnL = pnl
            });
        }
    }
} 