using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// An individual guided program offered by an expert within a category.
/// Status flow: DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED.
/// Soft-deletable.
/// </summary>
public class Program
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the category this program belongs to.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>FK to the expert who created this program.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>Full program name shown in the catalog.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL slug, e.g. "break-stress-hormone-health-triangle". Must be unique.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Short description shown in the program grid card (max 500 chars).</summary>
    public string GridDescription { get; set; } = string.Empty;

    /// <summary>Grid card image URL (hosted on Cloudflare R2).</summary>
    public string? GridImageUrl { get; set; }

    /// <summary>Full program overview shown on the detail page.</summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    public ProgramStatus Status { get; set; } = ProgramStatus.Draft;

    /// <summary>Optional program start date.</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Optional program end date.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Whether this program is visible in catalog queries.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft-delete flag. Never hard-delete programs.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Display ordering within the category (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The category this program belongs to.</summary>
    public GuidedCategory Category { get; set; } = null!;

    /// <summary>The expert who offers this program.</summary>
    public Expert Expert { get; set; } = null!;

    /// <summary>What participants receive ("What You Get" bullets).</summary>
    public ICollection<ProgramWhatYouGet> WhatYouGet { get; set; } = new List<ProgramWhatYouGet>();

    /// <summary>Target audience items ("Who Is This For" bullets).</summary>
    public ICollection<ProgramWhoIsThisFor> WhoIsThisFor { get; set; } = new List<ProgramWhoIsThisFor>();

    /// <summary>Filter tags, e.g. "stress", "pcos".</summary>
    public ICollection<ProgramTag> Tags { get; set; } = new List<ProgramTag>();

    /// <summary>User testimonials for this program.</summary>
    public ICollection<ProgramTestimonial> Testimonials { get; set; } = new List<ProgramTestimonial>();

    /// <summary>Duration options (4 weeks, 6 weeks, etc.) with their location-specific prices.</summary>
    public ICollection<ProgramDuration> Durations { get; set; } = new List<ProgramDuration>();

    /// <summary>Heading + description sections shown on the program detail page.</summary>
    public ICollection<ProgramDetailSection> DetailSections { get; set; } = new List<ProgramDetailSection>();
}
