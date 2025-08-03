using System.Text;
using Microsoft.Extensions.Logging;
using TradeBot.Models;

namespace TradeBot.Services.NewsAnalysis;

public class SignalGenerationService : ISignalGenerationService
{
    private readonly ILogger<SignalGenerationService> _logger;
    private readonly Dictionary<string, double> _categoryWeights;

    public SignalGenerationService(ILogger<SignalGenerationService> logger)
    {
        _logger = logger;
        _categoryWeights = InitializeCategoryWeights();
    }

    public async Task<TradingSignal?> GenerateSignalAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!sentiments.Any())
            {
                _logger.LogWarning("No sentiments provided for signal generation");
                return null;
            }

            _logger.LogInformation("Generating trading signal from {Count} sentiment analyses", sentiments.Count);

            // Calculate overall sentiment score
            var overallSentiment = CalculateOverallSentiment(sentiments);
            
            // Determine signal direction
            var direction = await DetermineDirectionAsync(sentiments, cancellationToken);
            
            // Determine signal strength
            var strength = await DetermineStrengthAsync(sentiments, cancellationToken);
            
            // Calculate confidence
            var confidence = await CalculateSignalConfidenceAsync(sentiments, cancellationToken);
            
            // Generate reasoning
            var reasoning = await GenerateReasoningAsync(sentiments, cancellationToken);

            // Only generate signal if confidence is above threshold
            if (confidence < 0.6)
            {
                _logger.LogInformation("Signal confidence too low ({Confidence:F2}%), skipping signal generation", confidence);
                return null;
            }

            var signal = new TradingSignal
            {
                Direction = direction,
                Strength = strength,
                Confidence = confidence,
                Symbol = "BTCUSDT",
                GeneratedAt = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddHours(4), // Signal valid for 4 hours
                ContributingArticleIds = sentiments.Select(s => s.ArticleId).ToList(),
                Reasoning = reasoning
            };

            _logger.LogInformation("Generated {Direction} signal with {Strength} strength and {Confidence:F2}% confidence", 
                direction, strength, confidence * 100);

            return signal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating trading signal");
            return null;
        }
    }

    public async Task<double> CalculateSignalConfidenceAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!sentiments.Any()) return 0.0;

            var totalConfidence = 0.0;
            var weightedConfidence = 0.0;
            var totalWeight = 0.0;

            foreach (var sentiment in sentiments)
            {
                // Weight by sentiment confidence and recency
                var weight = CalculateSentimentWeight(sentiment);
                weightedConfidence += sentiment.Confidence * weight;
                totalWeight += weight;
                totalConfidence += sentiment.Confidence;
            }

            var averageConfidence = totalConfidence / sentiments.Count;
            var weightedConfidenceScore = totalWeight > 0 ? weightedConfidence / totalWeight : 0.0;

            // Consider number of articles analyzed
            var articleCountFactor = Math.Min(1.0, sentiments.Count / 10.0);
            
            // Consider sentiment consistency
            var consistencyFactor = CalculateSentimentConsistency(sentiments);

            var finalConfidence = (averageConfidence * 0.4 + weightedConfidenceScore * 0.4 + 
                                 articleCountFactor * 0.1 + consistencyFactor * 0.1);

            return Math.Min(1.0, Math.Max(0.0, finalConfidence));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating signal confidence");
            return 0.0;
        }
    }

    public async Task<SignalDirection> DetermineDirectionAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default)
    {
        try
        {
            var overallSentiment = CalculateOverallSentiment(sentiments);
            
            // Consider aspect-specific sentiments
            var bitcoinSentiment = CalculateAspectSentiment(sentiments, "bitcoin");
            var cryptoSentiment = CalculateAspectSentiment(sentiments, "cryptocurrency");
            var regulationSentiment = CalculateAspectSentiment(sentiments, "regulation");
            var economySentiment = CalculateAspectSentiment(sentiments, "economy");

            // Weighted decision based on multiple factors
            var bitcoinWeight = 0.4;
            var cryptoWeight = 0.3;
            var regulationWeight = 0.2;
            var economyWeight = 0.1;

            var weightedScore = (overallSentiment * 0.3) + 
                               (bitcoinSentiment * bitcoinWeight) +
                               (cryptoSentiment * cryptoWeight) +
                               (regulationSentiment * regulationWeight) +
                               (economySentiment * economyWeight);

            if (weightedScore > 0.3) return SignalDirection.Bullish;
            if (weightedScore < -0.3) return SignalDirection.Bearish;
            
            return SignalDirection.Neutral;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining signal direction");
            return SignalDirection.Neutral;
        }
    }

    public async Task<SignalStrength> DetermineStrengthAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default)
    {
        try
        {
            var overallSentiment = CalculateOverallSentiment(sentiments);
            var confidence = await CalculateSignalConfidenceAsync(sentiments, cancellationToken);
            var sentimentCount = sentiments.Count;

            // Calculate strength based on multiple factors
            var sentimentStrength = Math.Abs(overallSentiment);
            var confidenceStrength = confidence;
            var volumeStrength = Math.Min(1.0, sentimentCount / 20.0); // More articles = stronger signal

            var combinedStrength = (sentimentStrength * 0.5) + (confidenceStrength * 0.3) + (volumeStrength * 0.2);

            // Map to signal strength enum
            if (combinedStrength > 0.8) return SignalStrength.VeryStrong;
            if (combinedStrength > 0.6) return SignalStrength.Strong;
            if (combinedStrength > 0.4) return SignalStrength.Moderate;
            if (combinedStrength > 0.2) return SignalStrength.Weak;
            
            return SignalStrength.VeryWeak;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining signal strength");
            return SignalStrength.VeryWeak;
        }
    }

    public async Task<string> GenerateReasoningAsync(List<SentimentAnalysis> sentiments, CancellationToken cancellationToken = default)
    {
        try
        {
            var reasoning = new StringBuilder();
            var direction = await DetermineDirectionAsync(sentiments, cancellationToken);
            var strength = await DetermineStrengthAsync(sentiments, cancellationToken);

            reasoning.AppendLine($"Signal Analysis Summary:");
            reasoning.AppendLine($"- Direction: {direction}");
            reasoning.AppendLine($"- Strength: {strength}");
            reasoning.AppendLine($"- Articles Analyzed: {sentiments.Count}");

            // Add sentiment breakdown
            var positiveCount = sentiments.Count(s => s.Sentiment == SentimentType.Positive || s.Sentiment == SentimentType.VeryPositive);
            var negativeCount = sentiments.Count(s => s.Sentiment == SentimentType.Negative || s.Sentiment == SentimentType.VeryNegative);
            var neutralCount = sentiments.Count(s => s.Sentiment == SentimentType.Neutral);

            reasoning.AppendLine($"- Positive Articles: {positiveCount}");
            reasoning.AppendLine($"- Negative Articles: {negativeCount}");
            reasoning.AppendLine($"- Neutral Articles: {neutralCount}");

            // Add key insights
            var keyPhrases = sentiments
                .SelectMany(s => s.KeyPhrases)
                .Distinct()
                .Take(3)
                .ToList();

            if (keyPhrases.Any())
            {
                reasoning.AppendLine($"- Key Insights: {string.Join(", ", keyPhrases)}");
            }

            // Add aspect analysis
            var bitcoinSentiment = CalculateAspectSentiment(sentiments, "bitcoin");
            var regulationSentiment = CalculateAspectSentiment(sentiments, "regulation");
            var economySentiment = CalculateAspectSentiment(sentiments, "economy");

            reasoning.AppendLine($"- Bitcoin Sentiment: {bitcoinSentiment:F2}");
            reasoning.AppendLine($"- Regulatory Sentiment: {regulationSentiment:F2}");
            reasoning.AppendLine($"- Economic Sentiment: {economySentiment:F2}");

            return reasoning.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reasoning");
            return "Unable to generate detailed reasoning due to analysis error.";
        }
    }

    private double CalculateOverallSentiment(List<SentimentAnalysis> sentiments)
    {
        if (!sentiments.Any()) return 0.0;

        var weightedSum = 0.0;
        var totalWeight = 0.0;

        foreach (var sentiment in sentiments)
        {
            var weight = CalculateSentimentWeight(sentiment);
            var sentimentValue = (double)sentiment.Sentiment;
            
            weightedSum += sentimentValue * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 0.0;
    }

    private double CalculateAspectSentiment(List<SentimentAnalysis> sentiments, string aspect)
    {
        var aspectSentiments = sentiments
            .Where(s => s.AspectSentiments.ContainsKey(aspect))
            .ToList();

        if (!aspectSentiments.Any()) return 0.0;

        var weightedSum = 0.0;
        var totalWeight = 0.0;

        foreach (var sentiment in aspectSentiments)
        {
            var weight = CalculateSentimentWeight(sentiment);
            var aspectValue = sentiment.AspectSentiments[aspect];
            
            weightedSum += aspectValue * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 0.0;
    }

    private double CalculateSentimentWeight(SentimentAnalysis sentiment)
    {
        // Weight by confidence and recency
        var confidenceWeight = sentiment.Confidence;
        var recencyWeight = CalculateRecencyWeight(sentiment.AnalyzedAt);
        
        return confidenceWeight * recencyWeight;
    }

    private double CalculateRecencyWeight(DateTime analyzedAt)
    {
        var hoursSinceAnalysis = (DateTime.UtcNow - analyzedAt).TotalHours;
        
        // Exponential decay: newer analyses have higher weight
        return Math.Exp(-hoursSinceAnalysis / 24.0); // 24-hour half-life
    }

    private double CalculateSentimentConsistency(List<SentimentAnalysis> sentiments)
    {
        if (sentiments.Count < 2) return 1.0;

        var sentimentValues = sentiments.Select(s => (double)s.Sentiment).ToList();
        var mean = sentimentValues.Average();
        var variance = sentimentValues.Select(v => Math.Pow(v - mean, 2)).Average();
        var standardDeviation = Math.Sqrt(variance);

        // Lower standard deviation = higher consistency
        var consistency = Math.Max(0.0, 1.0 - (standardDeviation / 4.0)); // Normalize to 0-1
        return consistency;
    }

    private Dictionary<string, double> InitializeCategoryWeights()
    {
        return new Dictionary<string, double>
        {
            ["bitcoin"] = 1.0,
            ["cryptocurrency"] = 0.8,
            ["regulation"] = 0.6,
            ["economy"] = 0.7,
            ["markets"] = 0.9,
            ["technology"] = 0.5,
            ["political"] = 0.4
        };
    }
} 