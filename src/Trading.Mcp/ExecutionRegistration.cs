using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Trading.Core.Execution;
using Trading.Execution;
using Trading.Execution.Binance;
using Trading.Risk;

namespace Trading.Mcp;

/// <summary>
/// Wires the execution layer into the MCP host, but only when explicitly enabled
/// (<c>TRADING_EXEC_ENABLED=true</c>). The default adapter is the paper simulator; the Binance
/// adapter defaults to the testnet and refuses to start without keys. Keys are read from the
/// environment here and never leave the server.
/// </summary>
internal static class ExecutionRegistration
{
    /// <summary>Registers the execution services if enabled; returns whether the tools should be exposed.</summary>
    /// <param name="services">The service collection.</param>
    public static bool AddExecutionIfEnabled(IServiceCollection services)
    {
        if (!EnvFlag("TRADING_EXEC_ENABLED", fallback: false))
        {
            return false;
        }

        services.AddSingleton(BuildRiskLimits());
        services.AddSingleton<ISymbolFilterProvider>(_ => new StaticSymbolFilterProvider());

        var mode = (Environment.GetEnvironmentVariable("TRADING_EXEC_MODE") ?? "paper").ToUpperInvariant();
        if (string.Equals(mode, "BINANCE", StringComparison.Ordinal))
        {
            var options = BuildBinanceOptions();
            services.AddSingleton(options);
            services.AddSingleton<IExecutionAdapter>(provider => new BinanceExecutionAdapter(
                new HttpClient { Timeout = TimeSpan.FromSeconds(15) },
                options,
                provider.GetRequiredService<TimeProvider>()));
        }
        else
        {
            var startingQuote = EnvDecimal("TRADING_PAPER_QUOTE", 10_000m);
            services.AddSingleton<IExecutionAdapter>(_ => new PaperExecutionAdapter(startingQuote));
        }

        services.AddSingleton<ExecutionService>();
        return true;
    }

    private static RiskLimits BuildRiskLimits()
    {
        var defaults = RiskLimits.Default;
        return RiskLimits.Create(
            EnvDecimal("TRADING_MAX_POSITION_FRACTION", defaults.MaxPositionFraction),
            EnvDecimal("TRADING_MAX_ORDER_NOTIONAL", defaults.MaxOrderNotional),
            EnvDecimal("TRADING_DAILY_LOSS_FRACTION", defaults.DailyLossLimitFraction),
            EnvFlag("TRADING_ALLOW_SHORTING", defaults.AllowShorting),
            EnvFlag("TRADING_KILL_SWITCH", defaults.KillSwitch));
    }

    private static BinanceExecutionOptions BuildBinanceOptions()
    {
        var apiKey = Environment.GetEnvironmentVariable("TRADING_BINANCE_API_KEY");
        var apiSecret = Environment.GetEnvironmentVariable("TRADING_BINANCE_API_SECRET");
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "TRADING_EXEC_MODE=binance requires TRADING_BINANCE_API_KEY and TRADING_BINANCE_API_SECRET.");
        }

        var isLive = EnvFlag("TRADING_BINANCE_LIVE", fallback: false);
        return BinanceExecutionOptions.Create(apiKey, apiSecret, isLive);
    }

    private static bool EnvFlag(string name, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return value is null ? fallback : string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal EnvDecimal(string name, decimal fallback) =>
        decimal.TryParse(
            Environment.GetEnvironmentVariable(name),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : fallback;
}
