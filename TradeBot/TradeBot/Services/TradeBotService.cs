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
    private readonly ILogger<TradeBotService> _logger;

    public TradeBotService(ITelegramBotClient botClient, ILogger<TradeBotService> logger)
    {
        _botClient = botClient;
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
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var userName = message.From?.Username ?? message.From?.FirstName ?? "Unknown";

        _logger.LogInformation("Received message from @{Username} (ID: {UserId}): {Message}",
            userName, message.From?.Id, messageText);

        // Handle different commands
        var response = messageText.ToLower() switch
        {
            "/start" => "🤖 Welcome to TradeBot! I'm here to help you with trading information.\n",
            _ => "❓ I don't understand that command. Type /help to see available commands."
        };

        // Send the response
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: response,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        if (callbackQuery.Data is null)
            return;

        var response = callbackQuery.Data switch
        {
            "portfolio" => "Portfolio details loaded...",
            "market" => "Market data updated...",
            _ => "Unknown action"
        };

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: response,
            cancellationToken: cancellationToken);
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