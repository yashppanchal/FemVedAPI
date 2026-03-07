using FemVed.Application.Guided.Commands.UpdateProgram;
using FluentValidation.TestHelper;

namespace FemVed.Tests.Unit.Guided;

public class UpdateProgramCommandValidatorTests
{
    private readonly UpdateProgramCommandValidator _validator = new();

    private static UpdateProgramCommand BaseCommand() => new(
        ProgramId:        Guid.NewGuid(),
        RequestingUserId: Guid.NewGuid(),
        IsAdmin:          false,
        Name:             null,
        GridDescription:  null,
        GridImageUrl:     null,
        Overview:         null,
        SortOrder:        null,
        WhatYouGet:       null,
        WhoIsThisFor:     null,
        Tags:             null,
        DetailSections:   null);

    [Fact]
    public void Validate_NoPatchFields_IsValid()
    {
        // Sending no patch fields (all null) is valid for a PATCH-style update
        var result = _validator.TestValidate(BaseCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProgramId_HasError()
    {
        var cmd = BaseCommand() with { ProgramId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ProgramId);
    }

    [Fact]
    public void Validate_NonNullBlankName_HasError()
    {
        // When Name is provided it must be non-whitespace
        var cmd = BaseCommand() with { Name = "   " };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ValidName_HasNoError()
    {
        var cmd = BaseCommand() with { Name = "Hormonal Health Reset" };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_HasError()
    {
        var cmd = BaseCommand() with { Name = new string('A', 301) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NonNullBlankGridDescription_HasError()
    {
        var cmd = BaseCommand() with { GridDescription = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.GridDescription);
    }
}
