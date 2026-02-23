using FemVed.Application.Interfaces;
using FemVed.Infrastructure.BackgroundJobs;
using FemVed.Infrastructure.Guided;
using FemVed.Infrastructure.Notifications;
using FemVed.Infrastructure.Notifications.Options;
using FemVed.Infrastructure.Payment;
using FemVed.Infrastructure.Payment.Options;
using FemVed.Infrastructure.Persistence;
using FemVed.Infrastructure.Persistence.Repositories;
using FemVed.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FemVed.Infrastructure.Extensions;

/// <summary>
/// Registers all Infrastructure-layer services into the DI container.
/// Call <c>services.AddInfrastructure(configuration)</c> from <c>Program.cs</c>.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers <see cref="AppDbContext"/>, repositories, and unit-of-work with the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration (reads DB_CONNECTION_STRING).</param>
    /// <returns>The mutated service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("DB_CONNECTION_STRING environment variable is not set.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // Allow Repository<T> (which injects DbContext) to resolve AppDbContext
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Security
        services.AddScoped<IJwtService, JwtService>();

        // ── Notifications ─────────────────────────────────────────────────────
        services.Configure<SendGridOptions>(opts =>
        {
            opts.ApiKey    = configuration["SENDGRID_API_KEY"]    ?? throw new InvalidOperationException("SENDGRID_API_KEY is not set.");
            opts.FromEmail = configuration["SENDGRID_FROM_EMAIL"] ?? throw new InvalidOperationException("SENDGRID_FROM_EMAIL is not set.");
            opts.FromName  = configuration["SENDGRID_FROM_NAME"]  ?? "FemVed";

            // Template IDs: SENDGRID_TEMPLATE_<KEY>=d-xxx
            // e.g. SENDGRID_TEMPLATE_PURCHASE_SUCCESS=d-abc123
            opts.TemplateIds = new Dictionary<string, string>
            {
                ["purchase_success"]      = configuration["SENDGRID_TEMPLATE_PURCHASE_SUCCESS"]      ?? string.Empty,
                ["purchase_failed"]       = configuration["SENDGRID_TEMPLATE_PURCHASE_FAILED"]       ?? string.Empty,
                ["program_reminder"]      = configuration["SENDGRID_TEMPLATE_PROGRAM_REMINDER"]      ?? string.Empty,
                ["expert_new_enrollment"] = configuration["SENDGRID_TEMPLATE_EXPERT_NEW_ENROLLMENT"] ?? string.Empty,
                ["password_reset"]        = configuration["SENDGRID_TEMPLATE_PASSWORD_RESET"]        ?? string.Empty,
                ["email_verify"]          = configuration["SENDGRID_TEMPLATE_EMAIL_VERIFY"]          ?? string.Empty,
                ["expert_progress_update"]= configuration["SENDGRID_TEMPLATE_EXPERT_PROGRESS_UPDATE"]?? string.Empty,
            };
        });

        services.Configure<TwilioOptions>(opts =>
        {
            opts.AccountSid   = configuration["TWILIO_ACCOUNT_SID"]    ?? string.Empty;
            opts.AuthToken    = configuration["TWILIO_AUTH_TOKEN"]      ?? string.Empty;
            opts.WhatsAppFrom = configuration["TWILIO_WHATSAPP_FROM"]  ?? string.Empty;
            opts.IsEnabled    = string.Equals(configuration["WHATSAPP_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
        });

        services.AddScoped<IEmailService, SendGridEmailService>();
        services.AddScoped<IWhatsAppService, TwilioWhatsAppService>();

        // Guided catalog read service (complex EF Core projections)
        services.AddScoped<IGuidedCatalogReadService, GuidedCatalogReadService>();

        // In-memory cache for guided tree (10-min TTL per location code)
        services.AddMemoryCache();

        // ── Payment gateways ─────────────────────────────────────────────────
        services.Configure<CashfreeOptions>(opts =>
        {
            opts.BaseUrl      = configuration["CASHFREE_BASE_URL"]      ?? throw new InvalidOperationException("CASHFREE_BASE_URL is not set.");
            opts.ClientId     = configuration["CASHFREE_CLIENT_ID"]     ?? throw new InvalidOperationException("CASHFREE_CLIENT_ID is not set.");
            opts.ClientSecret = configuration["CASHFREE_CLIENT_SECRET"] ?? throw new InvalidOperationException("CASHFREE_CLIENT_SECRET is not set.");
        });

        services.Configure<PaypalOptions>(opts =>
        {
            opts.BaseUrl   = configuration["PAYPAL_BASE_URL"]   ?? throw new InvalidOperationException("PAYPAL_BASE_URL is not set.");
            opts.ClientId  = configuration["PAYPAL_CLIENT_ID"]  ?? throw new InvalidOperationException("PAYPAL_CLIENT_ID is not set.");
            opts.Secret    = configuration["PAYPAL_SECRET"]     ?? throw new InvalidOperationException("PAYPAL_SECRET is not set.");
            opts.WebhookId = configuration["PAYPAL_WEBHOOK_ID"] ?? throw new InvalidOperationException("PAYPAL_WEBHOOK_ID is not set.");
        });

        services.AddHttpClient("cashfree", (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CashfreeOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient("paypal", (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaypalOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddScoped<CashfreePaymentGateway>();
        services.AddScoped<PaypalPaymentGateway>();
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        // ── Health checks ─────────────────────────────────────────────────────
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgres");

        // ── Background jobs ───────────────────────────────────────────────────
        services.AddHostedService<ProgramReminderJob>();

        return services;
    }
}
