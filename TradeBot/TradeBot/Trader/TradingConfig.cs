namespace TradeBot.Trader;

public class TradingConfig
{
    public string Symbol { get; set; } = "BTCUSDT";
    public decimal OrderSize { get; set; } = 10m; // размер ордера в USDT
    public int PeriodMinutes { get; set; } = 15; // период для анализа
    public int AnalysisPeriods { get; set; } = 20; // количество периодов для анализа
    public decimal SidewaysThreshold { get; set; } = 0.02m; // порог для определения бокового движения (2%)
    public decimal TakeProfitPercent { get; set; } = 0.5m; // тейк профит 0.5%
    public decimal StopLossPercent { get; set; } = 0.3m; // стоп лосс 0.3%

}