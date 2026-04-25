using FemVed.Application.Contact.Commands.SubmitContact;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles public contact-form submissions.
/// Base route: /api/v1/contact
/// </summary>
[ApiController]
[Route("api/v1/contact")]
public sealed class ContactController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public ContactController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Accepts a contact-form submission. Sends a thank-you email to the submitter and a
    /// notification email to every address listed in the <c>ADMIN_NOTIFICATION_EMAILS</c>
    /// configuration value.
    /// </summary>
    /// <param name="request">Submitter name, email, and message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted on success.</returns>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitContactCommand request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(request, cancellationToken);
        return Accepted();
    }
}
