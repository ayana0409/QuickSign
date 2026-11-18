using FluentValidation;
using QuickSign.Application.Contracts.DTOs.Certificates;

namespace QuickSign.Application.Validators;

public sealed class RevokeCertificateRequestValidator : AbstractValidator<RevokeCertificateRequestDto>
{
    public RevokeCertificateRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(255)
            .WithMessage("Revocation reason cannot exceed 255 characters.");
    }
}
