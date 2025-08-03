using TradeBot.Models;

namespace TradeBot.Services.NewsAnalysis;

public interface INewsAnalysisService
{
    Task<NewsAnalysisResult> AnalyzeNewsAsync(CancellationToken cancellationToken = default);
    Task<List<NewsArticle>> FetchLatestNewsAsync(CancellationToken cancellationToken = default);
    Task<List<SentimentAnalysis>> AnalyzeSentimentAsync(List<NewsArticle> articles, CancellationToken cancellationToken = default);
    Task<TradingSignal?> GenerateTradingSignalAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default);
    Task<bool> IsSignalValidAsync(TradingSignal signal, CancellationToken cancellationToken = default);
    Task<List<NewsArticle>> GetRelevantArticlesAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
} 