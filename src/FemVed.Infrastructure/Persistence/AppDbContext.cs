using FemVed.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FemVed.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the FemVed platform.
/// All entity configurations are loaded automatically from the <c>Configurations</c> folder
/// via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Initializes a new instance of <see cref="AppDbContext"/> with the given options.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ───────────────────────────────────────────────────────────────

    /// <summary>Platform roles (Admin, Expert, User).</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>All platform users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>JWT refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Password reset tokens.</summary>
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    /// <summary>Top-level guided domains.</summary>
    public DbSet<GuidedDomain> GuidedDomains => Set<GuidedDomain>();

    /// <summary>Guided category pages.</summary>
    public DbSet<GuidedCategory> GuidedCategories => Set<GuidedCategory>();

    /// <summary>"What's Included" bullet points per category.</summary>
    public DbSet<CategoryWhatsIncluded> CategoryWhatsIncluded => Set<CategoryWhatsIncluded>();

    /// <summary>Key area items per category.</summary>
    public DbSet<CategoryKeyArea> CategoryKeyAreas => Set<CategoryKeyArea>();

    /// <summary>Expert practitioner profiles.</summary>
    public DbSet<Expert> Experts => Set<Expert>();

    /// <summary>Guided programs.</summary>
    public DbSet<Program> Programs => Set<Program>();

    /// <summary>"What You Get" bullets per program.</summary>
    public DbSet<ProgramWhatYouGet> ProgramWhatYouGet => Set<ProgramWhatYouGet>();

    /// <summary>"Who Is This For" bullets per program.</summary>
    public DbSet<ProgramWhoIsThisFor> ProgramWhoIsThisFor => Set<ProgramWhoIsThisFor>();

    /// <summary>Filter tags per program.</summary>
    public DbSet<ProgramTag> ProgramTags => Set<ProgramTag>();

    /// <summary>User testimonials per program.</summary>
    public DbSet<ProgramTestimonial> ProgramTestimonials => Set<ProgramTestimonial>();

    /// <summary>Duration options per program.</summary>
    public DbSet<ProgramDuration> ProgramDurations => Set<ProgramDuration>();

    /// <summary>Location-specific prices per duration.</summary>
    public DbSet<DurationPrice> DurationPrices => Set<DurationPrice>();

    /// <summary>Discount coupons.</summary>
    public DbSet<Coupon> Coupons => Set<Coupon>();

    /// <summary>Purchase orders.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Refund records.</summary>
    public DbSet<Refund> Refunds => Set<Refund>();

    /// <summary>Post-purchase program access records.</summary>
    public DbSet<UserProgramAccess> UserProgramAccess => Set<UserProgramAccess>();

    /// <summary>Expert progress update messages.</summary>
    public DbSet<ExpertProgressUpdate> ExpertProgressUpdates => Set<ExpertProgressUpdate>();

    /// <summary>Notification delivery audit log.</summary>
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    /// <summary>Admin/Expert mutation audit log.</summary>
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    /// <summary>GDPR data-erasure requests.</summary>
    public DbSet<GdprDeletionRequest> GdprDeletionRequests => Set<GdprDeletionRequest>();

    // ── Model configuration ──────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discover all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
