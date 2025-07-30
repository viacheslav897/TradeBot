using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeBot.Trader;

namespace TradeBot.Services;

public class TradingBotHostedService : BackgroundService
{
    private readonly BinanceTradingService _tradingService;
    private readonly IOrderManagementService _orderManagement;
    private readonly ILogger<TradingBotHostedService> _logger;
    private readonly TradingConfig _config;

    public TradingBotHostedService(
        BinanceTradingService tradingService,
        IOrderManagementService orderManagement,
        ILogger<TradingBotHostedService> logger,
        TradingConfig config)
    {
        _tradingService = tradingService;
        _orderManagement = orderManagement;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Торговый бот запущен");

        // Проверяем подключение к Binance
        if (!await _tradingService.TestConnectionAsync())
        {
            _logger.LogError("Не удалось подключиться к Binance. Остановка бота.");
            return;
        }

        // Получаем начальный баланс
        var initialBalance = await _orderManagement.GetAccountBalanceAsync("USDT");
        _logger.LogInformation($"Начальный баланс: {initialBalance} USDT");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Анализируем рынок
                await _tradingService.AnalyzeMarketAsync();
                
                // Мониторим активные позиции
                await _orderManagement.MonitorPositionsAsync();
                
                // Логируем статус позиций
                var activePositions = _orderManagement.GetAllActivePositions();
                if (activePositions.Any())
                {
                    _logger.LogInformation($"Активные позиции: {activePositions.Count}");
                    foreach (var position in activePositions)
                    {
                        var pnl = await _orderManagement.GetPositionPnLAsync(position.Symbol);
                        _logger.LogInformation($"Позиция {position.Symbol}: P&L = {pnl:F2} USDT");
                    }
                }
                    
                // Ждем перед следующим анализом (например, каждые 5 минут)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Торговый бот остановлен");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в работе торгового бота");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
