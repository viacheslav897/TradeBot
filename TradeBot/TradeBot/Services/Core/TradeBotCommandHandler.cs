using Binance.Net.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TradeBot.Models;
using TradeBot.Services.Notifications;
using TradeBot.Services.OrderManagement;
using TradeBot.Services.Trading;

namespace TradeBot.Services.Core;

public class TradeBotCommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramNotificationService _notificationService;
    private readonly IOrderManagementService _orderManagement;
    private readonly IBinanceTradingService _tradingService;
    private readonly TelegramConfig _config;
    private readonly ILogger<TradeBotCommandHandler> _logger;

    public TradeBotCommandHandler(
        ITelegramBotClient botClient,
        ITelegramNotificationService notificationService,
        IOrderManagementService orderManagement,
        IBinanceTradingService tradingService,
        IOptions<TelegramConfig> config,
        ILogger<TradeBotCommandHandler> logger)
    {
        _botClient = botClient;
        _notificationService = notificationService;
        _orderManagement = orderManagement;
        _tradingService = tradingService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var userName = message.From?.Username ?? message.From?.FirstName ?? "Unknown";

        _logger.LogInformation("Received message from @{Username} (ID: {UserId}): {Message}",
            userName, message.From?.Id, messageText);

        // Check if user is authorized
        if (!await _notificationService.IsUserAuthorizedAsync(chatId))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ You are not authorized to use this bot.",
                cancellationToken: cancellationToken);
            return;
        }

        var command = messageText.Split(' ')[0].ToLower();
        var args = messageText.Split(' ').Skip(1).ToArray();

        var response = command switch
        {
            "/start" => await HandleStartCommandAsync(chatId, cancellationToken),
            "/status" => await HandleStatusCommandAsync(chatId, cancellationToken),
            "/balance" => await HandleBalanceCommandAsync(chatId, cancellationToken),
            "/positions" => await HandlePositionsCommandAsync(chatId, cancellationToken),
            "/settings" => await HandleSettingsCommandAsync(chatId, cancellationToken),
            "/help" => await HandleHelpCommandAsync(chatId, cancellationToken),
            "/pause" => await HandlePauseCommandAsync(chatId, cancellationToken),
            "/resume" => await HandleResumeCommandAsync(chatId, cancellationToken),
            _ => "❓ Unknown command. Type /help to see available commands."
        };

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: response,
            parseMode: ParseMode.Html,
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task<string> HandleStartCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        return "🤖 <b>Welcome to TradeBot!</b>\n\n" +
               "I'm here to help you monitor your trading activities.\n\n" +
               "Use the menu below or type /help to see available commands.";
    }

    private async Task<string> HandleStatusCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var balance = await _orderManagement.GetAccountBalanceAsync();
            var positions = _orderManagement.GetAllActivePositions();
            
            var response = $"📊 <b>Account Status</b>\n\n" +
                          $"💰 Balance: {balance:F2} USDT\n" +
                          $"📈 Active Positions: {positions.Count}\n\n";

            if (positions.Any())
            {
                response += "<b>Active Positions:</b>\n";
                foreach (var position in positions.Take(5)) // Limit to 5 positions
                {
                    var pnl = await _orderManagement.GetPositionPnLAsync(position.Symbol);
                    var pnlEmoji = pnl >= 0 ? "🟢" : "🔴";
                    response += $"{pnlEmoji} {position.Symbol}: ${pnl:F2}\n";
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status");
            return "❌ Error retrieving account status.";
        }
    }

    private async Task<string> HandleBalanceCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var balance = await _orderManagement.GetAccountBalanceAsync();
            return $"💰 <b>Account Balance</b>\n\n" +
                   $"Current Balance: {balance:F2} USDT";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance");
            return "❌ Error retrieving balance.";
        }
    }

    private async Task<string> HandlePositionsCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var positions = _orderManagement.GetAllActivePositions();
            
            if (!positions.Any())
            {
                return "📈 <b>Active Positions</b>\n\nNo active positions.";
            }

            var response = $"📈 <b>Active Positions ({positions.Count})</b>\n\n";
            
            foreach (var position in positions)
            {
                var pnl = await _orderManagement.GetPositionPnLAsync(position.Symbol);
                var pnlEmoji = pnl >= 0 ? "🟢" : "🔴";
                var side = position.Side == OrderSide.Buy ? "LONG" : "SHORT";
                
                response += $"{pnlEmoji} <b>{position.Symbol}</b> ({side})\n" +
                           $"   Quantity: {position.Quantity:F8}\n" +
                           $"   Entry Price: ${position.EntryPrice:F2}\n" +
                           $"   Current Price: ${position.CurrentPrice:F2}\n" +
                           $"   P&L: ${pnl:F2}\n\n";
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positions");
            return "❌ Error retrieving positions.";
        }
    }

    private async Task<string> HandleSettingsCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        var settings = _config.NotificationSettings;
        var quietHours = settings.QuietHours;
        
        return $"⚙️ <b>Notification Settings</b>\n\n" +
               $"📋 Order Notifications: {(settings.EnableOrderNotifications ? "✅" : "❌")}\n" +
               $"📈 Position Notifications: {(settings.EnablePositionNotifications ? "✅" : "❌")}\n" +
               $"📊 Market Analysis: {(settings.EnableMarketAnalysis ? "✅" : "❌")}\n" +
               $"🚨 System Alerts: {(settings.EnableSystemAlerts ? "✅" : "❌")}\n\n" +
               $"🌙 Quiet Hours: {(quietHours.Enabled ? "✅" : "❌")}\n" +
               $"   Time: {quietHours.StartTime} - {quietHours.EndTime}\n" +
               $"   Critical Only: {(quietHours.AllowCriticalOnly ? "✅" : "❌")}";
    }

    private async Task<string> HandleHelpCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        return "📚 <b>Available Commands</b>\n\n" +
               "/start - Welcome message\n" +
               "/status - Account status and active positions\n" +
               "/balance - Current account balance\n" +
               "/positions - Detailed position information\n" +
               "/settings - Notification settings\n" +
               "/help - Show this help message\n\n" +
               "💡 <i>All trading operations are automatically monitored and you'll receive notifications for important events.</i>";
    }

    private async Task<string> HandlePauseCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        // This would require additional implementation to pause notifications
        return "⏸️ <b>Pause Notifications</b>\n\n" +
               "This feature is not yet implemented.\n" +
               "You can modify notification settings in the configuration.";
    }

    private async Task<string> HandleResumeCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        // This would require additional implementation to resume notifications
        return "▶️ <b>Resume Notifications</b>\n\n" +
               "This feature is not yet implemented.\n" +
               "You can modify notification settings in the configuration.";
    }

    private InlineKeyboardMarkup GetMainMenuKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("📊 Status", "status"),
                InlineKeyboardButton.WithCallbackData("💰 Balance", "balance")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("📈 Positions", "positions"),
                InlineKeyboardButton.WithCallbackData("⚙️ Settings", "settings")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("❓ Help", "help")
            }
        });
    }

    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.Data is null)
            return;

        var chatId = callbackQuery.Message?.Chat.Id;
        if (chatId == null)
            return;

        var response = callbackQuery.Data switch
        {
            "status" => await HandleStatusCommandAsync(chatId.Value, cancellationToken),
            "balance" => await HandleBalanceCommandAsync(chatId.Value, cancellationToken),
            "positions" => await HandlePositionsCommandAsync(chatId.Value, cancellationToken),
            "settings" => await HandleSettingsCommandAsync(chatId.Value, cancellationToken),
            "help" => await HandleHelpCommandAsync(chatId.Value, cancellationToken),
            _ => "❓ Unknown action"
        };

        await _botClient.SendTextMessageAsync(
            chatId: chatId.Value,
            text: response,
            parseMode: ParseMode.Html,
            replyMarkup: GetMainMenuKeyboard(),
            cancellationToken: cancellationToken);

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }
} 