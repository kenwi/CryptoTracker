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
- Data export and historical tracking:
  - CSV and JSON formats
  - Continuous data appending
  - Separate value and total tracking
  - Timestamp-based history
  - Multiple currency conversions
- Command-line features:
  - View and filter historical data
  - List unique assets and sources
  - Flexible data analysis options
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
  "CoinGecko": {
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
  },
  "Export": {
    "Enabled": true,
    "Format": "csv",  // csv, json
    "ValuesFilename": "crypto-portfolio-values",
    "TotalsFilename": "crypto-portfolio-totals",
    "OutputPath": "exports"
  }
}
```

## Command-Line Features

The application supports various command-line operations for analyzing historical data:

### View Historical Data

View and filter historical portfolio data from exported CSV files:

```bash
# View all historical data
dotnet run view-history --file exports/crypto-portfolio-values.csv

# View most recent entries first
dotnet run view-history --file exports/crypto-portfolio-values.csv --reverse

# Filter by specific asset (most recent first)
dotnet run view-history --file exports/crypto-portfolio-values.csv --asset BTC --reverse

# Filter by asset and source (last 10 entries)
dotnet run view-history --file exports/crypto-portfolio-values.csv --asset BTC --source Binance --limit 10 --reverse
```

Example output:

```text
Historical Data:
Timestamp           | Asset | Balance      | Price (USDT)  | Value (USDT)  | Value (NOK)   | Value (BTC)   | Source
-------------------------------------------------------------------------------------------------------------------
2024-01-01 12:00:00 | BTC   | 0.123        | 65000.000     | 7995.00       | 83947.50      | 1.00000000    | Binance
2024-01-01 12:30:00 | BTC   | 0.123        | 65500.000     | 8056.50       | 84593.25      | 1.00000000    | Binance
```

### List Available Assets

View all unique assets and their sources in the exported data:

```bash
dotnet run list-assets --file exports/crypto-portfolio-values.csv
```

Example output:

```text
Available Assets:
Asset | Source
----------------
ADA   | Binance
BTC   | Binance
ETH   | Manual
MYRIA | Myria
```

### Command Help

Each command supports the `--help` option for detailed usage information:

```bash
dotnet run view-history --help
dotnet run list-assets --help
```

### Optional Services

The application is designed to work with any combination of these services:

- **Binance**: Required for real market data (or use Demo Mode)
- **Directus**: Optional data persistence
- **CoinGecko**: Optional token tracking for any listed cryptocurrency
- **Manual Balances**: Optional static balance entries
- **Export**: Optional data export to CSV or JSON

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
  "source": "Binance",
  "btc_value": 0.12345678
}
```

Example POST body for total value:

```json
{
  "total_value": 15449.68,
  "btc_value": 0.12345678
}
```

Note: The `date_created` field is automatically handled by Directus.

### Data Export

The application can continuously export portfolio data to files for historical tracking and analysis:

#### Export Configuration

```json
{
  "Export": {
    "Enabled": true,
    "Format": "csv",  // csv, json (excel coming soon)
    "ValuesFilename": "crypto-portfolio-values",
    "TotalsFilename": "crypto-portfolio-totals",
    "OutputPath": "exports"
  }
}
```

#### Export Formats

The application supports multiple export formats:

##### CSV Format

- Values file (`crypto-portfolio-values.csv`):

```csv
Timestamp,Asset,Balance,Price (USDT),Value (USDT),Value (NOK),Value (BTC),Source
2024-01-01 12:00:00,BTC,0.123,65000.00,7995.00,83947.50,1.00000000,Binance
2024-01-01 12:00:00,ETH,1.456,3400.00,4950.40,51979.20,0.06188000,Manual
```

- Totals file (`crypto-portfolio-totals.csv`):

```csv
Timestamp,Total (USDT),Total (NOK),Total (BTC)
2024-01-01 12:00:00,12945.40,135926.70,1.06188000
```

###### JSON Format

- Values file (`crypto-portfolio-values.json`):

```json
[
  {
    "Timestamp": "2024-01-01T12:00:00",
    "Balances": [
      {
        "Asset": "BTC",
        "Balance": 0.123,
        "Price": 65000.00,
        "UsdValue": 7995.00,
        "NokValue": 83947.50,
        "BtcValue": 1.00000000,
        "Source": "Binance"
      }
    ]
  }
]
```

- Totals file (`crypto-portfolio-totals.json`):

```json
[
  {
    "Timestamp": "2024-01-01T12:00:00",
    "UsdValue": 12945.40,
    "NokValue": 135926.70,
    "BtcValue": 1.06188000
  }
]
```

#### Export Features

- Continuous data appending
- Automatic file creation
- Multiple format support
- Separate files for values and totals
- Timestamp tracking
- Source attribution
- Multi-currency values

The exported data can be used for:

- Historical analysis
- Portfolio performance tracking
- Data visualization
- Custom reporting
- Backup purposes

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
