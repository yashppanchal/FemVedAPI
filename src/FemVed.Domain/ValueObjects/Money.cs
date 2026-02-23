namespace FemVed.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing an amount of money in a specific currency.
/// Enforces that amount is never negative.
/// </summary>
public sealed record Money
{
    /// <summary>The monetary amount (never negative).</summary>
    public decimal Amount { get; }

    /// <summary>ISO 4217 currency code, e.g. GBP, USD, INR.</summary>
    public string CurrencyCode { get; }

    /// <summary>Display symbol, e.g. £, $, ₹.</summary>
    public string Symbol { get; }

    /// <summary>Initializes a Money value object.</summary>
    /// <param name="amount">Must be &gt;= 0.</param>
    /// <param name="currencyCode">ISO 4217 code.</param>
    /// <param name="symbol">Currency symbol for display.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is negative.</exception>
    public Money(decimal amount, string currencyCode, string symbol)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        Amount = amount;
        CurrencyCode = currencyCode;
        Symbol = symbol;
    }

    /// <summary>Returns a formatted display string, e.g. "£320.00".</summary>
    public override string ToString() => $"{Symbol}{Amount:F2}";
}
