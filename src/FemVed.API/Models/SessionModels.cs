namespace FemVed.API.Controllers;

// Shared request/response records used by both UsersController and ExpertsController
// for enrollment lifecycle and comment endpoints.

/// <summary>HTTP request body for session lifecycle actions (start/pause/resume/end).</summary>
/// <param name="Note">Optional reason or message logged against this action.</param>
public record SessionActionRequest(string? Note);

/// <summary>Response returned after a successful session lifecycle action.</summary>
/// <param name="AccessId">UUID of the enrollment record acted upon.</param>
/// <param name="Action">The action performed: started, paused, resumed, or ended.</param>
public record SessionActionResponse(Guid AccessId, string Action);

/// <summary>HTTP request body for POST /comments endpoints.</summary>
/// <param name="UpdateNote">The comment text (10–2000 characters).</param>
public record SendCommentRequest(string UpdateNote);

/// <summary>Response returned after a comment is successfully sent.</summary>
/// <param name="AccessId">UUID of the enrollment record the comment was sent for.</param>
/// <param name="Sent">Always true when the operation succeeds.</param>
public record CommentSentResponse(Guid AccessId, bool Sent);
