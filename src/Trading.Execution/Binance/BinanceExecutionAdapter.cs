using System.Globalization;
using System.Text.Json;
using Trading.Core.Execution;

namespace Trading.Execution.Binance;

/// <summary>
/// An <see cref="IExecutionAdapter"/> for the Binance spot REST API (testnet by default). Builds the
/// signed query for place/cancel/account endpoints, sends it with the API key header, and maps the
/// JSON response back to the execution contracts. Failures map to a rejected result rather than
/// throwing, so a bad order does not crash the caller. Sizing/risk are handled upstream.
/// </summary>
public sealed class BinanceExecutionAdapter : IExecutionAdapter
{
    private const string OrderPath = "/api/v3/order";
    private const string AccountPath = "/api/v3/account";

    private readonly HttpClient _http;
    private readonly BinanceExecutionOptions _options;
    private readonly TimeProvider _time;

    /// <summary>Creates the adapter.</summary>
    /// <param name="httpClient">HTTP client (its base address is set from the options).</param>
    /// <param name="options">Endpoint, keys, and live/testnet selection.</param>
    /// <param name="time">Clock for request timestamps.</param>
    public BinanceExecutionAdapter(HttpClient httpClient, BinanceExecutionOptions options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);
        _http = httpClient;
        _options = options;
        _time = time;
    }

    /// <inheritdoc />
    public string Name => _options.IsLive ? "binance-live" : "binance-testnet";

    /// <inheritdoc />
    public async Task<OrderResult> SubmitAsync(OrderIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        var parameters = new List<(string Key, string Value)>
        {
            ("symbol", intent.Symbol.ToUpperInvariant()),
            ("side", intent.Side == OrderSide.Buy ? "BUY" : "SELL"),
            ("type", intent.Type == OrderType.Limit ? "LIMIT" : "MARKET"),
            ("quantity", FormatNumber(intent.Quantity)),
            ("newClientOrderId", intent.ClientOrderId),
        };

        if (intent.Type == OrderType.Limit && intent.Price is { } price)
        {
            parameters.Add(("timeInForce", "GTC"));
            parameters.Add(("price", FormatNumber(price)));
        }

        var (ok, body) = await SendSignedAsync(HttpMethod.Post, OrderPath, parameters, cancellationToken)
            .ConfigureAwait(false);
        return ok ? ParseOrder(body, intent.ClientOrderId) : OrderResult.Rejected(intent.ClientOrderId, Trim(body));
    }

    /// <inheritdoc />
    public async Task<OrderResult> CancelAsync(string symbol, string clientOrderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientOrderId);
        var parameters = new List<(string Key, string Value)>
        {
            ("symbol", symbol.ToUpperInvariant()),
            ("origClientOrderId", clientOrderId),
        };

        var (ok, body) = await SendSignedAsync(HttpMethod.Delete, OrderPath, parameters, cancellationToken)
            .ConfigureAwait(false);
        return ok
            ? OrderResult.Canceled(clientOrderId, ReadOrderId(body))
            : OrderResult.Rejected(clientOrderId, Trim(body));
    }

    /// <inheritdoc />
    public async Task<AccountSnapshot> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        var (ok, body) = await SendSignedAsync(HttpMethod.Get, AccountPath, [], cancellationToken)
            .ConfigureAwait(false);
        if (!ok)
        {
            return AccountSnapshot.Create([]);
        }

        var balances = new List<Balance>();
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("balances", out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in array.EnumerateArray())
            {
                var free = ParseDecimal(element, "free");
                var locked = ParseDecimal(element, "locked");
                if (free == 0m && locked == 0m)
                {
                    continue;
                }

                balances.Add(new Balance
                {
                    Asset = element.GetProperty("asset").GetString() ?? string.Empty,
                    Free = free,
                    Locked = locked,
                });
            }
        }

        return AccountSnapshot.Create(balances);
    }

    private async Task<(bool Ok, string Body)> SendSignedAsync(
        HttpMethod method,
        string path,
        List<(string Key, string Value)> parameters,
        CancellationToken cancellationToken)
    {
        var timestamp = _time.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var recvWindow = _options.RecvWindow.ToString(CultureInfo.InvariantCulture);

        var pairs = new List<string>(parameters.Count + 2);
        foreach (var (key, value) in parameters)
        {
            pairs.Add(key + "=" + value);
        }

        pairs.Add("recvWindow=" + recvWindow);
        pairs.Add("timestamp=" + timestamp);

        var query = string.Join("&", pairs);
        var signature = BinanceSigner.Sign(query, _options.ApiSecret);
        var requestUri = new Uri(_options.BaseAddress, path + "?" + query + "&signature=" + signature);

        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-MBX-APIKEY", _options.ApiKey);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return (response.IsSuccessStatusCode, body);
    }

    private static OrderResult ParseOrder(string body, string clientOrderId)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
        var executedQty = ParseDecimal(root, "executedQty");
        var cumulativeQuote = ParseDecimal(root, "cummulativeQuoteQty");
        var averagePrice = executedQty > 0m ? cumulativeQuote / executedQty : 0m;
        var (fee, feeAsset) = SumCommission(root);
        var exchangeOrderId = ReadOrderId(body);

        return new OrderResult
        {
            ClientOrderId = clientOrderId,
            ExchangeOrderId = exchangeOrderId,
            Status = MapStatus(status),
            FilledQuantity = executedQty,
            AveragePrice = averagePrice,
            Fee = fee,
            FeeAsset = feeAsset,
        };
    }

    private static (decimal Fee, string Asset) SumCommission(JsonElement root)
    {
        if (!root.TryGetProperty("fills", out var fills) || fills.ValueKind != JsonValueKind.Array)
        {
            return (0m, string.Empty);
        }

        var total = 0m;
        var asset = string.Empty;
        foreach (var fill in fills.EnumerateArray())
        {
            total += ParseDecimal(fill, "commission");
            if (asset.Length == 0 && fill.TryGetProperty("commissionAsset", out var a))
            {
                asset = a.GetString() ?? string.Empty;
            }
        }

        return (total, asset);
    }

    private static string? ReadOrderId(string body)
    {
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("orderId", out var id) && id.ValueKind == JsonValueKind.Number
            ? id.GetInt64().ToString(CultureInfo.InvariantCulture)
            : null;
    }

    private static OrderStatus MapStatus(string? status) => status switch
    {
        "FILLED" => OrderStatus.Filled,
        "PARTIALLY_FILLED" => OrderStatus.PartiallyFilled,
        "CANCELED" or "PENDING_CANCEL" => OrderStatus.Canceled,
        "REJECTED" or "EXPIRED" => OrderStatus.Rejected,
        _ => OrderStatus.New,
    };

    private static decimal ParseDecimal(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.GetString() is { Length: > 0 } text
        && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;

    private static string FormatNumber(decimal value) => value.ToString("0.##########", CultureInfo.InvariantCulture);

    private static string Trim(string body) => body.Length > 300 ? body[..300] : body;
}
