namespace TradeBot.Services;

public static class TradingConstants
{
    public static class Defaults
    {
        public const string DefaultAsset = "USDT";
        public const int DefaultAnalysisPeriods = 20;
        public const int DefaultKlineLimit = 100;
        public const decimal DefaultMockPrice = 50000m;
        public const decimal DefaultMockBalance = 10000m;
        public const decimal DefaultProfitPercent = 0.02m; // 2%
    }

    public static class Logging
    {
        public const string MarketAnalysis = "Market Analysis";
        const string OrderPlacement = "Order Placement";
        const string PositionManagement = "Position Management";
    }

    public static class ErrorMessages
    {
        public const string InsufficientData = "Insufficient data for analysis";
        public const string ConnectionFailed = "Failed to connect to Binance";
        public const string OrderPlacementFailed = "Order placement failed";
        public const string PositionNotFound = "Position not found";
        public const string InsufficientBalance = "Insufficient balance for order";
    }
} 