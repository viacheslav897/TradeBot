using System.Text.Json.Serialization;

namespace TradeBot.Models;

public enum NewsSource
{
    Reuters,
    Bloomberg,
    CoinDesk,
    CoinTelegraph,
    BitcoinCom,
    CryptoNews,
    EconomicTimes,
    FinancialTimes,
    CNBC,
    Forbes,
    Unknown
}

public enum NewsCategory
{
    Cryptocurrency,
    Bitcoin,
    Economic,
    Political,
    Regulatory,
    Technology,
    Market,
    General
}

public enum SentimentType
{
    VeryNegative = -2,
    Negative = -1,
    Neutral = 0,
    Positive = 1,
    VeryPositive = 2
}

public enum SignalStrength
{
    VeryWeak = 1,
    Weak = 2,
    Moderate = 3,
    Strong = 4,
    VeryStrong = 5
}

public enum SignalDirection
{
    Bearish,
    Neutral,
    Bullish
}

public class NewsArticle
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public NewsSource Source { get; set; }
    public NewsCategory Category { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public List<string> Keywords { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SentimentAnalysis
{
    public string ArticleId { get; set; } = string.Empty;
    public SentimentType Sentiment { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> AspectSentiments { get; set; } = new();
    public List<string> KeyPhrases { get; set; } = new();
    public Dictionary<string, object> AnalysisMetadata { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class TradingSignal
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SignalDirection Direction { get; set; }
    public SignalStrength Strength { get; set; }
    public double Confidence { get; set; }
    public string Symbol { get; set; } = "BTCUSDT";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public List<string> ContributingArticleIds { get; set; } = new();
    public Dictionary<string, object> SignalMetadata { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
}

public class NewsAnalysisResult
{
    public List<NewsArticle> Articles { get; set; } = new();
    public List<SentimentAnalysis> Sentiments { get; set; } = new();
    public TradingSignal? Signal { get; set; }
    public Dictionary<string, object> AnalysisMetadata { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class NewsSourceConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int RateLimitPerMinute { get; set; } = 60;
    public List<string> Keywords { get; set; } = new();
    public List<NewsCategory> Categories { get; set; } = new();
} 