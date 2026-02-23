namespace FemVed.Domain.Entities;

/// <summary>A progress note sent by an expert to a specific enrolled user.</summary>
public class ExpertProgressUpdate
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the user's program access record this update relates to.</summary>
    public Guid AccessId { get; set; }

    /// <summary>FK to the expert who sent this update.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>The update message content.</summary>
    public string UpdateNote { get; set; } = string.Empty;

    /// <summary>Whether to also send this update as an email via SendGrid.</summary>
    public bool SendEmail { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigations
    /// <summary>The access record this update is for.</summary>
    public UserProgramAccess Access { get; set; } = null!;

    /// <summary>The expert who sent this update.</summary>
    public Expert Expert { get; set; } = null!;
}
