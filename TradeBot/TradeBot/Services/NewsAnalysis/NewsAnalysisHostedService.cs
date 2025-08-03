using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeBot.Models;
using TradeBot.Services.Notifications;

namespace TradeBot.Services.NewsAnalysis;

public class NewsAnalysisHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NewsAnalysisHostedService> _logger;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeSpan _analysisInterval = TimeSpan.FromMinutes(15); // Run every 15 minutes
    private readonly TimeSpan _initialDelay = TimeSpan.FromMinutes(1); // Start after 1 minute

    public NewsAnalysisHostedService(
        IServiceProvider serviceProvider,
        ILogger<NewsAnalysisHostedService> logger,
        INotificationPublisher notificationPublisher)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _notificationPublisher = notificationPublisher;
    } 

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("News Analysis Hosted Service starting...");

        // Wait for initial delay
        // await Task.Delay(_initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformNewsAnalysisAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during news analysis cycle");
                
                await _notificationPublisher.PublishSystemEventAsync(new SystemEvent
                {
                    Type = NotificationType.Error,
                    Message = "News analysis cycle failed",
                    ErrorDetails = ex.Message
                });
            }

            // Wait for next analysis cycle
            await Task.Delay(_analysisInterval, stoppingToken);
        }

        _logger.LogInformation("News Analysis Hosted Service stopping...");
    }

    private async Task PerformNewsAnalysisAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting periodic news analysis...");

        using var scope = _serviceProvider.CreateScope();
        var newsAnalysisService = scope.ServiceProvider.GetRequiredService<INewsAnalysisService>();

        var result = await newsAnalysisService.AnalyzeNewsAsync(cancellationToken);

        if (result.Signal != null)
        {
            _logger.LogInformation("News analysis generated signal: {Direction} with {Strength} strength", 
                result.Signal.Direction, result.Signal.Strength);

            // Publish detailed signal notification
            await _notificationPublisher.PublishTradingEventAsync(new TradingEvent
            {
                Type = NotificationType.MarketAnalysis,
                Symbol = result.Signal.Symbol,
                Data = new Dictionary<string, object>
                {
                    ["SignalDirection"] = result.Signal.Direction.ToString(),
                    ["SignalStrength"] = result.Signal.Strength.ToString(),
                    ["Confidence"] = result.Signal.Confidence,
                    ["Reasoning"] = result.Signal.Reasoning,
                    ["ArticleCount"] = result.Articles.Count,
                    ["SentimentCount"] = result.Sentiments.Count,
                    ["ValidUntil"] = result.Signal.ValidUntil
                },
                Timestamp = result.Signal.GeneratedAt
            });

            // Log detailed analysis results
            LogAnalysisResults(result);
        }
        else
        {
            _logger.LogInformation("News analysis completed - no significant signal generated. Analyzed {ArticleCount} articles", 
                result.Articles.Count);
        }
    }

    private void LogAnalysisResults(NewsAnalysisResult result)
    {
        _logger.LogInformation("=== News Analysis Results ===");
        _logger.LogInformation("Articles analyzed: {Count}", result.Articles.Count);
        _logger.LogInformation("Sentiments analyzed: {Count}", result.Sentiments.Count);

        if (result.Signal != null)
        {
            _logger.LogInformation("Signal generated:");
            _logger.LogInformation("- Direction: {Direction}", result.Signal.Direction);
            _logger.LogInformation("- Strength: {Strength}", result.Signal.Strength);
            _logger.LogInformation("- Confidence: {Confidence:F2}%", result.Signal.Confidence * 100);
            _logger.LogInformation("- Valid until: {ValidUntil}", result.Signal.ValidUntil);
            _logger.LogInformation("- Reasoning: {Reasoning}", result.Signal.Reasoning);
        }

        // Log sentiment breakdown
        var positiveCount = result.Sentiments.Count(s => s.Sentiment == SentimentType.Positive || s.Sentiment == SentimentType.VeryPositive);
        var negativeCount = result.Sentiments.Count(s => s.Sentiment == SentimentType.Negative || s.Sentiment == SentimentType.VeryNegative);
        var neutralCount = result.Sentiments.Count(s => s.Sentiment == SentimentType.Neutral);

        _logger.LogInformation("Sentiment breakdown:");
        _logger.LogInformation("- Positive: {Count}", positiveCount);
        _logger.LogInformation("- Negative: {Count}", negativeCount);
        _logger.LogInformation("- Neutral: {Count}", neutralCount);

        // Log source breakdown
        var sourceGroups = result.Articles.GroupBy(a => a.Source).ToList();
        _logger.LogInformation("Articles by source:");
        foreach (var group in sourceGroups)
        {
            _logger.LogInformation("- {Source}: {Count}", group.Key, group.Count());
        }

        _logger.LogInformation("=== End Analysis Results ===");
    }
} 