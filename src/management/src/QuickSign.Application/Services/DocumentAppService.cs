using AutoMapper;
using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Contracts.Services;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;

namespace QuickSign.Application.Services;

/// <summary>
/// Application service providing signed document metadata lookup and verification audit history.
/// </summary>
public sealed class DocumentAppService : IDocumentAppService
{
    private readonly ISignedDocumentRepository _signedDocumentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public DocumentAppService(
        ISignedDocumentRepository signedDocumentRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _signedDocumentRepository = signedDocumentRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SignedDocumentDto>> GetMyDocumentsAsync(
        int skipCount = 0,
        int maxResultCount = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var documents = await _signedDocumentRepository.GetListByUserIdAsync(userId, skipCount, maxResultCount, cancellationToken);
        return _mapper.Map<IReadOnlyList<SignedDocumentDto>>(documents);
    }

    public Task<int> GetMyDocumentsCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        return _signedDocumentRepository.GetCountByUserIdAsync(userId, cancellationToken);
    }

    public async Task<SignedDocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var document = await _signedDocumentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessException(
                QuickSignDomainErrorCodes.DocumentNotFound,
                "Document not found.",
                404);

        if (document.UserId != userId && !_currentUserService.IsInRole("Admin"))
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.ForbiddenAccess,
                "You are not authorized to view this document.",
                403);
        }

        return _mapper.Map<SignedDocumentDto>(document);
    }

    public async Task<SignedDocumentDto> GetByCodeAsync(string documentCode, CancellationToken cancellationToken = default)
    {
        var document = await _signedDocumentRepository.GetByCodeAsync(documentCode, cancellationToken)
            ?? throw new BusinessException(
                QuickSignDomainErrorCodes.DocumentNotFound,
                "Document not found with the specified code.",
                404);

        return _mapper.Map<SignedDocumentDto>(document);
    }

    public async Task<IReadOnlyList<VerificationLogDto>> GetVerificationHistoryAsync(
        Guid documentId,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var document = await _signedDocumentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new BusinessException(
                QuickSignDomainErrorCodes.DocumentNotFound,
                "Document not found.",
                404);

        if (document.UserId != userId && !_currentUserService.IsInRole("Admin"))
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.ForbiddenAccess,
                "You are not authorized to view verification history for this document.",
                403);
        }

        var logs = await _signedDocumentRepository.GetVerificationLogsAsync(documentId, skipCount, maxResultCount, cancellationToken);
        return _mapper.Map<IReadOnlyList<VerificationLogDto>>(logs);
    }
}
