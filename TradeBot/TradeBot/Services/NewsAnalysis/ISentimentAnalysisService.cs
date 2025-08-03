using TradeBot.Models;

namespace TradeBot.Services.NewsAnalysis;

public interface ISentimentAnalysisService
{
    Task<SentimentAnalysis> AnalyzeArticleAsync(NewsArticle article, CancellationToken cancellationToken = default);
    Task<List<SentimentAnalysis>> AnalyzeArticlesAsync(List<NewsArticle> articles, CancellationToken cancellationToken = default);
    Task<SentimentType> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default);
    Task<Dictionary<string, double>> AnalyzeAspectsAsync(string text, List<string> aspects, CancellationToken cancellationToken = default);
} 