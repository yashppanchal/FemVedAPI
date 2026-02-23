using MediatR;

namespace FemVed.Application.Guided.Commands.CreateProgram;

/// <summary>Input model for a duration option when creating a program.</summary>
/// <param name="Label">Human-readable label, e.g. "6 weeks".</param>
/// <param name="Weeks">Number of weeks (used for data integrity).</param>
/// <param name="SortOrder">Display ordering.</param>
/// <param name="Prices">Location-specific prices for this duration.</param>
public record DurationInput(string Label, short Weeks, int SortOrder, List<DurationPriceInput> Prices);

/// <summary>Input model for a single location-specific price.</summary>
/// <param name="LocationCode">ISO country code, e.g. "GB", "IN", "US".</param>
/// <param name="Amount">Price amount, e.g. 320.00.</param>
/// <param name="CurrencyCode">ISO 4217 code, e.g. "GBP".</param>
/// <param name="CurrencySymbol">Display symbol, e.g. "£".</param>
public record DurationPriceInput(string LocationCode, decimal Amount, string CurrencyCode, string CurrencySymbol);

/// <summary>
/// Creates a new program as DRAFT. Expert creates for their own profile.
/// The requesting user's expert profile ID is resolved from <paramref name="RequestingUserId"/>.
/// </summary>
/// <param name="RequestingUserId">Authenticated user ID — used to resolve the expert profile.</param>
/// <param name="CategoryId">The category this program belongs to.</param>
/// <param name="Name">Full program name.</param>
/// <param name="Slug">Unique URL slug, e.g. "break-stress-hormone-health-triangle".</param>
/// <param name="GridDescription">Short description for the grid card (max 500 chars).</param>
/// <param name="GridImageUrl">Optional grid card image URL.</param>
/// <param name="Overview">Full program overview for the detail page.</param>
/// <param name="SortOrder">Display ordering within the category.</param>
/// <param name="Durations">Duration options with location-specific prices.</param>
/// <param name="WhatYouGet">Benefit bullet points.</param>
/// <param name="WhoIsThisFor">Target audience bullet points.</param>
/// <param name="Tags">Filter tag values, e.g. ["stress", "hormones"].</param>
public record CreateProgramCommand(
    Guid RequestingUserId,
    Guid CategoryId,
    string Name,
    string Slug,
    string GridDescription,
    string? GridImageUrl,
    string Overview,
    int SortOrder,
    List<DurationInput> Durations,
    List<string> WhatYouGet,
    List<string> WhoIsThisFor,
    List<string> Tags) : IRequest<Guid>;
