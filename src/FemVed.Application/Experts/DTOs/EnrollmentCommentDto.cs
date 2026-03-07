namespace FemVed.Application.Experts.DTOs;

/// <summary>
/// A single comment (progress update) sent by an expert or admin to an enrolled user.
/// Returned by GET /api/v1/experts/me/enrollments/{accessId}/comments
/// and GET /api/v1/admin/enrollments/{accessId}/comments.
/// </summary>
/// <param name="CommentId">UUID of the ExpertProgressUpdate record.</param>
/// <param name="AccessId">UUID of the UserProgramAccess record this comment belongs to.</param>
/// <param name="ExpertId">UUID of the expert who wrote this comment.</param>
/// <param name="UpdateNote">The comment text.</param>
/// <param name="CreatedAt">UTC timestamp when the comment was sent.</param>
public record EnrollmentCommentDto(
    Guid CommentId,
    Guid AccessId,
    Guid ExpertId,
    string UpdateNote,
    DateTimeOffset CreatedAt);
