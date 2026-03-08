namespace FemVed.Application.Admin.DTOs;

// ── Program & Expert Performance Analytics ────────────────────────────────────

/// <summary>Program and expert performance analytics for the admin dashboard.</summary>
/// <param name="Programs">Per-program sales and enrollment stats.</param>
/// <param name="Experts">Per-expert revenue and enrollment aggregates.</param>
public record ProgramAnalyticsDto(
    List<ProgramStatsDto> Programs,
    List<ExpertRevenueDto> Experts);

/// <summary>Sales and enrollment statistics for a single program.</summary>
/// <param name="ProgramId">UUID of the program.</param>
/// <param name="ProgramName">Full program name.</param>
/// <param name="ExpertName">Display name of the expert who owns it.</param>
/// <param name="CategoryName">Name of the parent category.</param>
/// <param name="Status">Current lifecycle status: Draft, PendingReview, Published, Archived.</param>
/// <param name="TotalSales">Number of paid orders for this program's durations.</param>
/// <param name="ActiveEnrollments">Enrollments in NotStarted, Active, or Paused state.</param>
/// <param name="CompletedEnrollments">Enrollments in Completed state.</param>
/// <param name="TotalEnrollments">All enrollment records for this program.</param>
/// <param name="Revenue">Revenue per currency from paid orders.</param>
public record ProgramStatsDto(
    Guid ProgramId,
    string? ProgramName,
    string ExpertName,
    string CategoryName,
    string Status,
    int TotalSales,
    int ActiveEnrollments,
    int CompletedEnrollments,
    int TotalEnrollments,
    List<CurrencyAmountDto> Revenue);

/// <summary>Revenue and enrollment summary for a single expert across all their programs.</summary>
/// <param name="ExpertId">UUID of the expert profile.</param>
/// <param name="ExpertName">Display name.</param>
/// <param name="CommissionRate">Expert's revenue share percentage, e.g. 80.00.</param>
/// <param name="TotalSales">Total paid orders across all their programs.</param>
/// <param name="TotalEnrollments">Total enrollment records across all their programs.</param>
/// <param name="ActiveEnrollments">Active + paused + not-started enrollments.</param>
/// <param name="TotalRevenue">Gross revenue per currency from their programs.</param>
/// <param name="ExpertShare">Expert's portion (CommissionRate%) of TotalRevenue per currency.</param>
/// <param name="PlatformRevenue">Platform's portion ((100-CommissionRate)%) per currency.</param>
public record ExpertRevenueDto(
    Guid ExpertId,
    string ExpertName,
    decimal CommissionRate,
    int TotalSales,
    int TotalEnrollments,
    int ActiveEnrollments,
    List<CurrencyAmountDto> TotalRevenue,
    List<CurrencyAmountDto> ExpertShare,
    List<CurrencyAmountDto> PlatformRevenue);
