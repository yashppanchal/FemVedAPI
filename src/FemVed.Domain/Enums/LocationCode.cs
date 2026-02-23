namespace FemVed.Domain.Enums;

/// <summary>
/// ISO country codes used for payment gateway selection and currency pricing.
/// IN → CashFree/INR | GB → PayPal/GBP | US → PayPal/USD.
/// </summary>
public enum LocationCode
{
    IN,
    GB,
    US
}
