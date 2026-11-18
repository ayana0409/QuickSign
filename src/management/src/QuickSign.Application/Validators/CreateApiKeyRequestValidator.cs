using FluentValidation;
using QuickSign.Application.Contracts.DTOs.ApiKeys;
using QuickSign.Domain.Shared.ApiKeys;

namespace QuickSign.Application.Validators;

public sealed class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequestDto>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API Key friendly name is required.")
            .MaximumLength(ApiKeyConsts.MaxNameLength)
            .WithMessage($"API Key name cannot exceed {ApiKeyConsts.MaxNameLength} characters.");

        RuleFor(x => x.Scopes)
            .NotEmpty().WithMessage("Scopes cannot be empty.")
            .MaximumLength(ApiKeyConsts.MaxScopesLength)
            .WithMessage($"Scopes cannot exceed {ApiKeyConsts.MaxScopesLength} characters.");
    }
}
