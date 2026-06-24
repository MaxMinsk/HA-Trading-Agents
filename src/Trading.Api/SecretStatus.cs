namespace Trading.Api;

/// <summary>Masked view of a secret: whether it is set and a short hint (last 4 chars). Never the value.</summary>
public sealed record SecretStatus(bool Set, string? Hint)
{
    public static SecretStatus Of(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new SecretStatus(false, null);
        }

        var hint = value.Length <= 4 ? "****" : "…" + value[^4..];
        return new SecretStatus(true, hint);
    }
}
