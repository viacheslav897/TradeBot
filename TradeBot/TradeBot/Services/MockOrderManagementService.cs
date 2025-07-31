using Binance.Net.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeBot.Db;
using TradeBot.Db.Models;
using TradeBot.Models;
using TradeBot.Trader;
using OrderType = TradeBot.Models.OrderType;

namespace TradeBot.Services;

public class MockOrderManagementService : IOrderManagementService
{
    private readonly ILogger<MockOrderManagementService> _logger;
    private readonly TradingConfig _tradingConfig;
    private readonly TradeBotDbContext _dbContext;
    private readonly Dictionary<string, Position> _activePositions;
    private readonly Dictionary<long, OrderInfo> _activeOrders;
    private long _nextOrderId = 1;

    public MockOrderManagementService(
        TradingConfig tradingConfig,
        TradeBotDbContext dbContext,
        ILogger<MockOrderManagementService> logger)
    {
        _tradingConfig = tradingConfig;
        _dbContext = dbContext;
        _logger = logger;
        _activePositions = new Dictionary<string, Position>();
        _activeOrders = new Dictionary<long, OrderInfo>();
    }

    public async Task<OrderInfo?> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity)
    {
        try
        {
            _logger.LogInformation($"MOCK: Размещение рыночного ордера: {side} {quantity} {symbol}");

            var orderInfo = new OrderInfo
            {
                OrderId = _nextOrderId++,
                Symbol = symbol,
                Side = side,
                Type = MapOrderType(Db.Models.OrderType.Market),
                Quantity = quantity,
                Price = 0, // Market orders don't have a specific price
                CreateTime = DateTime.UtcNow,
                Status = OrderStatus.Filled, // Mock orders are immediately filled
                ClientOrderId = Guid.NewGuid().ToString()
            };

            // Save to database
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
            _logger.LogInformation($"MOCK: Ордер размещен успешно: {orderInfo.OrderId}");

            return orderInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при размещении рыночного ордера");
            return null;
        }
    }

    public async Task<OrderInfo?> PlaceLimitOrderAsync(string symbol, OrderSide side, decimal quantity, decimal price)
    {
        try
        {
            _logger.LogInformation($"MOCK: Размещение лимитного ордера: {side} {quantity} {symbol} по цене {price}");

            var orderInfo = new OrderInfo
            {
                OrderId = _nextOrderId++,
                Symbol = symbol,
                Side = side,
                Type = MapOrderType(Db.Models.OrderType.Limit),
                Quantity = quantity,
                Price = price,
                CreateTime = DateTime.UtcNow,
                Status = OrderStatus.New,
                ClientOrderId = Guid.NewGuid().ToString()
            };

            // Save to database
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
            _logger.LogInformation($"MOCK: Лимитный ордер размещен успешно: {orderInfo.OrderId}");

            return orderInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при размещении лимитного ордера");
            return null;
        }
    }

    public async Task<bool> CancelOrderAsync(string symbol, long orderId)
    {
        try
        {
            _logger.LogInformation($"MOCK: Отмена ордера: {orderId} для {symbol}");

            var fakeOrder = await _dbContext.FakeOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (fakeOrder != null)
            {
                fakeOrder.Status = OrderStatus.Canceled;
                fakeOrder.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            _activeOrders.Remove(orderId);
            _logger.LogInformation($"MOCK: Ордер {orderId} отменен успешно");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при отмене ордера");
            return false;
        }
    }

    public async Task<OrderInfo?> GetOrderAsync(string symbol, long orderId)
    {
        try
        {
            var fakeOrder = await _dbContext.FakeOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (fakeOrder != null)
            {
                return new OrderInfo
                {
                    OrderId = fakeOrder.OrderId,
                    Symbol = fakeOrder.Symbol,
                    Side = fakeOrder.Side,
                    Type = MapOrderType(fakeOrder.Type),
                    Quantity = fakeOrder.Quantity,
                    Price = fakeOrder.Price,
                    StopPrice = fakeOrder.StopPrice,
                    CreateTime = fakeOrder.CreateTime,
                    Status = fakeOrder.Status,
                    ClientOrderId = fakeOrder.ClientOrderId
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при получении ордера");
            return null;
        }
    }

    public async Task<List<OrderInfo>> GetOpenOrdersAsync(string symbol)
    {
        try
        {
            var fakeOrders = await _dbContext.FakeOrders
                .Where(o => o.Symbol == symbol && o.Status == OrderStatus.New)
                .ToListAsync();

            return fakeOrders.Select(o => new OrderInfo
            {
                OrderId = o.OrderId,
                Symbol = o.Symbol,
                Side = o.Side,
                Type = MapOrderType(o.Type),
                Quantity = o.Quantity,
                Price = o.Price,
                StopPrice = o.StopPrice,
                CreateTime = o.CreateTime,
                Status = o.Status,
                ClientOrderId = o.ClientOrderId
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при получении открытых ордеров");
            return new List<OrderInfo>();
        }
    }

    public async Task<Position?> CreatePositionAsync(string symbol, OrderSide side, decimal quantity,
        decimal entryPrice)
    {
        try
        {
            var position = new Position
            {
                Symbol = symbol,
                Side = side,
                Quantity = quantity,
                EntryPrice = entryPrice,
                EntryTime = DateTime.UtcNow,
                IsActive = true
            };

            // Save to database
            var fakePosition = new FakePosition
            {
                Symbol = position.Symbol,
                Side = position.Side,
                Quantity = position.Quantity,
                EntryPrice = position.EntryPrice,
                EntryTime = position.EntryTime,
                IsActive = position.IsActive
            };

            _dbContext.FakePositions.Add(fakePosition);
            await _dbContext.SaveChangesAsync();

            _activePositions[symbol] = position;
            _logger.LogInformation($"MOCK: Позиция создана: {symbol} {side} {quantity} по цене {entryPrice}");

            return position;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при создании позиции");
            return null;
        }
    }

    public async Task<bool> ClosePositionAsync(string symbol)
    {
        try
        {
            if (!_activePositions.ContainsKey(symbol))
            {
                _logger.LogWarning($"MOCK: Позиция для {symbol} не найдена");
                return false;
            }

            var position = _activePositions[symbol];

            // Закрываем позицию рыночным ордером
            var closeSide = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            var closeOrder = await PlaceMarketOrderAsync(symbol, closeSide, position.Quantity);

            if (closeOrder != null)
            {
                // Update position in database
                var fakePosition =
                    await _dbContext.FakePositions.FirstOrDefaultAsync(p => p.Symbol == symbol && p.IsActive);
                if (fakePosition != null)
                {
                    fakePosition.IsActive = false;
                    fakePosition.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                _activePositions.Remove(symbol);
                _logger.LogInformation($"MOCK: Позиция {symbol} закрыта успешно");
                return true;
            }
            else
            {
                _logger.LogError($"MOCK: Ошибка закрытия позиции {symbol}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при закрытии позиции");
            return false;
        }
    }

    public Position? GetActivePosition(string symbol)
    {
        return _activePositions.ContainsKey(symbol) ? _activePositions[symbol] : null;
    }

    public List<Position> GetAllActivePositions()
    {
        return _activePositions.Values.Where(p => p.IsActive).ToList();
    }

    public List<OrderInfo> GetAllActiveOrders()
    {
        return _activeOrders.Values.ToList();
    }

    public async Task<decimal> GetAccountBalanceAsync(string asset = "USDT")
    {
        // Mock balance - you can modify this to return different values for testing
        return 10000m;
    }

    public async Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal orderSize)
    {
        // Mock calculation - you can modify this for testing
        return orderSize / 50000m; // Assuming BTC price around 50000
    }

    public async Task MonitorPositionsAsync()
    {
        try
        {
            var activePositions = GetAllActivePositions();

            foreach (var position in activePositions)
            {
                // Проверяем, не истекло ли время позиции
                var positionAge = DateTime.UtcNow - position.EntryTime;
                if (positionAge.TotalHours >= _tradingConfig.MaxPositionHoldHours)
                {
                    _logger.LogInformation($"MOCK: Позиция {position.Symbol} истекла по времени ({_tradingConfig.MaxPositionHoldHours} часов). Закрываем...");
                    await ClosePositionAsync(position.Symbol);
                    continue;
                }

                // В новой стратегии нет автоматических стоп-лоссов и тейк-профитов
                // Позиции закрываются только при достижении минимальной прибыли
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при мониторинге позиций");
        }
    }

    public async Task<decimal> GetPositionPnLAsync(string symbol)
    {
        try
        {
            var position = GetActivePosition(symbol);
            if (position == null)
                return 0m;

            // Mock P&L calculation - you can modify this for testing
            var mockCurrentPrice = position.EntryPrice * 1.02m; // 2% profit for testing

            if (position.Side == OrderSide.Buy)
            {
                return (mockCurrentPrice - position.EntryPrice) * position.Quantity;
            }
            else
            {
                return (position.EntryPrice - mockCurrentPrice) * position.Quantity;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Исключение при расчете P&L");
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