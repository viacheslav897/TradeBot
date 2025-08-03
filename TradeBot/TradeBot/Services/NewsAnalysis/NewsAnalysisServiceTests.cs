using Microsoft.Extensions.Logging;
using Moq;
using TradeBot.Models;
using TradeBot.Services.Notifications;
using Xunit;

namespace TradeBot.Services.NewsAnalysis;

public class NewsAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeNewsAsync_WithValidArticles_ShouldGenerateSignal()
    {
        // Arrange
        var mockNewsFetcher = new Mock<INewsFetcherService>();
        var mockSentimentAnalyzer = new Mock<ISentimentAnalysisService>();
        var mockSignalGenerator = new Mock<ISignalGenerationService>();
        var mockNotificationPublisher = new Mock<INotificationPublisher>();
        var mockLogger = new Mock<ILogger<NewsAnalysisService>>();

        var testArticles = new List<NewsArticle>
        {
            new NewsArticle
            {
                Id = "1",
                Title = "Bitcoin Surges to New Highs",
                Content = "Bitcoin has reached new all-time highs as institutional adoption increases.",
                Source = NewsSource.CoinDesk,
                Category = NewsCategory.Bitcoin,
                PublishedAt = DateTime.UtcNow.AddHours(-1),
                Keywords = new List<string> { "bitcoin", "surge", "highs" }
            },
            new NewsArticle
            {
                Id = "2",
                Title = "Major Bank Announces Bitcoin Investment",
                Content = "A major financial institution has announced significant Bitcoin investments.",
                Source = NewsSource.Bloomberg,
                Category = NewsCategory.Economic,
                PublishedAt = DateTime.UtcNow.AddHours(-2),
                Keywords = new List<string> { "bitcoin", "investment", "bank" }
            }
        };

        var testSentiments = new List<SentimentAnalysis>
        {
            new SentimentAnalysis
            {
                ArticleId = "1",
                Sentiment = SentimentType.Positive,
                Confidence = 0.8,
                AnalyzedAt = DateTime.UtcNow
            },
            new SentimentAnalysis
            {
                ArticleId = "2",
                Sentiment = SentimentType.Positive,
                Confidence = 0.7,
                AnalyzedAt = DateTime.UtcNow
            }
        };

        var testSignal = new TradingSignal
        {
            Direction = SignalDirection.Bullish,
            Strength = SignalStrength.Strong,
            Confidence = 0.75,
            Symbol = "BTCUSDT",
            GeneratedAt = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddHours(4),
            Reasoning = "Positive sentiment from institutional adoption news"
        };

        mockNewsFetcher.Setup(x => x.FetchFromAllSourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testArticles);

        mockSentimentAnalyzer.Setup(x => x.AnalyzeArticlesAsync(It.IsAny<List<NewsArticle>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testSentiments);

        mockSignalGenerator.Setup(x => x.GenerateSignalAsync(It.IsAny<List<SentimentAnalysis>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testSignal);

        var service = new NewsAnalysisService(
            mockNewsFetcher.Object,
            mockSentimentAnalyzer.Object,
            mockSignalGenerator.Object,
            mockNotificationPublisher.Object,
            mockLogger.Object);

        // Act
        var result = await service.AnalyzeNewsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Articles.Count);
        Assert.Equal(2, result.Sentiments.Count);
        Assert.NotNull(result.Signal);
        Assert.Equal(SignalDirection.Bullish, result.Signal.Direction);
        Assert.Equal(SignalStrength.Strong, result.Signal.Strength);
        Assert.Equal(0.75, result.Signal.Confidence);

        mockNotificationPublisher.Verify(x => x.PublishTradingEventAsync(It.IsAny<TradingEvent>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeNewsAsync_WithNoArticles_ShouldReturnEmptyResult()
    {
        // Arrange
        var mockNewsFetcher = new Mock<INewsFetcherService>();
        var mockSentimentAnalyzer = new Mock<ISentimentAnalysisService>();
        var mockSignalGenerator = new Mock<ISignalGenerationService>();
        var mockNotificationPublisher = new Mock<INotificationPublisher>();
        var mockLogger = new Mock<ILogger<NewsAnalysisService>>();

        mockNewsFetcher.Setup(x => x.FetchFromAllSourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NewsArticle>());

        var service = new NewsAnalysisService(
            mockNewsFetcher.Object,
            mockSentimentAnalyzer.Object,
            mockSignalGenerator.Object,
            mockNotificationPublisher.Object,
            mockLogger.Object);

        // Act
        var result = await service.AnalyzeNewsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Articles);
        Assert.Empty(result.Sentiments);
        Assert.Null(result.Signal);

        mockSentimentAnalyzer.Verify(x => x.AnalyzeArticlesAsync(It.IsAny<List<NewsArticle>>(), It.IsAny<CancellationToken>()), Times.Never);
        mockSignalGenerator.Verify(x => x.GenerateSignalAsync(It.IsAny<List<SentimentAnalysis>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsSignalValidAsync_WithValidSignal_ShouldReturnTrue()
    {
        // Arrange
        var mockNewsFetcher = new Mock<INewsFetcherService>();
        var mockSentimentAnalyzer = new Mock<ISentimentAnalysisService>();
        var mockSignalGenerator = new Mock<ISignalGenerationService>();
        var mockNotificationPublisher = new Mock<INotificationPublisher>();
        var mockLogger = new Mock<ILogger<NewsAnalysisService>>();

        var service = new NewsAnalysisService(
            mockNewsFetcher.Object,
            mockSentimentAnalyzer.Object,
            mockSignalGenerator.Object,
            mockNotificationPublisher.Object,
            mockLogger.Object);

        var validSignal = new TradingSignal
        {
            Confidence = 0.8,
            ValidUntil = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var isValid = await service.IsSignalValidAsync(validSignal);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task IsSignalValidAsync_WithExpiredSignal_ShouldReturnFalse()
    {
        // Arrange
        var mockNewsFetcher = new Mock<INewsFetcherService>();
        var mockSentimentAnalyzer = new Mock<ISentimentAnalysisService>();
        var mockSignalGenerator = new Mock<ISignalGenerationService>();
        var mockNotificationPublisher = new Mock<INotificationPublisher>();
        var mockLogger = new Mock<ILogger<NewsAnalysisService>>();

        var service = new NewsAnalysisService(
            mockNewsFetcher.Object,
            mockSentimentAnalyzer.Object,
            mockSignalGenerator.Object,
            mockNotificationPublisher.Object,
            mockLogger.Object);

        var expiredSignal = new TradingSignal
        {
            Confidence = 0.8,
            ValidUntil = DateTime.UtcNow.AddHours(-1) // Expired
        };

        // Act
        var isValid = await service.IsSignalValidAsync(expiredSignal);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task IsSignalValidAsync_WithLowConfidence_ShouldReturnFalse()
    {
        // Arrange
        var mockNewsFetcher = new Mock<INewsFetcherService>();
        var mockSentimentAnalyzer = new Mock<ISentimentAnalysisService>();
        var mockSignalGenerator = new Mock<ISignalGenerationService>();
        var mockNotificationPublisher = new Mock<INotificationPublisher>();
        var mockLogger = new Mock<ILogger<NewsAnalysisService>>();

        var service = new NewsAnalysisService(
            mockNewsFetcher.Object,
            mockSentimentAnalyzer.Object,
            mockSignalGenerator.Object,
            mockNotificationPublisher.Object,
            mockLogger.Object);

        var lowConfidenceSignal = new TradingSignal
        {
            Confidence = 0.4, // Below threshold
            ValidUntil = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var isValid = await service.IsSignalValidAsync(lowConfidenceSignal);

        // Assert
        Assert.False(isValid);
    }
} 