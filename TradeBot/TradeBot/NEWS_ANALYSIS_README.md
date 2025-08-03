# News Analysis Service

The News Analysis Service is a comprehensive AI-powered system that analyzes cryptocurrency and economic news to generate Bitcoin trading signals. It fetches news from multiple sources, performs sentiment analysis, and generates trading signals based on the aggregated sentiment data.

## Features

### 🔍 Multi-Source News Fetching
- **Cryptocurrency Sources**: CoinDesk, CoinTelegraph, Bitcoin.com, CryptoNews
- **Economic Sources**: Reuters, Bloomberg
- **RSS Feed Support**: Automatic parsing of RSS feeds
- **Duplicate Detection**: Removes duplicate articles based on title similarity
- **Rate Limiting**: Configurable rate limits per source

### 🤖 AI-Powered Sentiment Analysis
- **Keyword-Based Analysis**: Rule-based sentiment analysis with weighted keywords
- **Aspect-Specific Analysis**: Analyzes sentiment for specific aspects (Bitcoin, regulation, economy, etc.)
- **Confidence Scoring**: Calculates confidence based on text length and keyword presence
- **Key Phrase Extraction**: Extracts important phrases from articles

### 📊 Trading Signal Generation
- **Multi-Factor Analysis**: Combines overall sentiment with aspect-specific sentiments
- **Signal Strength**: Determines signal strength based on sentiment intensity and confidence
- **Signal Direction**: Bullish, Bearish, or Neutral based on weighted sentiment scores
- **Reasoning Generation**: Provides detailed reasoning for each signal
- **Signal Validation**: Ensures signals meet minimum confidence thresholds

### ⚙️ Configuration
- **Flexible Sources**: Enable/disable specific news sources
- **Custom Categories**: Configure relevant news categories
- **Sentiment Weights**: Adjust weights for different aspects
- **Analysis Intervals**: Configurable analysis frequency (default: 15 minutes)
- **Signal Thresholds**: Customizable confidence and validity thresholds

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  News Sources   │───▶│  News Fetcher    │───▶│  News Articles  │
│  (RSS/APIs)     │    │  Service         │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Trading        │◀───│  Signal          │◀───│  Sentiment      │
│  Signals        │    │  Generator       │    │  Analysis       │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Services

### 1. NewsFetcherService
- Fetches news from multiple sources
- Parses RSS feeds
- Removes duplicates
- Extracts keywords
- Handles rate limiting

### 2. SentimentAnalysisService
- Analyzes article sentiment using keyword-based approach
- Performs aspect-specific sentiment analysis
- Calculates confidence scores
- Extracts key phrases

### 3. SignalGenerationService
- Generates trading signals from sentiment data
- Determines signal direction and strength
- Calculates signal confidence
- Generates detailed reasoning

### 4. NewsAnalysisService
- Orchestrates the entire analysis process
- Manages signal validation
- Integrates with notification system
- Provides high-level API

### 5. NewsAnalysisHostedService
- Runs periodic analysis (every 15 minutes by default)
- Publishes notifications for generated signals
- Handles errors and logging
- Integrates with the trading bot

## Configuration

### appsettings.json
```json
{
  "NewsAnalysis": {
    "IsEnabled": true,
    "AnalysisIntervalMinutes": 15,
    "MaxArticlesPerAnalysis": 50,
    "MinimumSignalConfidence": 0.6,
    "SignalValidityHours": 4,
    "EnableDetailedLogging": true,
    "EnableNotifications": true,
    "EnabledSources": [
      "CoinDesk",
      "CoinTelegraph",
      "BitcoinCom",
      "CryptoNews",
      "Reuters",
      "Bloomberg"
    ],
    "RelevantCategories": [
      "Bitcoin",
      "Cryptocurrency",
      "Economic",
      "Regulatory",
      "Political",
      "Market"
    ],
    "SentimentWeights": {
      "bitcoin": 1.0,
      "cryptocurrency": 0.8,
      "regulation": 0.6,
      "economy": 0.7,
      "markets": 0.9
    }
  }
}
```

## Models

