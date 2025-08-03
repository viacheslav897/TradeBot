using TradeBot.Models;

namespace TradeBot.Services.NewsAnalysis;

public interface ISignalGenerationService
{
    Task<TradingSignal?> GenerateSignalAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default);
    Task<double> CalculateSignalConfidenceAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default);
    Task<SignalDirection> DetermineDirectionAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default);
    Task<SignalStrength> DetermineStrengthAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default);
    Task<string> GenerateReasoningAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default);
} 