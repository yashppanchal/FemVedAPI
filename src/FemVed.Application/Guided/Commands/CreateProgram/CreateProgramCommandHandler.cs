using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.CreateProgram;

/// <summary>
/// Handles <see cref="CreateProgramCommand"/>.
/// Resolves the expert profile from the requesting user, creates the program as DRAFT,
/// and persists all child records (durations, prices, WhatYouGet, WhoIsThisFor, tags).
/// </summary>
public sealed class CreateProgramCommandHandler : IRequestHandler<CreateProgramCommand, Guid>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<GuidedCategory> _categories;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IRepository<ProgramWhatYouGet> _whatYouGet;
    private readonly IRepository<ProgramWhoIsThisFor> _whoIsThisFor;
    private readonly IRepository<ProgramTag> _tags;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CreateProgramCommandHandler(
        IRepository<Expert> experts,
        IRepository<GuidedCategory> categories,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IRepository<ProgramWhatYouGet> whatYouGet,
        IRepository<ProgramWhoIsThisFor> whoIsThisFor,
        IRepository<ProgramTag> tags,
        IUnitOfWork uow,
        ILogger<CreateProgramCommandHandler> logger)
    {
        _experts = experts;
        _categories = categories;
        _programs = programs;
        _durations = durations;
        _prices = prices;
        _whatYouGet = whatYouGet;
        _whoIsThisFor = whoIsThisFor;
        _tags = tags;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Creates the program and all child records as DRAFT.</summary>
    /// <param name="request">The create program command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new program's primary key.</returns>
    /// <exception cref="NotFoundException">Thrown when the expert profile or category is not found.</exception>
    /// <exception cref="ValidationException">Thrown when the slug is already in use.</exception>
    public async Task<Guid> Handle(CreateProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating program with slug {Slug}", request.Slug);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.UserId == request.RequestingUserId && !e.IsDeleted && e.IsActive,
            cancellationToken)
            ?? throw new NotFoundException("Expert profile not found for this user. Only registered experts can create programs.");

        var categoryExists = await _categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);
        if (!categoryExists)
            throw new NotFoundException(nameof(GuidedCategory), request.CategoryId);

        var slugExists = await _programs.AnyAsync(p => p.Slug == request.Slug && !p.IsDeleted, cancellationToken);
        if (slugExists)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "slug", [$"A program with slug '{request.Slug}' already exists."] }
            });

        var program = new Domain.Entities.Program
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            ExpertId = expert.Id,
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            GridDescription = request.GridDescription.Trim(),
            GridImageUrl = request.GridImageUrl?.Trim(),
            Overview = request.Overview.Trim(),
            Status = ProgramStatus.Draft,
            SortOrder = request.SortOrder,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _programs.AddAsync(program);

        foreach (var dur in request.Durations)
        {
            var duration = new ProgramDuration
            {
                Id = Guid.NewGuid(),
                ProgramId = program.Id,
                Label = dur.Label.Trim(),
                Weeks = dur.Weeks,
                SortOrder = dur.SortOrder,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _durations.AddAsync(duration);

            foreach (var p in dur.Prices)
            {
                await _prices.AddAsync(new DurationPrice
                {
                    Id = Guid.NewGuid(),
                    DurationId = duration.Id,
                    LocationCode = p.LocationCode.ToUpperInvariant(),
                    Amount = p.Amount,
                    CurrencyCode = p.CurrencyCode.ToUpperInvariant(),
                    CurrencySymbol = p.CurrencySymbol,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        for (var i = 0; i < request.WhatYouGet.Count; i++)
            await _whatYouGet.AddAsync(new ProgramWhatYouGet
            {
                Id = Guid.NewGuid(), ProgramId = program.Id,
                ItemText = request.WhatYouGet[i].Trim(), SortOrder = i
            });

        for (var i = 0; i < request.WhoIsThisFor.Count; i++)
            await _whoIsThisFor.AddAsync(new ProgramWhoIsThisFor
            {
                Id = Guid.NewGuid(), ProgramId = program.Id,
                ItemText = request.WhoIsThisFor[i].Trim(), SortOrder = i
            });

        foreach (var tag in request.Tags)
            await _tags.AddAsync(new ProgramTag
            {
                Id = Guid.NewGuid(), ProgramId = program.Id,
                Tag = tag.Trim().ToLowerInvariant()
            });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Program {ProgramId} created as DRAFT for expert {ExpertId}", program.Id, expert.Id);
        return program.Id;
    }
}
