using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeBot.Trader;

namespace TradeBot.Services;

public class TradingBotHostedService : BackgroundService
{
    private readonly IBinanceTradingService _tradingService;
    private readonly IOrderManagementService _orderManagement;
    private readonly ILogger<TradingBotHostedService> _logger;
    private readonly TradingConfig _config;

    public TradingBotHostedService(
        IBinanceTradingService tradingService,
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
        _logger.LogInformation("Trading bot started");

        if (!await _tradingService.TestConnectionAsync())
        {
            _logger.LogError(TradingConstants.ErrorMessages.ConnectionFailed);
            return;
        }

        var initialBalance = await _orderManagement.GetAccountBalanceAsync(TradingConstants.Defaults.DefaultAsset);
        _logger.LogInformation("Initial balance: {Balance} {Asset}", initialBalance, TradingConstants.Defaults.DefaultAsset);

        const int analysisIntervalMinutes = 5;
        const int errorRetryMinutes = 1;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _tradingService.AnalyzeMarketAsync();
                await _orderManagement.MonitorPositionsAsync();
                
                var activePositions = _orderManagement.GetAllActivePositions();
                if (activePositions.Any())
                {
                    _logger.LogInformation("Active positions: {Count}", activePositions.Count);
                    foreach (var position in activePositions)
                    {
                        var pnl = await _orderManagement.GetPositionPnLAsync(position.Symbol);
                        _logger.LogInformation("Position {Symbol}: P&L = {PnL:F2} {Asset}", 
                            position.Symbol, pnl, TradingConstants.Defaults.DefaultAsset);
                    }
                }
                    
                await Task.Delay(TimeSpan.FromMinutes(analysisIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Trading bot stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in trading bot operation");
                await Task.Delay(TimeSpan.FromMinutes(errorRetryMinutes), stoppingToken);
            }
        }
    }
}
