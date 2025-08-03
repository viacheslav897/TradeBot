using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Services.Notifications;

namespace TradeBot.Services.NewsAnalysis;

public class NewsAnalysisService : INewsAnalysisService
{
    private readonly INewsFetcherService _newsFetcher;
    private readonly ISentimentAnalysisService _sentimentAnalyzer;
    private readonly ISignalGenerationService _signalGenerator;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<NewsAnalysisService> _logger;

    public NewsAnalysisService(
        INewsFetcherService newsFetcher,
        ISentimentAnalysisService sentimentAnalyzer,
        ISignalGenerationService signalGenerator,
        INotificationPublisher notificationPublisher,
        ILogger<NewsAnalysisService> logger)
    {
        _newsFetcher = newsFetcher;
        _sentimentAnalyzer = sentimentAnalyzer;
        _signalGenerator = signalGenerator;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<NewsAnalysisResult> AnalyzeNewsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting news analysis...");

            // Step 1: Fetch latest news
            var articles = await FetchLatestNewsAsync(cancellationToken);
            _logger.LogInformation("Fetched {ArticleCount} articles", articles.Count);

            if (!articles.Any())
            {
                _logger.LogWarning("No articles found for analysis");
                return new NewsAnalysisResult();
            }

            // Step 2: Analyze sentiment
            var sentiments = await AnalyzeSentimentAsync(articles, cancellationToken);
            _logger.LogInformation("Analyzed sentiment for {SentimentCount} articles", sentiments.Count);

            // Step 3: Generate trading signal
            var signal = await GenerateTradingSignalAsync(sentiments, cancellationToken);

            var result = new NewsAnalysisResult
            {
                Articles = articles,
                Sentiments = sentiments,
                Signal = signal,
                AnalyzedAt = DateTime.UtcNow
            };

            // Step 4: Publish notification if signal is generated
            if (signal != null)
            {
                await _notificationPublisher.PublishTradingEventAsync(new TradingEvent
                {
                    Type = NotificationType.MarketAnalysis,
                    Symbol = signal.Symbol,
                    Data = new Dictionary<string, object>
                    {
                        ["SignalDirection"] = signal.Direction.ToString(),
                        ["SignalStrength"] = signal.Strength.ToString(),
                        ["Confidence"] = signal.Confidence,
                        ["Reasoning"] = signal.Reasoning,
                        ["ArticleCount"] = articles.Count
                    },
                    Timestamp = signal.GeneratedAt
                });

                _logger.LogInformation("Generated {Direction} signal with {Strength} strength and {Confidence:F2}% confidence", 
                    signal.Direction, signal.Strength, signal.Confidence * 100);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during news analysis");
            await _notificationPublisher.PublishSystemEventAsync(new SystemEvent
            {
                Type = NotificationType.Error,
                Message = "News analysis failed",
                ErrorDetails = ex.Message
            });
            throw;
        }
    }

    public async Task<List<NewsArticle>> FetchLatestNewsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _newsFetcher.FetchFromAllSourcesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching news");
            throw;
        }
    }

    public async Task<List<SentimentAnalysis>> AnalyzeSentimentAsync(List<NewsArticle> articles, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _sentimentAnalyzer.AnalyzeArticlesAsync(articles, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            throw;
        }
    }

    public async Task<TradingSignal?> GenerateTradingSignalAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!sentiments.Any())
            {
                _logger.LogWarning("No sentiments available for signal generation");
                return null;
            }

            return await _signalGenerator.GenerateSignalAsync(sentiments, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating trading signal");
            throw;
        }
    }

    public async Task<bool> IsSignalValidAsync(TradingSignal signal, CancellationToken cancellationToken = default)
    {
        if (signal == null) return false;

        // Check if signal is still valid (not expired)
        if (DateTime.UtcNow > signal.ValidUntil)
        {
            _logger.LogInformation("Signal {SignalId} has expired", signal.Id);
            return false;
        }

        // Check if signal has minimum confidence
        if (signal.Confidence < 0.6)
        {
            _logger.LogInformation("Signal {SignalId} has low confidence: {Confidence:F2}", signal.Id, signal.Confidence);
            return false;
        }

        return true;
    }

    public async Task<List<NewsArticle>> GetRelevantArticlesAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            // This would typically query a database or cache
            // For now, we'll fetch fresh articles and filter by date
            var articles = await FetchLatestNewsAsync(cancellationToken);
            return articles.Where(a => a.PublishedAt >= from && a.PublishedAt <= to).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting relevant articles");
            throw;
        }
    }
} 