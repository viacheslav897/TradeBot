using Binance.Net.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeBot.Db;
using TradeBot.Db.Models;
using TradeBot.Models;
using TradeBot.Services.Core;
using TradeBot.Trader;
using OrderType = TradeBot.Models.OrderType;

namespace TradeBot.Services.OrderManagement;

public class MockOrderManagementService : BaseOrderManagementService
{
    private readonly TradeBotDbContext _dbContext;
    private long _nextOrderId = 1;

    public MockOrderManagementService(
        TradingConfig tradingConfig,
        TradeBotDbContext dbContext,
        ILogger<MockOrderManagementService> logger) : base(tradingConfig, logger)
    {
        _dbContext = dbContext;
    }

    public override async Task<OrderInfo?> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity)
    {
        try
        {
            _logger.LogInformation("MOCK: Placing market order: {Side} {Quantity} {Symbol}", side, quantity, symbol);

            var orderInfo = CreateOrderInfo(
                _nextOrderId++,
                symbol,
                side,
                MapOrderType(Db.Models.OrderType.Market),
                quantity,
                0, // Market orders don't have a specific price
                DateTime.UtcNow,
                OrderStatus.Filled, // Mock orders are immediately filled
                Guid.NewGuid().ToString());

            var fakeOrder = new FakeOrder
            {
                OrderId = orderInfo.OrderId,
                Symbol = orderInfo.Symbol,
                Side = orderInfo.Side,
                Type = MapToDbOrderType(orderInfo.Type),
                Quantity = orderInfo.Quantity,
                Price = orderInfo.Price,
                CreateTime = orderInfo.CreateTime,
                Status = orderInfo.Status,
                ClientOrderId = orderInfo.ClientOrderId
            };

            _dbContext.FakeOrders.Add(fakeOrder);
            await _dbContext.SaveChangesAsync();

            _activeOrders[orderInfo.OrderId] = orderInfo;
            _logger.LogInformation("MOCK: Order placed successfully: {OrderId}", orderInfo.OrderId);

            return orderInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Exception while placing market order");
            return null;
        }
    }

    public override async Task<OrderInfo?> PlaceLimitOrderAsync(string symbol, OrderSide side, decimal quantity, decimal price)
    {
        try
        {
            _logger.LogInformation("MOCK: Placing limit order: {Side} {Quantity} {Symbol} at price {Price}", side, quantity, symbol, price);

            var orderInfo = CreateOrderInfo(
                _nextOrderId++,
                symbol,
                side,
                MapOrderType(Db.Models.OrderType.Limit),
                quantity,
                price,
                DateTime.UtcNow,
                OrderStatus.New,
                Guid.NewGuid().ToString());

            var fakeOrder = new FakeOrder
            {
                OrderId = orderInfo.OrderId,
                Symbol = orderInfo.Symbol,
                Side = orderInfo.Side,
                Type = MapToDbOrderType(orderInfo.Type),
                Quantity = orderInfo.Quantity,
                Price = orderInfo.Price,
                CreateTime = orderInfo.CreateTime,
                Status = orderInfo.Status,
                ClientOrderId = orderInfo.ClientOrderId
            };

            _dbContext.FakeOrders.Add(fakeOrder);
            await _dbContext.SaveChangesAsync();

            _activeOrders[orderInfo.OrderId] = orderInfo;
            _logger.LogInformation("MOCK: Limit order placed successfully: {OrderId}", orderInfo.OrderId);

            return orderInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Exception while placing limit order");
            return null;
        }
    }

    public override async Task<bool> CancelOrderAsync(string symbol, long orderId)
    {
        try
        {
            _logger.LogInformation("MOCK: Canceling order: {OrderId} for {Symbol}", orderId, symbol);

            var fakeOrder = await _dbContext.FakeOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (fakeOrder != null)
            {
                fakeOrder.Status = OrderStatus.Canceled;
                fakeOrder.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            _activeOrders.Remove(orderId);
            _logger.LogInformation("MOCK: Order {OrderId} canceled successfully", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Exception while canceling order");
            return false;
        }
    }

    public override async Task<OrderInfo?> GetOrderAsync(string symbol, long orderId)
    {
        try
        {
            var fakeOrder = await _dbContext.FakeOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (fakeOrder != null)
            {
                return CreateOrderInfo(
                    fakeOrder.OrderId,
                    fakeOrder.Symbol,
                    fakeOrder.Side,
                    MapOrderType(fakeOrder.Type),
                    fakeOrder.Quantity,
                    fakeOrder.Price,
                    fakeOrder.CreateTime,
                    fakeOrder.Status,
                    fakeOrder.ClientOrderId,
                    fakeOrder.StopPrice);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Exception while getting order");
            return null;
        }
    }

    public override async Task<List<OrderInfo>> GetOpenOrdersAsync(string symbol)
    {
        try
        {
            var fakeOrders = await _dbContext.FakeOrders
                .Where(o => o.Symbol == symbol && o.Status == OrderStatus.New)
                .ToListAsync();

            return fakeOrders.Select(o => CreateOrderInfo(
                o.OrderId,
                o.Symbol,
                o.Side,
                MapOrderType(o.Type),
                o.Quantity,
                o.Price,
                o.CreateTime,
                o.Status,
                o.ClientOrderId,
                o.StopPrice)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Exception while getting open orders");
            return new List<OrderInfo>();
        }
    }



    public override async Task<decimal> GetAccountBalanceAsync(string asset = TradingConstants.Defaults.DefaultAsset)
    {
        // Mock balance - you can modify this to return different values for testing
        return TradingConstants.Defaults.DefaultMockBalance;
    }

    public override async Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal orderSize)
    {
        // Mock calculation - you can modify this for testing
        return orderSize / TradingConstants.Defaults.DefaultMockPrice; // Assuming BTC price around 50000
    }

    public override async Task<decimal> GetPositionPnLAsync(string symbol)
    {
        try
        {
            var position = GetActivePosition(symbol);
            if (position == null)
                return 0m;

            // Mock P&L calculation - you can modify this for testing
            var mockCurrentPrice = position.EntryPrice * (1 + TradingConstants.Defaults.DefaultProfitPercent);

            return position.Side == OrderSide.Buy
                ? (mockCurrentPrice - position.EntryPrice) * position.Quantity
                : (position.EntryPrice - mockCurrentPrice) * position.Quantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Exception while calculating P&L");
            return 0m;
        }
    }

    private OrderType MapOrderType(Db.Models.OrderType dbOrderType)
    {
        return dbOrderType switch
        {
            Db.Models.OrderType.Market => OrderType.Market,
            Db.Models.OrderType.Limit => OrderType.Limit,
            Db.Models.OrderType.StopLoss => OrderType.StopLoss,
            Db.Models.OrderType.TakeProfit => OrderType.TakeProfit,
            _ => OrderType.Market
        };
    }

    private Db.Models.OrderType MapToDbOrderType(OrderType orderType)
    {
        return orderType switch
        {
            OrderType.Market => Db.Models.OrderType.Market,
            OrderType.Limit => Db.Models.OrderType.Limit,
            OrderType.StopLoss => Db.Models.OrderType.StopLoss,
            OrderType.TakeProfit => Db.Models.OrderType.TakeProfit,
            _ => Db.Models.OrderType.Market
        };
    }
}