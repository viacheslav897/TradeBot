// Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeBot.Db;
using TradeBot.Services;
using TradeBot.Trader;

var builder = Host.CreateApplicationBuilder(args);

// Конфигурация
var binanceConfig = builder.Configuration.GetSection("Binance").Get<BinanceConfig>() ?? new BinanceConfig();
var tradingConfig = builder.Configuration.GetSection("Trading").Get<TradingConfig>() ?? new TradingConfig();

// Database configuration
builder.Services.AddDbContext<TradeBotDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация сервисов
builder.Services.AddSingleton(binanceConfig);
builder.Services.AddSingleton(tradingConfig);
builder.Services.AddSingleton<SidewaysDetectionService>();

// Choose between real and mock OrderManagementService based on configuration
var useMockService = builder.Configuration.GetValue<bool>("UseMockTrading", false);
if (useMockService)
{
    builder.Services.AddSingleton<IOrderManagementService, MockOrderManagementService>();
    builder.Services.AddSingleton<BinanceTradingService>();
}
else
{
    builder.Services.AddSingleton<IOrderManagementService, OrderManagementService>();
    builder.Services.AddSingleton<BinanceTradingService>();
}

builder.Services.AddHostedService<TradingBotHostedService>();

// Настройка логирования
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Запуск торгового бота для работы в боковых трендах");

await host.RunAsync();