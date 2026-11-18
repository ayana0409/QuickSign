using AutoMapper;
using QuickSign.Application.Contracts.DTOs.ApiKeys;
using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Domain.ApiKeys;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Documents;

namespace QuickSign.Application.Mappings;

/// <summary>
/// AutoMapper profile configuring mappings between Domain Aggregate Roots and Application Contracts DTOs.
/// </summary>
public sealed class QuickSignApplicationAutoMapperProfile : Profile
{
    public QuickSignApplicationAutoMapperProfile()
    {
        // Certificate -> CertificateResponseDto
        CreateMap<Certificate, CertificateResponseDto>()
            .ForMember(d => d.RevokedAt, opt => opt.MapFrom(s => s.Revocation != null ? s.Revocation.RevokedAt : (DateTimeOffset?)null))
            .ForMember(d => d.InvalidatedFrom, opt => opt.MapFrom(s => s.Revocation != null ? s.Revocation.InvalidatedFrom : (DateTimeOffset?)null))
            .ForMember(d => d.RevokedReason, opt => opt.MapFrom(s => s.Revocation != null ? s.Revocation.Reason : null));

        // ApiKey -> ApiKeyItemDto
        CreateMap<ApiKey, ApiKeyItemDto>();

        // SignedDocument -> SignedDocumentDto
        CreateMap<SignedDocument, SignedDocumentDto>()
            .ForMember(d => d.DocumentCode, opt => opt.MapFrom(s => s.DocumentCode.Value))
            .ForMember(d => d.OriginalHash, opt => opt.MapFrom(s => s.OriginalHash.Value))
            .ForMember(d => d.SignedHash, opt => opt.MapFrom(s => s.SignedHash.Value));

        // VerificationLog -> VerificationLogDto
        CreateMap<VerificationLog, VerificationLogDto>()
            .ForMember(d => d.SubmittedHash, opt => opt.MapFrom(s => s.SubmittedHash.Value));
    }
}
