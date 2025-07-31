namespace TradeBot.Trader;

public class TradingConfig
{
    public string Symbol { get; set; } = "BTCUSDT";
    public decimal OrderSize { get; set; } = 10m; // размер ордера в USDT
    public int PeriodMinutes { get; set; } = 15; // период для анализа
    public int AnalysisPeriods { get; set; } = 20; // количество периодов для анализа
    public decimal SidewaysThreshold { get; set; } = 0.02m; // порог для определения бокового движения (2%)
    
    // Параметры для стратегии торговли в боковике
    public decimal BuyDistanceFromSupport { get; set; } = 0.005m; // расстояние от поддержки для покупки (0.5%)
    public decimal SellDistanceFromResistance { get; set; } = 0.005m; // расстояние от сопротивления для продажи (0.5%)
    public decimal MinProfitPercent { get; set; } = 0.003m; // минимальная прибыль для закрытия позиции (0.3%)
    public int MaxPositionHoldHours { get; set; } = 24; // максимальное время удержания позиции в часах
    
    // Устаревшие параметры - больше не используются
    [Obsolete("Используйте MinProfitPercent вместо TakeProfitPercent")]
    public decimal TakeProfitPercent { get; set; } = 0.5m;
    [Obsolete("Стоп-лосс не используется в данной стратегии")]
    public decimal StopLossPercent { get; set; } = 0.3m;
}