### NewsArticle
- Contains article metadata (title, content, source, etc.)
- Includes keywords and categories
- Tracks publication and retrieval times

### SentimentAnalysis
- Stores sentiment analysis results
- Includes confidence scores and aspect sentiments
- Contains key phrases and analysis metadata

### TradingSignal
- Represents generated trading signals
- Includes direction, strength, and confidence
- Contains reasoning and validity period
- Lists contributing article IDs

## Usage

### Basic Usage
```csharp
// Get the news analysis service
var newsAnalysisService = serviceProvider.GetRequiredService<INewsAnalysisService>();

// Perform analysis
var result = await newsAnalysisService.AnalyzeNewsAsync();

if (result.Signal != null)
{
    Console.WriteLine($"Signal: {result.Signal.Direction} with {result.Signal.Strength} strength");
    Console.WriteLine($"Confidence: {result.Signal.Confidence:P}");
    Console.WriteLine($"Reasoning: {result.Signal.Reasoning}");
}
```

### Manual Analysis
```csharp
// Fetch news from specific source
var articles = await newsFetcher.FetchFromSourceAsync(NewsSource.CoinDesk);

// Analyze sentiment
var sentiments = await sentimentAnalyzer.AnalyzeArticlesAsync(articles);

// Generate signal
var signal = await signalGenerator.GenerateSignalAsync(sentiments);
```

## Integration with Trading Bot

The news analysis service integrates seamlessly with the existing trading bot:

1. **Automatic Analysis**: Runs every 15 minutes automatically
2. **Signal Notifications**: Publishes notifications when signals are generated
3. **Trading Integration**: Signals can be used to influence trading decisions
4. **Logging**: Comprehensive logging for monitoring and debugging

## Notifications

The service publishes various notification types:

- **NewsAnalysis**: When analysis cycle completes
- **SignalGenerated**: When a new trading signal is generated
- **SentimentAnalysis**: When sentiment analysis completes

## Error Handling

- **Source Failures**: Individual source failures don't stop the entire analysis
- **Network Issues**: Automatic retry with exponential backoff
- **Invalid Data**: Graceful handling of malformed RSS feeds
- **Rate Limiting**: Respects rate limits to avoid being blocked

## Performance Considerations

- **Parallel Processing**: News fetching and sentiment analysis run in parallel
- **Caching**: Consider implementing caching for frequently accessed data
- **Database Storage**: Articles and sentiments can be stored for historical analysis
- **Memory Management**: Large article collections are processed in batches

## Future Enhancements

1. **Machine Learning**: Replace rule-based sentiment analysis with ML models
2. **Real-time Processing**: Stream processing for immediate signal generation
3. **Advanced NLP**: Use more sophisticated NLP techniques for better sentiment analysis
4. **Historical Analysis**: Analyze historical news patterns for better predictions
5. **API Integration**: Integrate with paid news APIs for better data quality
6. **Custom Sources**: Allow users to add custom RSS feeds
7. **Signal Backtesting**: Test signal accuracy against historical data

## Testing

The service includes comprehensive unit tests:

```bash
# Run tests
dotnet test --filter "NewsAnalysis"
```

## Monitoring

Monitor the service through:

1. **Logs**: Check application logs for analysis results
2. **Notifications**: Telegram notifications for signals
3. **Metrics**: Consider adding metrics for signal accuracy
4. **Health Checks**: Monitor service health and availability

## Security Considerations

1. **API Keys**: Store API keys securely using user secrets
2. **Rate Limiting**: Respect rate limits to avoid being blocked
3. **Data Privacy**: Ensure compliance with data privacy regulations
4. **Input Validation**: Validate all external data before processing

## Troubleshooting

### Common Issues

1. **No Articles Fetched**: Check network connectivity and source availability
2. **Low Signal Confidence**: Adjust sentiment weights or add more sources
3. **Service Not Starting**: Verify configuration and dependencies
4. **Memory Issues**: Reduce MaxArticlesPerAnalysis if needed

### Debug Mode

Enable detailed logging by setting:
```json
{
  "Logging": {
    "LogLevel": {
      "TradeBot.Services.NewsAnalysis": "Debug"
    }
  }
}
``` 