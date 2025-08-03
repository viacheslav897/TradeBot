namespace TradeBot.Models;

public class NewsAnalysisConfig
{
    public bool IsEnabled { get; set; } = true;
    public int AnalysisIntervalMinutes { get; set; } = 15;
    public int MaxArticlesPerAnalysis { get; set; } = 50;
    public double MinimumSignalConfidence { get; set; } = 0.6;
    public int SignalValidityHours { get; set; } = 4;
    
    public List<NewsSource> EnabledSources { get; set; } = new()
    {
        NewsSource.CoinDesk,
        NewsSource.CoinTelegraph,
        NewsSource.BitcoinCom,
        NewsSource.CryptoNews,
        NewsSource.Reuters,
        NewsSource.Bloomberg
    };
    
    public List<NewsCategory> RelevantCategories { get; set; } = new()
    {
        NewsCategory.Bitcoin,
        NewsCategory.Cryptocurrency,
        NewsCategory.Economic,
        NewsCategory.Regulatory,
        NewsCategory.Political,
        NewsCategory.Market
    };
    
    public Dictionary<string, double> SentimentWeights { get; set; } = new()
    {
        ["bitcoin"] = 1.0,
        ["cryptocurrency"] = 0.8,
        ["regulation"] = 0.6,
        ["economy"] = 0.7,
        ["markets"] = 0.9
    };
    
    public Dictionary<string, string> ApiKeys { get; set; } = new();
    public bool EnableDetailedLogging { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
} 