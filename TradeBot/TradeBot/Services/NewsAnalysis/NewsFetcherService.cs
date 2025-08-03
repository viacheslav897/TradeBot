using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Logging;
using TradeBot.Models;

namespace TradeBot.Services.NewsAnalysis;

public class NewsFetcherService : INewsFetcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsFetcherService> _logger;
    private readonly Dictionary<NewsSource, NewsSourceConfig> _sourceConfigs;

    public NewsFetcherService(
        HttpClient httpClient,
        ILogger<NewsFetcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _sourceConfigs = InitializeSourceConfigs();
    }

    public async Task<List<NewsArticle>> FetchFromSourceAsync(NewsSource source, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_sourceConfigs.ContainsKey(source))
            {
                _logger.LogWarning("No configuration found for source: {Source}", source);
                return new List<NewsArticle>();
            }

            var config = _sourceConfigs[source];
            if (!config.IsEnabled)
            {
                _logger.LogInformation("Source {Source} is disabled", source);
                 return new List<NewsArticle>();
            }

            _logger.LogInformation("Fetching news from {Source}", source);

            var articles = source switch
            {
                NewsSource.CoinDesk => await FetchFromCoinDeskAsync(cancellationToken),
                NewsSource.CoinTelegraph => await FetchFromCoinTelegraphAsync(cancellationToken),
                NewsSource.BitcoinCom => await FetchFromBitcoinComAsync(cancellationToken),
                NewsSource.CryptoNews => await FetchFromCryptoNewsAsync(cancellationToken),
                NewsSource.Reuters => await FetchFromReutersAsync(cancellationToken),
                NewsSource.Bloomberg => await FetchFromBloombergAsync(cancellationToken),
                _ => await FetchFromRssFeedAsync(config, cancellationToken)
            };

            _logger.LogInformation("Fetched {Count} articles from {Source}", articles.Count, source);
            return articles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching news from {Source}", source);
            return new List<NewsArticle>();
        }
    }

    public async Task<List<NewsArticle>> FetchFromAllSourcesAsync(CancellationToken cancellationToken = default)
    {
        var allArticles = new List<NewsArticle>();
        var tasks = new List<Task<List<NewsArticle>>>();

        foreach (var source in _sourceConfigs.Keys)
        {
            tasks.Add(FetchFromSourceAsync(source, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        foreach (var articles in results)
        {
            allArticles.AddRange(articles);
        }

        // Remove duplicates based on title similarity
        allArticles = RemoveDuplicates(allArticles);
        
        _logger.LogInformation("Fetched {TotalCount} unique articles from all sources", allArticles.Count);
        return allArticles;
    }

    public async Task<bool> IsSourceAvailableAsync(NewsSource source, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_sourceConfigs.ContainsKey(source))
                return false;

            var config = _sourceConfigs[source];
            var response = await _httpClient.GetAsync(config.BaseUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<NewsSourceConfig> GetSourceConfigAsync(NewsSource source)
    {
        return _sourceConfigs.GetValueOrDefault(source, new NewsSourceConfig());
    }

    private async Task<List<NewsArticle>> FetchFromCoinDeskAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://www.coindesk.com/arc/outboundfeeds/rss/", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRssFeed(content, NewsSource.CoinDesk, NewsCategory.Cryptocurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from CoinDesk");
            return new List<NewsArticle>();
        }
    }

    private async Task<List<NewsArticle>> FetchFromCoinTelegraphAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://cointelegraph.com/rss", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRssFeed(content, NewsSource.CoinTelegraph, NewsCategory.Cryptocurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from CoinTelegraph");
            return new List<NewsArticle>();
        }
    }

    private async Task<List<NewsArticle>> FetchFromBitcoinComAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://news.bitcoin.com/feed/", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRssFeed(content, NewsSource.BitcoinCom, NewsCategory.Bitcoin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from Bitcoin.com");
            return new List<NewsArticle>();
        }
    }

    private async Task<List<NewsArticle>> FetchFromCryptoNewsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://cryptonews.com/news/feed/", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRssFeed(content, NewsSource.CryptoNews, NewsCategory.Cryptocurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from CryptoNews");
            return new List<NewsArticle>();
        }
    }

    private async Task<List<NewsArticle>> FetchFromReutersAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Reuters API would require authentication
            // For demo purposes, we'll use a mock implementation
            return await Task.FromResult(new List<NewsArticle>
            {
                new NewsArticle
                {
                    Title = "Global Markets React to Economic Policy Changes",
                    Content = "Major financial markets showed mixed reactions to recent economic policy announcements...",
                    Source = NewsSource.Reuters,
                    Category = NewsCategory.Economic,
                    PublishedAt = DateTime.UtcNow.AddHours(-2),
                    Keywords = new List<string> { "markets", "economy", "policy" }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from Reuters");
            return new List<NewsArticle>();
        }
    }

    private async Task<List<NewsArticle>> FetchFromBloombergAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Bloomberg API would require authentication
            // For demo purposes, we'll use a mock implementation
            return await Task.FromResult(new List<NewsArticle>
            {
                new NewsArticle
                {
                    Title = "Cryptocurrency Markets Face Regulatory Pressure",
                    Content = "Digital asset markets are experiencing increased regulatory scrutiny as governments worldwide...",
                    Source = NewsSource.Bloomberg,
                    Category = NewsCategory.Regulatory,
                    PublishedAt = DateTime.UtcNow.AddHours(-1),
                    Keywords = new List<string> { "cryptocurrency", "regulation", "markets" }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from Bloomberg");
            return new List<NewsArticle>();
        }
    }

    private async Task<List<NewsArticle>> FetchFromRssFeedAsync(NewsSourceConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(config.BaseUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRssFeed(content, NewsSource.Unknown, NewsCategory.General);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from RSS feed: {Url}", config.BaseUrl);
            return new List<NewsArticle>();
        }
    }

    private List<NewsArticle> ParseRssFeed(string content, NewsSource source, NewsCategory category)
    {
        var articles = new List<NewsArticle>();

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);

            var items = doc.SelectNodes("//item");
            if (items == null) return articles;

            foreach (XmlNode item in items)
            {
                var title = item.SelectSingleNode("title")?.InnerText ?? "";
                var description = item.SelectSingleNode("description")?.InnerText ?? "";
                var link = item.SelectSingleNode("link")?.InnerText ?? "";
                var pubDate = item.SelectSingleNode("pubDate")?.InnerText ?? "";

                if (string.IsNullOrEmpty(title)) continue;

                var article = new NewsArticle
                {
                    Title = title,
                    Content = description,
                    Summary = description.Length > 200 ? description.Substring(0, 200) + "..." : description,
                    Url = link,
                    Source = source,
                    Category = category,
                    PublishedAt = ParseRssDate(pubDate),
                    Keywords = ExtractKeywords(title + " " + description)
                };

                articles.Add(article);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing RSS feed for {Source}", source);
        }

        return articles;
    }

    private DateTime ParseRssDate(string dateString)
    {
        if (DateTime.TryParse(dateString, out var result))
            return result;
        
        return DateTime.UtcNow;
    }

    private List<string> ExtractKeywords(string text)
    {
        var keywords = new List<string>();
        var cryptoKeywords = new[] { "bitcoin", "btc", "crypto", "cryptocurrency", "blockchain", "defi", "nft" };
        var economicKeywords = new[] { "economy", "inflation", "interest", "rate", "fed", "central bank" };
        var politicalKeywords = new[] { "regulation", "policy", "government", "law", "compliance" };

        var lowerText = text.ToLower();
        
        foreach (var keyword in cryptoKeywords.Concat(economicKeywords).Concat(politicalKeywords))
        {
            if (lowerText.Contains(keyword))
                keywords.Add(keyword);
        }

        return keywords;
    }

    private List<NewsArticle> RemoveDuplicates(List<NewsArticle> articles)
    {
        var uniqueArticles = new List<NewsArticle>();
        var seenTitles = new HashSet<string>();

        foreach (var article in articles)
        {
            var normalizedTitle = NormalizeTitle(article.Title);
            if (!seenTitles.Contains(normalizedTitle))
            {
                seenTitles.Add(normalizedTitle);
                uniqueArticles.Add(article);
            }
        }

        return uniqueArticles;
    }

    private string NormalizeTitle(string title)
    {
        return title.ToLower()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "")
            .Replace(".", "");
    }

    private Dictionary<NewsSource, NewsSourceConfig> InitializeSourceConfigs()
    {
        return new Dictionary<NewsSource, NewsSourceConfig>
        {
            [NewsSource.CoinDesk] = new NewsSourceConfig
            {
                Name = "CoinDesk",
                BaseUrl = "https://www.coindesk.com/arc/outboundfeeds/rss/",
                IsEnabled = true,
                RateLimitPerMinute = 60,
                Keywords = new List<string> { "bitcoin", "crypto", "blockchain" },
                Categories = new List<NewsCategory> { NewsCategory.Cryptocurrency, NewsCategory.Bitcoin }
            },
            
            [NewsSource.CoinTelegraph] = new NewsSourceConfig
            {
                Name = "CoinTelegraph",
                BaseUrl = "https://cointelegraph.com/rss",
                IsEnabled = true,
                RateLimitPerMinute = 60,
                Keywords = new List<string> { "bitcoin", "crypto", "defi" },
                Categories = new List<NewsCategory> { NewsCategory.Cryptocurrency, NewsCategory.Bitcoin }
            },
            
            [NewsSource.BitcoinCom] = new NewsSourceConfig
            {
                Name = "Bitcoin.com",
                BaseUrl = "https://news.bitcoin.com/feed/",
                IsEnabled = true,
                RateLimitPerMinute = 60,
                Keywords = new List<string> { "bitcoin", "btc" },
                Categories = new List<NewsCategory> { NewsCategory.Bitcoin }
            },
            
            [NewsSource.CryptoNews] = new NewsSourceConfig
            {
                Name = "CryptoNews",
                BaseUrl = "https://cryptonews.com/news/feed/",
                IsEnabled = true,
                RateLimitPerMinute = 60,
                Keywords = new List<string> { "crypto", "bitcoin", "altcoin" },
                Categories = new List<NewsCategory> { NewsCategory.Cryptocurrency }
            },
            
            [NewsSource.Reuters] = new NewsSourceConfig
            {
                Name = "Reuters",
                BaseUrl = "https://feeds.reuters.com/reuters/businessNews",
                IsEnabled = true,
                RateLimitPerMinute = 30,
                Keywords = new List<string> { "economy", "markets", "finance" },
                Categories = new List<NewsCategory> { NewsCategory.Economic, NewsCategory.Market }
            },
            
            [NewsSource.Bloomberg] = new NewsSourceConfig
            {
                Name = "Bloomberg",
                BaseUrl = "https://feeds.bloomberg.com/markets/news.rss",
                IsEnabled = true,
                RateLimitPerMinute = 30,
                Keywords = new List<string> { "markets", "finance", "economy" },
                Categories = new List<NewsCategory> { NewsCategory.Economic, NewsCategory.Market }
            }
        };
    }
} 