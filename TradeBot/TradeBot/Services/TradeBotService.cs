using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TradeBot.Services;

public class TradeBotService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TradeBotCommandHandler _commandHandler;
    private readonly ILogger<TradeBotService> _logger;

    public TradeBotService(
        ITelegramBotClient botClient, 
        TradeBotCommandHandler commandHandler,
        ILogger<TradeBotService> logger)
    {
        _botClient = botClient;
        _commandHandler = commandHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram bot...");

        // Configure bot to receive all message types except ChatMember related updates
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        // Start receiving updates
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        var me = await _botClient.GetMeAsync(stoppingToken);
        _logger.LogInformation("Bot started successfully. Bot username: @{BotUsername}", me.Username);

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        // Handle different types of updates
        switch (update.Type)
        {
            case UpdateType.Message:
                await HandleMessageAsync(botClient, update.Message!, cancellationToken);
                break;
            case UpdateType.CallbackQuery:
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken);
                break;
            default:
                _logger.LogWarning("Received unsupported update type: {UpdateType}", update.Type);
                break;
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await _commandHandler.HandleMessageAsync(message, cancellationToken);
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await _commandHandler.HandleCallbackQueryAsync(callbackQuery, cancellationToken);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        HandleErrorSource errorSource, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError("Polling error: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }
}