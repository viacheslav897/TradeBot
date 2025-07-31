using Binance.Net.Interfaces;

namespace TradeBot.Services.Analysis;

public interface ISidewaysDetectionService
{
    bool IsSidewaysMarket(IEnumerable<IBinanceKline> klines);
    (decimal resistance, decimal support) GetSupportResistanceLevels(IEnumerable<IBinanceKline> klines);
} 