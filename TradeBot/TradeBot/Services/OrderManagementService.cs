using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Trader;
using OrderSide = TradeBot.Models.OrderSide;
using OrderStatus = TradeBot.Models.OrderStatus;

namespace TradeBot.Services;

public class OrderManagementService
{
    private readonly BinanceRestClient _restClient;
    private readonly ILogger<OrderManagementService> _logger;
    private readonly TradingConfig _tradingConfig;
    private readonly Dictionary<string, Position> _activePositions;
    private readonly Dictionary<long, OrderInfo> _activeOrders;

    public OrderManagementService(
        BinanceConfig binanceConfig,
        TradingConfig tradingConfig,
        ILogger<OrderManagementService> logger)
    {
        _tradingConfig = tradingConfig;
        _logger = logger;
        _activePositions = new Dictionary<string, Position>();
        _activeOrders = new Dictionary<long, OrderInfo>();

        // Создаем BinanceRestClient для OrderManagementService
        _restClient = new BinanceRestClient(options =>
        {
            options.ApiCredentials =
                new ApiCredentials(binanceConfig.ApiKey, binanceConfig.ApiSecret);
            if (binanceConfig.IsTestNet)
                options.Environment = Binance.Net.Enums.BinanceEnvironment.Testnet;
        });
    }

