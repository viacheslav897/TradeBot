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
            "/start" => "🤖 Welcome to TradeBot! I'm here to help you with trading information.\n\n" +
                        "Available commands:\n" +
                        "• /help - Show this help message\n" +
                        "• /status - Check bot status\n" +
                        "• /portfolio - View your portfolio\n" +
                        "• /market - Get market updates\n" +
                        "• /trade - Execute a trade",

            "/help" => "📚 Available commands:\n\n" +
                       "• /start - Welcome message\n" +
                       "• /status - Check bot status\n" +
                       "• /portfolio - View your portfolio\n" +
                       "• /market - Get market updates\n" +
                       "• /trade - Execute a trade\n\n" +
                       "For more information, contact support.",

            "/status" => "✅ Bot is running normally!\n" +
                         $"📊 Server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                         "🔗 Connected to trading services",

            "/portfolio" => "📈 Your Portfolio:\n\n" +
                            "💰 Balance: $10,000.00\n" +
                            "📊 Holdings:\n" +
                            "• AAPL: 10 shares ($1,800.00)\n" +
                            "• GOOGL: 5 shares ($1,500.00)\n" +
                            "• TSLA: 8 shares ($2,000.00)\n\n" +
                            "📈 Total Value: $15,300.00\n" +
                            "📊 Daily P&L: +$250.00 (+1.66%)",

            "/market" => "📊 Market Overview:\n\n" +
                         "📈 Major Indices:\n" +
                         "• S&P 500: 5,850.25 (+0.5%)\n" +
                         "• NASDAQ: 18,500.75 (+0.8%)\n" +
                         "• DOW: 42,150.50 (+0.3%)\n\n" +
                         "🔥 Top Movers:\n" +
                         "• NVDA: +3.2%\n" +
                         "• AAPL: +1.8%\n" +
                         "• TSLA: -2.1%",

            "/trade" => "💼 Trade Execution:\n\n" +
                        "To execute a trade, please specify:\n" +
                        "• Symbol (e.g., AAPL)\n" +
                        "• Action (BUY/SELL)\n" +
                        "• Quantity\n\n" +
                        "Example: BUY 10 AAPL\n" +
                        "⚠️ This is a demo bot - no real trades will be executed.",

            _ when messageText.Contains("BUY") || messageText.Contains("SELL") =>
                HandleTradeCommand(messageText),

            _ => "❓ I don't understand that command. Type /help to see available commands."
        };

        // Send the response
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: response,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private string HandleTradeCommand(string command)
    {
        try
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                var action = parts[0].ToUpper();
                var quantity = parts[1];
                var symbol = parts[2].ToUpper();

                return $"📋 Trade Order Preview:\n\n" +
                       $"🔄 Action: {action}\n" +
                       $"📊 Symbol: {symbol}\n" +
                       $"📈 Quantity: {quantity}\n" +
                       $"💰 Estimated Value: $1,234.56\n\n" +
                       $"⚠️ This is a demo - no real trade executed.\n" +
                       $"✅ Order would be placed successfully!";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error processing trade command: {ex.Message}";
        }

        return "❌ Invalid trade format. Use: BUY/SELL [quantity] [symbol]";
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