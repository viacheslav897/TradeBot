using Binance.Net.Enums;
using TradeBot.Models;

namespace TradeBot.Services;

public interface IOrderManagementService
{
    Task<OrderInfo?> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity);
    Task<OrderInfo?> PlaceLimitOrderAsync(string symbol, OrderSide side, decimal quantity, decimal price);
    Task<OrderInfo?> PlaceStopLossOrderAsync(string symbol, decimal quantity, decimal stopPrice);
    Task<OrderInfo?> PlaceTakeProfitOrderAsync(string symbol, decimal quantity, decimal takeProfitPrice);
    Task<bool> CancelOrderAsync(string symbol, long orderId);
    Task<OrderInfo?> GetOrderAsync(string symbol, long orderId);
    Task<List<OrderInfo>> GetOpenOrdersAsync(string symbol);
    Task<Position?> CreatePositionAsync(string symbol, OrderSide side, decimal quantity, decimal entryPrice);
    Task<bool> ClosePositionAsync(string symbol);
    Position? GetActivePosition(string symbol);
    List<Position> GetAllActivePositions();
    List<OrderInfo> GetAllActiveOrders();
    Task<decimal> GetAccountBalanceAsync(string asset = "USDT");
    Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal orderSize);
    Task MonitorPositionsAsync();
    Task<decimal> GetPositionPnLAsync(string symbol);
} 