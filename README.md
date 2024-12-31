# Crypto Portfolio Tracker

A .NET Core application that tracks cryptocurrency portfolio values across different sources and displays real-time updates.

## Features

- Real-time tracking of cryptocurrency balances and values
- Multiple data sources (all optional):
  - Binance exchange balances
  - CoinGecko price integration for any listed token
  - Manual balance entries
  - Directus API integration for data persistence
- Multi-currency display with culture-specific formatting:
  - USD/USDT values (US format with commas)
  - NOK conversion using live exchange rates (Norwegian format with spaces)
  - BTC equivalent values
- Console-based UI:
  - Color-coded price changes
  - Detailed balance table with changes
  - Total portfolio summary
  - Automatic updates every 30 minutes (configurable)
  - Manual update trigger via spacebar
  - Clean exit with Enter key
- Demo mode:
  - Real market prices from Binance
  - Simulated portfolio balances
  - Full UI functionality

## Configuration

Configuration is managed through `appsettings.json`:

```json
{
  "Binance": {
    "Secret": "your-binance-secret",
    "ApiKey": "your-binance-api-key",
    "ExcludedSymbols": ["BRD", "USDT", "ETHW", "FLR"]
  },
  "Directus": {
    "Host": "https://your-directus-host.com",
    "ApiKey": "your-directus-api-key",
    "CoinValuesEndpoint": "coin_values",
    "CryptoValueEndpoint": "crypto_value",
    "Enabled": true
  },
  "ManualBalances": {
    "Balances": [
      {
        "Asset": "SOL",
        "Available": 73.24
      }
    ]
  },
  "CoinGeckoIntegration": {
    "BaseUrl": "https://api.coingecko.com/api/v3",
    "Assets": [
      {
        "AssetName": "MYRIA",
        "CoinGeckoId": "myria",
        "TotalDate": "2024-12-12",
        "InitialTotal": 1234567.789,
        "TokensPerDay": 1233
      },
      {
        "AssetName": "PEPE",
        "CoinGeckoId": "pepe",
        "TotalDate": "2024-01-01",
        "InitialTotal": 100000000,
        "TokensPerDay": 0
      }
    ]
  },
  "ExchangeRate": {
    "ApiUrl": "https://api.exchangerate-api.com/v4/latest/USD"
  },
  "CryptoTracking": {
    "UpdateIntervalMinutes": 30,
    "DemoMode": false
  }
}
```

### Optional Services

The application is designed to work with any combination of these services:

- **Binance**: Required for real market data (or use Demo Mode)
- **Directus**: Optional data persistence
- **CoinGecko**: Optional token tracking for any listed cryptocurrency
- **Manual Balances**: Optional static balance entries

### CoinGecko Integration

Track any cryptocurrency listed on CoinGecko:

- Specify multiple assets in configuration
- Each asset can have its own vesting schedule
- Uses CoinGecko's public API
- Automatic balance calculation based on initial amount and daily accrual

Example output:

```text
Coin     | Balance      | Price           | Value (USDT)      | Value (NOK)       | Change (USDT)  | Change % | Source
-------------------------------------------------------------------------------------------------------------------------
BTC      | 0.123       | 65,000.000 USDT | 7,995.00 USDT    | 83 947.50 NOK     | +195.00        | +2.50%   | Binance
ETH      | 1.456       | 3,400.000 USDT  | 4,950.40 USDT    | 51 979.20 NOK     | -50.40         | -1.01%   | Manual
MYRIA    | 1017587.025 | 0.002 USDT      | 2,035.17 USDT    | 21 369.29 NOK     | +4.28          | +0.21%   | MYRIA
```

### Directus Integration

The application can persist balance data to a Directus instance. It requires two collections:

#### Collection: coin_values

Tracks individual coin balances:

```json
{
  "id": "1",                    // Integer, Primary Key, Auto-increment
  "date_created": "2024-01-01", // DateTime, Auto-generate
  "asset": "BTC",              // String
  "balance": 0.12345,          // Decimal/Float
  "price": 65000.00,           // Decimal/Float
  "value": 7995.00,            // Decimal/Float
  "source": "Binance"          // String
}
```

#### Collection: crypto_value

Tracks total portfolio value:

```json
{
  "id": "1",                    // Integer, Primary Key, Auto-increment
  "date_created": "2024-01-01", // DateTime, Auto-generate
  "total_value": 15449.68       // Decimal/Float
}
```

#### API Endpoints

The service makes POST requests to:

- `{DirectusHost}/items/coin_values`
- `{DirectusHost}/items/crypto_value`

Example configuration:

```json
"Directus": {
  "Host": "https://your-directus-host.com",
  "ApiKey": "your-directus-api-key",
  "CoinValuesEndpoint": "coin_values",
  "CryptoValueEndpoint": "crypto_value",
  "Enabled": true
}
```

Headers used for all requests:

```text
Authorization: Bearer your-directus-api-key
Content-Type: application/json
```

Example POST body for coin values:

```json
{
  "asset": "BTC",
  "balance": 0.12345,
  "price": 65000.00,
  "value": 7995.00,
  "source": "Binance"
}
```

Example POST body for total value:

```json
{
  "total_value": 15449.68
}
```

Note: The `date_created` field is automatically handled by Directus.

## Prerequisites

- .NET 8.0 SDK
- Binance API credentials (or use Demo Mode)
- Optional: Directus instance
- Optional: CoinGecko-listed tokens to track

## Installation & Usage

1. Clone the repository

```bash
git clone [repository-url]
```

1. Configure your settings in `appsettings.json`

1. Build the application

```bash
dotnet build
```

1. Run the application

```bash
dotnet run
```

## Usage

The application will automatically start tracking your portfolio:

- Values update every 30 minutes by default
- Press spacebar to trigger a manual update
- Press Enter to exit cleanly
- Console displays:
  - Individual coin balances
  - Current prices
  - Value in USDT and NOK
  - Price change indicators (green/red)
  - Source of each balance
  - Total portfolio value with changes

## Architecture

- Built using .NET 8.0
- Modular service architecture
- Interface-based design
- Configuration-driven setup
- Async/await patterns
- Culture-aware formatting
- Optional service registration
- Parallel API processing

## Services

- `BinanceService`: Handles Binance exchange integration
- `DirectusService`: Manages data persistence (optional)
- `CoinGeckoService`: Tracks any CoinGecko-listed token (optional)
- `ExchangeRateService`: Provides currency conversion
- `DisplayService`: Handles console output formatting
- `KeyPressHandlerService`: Manages user input
- `DemoBalanceGenerator`: Provides simulated balances in demo mode

## Contributing

Feel free to submit issues and enhancement requests!

## License

[MIT License](LICENSE)
