namespace TradeBot.Models;

public class TelegramConfig
{
    public string Token { get; set; } = string.Empty;
    public List<long> AuthorizedUsers { get; set; } = new();
    public NotificationSettings NotificationSettings { get; set; } = new();
    public MessageSettings MessageSettings { get; set; } = new();
}

public class NotificationSettings
{
    public bool EnableOrderNotifications { get; set; } = true;
    public bool EnablePositionNotifications { get; set; } = true;
    public bool EnableMarketAnalysis { get; set; } = true;
    public bool EnableSystemAlerts { get; set; } = true;
    public QuietHours QuietHours { get; set; } = new();
}

public class QuietHours
{
    public bool Enabled { get; set; } = false;
    public string StartTime { get; set; } = "23:00";
    public string EndTime { get; set; } = "07:00";
    public string TimeZone { get; set; } = "UTC";
    public bool AllowCriticalOnly { get; set; } = true;
}

public class MessageSettings
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int QueueBatchSize { get; set; } = 10;
    public int RateLimitDelay { get; set; } = 100;
} 