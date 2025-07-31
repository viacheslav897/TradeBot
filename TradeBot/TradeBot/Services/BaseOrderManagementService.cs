using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Trader;

namespace TradeBot.Services;

public abstract class BaseOrderManagementService : IOrderManagementService
{
    protected readonly ILogger _logger;
    protected readonly TradingConfig _tradingConfig;
    protected readonly Dictionary<string, Position> _activePositions;
    protected readonly Dictionary<long, OrderInfo> _activeOrders;

    protected BaseOrderManagementService(
        TradingConfig tradingConfig,
        ILogger logger)
    {
        _tradingConfig = tradingConfig;
        _logger = logger;
        _activePositions = new Dictionary<string, Position>();
        _activeOrders = new Dictionary<long, OrderInfo>();
    }

    public abstract Task<OrderInfo?> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity);
    public abstract Task<OrderInfo?> PlaceLimitOrderAsync(string symbol, OrderSide side, decimal quantity, decimal price);
    public abstract Task<bool> CancelOrderAsync(string symbol, long orderId);
    public abstract Task<OrderInfo?> GetOrderAsync(string symbol, long orderId);
    public abstract Task<List<OrderInfo>> GetOpenOrdersAsync(string symbol);
    public abstract Task<decimal> GetAccountBalanceAsync(string asset = TradingConstants.Defaults.DefaultAsset);
    public abstract Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal orderSize);
    public abstract Task<decimal> GetPositionPnLAsync(string symbol);

    public virtual async Task<Position?> CreatePositionAsync(string symbol, OrderSide side, decimal quantity, decimal entryPrice)
    {
        try
        {
            var position = new Position
            {
                Symbol = symbol,
                Side = side,
                Quantity = quantity,
                EntryPrice = entryPrice,
                CurrentPrice = entryPrice, // Initially set to entry price
                EntryTime = DateTime.UtcNow,
                IsActive = true
            };

            _activePositions[symbol] = position;
            _logger.LogInformation("Position created: {Symbol} {Side} {Quantity} at price {EntryPrice}", 
                symbol, side, quantity, entryPrice);

            return position;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating position");
            return null;
        }
    }

    public virtual async Task<bool> ClosePositionAsync(string symbol)
    {
        try
        {
            if (!_activePositions.ContainsKey(symbol))
            {
                _logger.LogWarning("Position for {Symbol} not found", symbol);
                return false;
            }

            var position = _activePositions[symbol];
            var closeSide = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            var closeOrder = await PlaceMarketOrderAsync(symbol, closeSide, position.Quantity);

            if (closeOrder != null)
            {
                _activePositions.Remove(symbol);
                _logger.LogInformation("Position {Symbol} closed successfully", symbol);
                return true;
            }
            else
            {
                _logger.LogError("Error closing position {Symbol}", symbol);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while closing position");
            return false;
        }
    }

    public virtual Position? GetActivePosition(string symbol)
    {
        return _activePositions.ContainsKey(symbol) ? _activePositions[symbol] : null;
    }

    public virtual List<Position> GetAllActivePositions()
    {
        return _activePositions.Values.Where(p => p.IsActive).ToList();
    }

    public virtual List<OrderInfo> GetAllActiveOrders()
    {
        return _activeOrders.Values.ToList();
    }

    public virtual async Task MonitorPositionsAsync()
    {
        try
        {
            var activePositions = GetAllActivePositions();

            foreach (var position in activePositions)
            {
                // Update current price for the position
                await UpdatePositionCurrentPriceAsync(position);
                
                var positionAge = DateTime.UtcNow - position.EntryTime;
                if (positionAge.TotalHours >= _tradingConfig.MaxPositionHoldHours)
                {
                    _logger.LogInformation("Position {Symbol} expired after {Hours} hours. Closing...", 
                        position.Symbol, _tradingConfig.MaxPositionHoldHours);
                    await ClosePositionAsync(position.Symbol);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while monitoring positions");
        }
    }

    protected virtual async Task UpdatePositionCurrentPriceAsync(Position position)
    {
        try
        {
            // This is a base implementation - derived classes should override this
            // to get the actual current price from the exchange
            _logger.LogDebug("Updating current price for position {Symbol}", position.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating current price for position {Symbol}", position.Symbol);
        }
    }

    protected virtual OrderInfo CreateOrderInfo(long orderId, string symbol, OrderSide side, OrderType type, 
        decimal quantity, decimal price, DateTime createTime, OrderStatus status, string clientOrderId, decimal? stopPrice = null)
    {
        return new OrderInfo
        {
            OrderId = orderId,
            Symbol = symbol,
            Side = side,
            Type = type,
            Quantity = quantity,
            Price = price,
            StopPrice = stopPrice,
            CreateTime = createTime,
            Status = status,
            ClientOrderId = clientOrderId
        };
    }

    protected virtual OrderType MapOrderType(SpotOrderType type)
    {
        return type switch
        {
            SpotOrderType.Market => OrderType.Market,
            SpotOrderType.Limit => OrderType.Limit,
            SpotOrderType.StopLoss => OrderType.StopLoss,
            SpotOrderType.TakeProfit => OrderType.TakeProfit,
            _ => OrderType.Market
        };
    }
} 