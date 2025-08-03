using TradeBot.Models;

namespace TradeBot.Services.NewsAnalysis;

public interface INewsFetcherService
{
    Task<List<NewsArticle>> FetchFromSourceAsync(NewsSource source, CancellationToken cancellationToken = default);
    Task<List<NewsArticle>> FetchFromAllSourcesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsSourceAvailableAsync(NewsSource source, CancellationToken cancellationToken = default);
    Task<NewsSourceConfig> GetSourceConfigAsync(NewsSource source);
} 