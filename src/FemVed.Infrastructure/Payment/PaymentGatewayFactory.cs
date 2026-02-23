using FemVed.Application.Interfaces;
using FemVed.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace FemVed.Infrastructure.Payment;

/// <summary>
/// Implements <see cref="IPaymentGatewayFactory"/>.
/// Selects <see cref="CashfreePaymentGateway"/> for Indian customers (IN),
/// and <see cref="PaypalPaymentGateway"/> for all other locations.
/// </summary>
public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Initialises the factory with the service provider.</summary>
    public PaymentGatewayFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public IPaymentGateway GetGateway(string countryIsoCode)
    {
        return countryIsoCode.Equals("IN", StringComparison.OrdinalIgnoreCase)
            ? _serviceProvider.GetRequiredService<CashfreePaymentGateway>()
            : _serviceProvider.GetRequiredService<PaypalPaymentGateway>();
    }

    /// <inheritdoc/>
    public IPaymentGateway GetGatewayByType(PaymentGateway gatewayType)
    {
        return gatewayType switch
        {
            PaymentGateway.CashFree => _serviceProvider.GetRequiredService<CashfreePaymentGateway>(),
            PaymentGateway.PayPal   => _serviceProvider.GetRequiredService<PaypalPaymentGateway>(),
            _ => throw new ArgumentOutOfRangeException(nameof(gatewayType), gatewayType, "Unknown payment gateway type.")
        };
    }
}
