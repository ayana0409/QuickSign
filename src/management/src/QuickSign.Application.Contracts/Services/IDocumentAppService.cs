using QuickSign.Application.Contracts.DTOs.Documents;

namespace QuickSign.Application.Contracts.Services;

public interface IDocumentAppService
{
    Task<IReadOnlyList<SignedDocumentDto>> GetMyDocumentsAsync(int skipCount = 0, int maxResultCount = 20, CancellationToken cancellationToken = default);
    Task<int> GetMyDocumentsCountAsync(CancellationToken cancellationToken = default);
    Task<SignedDocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SignedDocumentDto> GetByCodeAsync(string documentCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VerificationLogDto>> GetVerificationHistoryAsync(Guid documentId, int skipCount = 0, int maxResultCount = 50, CancellationToken cancellationToken = default);
}
