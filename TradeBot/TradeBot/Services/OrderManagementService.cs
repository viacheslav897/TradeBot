using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Trader;

namespace TradeBot.Services;

public class OrderManagementService : BaseOrderManagementService
{
    private readonly BinanceRestClient _restClient;

    public OrderManagementService(
        BinanceConfig binanceConfig,
        TradingConfig tradingConfig,
        ILogger<OrderManagementService> logger) : base(tradingConfig, logger)
    {
        _restClient = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(binanceConfig.ApiKey, binanceConfig.ApiSecret);
            if (binanceConfig.IsTestNet)
                options.Environment = BinanceEnvironment.Testnet;
        });
    }

    public override async Task<OrderInfo?> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity)
    {
        try
        {
            _logger.LogInformation("Placing market order: {Side} {Quantity} {Symbol}", side, quantity, symbol);

            var result = await _restClient.SpotApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: side,
                type: SpotOrderType.Market,
                quantity: quantity);

            if (result.Success)
            {
                var orderInfo = CreateOrderInfo(
                    result.Data.Id,
                    result.Data.Symbol,
                    result.Data.Side,
                    OrderType.Market,
                    result.Data.Quantity,
                    result.Data.Price,
                    result.Data.CreateTime,
                    result.Data.Status,
                    result.Data.ClientOrderId);

                _activeOrders[orderInfo.OrderId] = orderInfo;
                _logger.LogInformation("Order placed successfully: {OrderId}", orderInfo.OrderId);

                return orderInfo;
            }
            else
            {
                _logger.LogError("Order placement error: {Error}", result.Error);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while placing market order");
            return null;
        }
    }

    public override async Task<OrderInfo?> PlaceLimitOrderAsync(string symbol, OrderSide side, decimal quantity, decimal price)
    {
        try
        {
            _logger.LogInformation("Placing limit order: {Side} {Quantity} {Symbol} at price {Price}", side, quantity, symbol, price);

            var result = await _restClient.SpotApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: side,
                type: SpotOrderType.Limit,
                quantity: quantity,
                price: price,
                timeInForce: TimeInForce.GoodTillCanceled);

            if (result.Success)
            {
                var orderInfo = CreateOrderInfo(
                    result.Data.Id,
                    result.Data.Symbol,
                    result.Data.Side,
                    OrderType.Limit,
                    result.Data.Quantity,
                    result.Data.Price,
                    result.Data.CreateTime,
                    result.Data.Status,
                    result.Data.ClientOrderId);

                _activeOrders[orderInfo.OrderId] = orderInfo;
                _logger.LogInformation("Limit order placed successfully: {OrderId}", orderInfo.OrderId);

                return orderInfo;
            }
            else
            {
                _logger.LogError("Limit order placement error: {Error}", result.Error);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while placing limit order");
            return null;
        }
    }

    public override async Task<bool> CancelOrderAsync(string symbol, long orderId)
    {
        try
        {
            _logger.LogInformation("Canceling order: {OrderId} for {Symbol}", orderId, symbol);

            var result = await _restClient.SpotApi.Trading.CancelOrderAsync(symbol, orderId);

            if (result.Success)
            {
                _activeOrders.Remove(orderId);
                _logger.LogInformation("Order {OrderId} canceled successfully", orderId);
                return true;
            }
            else
            {
                _logger.LogError("Order cancellation error: {Error}", result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while canceling order");
            return false;
        }
    }

    public override async Task<OrderInfo?> GetOrderAsync(string symbol, long orderId)
    {
        try
        {
            var result = await _restClient.SpotApi.Trading.GetOrderAsync(symbol, orderId);

            if (result.Success)
            {
                return CreateOrderInfo(
                    result.Data.Id,
                    result.Data.Symbol,
                    result.Data.Side,
                    MapOrderType(result.Data.Type),
                    result.Data.Quantity,
                    result.Data.Price,
                    result.Data.CreateTime,
                    result.Data.Status,
                    result.Data.ClientOrderId,
                    result.Data.StopPrice);
            }
            else
            {
                _logger.LogError("Error getting order: {Error}", result.Error);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting order");
            return null;
        }
    }

    public override async Task<List<OrderInfo>> GetOpenOrdersAsync(string symbol)
    {
        try
        {
            var result = await _restClient.SpotApi.Trading.GetOpenOrdersAsync(symbol);

            if (result.Success)
            {
                return result.Data.Select(order => CreateOrderInfo(
                    order.Id,
                    order.Symbol,
                    order.Side,
                    MapOrderType(order.Type),
                    order.Quantity,
                    order.Price,
                    order.CreateTime,
                    order.Status,
                    order.ClientOrderId,
                    order.StopPrice)).ToList();
            }
            else
            {
                _logger.LogError("Error getting open orders: {Error}", result.Error);
                return new List<OrderInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting open orders");
            return new List<OrderInfo>();
        }
    }



    public override async Task<decimal> GetAccountBalanceAsync(string asset = TradingConstants.Defaults.DefaultAsset)
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
                _logger.LogError("Error getting balance: {Error}", result.Error);
                return 0m;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting balance");
            return 0m;
        }
    }

    public override async Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal orderSize)
    {
        try
        {
            var tickerResult = await _restClient.SpotApi.ExchangeData.GetTickerAsync(symbol);

            if (tickerResult.Success)
            {
                var currentPrice = tickerResult.Data.LastPrice;
                var quantity = orderSize / currentPrice;

                var symbolInfo = await _restClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
                if (symbolInfo.Success)
                {
                    var symbolData = symbolInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol);
                    if (symbolData?.LotSizeFilter != null)
                    {
                        var stepSize = symbolData.LotSizeFilter.StepSize;
                        var precision = (int)Math.Abs(Math.Log10((double)stepSize));
                        quantity = Math.Round(quantity, precision);
                    }
                }

                return quantity;
            }
            else
            {
                _logger.LogError("Error getting price for {Symbol}: {Error}", symbol, tickerResult.Error);
                return 0m;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calculating order quantity");
            return 0m;
        }
    }

    public override async Task<decimal> GetPositionPnLAsync(string symbol)
    {
        try
        {
            var position = GetActivePosition(symbol);
            if (position == null)
                return 0m;

            var tickerResult = await _restClient.SpotApi.ExchangeData.GetTickerAsync(symbol);
            if (!tickerResult.Success)
                return 0m;

            var currentPrice = tickerResult.Data.LastPrice;

            return position.Side == OrderSide.Buy
                ? (currentPrice - position.EntryPrice) * position.Quantity
                : (position.EntryPrice - currentPrice) * position.Quantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calculating P&L");
            return 0m;
        }
    }
}