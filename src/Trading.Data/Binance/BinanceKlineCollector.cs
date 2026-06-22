using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using Trading.Core.MarketData;

namespace Trading.Data.Binance;

/// <summary>
/// Live Binance kline collector over a combined WebSocket stream. Connects, receives kline messages,
/// and invokes a callback for each <em>closed</em> candle (so only finalized data is stored). Survives
/// drops by reconnecting with exponential backoff; the framework auto-replies to server ping frames
/// (heartbeat). The stream base URL is resolved per market and overridable (e.g. a public mirror).
/// </summary>
/// <param name="streamBaseUrlResolver">Optional WS base-URL resolver; defaults to <see cref="BinanceIntervals.StreamBaseUrl"/>.</param>
/// <param name="backoff">Optional reconnect backoff; defaults to 1s..30s exponential.</param>
public sealed class BinanceKlineCollector(Func<Market, string>? streamBaseUrlResolver = null, BackoffPolicy? backoff = null)
{
    private readonly Func<Market, string> _streamBaseUrl = streamBaseUrlResolver ?? BinanceIntervals.StreamBaseUrl;
    private readonly BackoffPolicy _backoff = backoff ?? new BackoffPolicy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));

    /// <summary>Runs the collector until <paramref name="cancellationToken"/> is cancelled.</summary>
    /// <param name="market">The market to stream.</param>
    /// <param name="subscriptions">Symbols + intervals to subscribe to (at least one).</param>
    /// <param name="onClosedCandle">Invoked for each closed candle (e.g. upsert into the store).</param>
    /// <param name="onEvent">Optional lifecycle log sink (connected / disconnected / reconnecting / stopped).</param>
    /// <param name="cancellationToken">Stops the collector when cancelled.</param>
    public async Task RunAsync(
        Market market,
        IReadOnlyList<KlineSubscription> subscriptions,
        Func<Candle, CancellationToken, Task> onClosedCandle,
        Action<string>? onEvent = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(onClosedCandle);
        if (subscriptions.Count == 0)
        {
            throw new ArgumentException("At least one subscription is required.", nameof(subscriptions));
        }

        var url = BuildUrl(market, subscriptions);
        var counters = new StreamCounters();
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                using var socket = new ClientWebSocket();
                await socket.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
                attempt = 0;
                onEvent?.Invoke($"connected: {url}");
                await ReceiveLoopAsync(socket, market, onClosedCandle, counters, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (WebSocketException ex)
            {
                onEvent?.Invoke($"disconnected: {ex.Message}");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var delay = _backoff.Delay(Math.Max(attempt, 1));
            onEvent?.Invoke(string.Create(CultureInfo.InvariantCulture, $"reconnecting in {delay.TotalSeconds:0.#}s"));
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        onEvent?.Invoke(string.Create(
            CultureInfo.InvariantCulture,
            $"stopped: received {counters.Received} messages, stored {counters.Stored} closed candles"));
    }

    private string BuildUrl(Market market, IReadOnlyList<KlineSubscription> subscriptions)
    {
#pragma warning disable CA1308 // Binance stream names are required to be lowercase.
        var streams = string.Join(
            "/",
            subscriptions.Select(s => $"{s.Symbol.ToLowerInvariant()}@kline_{BinanceIntervals.ToCode(s.Interval)}"));
#pragma warning restore CA1308
        return $"{_streamBaseUrl(market)}/stream?streams={streams}";
    }

    private static async Task ReceiveLoopAsync(
        ClientWebSocket socket,
        Market market,
        Func<Candle, CancellationToken, Task> onClosedCandle,
        StreamCounters counters,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var message = new MemoryStream();

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            message.SetLength(0);
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            counters.Received++;
            var json = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            if (BinanceKlineMessageParser.TryParseClosedCandle(json, market, out var candle) && candle is not null)
            {
                await onClosedCandle(candle, cancellationToken).ConfigureAwait(false);
                counters.Stored++;
            }
        }
    }

    private sealed class StreamCounters
    {
        public int Received { get; set; }

        public int Stored { get; set; }
    }
}
