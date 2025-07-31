using Binance.Net.Enums;
using Binance.Net.Interfaces;

namespace TradeBot.Services.Trading;

public interface IBinanceTradingService
{
    Task<bool> TestConnectionAsync();
    Task<IEnumerable<IBinanceKline>?> GetKlinesAsync(string symbol, KlineInterval interval, int limit = 100);
    Task AnalyzeMarketAsync();
    void Dispose();
} 