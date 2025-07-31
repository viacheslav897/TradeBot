using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Trader;

namespace TradeBot.Services;

public class BinanceTradingService : IBinanceTradingService
{
    private readonly BinanceRestClient _restClient;
    private readonly BinanceSocketClient _socketClient;
    private readonly SidewaysDetectionService _sidewaysDetection;
    private readonly IOrderManagementService _orderManagement;
    private readonly ILogger<BinanceTradingService> _logger;
    private readonly TradingConfig _tradingConfig;

    public BinanceTradingService(
        BinanceConfig binanceConfig,
        TradingConfig tradingConfig,
        SidewaysDetectionService sidewaysDetection,
        IOrderManagementService orderManagement,
        ILogger<BinanceTradingService> logger)
    {
        _tradingConfig = tradingConfig;
        _sidewaysDetection = sidewaysDetection;
        _orderManagement = orderManagement;
        _logger = logger;
        
        _restClient = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(binanceConfig.ApiKey, binanceConfig.ApiSecret);
            if (binanceConfig.IsTestNet)
                options.Environment = BinanceEnvironment.Testnet;
        });
        
        _socketClient = new BinanceSocketClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(binanceConfig.ApiKey, binanceConfig.ApiSecret);
            if (binanceConfig.IsTestNet)
                options.Environment = BinanceEnvironment.Testnet;
        });
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var result = await _restClient.SpotApi.ExchangeData.GetServerTimeAsync();
            if (result.Success)
            {
                _logger.LogInformation("Binance connection successful. Server time: {ServerTime}", result.Data);
                return true;
            }
            else
            {
                _logger.LogError("Binance connection error: {Error}", result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while testing Binance connection");
            return false;
        }
    }

    public async Task<IEnumerable<IBinanceKline>?> GetKlinesAsync(string symbol, KlineInterval interval,
        int limit = TradingConstants.Defaults.DefaultKlineLimit)
    {
        try
        {
            var result = await _restClient.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);
            if (result.Success)
            {
                return result.Data;
            }
            else
            {
                _logger.LogError("Error getting kline data: {Error}", result.Error);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting kline data");
            return null;
        }
    }

    public async Task AnalyzeMarketAsync()
    {
        try
        {
            _logger.LogInformation("Analyzing market for {Symbol}...", _tradingConfig.Symbol);

            var interval = GetKlineInterval(_tradingConfig.PeriodMinutes);
            var klines = await GetKlinesAsync(_tradingConfig.Symbol, interval, _tradingConfig.AnalysisPeriods + 10);

            if (klines == null || !klines.Any())
            {
                _logger.LogWarning(TradingConstants.ErrorMessages.InsufficientData);
                return;
            }

            var isSideways = _sidewaysDetection.IsSidewaysMarket(klines);

            if (isSideways)
            {
                _logger.LogInformation("Market is in sideways movement!");

                var (resistance, support) = _sidewaysDetection.GetSupportResistanceLevels(klines);
                _logger.LogInformation("Levels: Resistance = {Resistance}, Support = {Support}", resistance, support);

                var currentPrice = klines.Last().ClosePrice;
                _logger.LogInformation("Current price: {CurrentPrice}", currentPrice);

                await ManageExistingPositionsAsync(currentPrice);

                var activePosition = _orderManagement.GetActivePosition(_tradingConfig.Symbol);
                if (activePosition != null)
                {
                    _logger.LogInformation("Active position already exists for {Symbol}", _tradingConfig.Symbol);
                    return;
                }

                await ConsiderTradingOpportunityAsync(currentPrice, support, resistance);
            }
            else
            {
                _logger.LogInformation("Market is not in sideways movement. Waiting...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during market analysis");
        }
    }

    private async Task ManageExistingPositionsAsync(decimal currentPrice)
    {
        var activePosition = _orderManagement.GetActivePosition(_tradingConfig.Symbol);
        if (activePosition == null) return;

        var pnl = await _orderManagement.GetPositionPnLAsync(_tradingConfig.Symbol);
        var profitPercent = (currentPrice - activePosition.EntryPrice) / activePosition.EntryPrice;

        _logger.LogInformation($"Позиция: Вход = {activePosition.EntryPrice}, Текущая = {currentPrice}, P&L = {pnl:F2} USDT ({profitPercent:P2})");

        // Закрываем позицию только если есть прибыль выше минимального порога
        if (profitPercent >= _tradingConfig.MinProfitPercent)
        {
            _logger.LogInformation($"Закрытие позиции с прибылью {profitPercent:P2}");
            await ClosePositionWithProfitAsync(activePosition, currentPrice);
        }
        else if (profitPercent < 0)
        {
            _logger.LogInformation($"Позиция в убытке {profitPercent:P2}. Ждем роста цены выше {activePosition.EntryPrice}");
        }
        else
        {
            _logger.LogInformation($"Позиция в небольшой прибыли {profitPercent:P2}. Ждем достижения минимального порога {_tradingConfig.MinProfitPercent:P2}");
        }
    }

    private async Task ClosePositionWithProfitAsync(Position position, decimal currentPrice)
    {
        try
        {
            // Размещаем рыночный ордер на продажу
            var sellOrder = await _orderManagement.PlaceMarketOrderAsync(_tradingConfig.Symbol, OrderSide.Sell, position.Quantity);

            if (sellOrder != null && sellOrder.Status == OrderStatus.Filled)
            {
                _logger.LogInformation($"Позиция закрыта с прибылью: {sellOrder.OrderId}");
                
                // Закрываем позицию в системе
                await _orderManagement.ClosePositionAsync(_tradingConfig.Symbol);
                
                var profit = (sellOrder.Price - position.EntryPrice) * position.Quantity;
                _logger.LogInformation($"Реализованная прибыль: {profit:F2} USDT");
            }
            else
            {
                _logger.LogError("Ошибка закрытия позиции");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при закрытии позиции");
        }
    }

    private async Task ConsiderTradingOpportunityAsync(decimal currentPrice, decimal support, decimal resistance)
    {
        // Рассчитываем уровни входа с запасом от экстремумов
        var buyLevel = support * (1 + _tradingConfig.BuyDistanceFromSupport);
        var sellLevel = resistance * (1 - _tradingConfig.SellDistanceFromResistance);

        _logger.LogInformation($"Уровни входа: Покупка = {buyLevel}, Продажа = {sellLevel}");

        // Проверяем возможность покупки около поддержки
        if (currentPrice <= buyLevel)
        {
            _logger.LogInformation($"Возможность покупки около уровня поддержки (цена: {currentPrice}, уровень: {buyLevel})");
            await ExecuteBuyOrderAsync(currentPrice, support, resistance);
        }
        else
        {
            _logger.LogInformation($"Цена {currentPrice} выше уровня покупки {buyLevel}. Ждем снижения");
        }
    }

    private async Task ExecuteBuyOrderAsync(decimal currentPrice, decimal support, decimal resistance)
    {
        try
        {
            // Проверяем баланс
            var balance = await _orderManagement.GetAccountBalanceAsync("USDT");
            if (balance < _tradingConfig.OrderSize)
            {
                _logger.LogWarning($"Недостаточно средств для ордера. Баланс: {balance} USDT, требуется: {_tradingConfig.OrderSize} USDT");
                return;
            }

            // Рассчитываем количество для покупки
            var quantity = await _orderManagement.CalculateOrderQuantityAsync(_tradingConfig.Symbol, _tradingConfig.OrderSize);
            
            if (quantity <= 0)
            {
                _logger.LogError("Не удалось рассчитать количество для ордера");
                return;
            }
            
            _logger.LogInformation($"Размещение ордера на покупку: {quantity:F6} {_tradingConfig.Symbol} по цене {currentPrice}");

            // Размещаем рыночный ордер на покупку
            var buyOrder = await _orderManagement.PlaceMarketOrderAsync(_tradingConfig.Symbol, OrderSide.Buy, quantity);

            if (buyOrder != null && buyOrder.Status == OrderStatus.Filled)
            {
                _logger.LogInformation($"Ордер на покупку исполнен: {buyOrder.OrderId}");

                // Создаем позицию БЕЗ стоп-лосса и тейк-профита
                var position = await _orderManagement.CreatePositionAsync(
                    _tradingConfig.Symbol, 
                    OrderSide.Buy, 
                    buyOrder.Quantity, 
                    buyOrder.Price);

                if (position != null)
                {
                    _logger.LogInformation($"Позиция создана: {position.Symbol}, количество: {position.Quantity}, цена входа: {position.EntryPrice}");
                    _logger.LogInformation("Позиция будет закрыта только при достижении минимальной прибыли");
                }
            }
            else
            {
                _logger.LogError("Ошибка исполнения ордера на покупку");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при исполнении ордера на покупку");
        }
    }

    private KlineInterval GetKlineInterval(int minutes)
    {
        return minutes switch
        {
            1 => KlineInterval.OneMinute,
            5 => KlineInterval.FiveMinutes,
            15 => KlineInterval.FifteenMinutes,
            30 => KlineInterval.ThirtyMinutes,
            60 => KlineInterval.OneHour,
            240 => KlineInterval.FourHour,
            1440 => KlineInterval.OneDay,
            _ => KlineInterval.FifteenMinutes
        };
    }

    public void Dispose()
    {
        _restClient?.Dispose();
        _socketClient?.Dispose();
    }
}