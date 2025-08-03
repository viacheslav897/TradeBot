using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradeBot.Models;

namespace TradeBot.Services.NewsAnalysis;

public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SentimentAnalysisService> _logger;
    private readonly Dictionary<string, double> _sentimentWeights;

    public SentimentAnalysisService(
        HttpClient httpClient,
        ILogger<SentimentAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _sentimentWeights = InitializeSentimentWeights();
    }

    public async Task<SentimentAnalysis> AnalyzeArticleAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing sentiment for article: {Title}", article.Title);

            var textToAnalyze = $"{article.Title}. {article.Content}";
            
            // Analyze overall sentiment
            var sentiment = await AnalyzeTextAsync(textToAnalyze, cancellationToken);
            
            // Analyze specific aspects
            var aspects = new[] { "bitcoin", "cryptocurrency", "regulation", "economy", "markets" };
            var aspectSentiments = await AnalyzeAspectsAsync(textToAnalyze, aspects.ToList(), cancellationToken);
            
            // Extract key phrases
            var keyPhrases = ExtractKeyPhrases(textToAnalyze);
            
            // Calculate confidence based on text length and keyword presence
            var confidence = CalculateConfidence(textToAnalyze, article.Keywords);

            var analysis = new SentimentAnalysis
            {
                ArticleId = article.Id,
                Sentiment = sentiment,
                Confidence = confidence,
                AspectSentiments = aspectSentiments,
                KeyPhrases = keyPhrases,
                AnalyzedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Sentiment analysis completed: {Sentiment} with {Confidence:F2}% confidence", 
                sentiment, confidence * 100);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment for article: {ArticleId}", article.Id);
            return new SentimentAnalysis
            {
                ArticleId = article.Id,
                Sentiment = SentimentType.Neutral,
                Confidence = 0.0,
                AnalyzedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<List<SentimentAnalysis>> AnalyzeArticlesAsync(List<NewsArticle> articles, CancellationToken cancellationToken = default)
    {
        var analyses = new List<SentimentAnalysis>();
        var tasks = new List<Task<SentimentAnalysis>>();

        foreach (var article in articles)
        {
            tasks.Add(AnalyzeArticleAsync(article, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        analyses.AddRange(results);

        _logger.LogInformation("Completed sentiment analysis for {Count} articles", analyses.Count);
        return analyses;
    }

    public async Task<SentimentType> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // For production, you would integrate with an AI service like OpenAI, Azure Cognitive Services, etc.
            // For now, we'll use a rule-based approach with keyword analysis
            
            var lowerText = text.ToLower();
            var positiveScore = 0.0;
            var negativeScore = 0.0;

            // Positive keywords
            var positiveKeywords = new[]
            {
                "bullish", "surge", "rally", "gain", "up", "positive", "adoption", "institutional",
                "approval", "launch", "partnership", "investment", "growth", "innovation", "breakthrough"
            };

            // Negative keywords
            var negativeKeywords = new[]
            {
                "bearish", "crash", "drop", "decline", "down", "negative", "ban", "regulation",
                "hack", "scam", "fraud", "bubble", "correction", "sell-off", "panic"
            };

            // Calculate scores
            foreach (var keyword in positiveKeywords)
            {
                var count = CountOccurrences(lowerText, keyword);
                positiveScore += count * _sentimentWeights.GetValueOrDefault(keyword, 1.0);
            }

            foreach (var keyword in negativeKeywords)
            {
                var count = CountOccurrences(lowerText, keyword);
                negativeScore += count * _sentimentWeights.GetValueOrDefault(keyword, 1.0);
            }

            // Determine sentiment based on scores
            var netScore = positiveScore - negativeScore;
            
            if (netScore > 2) return SentimentType.VeryPositive;
            if (netScore > 0.5) return SentimentType.Positive;
            if (netScore < -2) return SentimentType.VeryNegative;
            if (netScore < -0.5) return SentimentType.Negative;
            
            return SentimentType.Neutral;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing text sentiment");
            return SentimentType.Neutral;
        }
    }

    public async Task<Dictionary<string, double>> AnalyzeAspectsAsync(string text, List<string> aspects, CancellationToken cancellationToken = default)
    {
        var aspectSentiments = new Dictionary<string, double>();
        var lowerText = text.ToLower();

        foreach (var aspect in aspects)
        {
            var aspectScore = 0.0;
            var aspectKeywords = GetAspectKeywords(aspect);
            
            foreach (var keyword in aspectKeywords)
            {
                var count = CountOccurrences(lowerText, keyword);
                aspectScore += count * _sentimentWeights.GetValueOrDefault(keyword, 1.0);
            }

            // Normalize score to -1 to 1 range
            aspectSentiments[aspect] = Math.Max(-1.0, Math.Min(1.0, aspectScore / 5.0));
        }

        return aspectSentiments;
    }

    private List<string> ExtractKeyPhrases(string text)
    {
        var phrases = new List<string>();
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var sentence in sentences.Take(3)) // Take first 3 sentences
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 3 && words.Length <= 8)
            {
                phrases.Add(sentence.Trim());
            }
        }

        return phrases.Take(5).ToList(); // Return max 5 phrases
    }

    private double CalculateConfidence(string text, List<string> keywords)
    {
        var baseConfidence = 0.5;
        
        // Increase confidence based on text length
        if (text.Length > 500) baseConfidence += 0.2;
        else if (text.Length > 200) baseConfidence += 0.1;
        
        // Increase confidence based on keyword presence
        var keywordMatches = keywords.Count(k => text.ToLower().Contains(k.ToLower()));
        baseConfidence += Math.Min(0.3, keywordMatches * 0.1);
        
        // Increase confidence based on source reliability
        baseConfidence += 0.1;
        
        return Math.Min(1.0, baseConfidence);
    }

    private int CountOccurrences(string text, string keyword)
    {
        return text.Split(new[] { keyword }, StringSplitOptions.None).Length - 1;
    }

    private List<string> GetAspectKeywords(string aspect)
    {
        return aspect.ToLower() switch
        {
            "bitcoin" => new[] { "bitcoin", "btc", "satoshi", "halving", "mining" }.ToList(),
            "cryptocurrency" => new[] { "crypto", "cryptocurrency", "digital", "token", "coin" }.ToList(),
            "regulation" => new[] { "regulation", "regulatory", "law", "compliance", "ban", "legal" }.ToList(),
            "economy" => new[] { "economy", "economic", "inflation", "interest", "rate", "fed", "sanctions" }.ToList(),
            "markets" => new[] { "market", "trading", "price", "volume", "exchange" }.ToList(),
            _ => new[] { aspect }.ToList()
        };
    }

    private Dictionary<string, double> InitializeSentimentWeights()
    {
        return new Dictionary<string, double>
        {
            // Strong positive keywords
            ["bullish"] = 2.0,
            ["surge"] = 2.0,
            ["rally"] = 2.0,
            ["adoption"] = 1.5,
            ["institutional"] = 1.5,
            ["approval"] = 1.5,
            
            // Moderate positive keywords
            ["gain"] = 1.0,
            ["up"] = 1.0,
            ["positive"] = 1.0,
            ["growth"] = 1.0,
            ["innovation"] = 1.0,
            
            // Strong negative keywords
            ["bearish"] = -2.0,
            ["crash"] = -2.0,
            ["ban"] = -2.0,
            ["hack"] = -1.5,
            ["scam"] = -1.5,
            ["fraud"] = -1.5,
            
            // Moderate negative keywords
            ["drop"] = -1.0,
            ["decline"] = -1.0,
            ["down"] = -1.0,
            ["negative"] = -1.0,
            ["regulation"] = -0.5,
            ["correction"] = -0.5
        };
    }
} 