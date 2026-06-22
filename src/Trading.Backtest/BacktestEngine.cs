using Trading.Backtest.Execution;
using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Trading.Core.Strategies;

namespace Trading.Backtest;

/// <summary>
/// Walks an ascending candle series, builds a point-in-time snapshot from the prefix at each bar
/// (no look-ahead), asks the strategy for a decision, and simulates long-only spot execution
/// (all-in / all-out) at the bar close through the fee model. Produces an equity curve + metrics.
/// </summary>
public static class BacktestEngine
{
    /// <summary>Runs a backtest over <paramref name="candles"/> (ascending by close time).</summary>
    /// <param name="symbol">Symbol being tested.</param>
    /// <param name="market">Market (for snapshots).</param>
    /// <param name="interval">Bar interval (for snapshots and Sharpe annualisation).</param>
    /// <param name="candles">Ascending candle series.</param>
    /// <param name="strategy">The strategy under test.</param>
    /// <param name="fees">Fee/slippage model.</param>
    /// <param name="options">Backtest options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<BacktestResult> RunAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        IReadOnlyList<Candle> candles,
        IStrategy strategy,
        FeeModel fees,
        BacktestOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(candles);
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(fees);
        ArgumentNullException.ThrowIfNull(options);

        var cash = options.InitialCash;
        var units = 0m;
        var trades = 0;
        var curve = new List<EquityPoint>(candles.Count);

        for (var i = 0; i < candles.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bar = candles[i];
            var price = bar.Close;

            var start = Math.Max(0, i - options.LookbackCandles + 1);
            var snapshot = MarketSnapshot.Create(symbol, market, bar.CloseTimeUtc, Slice(candles, start, i + 1));
            var decision = await strategy.DecideAsync(snapshot, cancellationToken).ConfigureAwait(false);

            var wantLong = decision.Action == TradeAction.Buy && decision.SizeFraction > 0m;
            var wantFlat = decision.Action == TradeAction.Sell;

            if (wantLong && units == 0m && cash > 0m)
            {
                units = fees.UnitsForCash(cash, price);
                cash -= fees.BuyCost(units, price);
                trades++;
            }
            else if (wantFlat && units > 0m)
            {
                cash += fees.SellProceeds(units, price);
                units = 0m;
                trades++;
            }

            curve.Add(new EquityPoint(bar.CloseTimeUtc, cash + units * price));
        }

        var metrics = BacktestMetrics.FromEquityCurve(curve, interval, trades);
        return new BacktestResult(strategy.Name, symbol, interval, metrics, curve);
    }

    private static List<Candle> Slice(IReadOnlyList<Candle> candles, int start, int endExclusive)
    {
        var slice = new List<Candle>(endExclusive - start);
        for (var i = start; i < endExclusive; i++)
        {
            slice.Add(candles[i]);
        }

        return slice;
    }
}
