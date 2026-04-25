using MediatR;

namespace FemVed.Application.Contact.Commands.SubmitContact;

/// <summary>
/// Submits a public contact-form message. The handler emails a thank-you to the
/// submitter and a notification (with the message body) to all configured admins.
/// </summary>
/// <param name="Name">Submitter's full name.</param>
/// <param name="Email">Submitter's email address — also used as Reply-To on the admin notification.</param>
/// <param name="Message">Free-text message from the submitter.</param>
public sealed record SubmitContactCommand(
    string Name,
    string Email,
    string Message) : IRequest;