    public async Task<OrderInfo?> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity)
    {
        try
        {
            _logger.LogInformation($"Размещение рыночного ордера: {side} {quantity} {symbol}");

            var orderRequest = new BinanceOrderRequest
            {
                Symbol = symbol,
                Side = side == OrderSide.Buy ? SpotOrderSide.Buy : SpotOrderSide.Sell,
                Type = SpotOrderType.Market,
                Quantity = quantity
            };

            var result = await _restClient.SpotApi.Trading.PlaceOrderAsync(orderRequest);

            if (result.Success)
            {
                var orderInfo = new OrderInfo
                {
                    OrderId = result.Data.Id,
                    Symbol = result.Data.Symbol,
                    Side = result.Data.Side == SpotOrderSide.Buy ? OrderSide.Buy : OrderSide.Sell,
                    Type = OrderType.Market,
                    Quantity = result.Data.Quantity,
                    Price = result.Data.Price,
                    CreateTime = result.Data.CreateTime,
                    Status = MapOrderStatus(result.Data.Status),
                    ClientOrderId = result.Data.ClientOrderId
                };

                _activeOrders[orderInfo.OrderId] = orderInfo;
                _logger.LogInformation($"Ордер размещен успешно: {orderInfo.OrderId}");

                return orderInfo;
            }
            else
            {
                _logger.LogError($"Ошибка размещения ордера: {result.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при размещении рыночного ордера");
            return null;
        }
    }

    public async Task<OrderInfo?> PlaceLimitOrderAsync(string symbol, OrderSide side, decimal quantity, decimal price)
    {
        try
        {
            _logger.LogInformation($"Размещение лимитного ордера: {side} {quantity} {symbol} по цене {price}");

            var orderRequest = new BinanceOrderRequest
            {
                Symbol = symbol,
                Side = side == OrderSide.Buy ? SpotOrderSide.Buy : SpotOrderSide.Sell,
                Type = SpotOrderType.Limit,
                Quantity = quantity,
                Price = price,
                TimeInForce = TimeInForce.GoodTillCanceled
            };

            var result = await _restClient.SpotApi.Trading.PlaceOrderAsync(orderRequest);

            if (result.Success)
            {
                var orderInfo = new OrderInfo
                {
                    OrderId = result.Data.Id,
                    Symbol = result.Data.Symbol,
                    Side = result.Data.Side == SpotOrderSide.Buy ? OrderSide.Buy : OrderSide.Sell,
                    Type = OrderType.Limit,
                    Quantity = result.Data.Quantity,
                    Price = result.Data.Price,
                    CreateTime = result.Data.CreateTime,
                    Status = MapOrderStatus(result.Data.Status),
                    ClientOrderId = result.Data.ClientOrderId
                };

                _activeOrders[orderInfo.OrderId] = orderInfo;
                _logger.LogInformation($"Лимитный ордер размещен успешно: {orderInfo.OrderId}");

                return orderInfo;
            }
            else
            {
                _logger.LogError($"Ошибка размещения лимитного ордера: {result.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при размещении лимитного ордера");
            return null;
        }
    }

    public async Task<OrderInfo?> PlaceStopLossOrderAsync(string symbol, decimal quantity, decimal stopPrice)
    {
        try
        {
            _logger.LogInformation($"Размещение стоп-лосс ордера: {quantity} {symbol} по цене {stopPrice}");

            var orderRequest = new BinanceOrderRequest
            {
                Symbol = symbol,
                Side = SpotOrderSide.Sell,
                Type = SpotOrderType.StopLoss,
                Quantity = quantity,
                StopPrice = stopPrice
            };

            var result = await _restClient.SpotApi.Trading.PlaceOrderAsync(orderRequest);

            if (result.Success)
            {
                var orderInfo = new OrderInfo
                {
                    OrderId = result.Data.Id,
                    Symbol = result.Data.Symbol,
                    Side = OrderSide.Sell,
                    Type = OrderType.StopLoss,
                    Quantity = result.Data.Quantity,
                    Price = result.Data.Price,
                    StopPrice = result.Data.StopPrice,
                    CreateTime = result.Data.CreateTime,
                    Status = MapOrderStatus(result.Data.Status),
                    ClientOrderId = result.Data.ClientOrderId
                };

                _activeOrders[orderInfo.OrderId] = orderInfo;
                _logger.LogInformation($"Стоп-лосс ордер размещен успешно: {orderInfo.OrderId}");

                return orderInfo;
            }
            else
            {
                _logger.LogError($"Ошибка размещения стоп-лосс ордера: {result.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при размещении стоп-лосс ордера");
            return null;
        }
    }

    public async Task<OrderInfo?> PlaceTakeProfitOrderAsync(string symbol, decimal quantity, decimal takeProfitPrice)
    {
        try
        {
            _logger.LogInformation($"Размещение тейк-профит ордера: {quantity} {symbol} по цене {takeProfitPrice}");

            var orderRequest = new BinanceOrderRequest
            {
                Symbol = symbol,
                Side = SpotOrderSide.Sell,
                Type = SpotOrderType.TakeProfit,
                Quantity = quantity,
                Price = takeProfitPrice
            };

            var result = await _restClient.SpotApi.Trading.PlaceOrderAsync(orderRequest);

            if (result.Success)
            {
                var orderInfo = new OrderInfo
                {
                    OrderId = result.Data.Id,
                    Symbol = result.Data.Symbol,
                    Side = OrderSide.Sell,
                    Type = OrderType.TakeProfit,
                    Quantity = result.Data.Quantity,
                    Price = result.Data.Price,
                    CreateTime = result.Data.CreateTime,
                    Status = MapOrderStatus(result.Data.Status),
                    ClientOrderId = result.Data.ClientOrderId
                };

                _activeOrders[orderInfo.OrderId] = orderInfo;
                _logger.LogInformation($"Тейк-профит ордер размещен успешно: {orderInfo.OrderId}");

                return orderInfo;
            }
            else
            {
                _logger.LogError($"Ошибка размещения тейк-профит ордера: {result.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при размещении тейк-профит ордера");
            return null;
        }
    }

    public async Task<bool> CancelOrderAsync(string symbol, long orderId)
    {
        try
        {
            _logger.LogInformation($"Отмена ордера: {orderId} для {symbol}");

            var result = await _restClient.SpotApi.Trading.CancelOrderAsync(symbol, orderId);

            if (result.Success)
            {
                _activeOrders.Remove(orderId);
                _logger.LogInformation($"Ордер {orderId} отменен успешно");
                return true;
            }
            else
            {
                _logger.LogError($"Ошибка отмены ордера: {result.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при отмене ордера");
            return false;
        }
    }

    public async Task<OrderInfo?> GetOrderAsync(string symbol, long orderId)
    {
        try
        {
            var result = await _restClient.SpotApi.Trading.GetOrderAsync(symbol, orderId);

            if (result.Success)
            {
                var orderInfo = new OrderInfo
                {
                    OrderId = result.Data.Id,
                    Symbol = result.Data.Symbol,
                    Side = result.Data.Side == SpotOrderSide.Buy ? OrderSide.Buy : OrderSide.Sell,
                    Type = MapOrderType(result.Data.Type),
                    Quantity = result.Data.Quantity,
                    Price = result.Data.Price,
                    StopPrice = result.Data.StopPrice,
                    CreateTime = result.Data.CreateTime,
                    Status = MapOrderStatus(result.Data.Status),
                    ClientOrderId = result.Data.ClientOrderId
                };

                return orderInfo;
            }
            else
            {
                _logger.LogError($"Ошибка получения ордера: {result.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при получении ордера");
            return null;
        }
    }

    public async Task<List<OrderInfo>> GetOpenOrdersAsync(string symbol)
    {
        try
        {
            var result = await _restClient.SpotApi.Trading.GetOpenOrdersAsync(symbol);

            if (result.Success)
            {
                var orders = result.Data.Select(order => new OrderInfo
                {
                    OrderId = order.Id,
                    Symbol = order.Symbol,
                    Side = order.Side == SpotOrderSide.Buy ? OrderSide.Buy : OrderSide.Sell,
                    Type = MapOrderType(order.Type),
                    Quantity = order.Quantity,
                    Price = order.Price,
                    StopPrice = order.StopPrice,
                    CreateTime = order.CreateTime,
                    Status = MapOrderStatus(order.Status),
                    ClientOrderId = order.ClientOrderId
                }).ToList();

                return orders;
            }
            else
            {
                _logger.LogError($"Ошибка получения открытых ордеров: {result.Error}");
                return new List<OrderInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при получении открытых ордеров");
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

            // Рассчитываем уровни стоп-лосс и тейк-профит
            if (side == OrderSide.Buy)
            {
                position.StopLossPrice = entryPrice * (1 - _tradingConfig.StopLossPercent / 100);
                position.TakeProfitPrice = entryPrice * (1 + _tradingConfig.TakeProfitPercent / 100);
            }
            else
            {
                position.StopLossPrice = entryPrice * (1 + _tradingConfig.StopLossPercent / 100);
                position.TakeProfitPrice = entryPrice * (1 - _tradingConfig.TakeProfitPercent / 100);
            }

            _activePositions[symbol] = position;
            _logger.LogInformation($"Позиция создана: {symbol} {side} {quantity} по цене {entryPrice}");

            return position;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при создании позиции");
            return null;
        }
    }

    public async Task<bool> ClosePositionAsync(string symbol)
    {
        try
        {
            if (!_activePositions.ContainsKey(symbol))
            {
                _logger.LogWarning($"Позиция для {symbol} не найдена");
                return false;
            }

            var position = _activePositions[symbol];

            // Отменяем связанные ордера
            if (position.TakeProfitOrderId.HasValue)
            {
                await CancelOrderAsync(symbol, position.TakeProfitOrderId.Value);
            }

            if (position.StopLossOrderId.HasValue)
            {
                await CancelOrderAsync(symbol, position.StopLossOrderId.Value);
            }

            // Закрываем позицию рыночным ордером
            var closeSide = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            var closeOrder = await PlaceMarketOrderAsync(symbol, closeSide, position.Quantity);

            if (closeOrder != null)
            {
                _activePositions.Remove(symbol);
                _logger.LogInformation($"Позиция {symbol} закрыта успешно");
                return true;
            }
            else
            {
                _logger.LogError($"Ошибка закрытия позиции {symbol}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при закрытии позиции");
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
        try
        {
            var result = await _restClient.SpotApi.Account.GetAccountInfoAsync();

            if (result.Success)
            {
                var balance = result.Data.Balances.FirstOrDefault(b => b.Asset == asset);
                return balance?.Available ?? 0m;
            }
            else
            {
                _logger.LogError($"Ошибка получения баланса: {result.Error}");
                return 0m;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при получении баланса");
            return 0m;
        }
    }

    public async Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal orderSize)
    {
        try
        {
            // Получаем текущую цену символа
            var tickerResult = await _restClient.SpotApi.ExchangeData.GetTickerAsync(symbol);

            if (tickerResult.Success)
            {
                var currentPrice = tickerResult.Data.LastPrice;
                var quantity = orderSize / currentPrice;

                // Округляем количество согласно правилам Binance
                var symbolInfo = await _restClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
                if (symbolInfo.Success)
                {
                    var symbolData = symbolInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol);
                    if (symbolData != null)
                    {
                        var lotSizeFilter = symbolData.LotSizeFilter;
                        if (lotSizeFilter != null)
                        {
                            var stepSize = lotSizeFilter.StepSize;
                            var precision = (int)Math.Abs(Math.Log10((double)stepSize));
                            quantity = Math.Round(quantity, precision);
                        }
                    }
                }

                return quantity;
            }
            else
            {
                _logger.LogError($"Ошибка получения цены для {symbol}: {tickerResult.Error}");
                return 0m;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при расчете количества ордера");
            return 0m;
        }
    }

    public async Task MonitorPositionsAsync()
    {
        try
        {
            var activePositions = GetAllActivePositions();

            foreach (var position in activePositions)
            {
                // Проверяем, не истекло ли время позиции (максимум 4 часа)
                var positionAge = DateTime.UtcNow - position.EntryTime;
                if (positionAge.TotalHours >= 4)
                {
                    _logger.LogInformation($"Позиция {position.Symbol} истекла по времени. Закрываем...");
                    await ClosePositionAsync(position.Symbol);
                    continue;
                }

                // Проверяем статус связанных ордеров
                if (position.TakeProfitOrderId.HasValue)
                {
                    var takeProfitOrder = await GetOrderAsync(position.Symbol, position.TakeProfitOrderId.Value);
                    if (takeProfitOrder?.Status == OrderStatus.Filled)
                    {
                        _logger.LogInformation($"Тейк-профит исполнен для позиции {position.Symbol}");
                        position.IsActive = false;
                        _activePositions.Remove(position.Symbol);
                    }
                }

                if (position.StopLossOrderId.HasValue)
                {
                    var stopLossOrder = await GetOrderAsync(position.Symbol, position.StopLossOrderId.Value);
                    if (stopLossOrder?.Status == OrderStatus.Filled)
                    {
                        _logger.LogInformation($"Стоп-лосс исполнен для позиции {position.Symbol}");
                        position.IsActive = false;
                        _activePositions.Remove(position.Symbol);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при мониторинге позиций");
        }
    }

    public async Task<decimal> GetPositionPnLAsync(string symbol)
    {
        try
        {
            var position = GetActivePosition(symbol);
            if (position == null)
                return 0m;

            // Получаем текущую цену
            var tickerResult = await _restClient.SpotApi.ExchangeData.GetTickerAsync(symbol);
            if (!tickerResult.Success)
                return 0m;

            var currentPrice = tickerResult.Data.LastPrice;

            // Рассчитываем P&L
            if (position.Side == OrderSide.Buy)
            {
                return (currentPrice - position.EntryPrice) * position.Quantity;
            }
            else
            {
                return (position.EntryPrice - currentPrice) * position.Quantity;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при расчете P&L");
            return 0m;
        }
    }

    private OrderStatus MapOrderStatus(SpotOrderStatus status)
    {
        return status switch
        {
            SpotOrderStatus.New => OrderStatus.New,
            SpotOrderStatus.PartiallyFilled => OrderStatus.PartiallyFilled,
            SpotOrderStatus.Filled => OrderStatus.Filled,
            SpotOrderStatus.Canceled => OrderStatus.Canceled,
            SpotOrderStatus.Rejected => OrderStatus.Rejected,
            SpotOrderStatus.Expired => OrderStatus.Expired,
            _ => OrderStatus.New
        };
    }

    private OrderType MapOrderType(SpotOrderType type)
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