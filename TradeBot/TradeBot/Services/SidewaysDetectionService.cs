using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeBot.Trader;

namespace TradeBot.Services;

public class SidewaysDetectionService
{
    private readonly ILogger<SidewaysDetectionService> _logger;
    private readonly TradingConfig _config;

    public SidewaysDetectionService(ILogger<SidewaysDetectionService> logger, TradingConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public bool IsSidewaysMarket(IEnumerable<IBinanceKline> klines)
    {
        var klinesArray = klines.ToArray();
        if (klinesArray.Length < _config.AnalysisPeriods)
        {
            _logger.LogWarning("Недостаточно данных для анализа");
            return false;
        }

        // Берем последние N периодов
        var recentKlines = klinesArray.TakeLast(_config.AnalysisPeriods).ToArray();

        // Находим максимум и минимум за период
        var highestHigh = recentKlines.Max(k => k.HighPrice);
        var lowestLow = recentKlines.Min(k => k.LowPrice);

        // Вычисляем диапазон в процентах
        var range = (highestHigh - lowestLow) / lowestLow;

        _logger.LogInformation($"Диапазон цены за {_config.AnalysisPeriods} периодов: {range:P2}");

        // Проверяем дополнительные условия для бокового движения
        var isInRange = range <= _config.SidewaysThreshold;
        var hasMultipleTouches = HasMultipleTouchesOfLevels(recentKlines, highestHigh, lowestLow);
        var isNotTrending = !IsStrongTrend(recentKlines);

        var isSideways = isInRange && hasMultipleTouches && isNotTrending;

        _logger.LogInformation(
            $"Боковое движение: {isSideways} (Range: {isInRange}, Touches: {hasMultipleTouches}, NotTrending: {isNotTrending})");

        return isSideways;
    }

    private bool HasMultipleTouchesOfLevels(IBinanceKline[] klines, decimal resistance, decimal support)
    {
        var tolerance = (resistance - support) * 0.1m; // 10% толерантность

        var resistanceTouches = klines.Count(k => Math.Abs(k.HighPrice - resistance) <= tolerance);
        var supportTouches = klines.Count(k => Math.Abs(k.LowPrice - support) <= tolerance);

        // Должно быть минимум 2 касания каждого уровня
        return resistanceTouches >= 2 && supportTouches >= 2;
    }

    private bool IsStrongTrend(IBinanceKline[] klines)
    {
        // Простой алгоритм определения сильного тренда
        // Сравниваем первую и последнюю цену закрытия
        var firstClose = klines.First().ClosePrice;
        var lastClose = klines.Last().ClosePrice;

        var priceChange = Math.Abs(lastClose - firstClose) / firstClose;

        // Если изменение больше половины от порога бокового движения, считаем трендом
        return priceChange > (_config.SidewaysThreshold / 2);
    }

    public (decimal resistance, decimal support) GetSupportResistanceLevels(IEnumerable<IBinanceKline> klines)
    {
        var klinesArray = klines.TakeLast(_config.AnalysisPeriods).ToArray();

        var resistance = klinesArray.Max(k => k.HighPrice);
        var support = klinesArray.Min(k => k.LowPrice);

        return (resistance, support);
    }
}