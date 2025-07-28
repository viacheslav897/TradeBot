# TradeBot Project Structure

## Overview

TradeBot is a .NET 9.0 console application designed for automated cryptocurrency trading on Binance. The bot specializes in sideways market detection and executes trades based on support/resistance levels.

## Project Organization

### Root Structure
```
TradeBot/
├── TradeBot/                    # Main application directory
│   ├── Program.cs              # Application entry point
│   ├── appsettings.json        # Configuration file
│   ├── TradeBot.csproj         # Project file
│   ├── Models/                 # Data models
│   ├── Services/               # Business logic services
│   └── Trader/                 # Trading configuration
└── TradeBot.sln               # Solution file
```

## Core Components

### Entry Point
- **Program.cs**: Application startup, dependency injection setup, and service registration

### Configuration
- **appsettings.json**: Contains Binance API credentials, Telegram bot token, and trading parameters
- **Trader/BinanceConfig.cs**: Binance API configuration model
- **Trader/TradingConfig.cs**: Trading strategy parameters and settings

### Data Models
- **Models/OrderInfo.cs**: Order data structure with enums for order side, type, and status
- **Models/Position.cs**: Position tracking with entry price, take profit, and stop loss levels

### Services Layer

#### TradingBotHostedService
- Main orchestrator service running as a background service
- Manages the trading bot lifecycle
- Coordinates market analysis and trading decisions
- Handles connection testing and error recovery

#### BinanceTradingService
- Core trading service for Binance API integration
- Manages REST and WebSocket connections
- Provides market data retrieval (klines/candlesticks)
- Implements market analysis and trading opportunity detection
- Handles order placement and management

#### SidewaysDetectionService
- Analyzes market conditions for sideways movement
- Calculates support and resistance levels
- Implements trend detection algorithms
- Determines optimal entry/exit points

#### OrderManagementService
- Currently empty placeholder for order management logic
- Intended for order lifecycle management

#### TradeBotService
- Telegram bot integration for user interaction
- Provides trading status and portfolio information
- Handles user commands and trade requests
- Implements demo trading functionality

## Dependencies

### NuGet Packages
- **Binance.Net**: Official Binance API client
- **CryptoExchange.Net**: Base cryptocurrency exchange library
- **Telegram.Bot**: Telegram Bot API client
- **Microsoft.Extensions.Hosting**: Dependency injection and hosting
- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.Logging**: Logging infrastructure

## Configuration Parameters

### Trading Strategy
- **Symbol**: Trading pair (default: BTCUSDT)
- **OrderSize**: Position size in USDT
- **PeriodMinutes**: Analysis timeframe (15 minutes)
- **AnalysisPeriods**: Number of periods for analysis (20)
- **SidewaysThreshold**: Threshold for sideways detection (2%)
- **TakeProfitPercent**: Take profit percentage (0.5%)
- **StopLossPercent**: Stop loss percentage (0.3%)

### Binance Settings
- **ApiKey/ApiSecret**: Binance API credentials
- **IsTestNet**: Test network flag for development

## Service Architecture

The application uses dependency injection with singleton services:
- Configuration objects are registered as singletons
- Trading services are registered as singletons
- TradingBotHostedService runs as a hosted background service
- Services communicate through dependency injection

## Execution Flow

1. **Startup**: Program.cs configures services and starts the host
2. **Connection Test**: TradingBotHostedService tests Binance connectivity
3. **Market Analysis**: BinanceTradingService retrieves market data
4. **Sideways Detection**: SidewaysDetectionService analyzes market conditions
5. **Trading Logic**: Executes trades based on support/resistance levels
6. **Telegram Integration**: TradeBotService provides user interface

## Key Features

- Automated sideways market detection
- Support/resistance level calculation
- Real-time market data analysis
- Telegram bot integration for monitoring
- Configurable trading parameters
- Error handling and logging
- Test network support for development 