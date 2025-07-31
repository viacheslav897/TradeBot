# User Secrets Setup

This project uses .NET User Secrets to store sensitive configuration data during development.

## Current Secrets

The following secrets are configured:

- `Binance:ApiKey` - Your Binance API key
- `Binance:ApiSecret` - Your Binance API secret
- `TelegramBot:Token` - Your Telegram bot token
- `ConnectionStrings:DefaultConnection` - Database connection string

## Setting Up Secrets

### 1. Initialize User Secrets (Already Done)
```bash
dotnet user-secrets init
```

### 2. Set Your Secrets
Replace the placeholder values with your actual credentials:

```bash
# Binance API
dotnet user-secrets set "Binance:ApiKey" "YOUR_ACTUAL_API_KEY"
dotnet user-secrets set "Binance:ApiSecret" "YOUR_ACTUAL_API_SECRET"

# Telegram Bot
dotnet user-secrets set "TelegramBot:Token" "YOUR_ACTUAL_BOT_TOKEN"

# Database Connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_ACTUAL_CONNECTION_STRING"
```

### 3. View Current Secrets
```bash
dotnet user-secrets list
```

### 4. Remove a Secret
```bash
dotnet user-secrets remove "SecretName"
```

## Security Notes

- User secrets are stored locally and are not committed to source control
- They are only available during development
- For production, use environment variables or Azure Key Vault
- The `appsettings.json` file contains empty placeholders for secrets

## Production Deployment

For production environments, replace user secrets with:
- Environment variables
- Azure Key Vault
- AWS Secrets Manager
- Or other secure configuration providers 