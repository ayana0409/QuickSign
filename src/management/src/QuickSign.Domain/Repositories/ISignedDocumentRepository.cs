using QuickSign.Domain.Documents;

namespace QuickSign.Domain.Repositories;

/// <summary>
/// Repository abstraction for SignedDocument aggregate roots and verification log querying.
/// </summary>
public interface ISignedDocumentRepository
{
    Task<SignedDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SignedDocument?> GetByCodeAsync(string documentCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SignedDocument>> GetListByUserIdAsync(string userId, int skipCount = 0, int maxResultCount = 20, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VerificationLog>> GetVerificationLogsAsync(Guid signedDocumentId, int skipCount = 0, int maxResultCount = 50, CancellationToken cancellationToken = default);
    Task InsertAsync(SignedDocument document, CancellationToken cancellationToken = default);
    Task InsertVerificationLogAsync(VerificationLog log, CancellationToken cancellationToken = default);
    void Update(SignedDocument document);
}
