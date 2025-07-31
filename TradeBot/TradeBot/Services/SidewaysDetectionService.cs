using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using TradeBot.Trader;

namespace TradeBot.Services;

public class SidewaysDetectionService : ISidewaysDetectionService
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
            _logger.LogWarning(TradingConstants.ErrorMessages.InsufficientData);
            return false;
        }

        var recentKlines = klinesArray.TakeLast(_config.AnalysisPeriods).ToArray();

        var highestHigh = recentKlines.Max(k => k.HighPrice);
        var lowestLow = recentKlines.Min(k => k.LowPrice);

        var range = (highestHigh - lowestLow) / lowestLow;

        _logger.LogInformation("Price range over {Periods} periods: {Range:P2}", _config.AnalysisPeriods, range);

        var isInRange = range <= _config.SidewaysThreshold;
        var hasMultipleTouches = HasMultipleTouchesOfLevels(recentKlines, highestHigh, lowestLow);
        var isNotTrending = !IsStrongTrend(recentKlines);

        var isSideways = isInRange && hasMultipleTouches && isNotTrending;

        _logger.LogInformation(
            "Sideways movement: {IsSideways} (Range: {IsInRange}, Touches: {HasMultipleTouches}, NotTrending: {IsNotTrending})",
            isSideways, isInRange, hasMultipleTouches, isNotTrending);

        return isSideways;
    }

    private bool HasMultipleTouchesOfLevels(IBinanceKline[] klines, decimal resistance, decimal support)
    {
        const decimal tolerancePercent = 0.1m; // 10% tolerance
        var tolerance = (resistance - support) * tolerancePercent;

        var resistanceTouches = klines.Count(k => Math.Abs(k.HighPrice - resistance) <= tolerance);
        var supportTouches = klines.Count(k => Math.Abs(k.LowPrice - support) <= tolerance);

        const int minTouches = 2;
        return resistanceTouches >= minTouches && supportTouches >= minTouches;
    }

    private bool IsStrongTrend(IBinanceKline[] klines)
    {
        var firstClose = klines.First().ClosePrice;
        var lastClose = klines.Last().ClosePrice;

        var priceChange = Math.Abs(lastClose - firstClose) / firstClose;

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