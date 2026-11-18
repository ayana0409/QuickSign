using FluentValidation;
using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.Application.Validators;

public sealed class IssueCertificateRequestValidator : AbstractValidator<IssueCertificateRequestDto>
{
    public IssueCertificateRequestValidator()
    {
        RuleFor(x => x.SubjectName)
            .NotEmpty().WithMessage("Subject name is required.")
            .MaximumLength(CertificateConsts.MaxSubjectNameLength)
            .WithMessage($"Subject name cannot exceed {CertificateConsts.MaxSubjectNameLength} characters.");

        RuleFor(x => x.ValidityDays)
            .InclusiveBetween(1, 3650)
            .WithMessage("Validity period must be between 1 and 3650 days (up to 10 years).");
    }
}
