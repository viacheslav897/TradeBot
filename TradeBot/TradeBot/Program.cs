// Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TradeBot.Db;
using TradeBot.Models;
using TradeBot.Services;
using TradeBot.Services.Analysis;
using TradeBot.Services.Core;
using TradeBot.Services.Notifications;
using TradeBot.Services.OrderManagement;
using TradeBot.Services.Trading;
using TradeBot.Trader;

var builder = Host.CreateApplicationBuilder(args);

// Конфигурация
var binanceConfig = builder.Configuration.GetSection("Binance").Get<BinanceConfig>() ?? new BinanceConfig();
var tradingConfig = builder.Configuration.GetSection("Trading").Get<TradingConfig>() ?? new TradingConfig();
var telegramConfig = builder.Configuration.GetSection("TelegramBot").Get<TelegramConfig>() ?? new TelegramConfig();

// Database configuration
builder.Services.AddDbContext<TradeBotDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация сервисов
builder.Services.AddSingleton(binanceConfig);
builder.Services.AddSingleton(tradingConfig);
builder.Services.AddSingleton(telegramConfig);
builder.Services.AddSingleton<SidewaysDetectionService>();

// Telegram notification services
builder.Services.AddSingleton<ITelegramBotClient>(provider => 
    new TelegramBotClient(telegramConfig.Token));
builder.Services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
builder.Services.AddSingleton<INotificationPublisher, NotificationPublisher>();
builder.Services.AddSingleton<NotificationFormatter>();
builder.Services.AddSingleton<TradeBotCommandHandler>();
builder.Services.AddHostedService<TelegramNotificationQueueProcessor>();

// Choose between real and mock OrderManagementService based on configuration 
var useMockService = builder.Configuration.GetValue<bool>("UseMockTrading", false);
if (useMockService)
{
    builder.Services.AddSingleton<IOrderManagementService>(provider =>
    {
        var mockService = new MockOrderManagementService(
            provider.GetRequiredService<TradingConfig>(),
            provider.GetRequiredService<TradeBotDbContext>(),
            provider.GetRequiredService<ILogger<MockOrderManagementService>>());
        
        return new NotificationDecoratedOrderManagementService(
            mockService,
            provider.GetRequiredService<INotificationPublisher>(),
            provider.GetRequiredService<ILogger<NotificationDecoratedOrderManagementService>>());
    });
    
    builder.Services.AddSingleton<IBinanceTradingService>(provider =>
    {
        var tradingService = new BinanceTradingService(
            provider.GetRequiredService<BinanceConfig>(),
            provider.GetRequiredService<TradingConfig>(),
            provider.GetRequiredService<SidewaysDetectionService>(),
            provider.GetRequiredService<IOrderManagementService>(),
            provider.GetRequiredService<ILogger<BinanceTradingService>>());
        
        return new NotificationDecoratedBinanceTradingService(
            tradingService,
            provider.GetRequiredService<INotificationPublisher>(),
            provider.GetRequiredService<ILogger<NotificationDecoratedBinanceTradingService>>());
    });
}
else
{
    builder.Services.AddSingleton<IOrderManagementService>(provider =>
    {
        var orderService = new OrderManagementService(
            provider.GetRequiredService<BinanceConfig>(),
            provider.GetRequiredService<TradingConfig>(),
            provider.GetRequiredService<ILogger<OrderManagementService>>());
        
        return new NotificationDecoratedOrderManagementService(
            orderService,
            provider.GetRequiredService<INotificationPublisher>(),
            provider.GetRequiredService<ILogger<NotificationDecoratedOrderManagementService>>());
    });
    
    builder.Services.AddSingleton<IBinanceTradingService>(provider =>
    {
        var tradingService = new BinanceTradingService(
            provider.GetRequiredService<BinanceConfig>(),
            provider.GetRequiredService<TradingConfig>(),
            provider.GetRequiredService<SidewaysDetectionService>(),
            provider.GetRequiredService<IOrderManagementService>(),
            provider.GetRequiredService<ILogger<BinanceTradingService>>());
        
        return new NotificationDecoratedBinanceTradingService(
            tradingService,
            provider.GetRequiredService<INotificationPublisher>(),
            provider.GetRequiredService<ILogger<NotificationDecoratedBinanceTradingService>>());
    });
}

builder.Services.AddHostedService<TradingBotHostedService>();

// Настройка логирования
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

var host = builder.Build();

// Apply database migrations
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TradeBotDbContext>();
    await context.Database.MigrateAsync();
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Запуск торгового бота для работы в боковых трендах");

await host.RunAsync();