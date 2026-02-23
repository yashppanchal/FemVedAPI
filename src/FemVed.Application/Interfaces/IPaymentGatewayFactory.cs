using FemVed.Domain.Enums;

namespace FemVed.Application.Interfaces;

/// <summary>
/// Selects the correct <see cref="IPaymentGateway"/> implementation at runtime.
/// IN → CashFree; everything else → PayPal.
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// Returns the gateway to use for the given ISO country code.
    /// "IN" returns the CashFree gateway; all other codes return the PayPal gateway.
    /// </summary>
    /// <param name="countryIsoCode">ISO 3166-1 alpha-2 code, e.g. "IN", "GB", "US".</param>
    IPaymentGateway GetGateway(string countryIsoCode);

    /// <summary>
    /// Returns a specific gateway by its enum type.
    /// Used by webhook handlers that already know which gateway sent the request.
    /// </summary>
    /// <param name="gatewayType">The payment gateway enum value.</param>
    IPaymentGateway GetGatewayByType(PaymentGateway gatewayType);
}
