# TradeBot

A cryptocurrency trading bot designed to work in sideways markets using Binance API.

## Features

- **Real Trading**: Connects to Binance API for actual trading
- **Mock Trading**: Test mode that saves fake orders to database
- **Sideways Market Detection**: Analyzes market conditions for sideways trends
- **Position Management**: Automatic stop-loss and take-profit orders
- **Database Storage**: Stores mock orders and positions for testing

## Configuration

### appsettings.json

```json
{
    "UseMockTrading": true,  // Set to false for real trading
    "ConnectionStrings": {
        "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TradeBotDb;Trusted_Connection=true;MultipleActiveResultSets=true"
    },
    "Binance": {
        "ApiKey": "YOUR_API_KEY_HERE",
        "ApiSecret": "YOUR_API_SECRET_HERE",
        "IsTestNet": true
    },
    "Trading": {
        "Symbol": "BTCUSDT",
        "OrderSize": 10.0,
        "PeriodMinutes": 15,
        "AnalysisPeriods": 20,
        "SidewaysThreshold": 0.02,
        "TakeProfitPercent": 0.005,
        "StopLossPercent": 0.003
    }
}
```

## Setup

### 1. Database Setup

Run the migration script to create the database:

```sql
-- Execute TradeBot.Db/Scripts/MigrationScript.sql in SQL Server Management Studio
-- or run it via command line
```

### 2. Configuration

1. Set `UseMockTrading` to `true` for testing mode
2. Set `UseMockTrading` to `false` for real trading
3. Configure your Binance API credentials
4. Adjust trading parameters as needed

### 3. Running the Bot

```bash
dotnet run
```

## Architecture

### Project Structure

- **TradeBot**: Main application with trading logic and services
- **TradeBot.Db**: Database models, context, and migration scripts

### Services

- **IOrderManagementService**: Interface for order management
- **OrderManagementService**: Real implementation using Binance API
- **MockOrderManagementService**: Test implementation saving to database
- **BinanceTradingService**: Market analysis and trading logic
- **SidewaysDetectionService**: Detects sideways market conditions

### Database Models (TradeBot.Db project)

- **FakeOrder**: Stores mock order information
- **FakePosition**: Stores mock position information
- **TradeBotDbContext**: Entity Framework context for database operations
- **Configurations**: Entity configurations for database schema

## Testing Mode

When `UseMockTrading` is set to `true`:

- All orders are simulated and saved to database
- No real trades are executed
- Balance is mocked (returns 10000 USDT)
- P&L calculations are simulated
- Perfect for testing strategies without risk

## Real Trading Mode

When `UseMockTrading` is set to `false`:

- Connects to real Binance API
- Executes actual trades
- Uses real account balance
- Real P&L calculations

## Safety Notes

- Always test with `UseMockTrading: true` first
- Use Binance TestNet for initial testing
- Start with small order sizes
- Monitor the bot closely during real trading 