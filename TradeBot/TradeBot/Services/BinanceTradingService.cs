using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Trader;

namespace TradeBot.Services;

public class BinanceTradingService
{
    private readonly BinanceRestClient _restClient;
    private readonly BinanceSocketClient _socketClient;
    private readonly SidewaysDetectionService _sidewaysDetection;
    private readonly OrderManagementService _orderManagement;
    private readonly ILogger<BinanceTradingService> _logger;
    private readonly TradingConfig _tradingConfig;

    public BinanceTradingService(
        BinanceConfig binanceConfig,
        TradingConfig tradingConfig,
        SidewaysDetectionService sidewaysDetection,
        OrderManagementService orderManagement,
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
                _logger.LogInformation($"Подключение к Binance успешно. Время сервера: {result.Data}");
                return true;
            }
            else
            {
                _logger.LogError($"Ошибка подключения к Binance: {result.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при тестировании подключения к Binance");
            return false;
        }
    }

    public async Task<IEnumerable<IBinanceKline>?> GetKlinesAsync(string symbol, KlineInterval interval,
        int limit = 100)
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
                _logger.LogError($"Ошибка получения данных свечей: {result.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при получении данных свечей");
            return null;
        }
    }

    public async Task AnalyzeMarketAsync()
    {
        try
        {
            _logger.LogInformation($"Анализ рынка для {_tradingConfig.Symbol}...");

            // Получаем данные свечей
            var interval = GetKlineInterval(_tradingConfig.PeriodMinutes);
            var klines = await GetKlinesAsync(_tradingConfig.Symbol, interval, _tradingConfig.AnalysisPeriods + 10);

            if (klines == null || !klines.Any())
            {
                _logger.LogWarning("Не удалось получить данные для анализа");
                return;
            }

            // Проверяем, находится ли рынок в боковике
            var isSideways = _sidewaysDetection.IsSidewaysMarket(klines);

            if (isSideways)
            {
                _logger.LogInformation("Рынок находится в боковом движении!");

                var (resistance, support) = _sidewaysDetection.GetSupportResistanceLevels(klines);
                _logger.LogInformation($"Уровни: Сопротивление = {resistance}, Поддержка = {support}");

                var currentPrice = klines.Last().ClosePrice;
                _logger.LogInformation($"Текущая цена: {currentPrice}");

                // Проверяем, есть ли уже активная позиция
                var activePosition = _orderManagement.GetActivePosition(_tradingConfig.Symbol);
                if (activePosition != null)
                {
                    _logger.LogInformation($"Активная позиция уже существует для {_tradingConfig.Symbol}");
                    return;
                }

                // Анализируем торговые возможности
                await ConsiderTradingOpportunityAsync(currentPrice, support, resistance);
            }
            else
            {
                _logger.LogInformation("Рынок не находится в боковом движении. Ожидаем...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе рынка");
        }
    }

    private async Task ConsiderTradingOpportunityAsync(decimal currentPrice, decimal support, decimal resistance)
    {
        var distanceToSupport = (currentPrice - support) / support;
        var distanceToResistance = (resistance - currentPrice) / currentPrice;

        _logger.LogInformation(
            $"Расстояние до поддержки: {distanceToSupport:P2}, до сопротивления: {distanceToResistance:P2}");

        // Простая логика: покупаем около поддержки, продаем около сопротивления
        var buyThreshold = 0.01m; // 1% от поддержки
        var sellThreshold = 0.01m; // 1% от сопротивления

        if (distanceToSupport <= buyThreshold)
        {
            _logger.LogInformation("Возможность покупки около уровня поддержки");
            await ExecuteBuyOrderAsync(currentPrice, support, resistance);
        }
        else if (distanceToResistance <= sellThreshold)
        {
            _logger.LogInformation("Возможность продажи около уровня сопротивления");
            await ExecuteSellOrderAsync(currentPrice, support, resistance);
        }
        else
        {
            _logger.LogInformation("Цена находится в середине диапазона, ждем лучшей возможности");
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

                // Создаем позицию
                var position = await _orderManagement.CreatePositionAsync(
                    _tradingConfig.Symbol, 
                    OrderSide.Buy, 
                    buyOrder.Quantity, 
                    buyOrder.Price);

                if (position != null)
                {
                    // Размещаем стоп-лосс ордер
                    var stopLossOrder = await _orderManagement.PlaceStopLossOrderAsync(
                        _tradingConfig.Symbol, 
                        position.Quantity, 
                        position.StopLossPrice!.Value);

                    if (stopLossOrder != null)
                    {
                        position.StopLossOrderId = stopLossOrder.OrderId;
                        _logger.LogInformation($"Стоп-лосс ордер размещен: {stopLossOrder.OrderId} по цене {position.StopLossPrice}");
                    }

                    // Размещаем тейк-профит ордер
                    var takeProfitOrder = await _orderManagement.PlaceTakeProfitOrderAsync(
                        _tradingConfig.Symbol, 
                        position.Quantity, 
                        position.TakeProfitPrice!.Value);

                    if (takeProfitOrder != null)
                    {
                        position.TakeProfitOrderId = takeProfitOrder.OrderId;
                        _logger.LogInformation($"Тейк-профит ордер размещен: {takeProfitOrder.OrderId} по цене {position.TakeProfitPrice}");
                    }
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

    private async Task ExecuteSellOrderAsync(decimal currentPrice, decimal support, decimal resistance)
    {
        try
        {
            // Для продажи нам нужна позиция, которую мы продаем
            // В данном случае мы продаем короткую позицию (если поддерживается)
            // Или просто логируем возможность продажи
            
            _logger.LogInformation($"Обнаружена возможность продажи около сопротивления {resistance}");
            
            // Здесь можно добавить логику для коротких позиций
            // Пока просто логируем возможность
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Исключение при исполнении ордера на продажу");
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