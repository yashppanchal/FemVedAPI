namespace FemVed.Application.Guided.DTOs;

// ── All record types below match the EXACT camelCase JSON contract in CLAUDE.md §13.
// Field names must not be changed — the React frontend binds directly to this shape.

/// <summary>Root response for GET /api/v1/guided/tree.</summary>
/// <param name="Domains">All active domains with their categories and programs.</param>
public record GuidedTreeResponse(List<GuidedDomainDto> Domains);

/// <summary>A top-level product domain, e.g. "Guided 1:1 Care".</summary>
/// <param name="DomainId">Domain primary key.</param>
/// <param name="DomainName">Display name.</param>
/// <param name="Categories">Active top-level categories (no parent) within this domain.</param>
public record GuidedDomainDto(
    Guid DomainId,
    string DomainName,
    List<GuidedCategoryDto> Categories);

/// <summary>A category within a domain, with all page content and programs.</summary>
/// <param name="CategoryId">Category primary key.</param>
/// <param name="CategoryName">URL slug, e.g. "hormonal-health-support".</param>
/// <param name="CategoryPageData">All content needed to render the category page.</param>
/// <param name="ProgramsInCategory">Published programs listed under this category.</param>
public record GuidedCategoryDto(
    Guid CategoryId,
    string CategoryName,
    CategoryPageDataDto CategoryPageData,
    List<ProgramInCategoryDto> ProgramsInCategory);

/// <summary>Content block for a category hero / detail page.</summary>
/// <param name="CategoryPageDataImage">Category hero image URL.</param>
/// <param name="CategoryType">Short display type, e.g. "Hormonal Health Support".</param>
/// <param name="CategoryHeroTitle">Main headline on the category page.</param>
/// <param name="CategoryHeroSubtext">Supporting copy below the headline.</param>
/// <param name="CategoryCtaLabel">Call-to-action button label.</param>
/// <param name="CategoryCtaTo">Call-to-action link target.</param>
/// <param name="WhatsIncludedInCategory">Bullet list for the "What's Included" hero section.</param>
/// <param name="CategoryPageHeader">Section header above the program grid.</param>
/// <param name="CategoryPageKeyAreas">Key support area bullet points.</param>
public record CategoryPageDataDto(
    string? CategoryPageDataImage,
    string CategoryType,
    string CategoryHeroTitle,
    string CategoryHeroSubtext,
    string? CategoryCtaLabel,
    string? CategoryCtaTo,
    List<string> WhatsIncludedInCategory,
    string? CategoryPageHeader,
    List<string> CategoryPageKeyAreas);

/// <summary>A single published program shown in the category grid.</summary>
/// <param name="ProgramId">Program primary key.</param>
/// <param name="ProgramName">Full program name.</param>
/// <param name="ProgramGridDescription">Short description for the grid card.</param>
/// <param name="ProgramGridImage">Grid card image URL.</param>
/// <param name="ExpertId">Expert primary key.</param>
/// <param name="ExpertName">Expert display name, e.g. "Dr. Prathima Nagesh".</param>
/// <param name="ExpertTitle">Expert professional title.</param>
/// <param name="ExpertGridDescription">Short bio used in the program grid card (max 500 chars).</param>
/// <param name="ExpertDetailedDescription">Detailed expert description shown on the program detail page.</param>
/// <param name="ExpertGridImageUrl">Expert image URL for the program grid card.</param>
/// <param name="ProgramDurations">Available duration options with location-formatted prices.</param>
/// <param name="ProgramPageDisplayDetails">Full detail page content.</param>
public record ProgramInCategoryDto(
    Guid ProgramId,
    string ProgramName,
    string ProgramGridDescription,
    string? ProgramGridImage,
    Guid ExpertId,
    string ExpertName,
    string ExpertTitle,
    string? ExpertGridDescription,
    string? ExpertDetailedDescription,
    string? ExpertGridImageUrl,
    List<ProgramDurationDto> ProgramDurations,
    ProgramPageDisplayDetailsDto ProgramPageDisplayDetails);

/// <summary>A duration option with its location-specific price formatted for display.</summary>
/// <param name="DurationId">Duration primary key.</param>
/// <param name="DurationLabel">Human-readable label, e.g. "6 weeks".</param>
/// <param name="DurationPrice">Formatted price string, e.g. "£320", "$400", "₹33,000".</param>
public record ProgramDurationDto(
    Guid DurationId,
    string DurationLabel,
    string DurationPrice);

/// <summary>Full detail page content for a program.</summary>
/// <param name="Overview">Full program overview text.</param>
/// <param name="WhatYouGet">Benefit bullet points.</param>
/// <param name="WhoIsThisFor">Target audience bullet points.</param>
/// <param name="DetailSections">Ordered heading + description sections on the program detail page.</param>
public record ProgramPageDisplayDetailsDto(
    string Overview,
    List<string> WhatYouGet,
    List<string> WhoIsThisFor,
    List<ProgramDetailSectionDto> DetailSections);

/// <summary>A single heading + description section on the program detail page.</summary>
/// <param name="Heading">Section title, e.g. "Reset Stress Patterns, Restore Hormonal Balance".</param>
/// <param name="Description">Section body text shown beneath the heading.</param>
public record ProgramDetailSectionDto(string Heading, string Description);
