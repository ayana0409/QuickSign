using AutoMapper;
using Microsoft.Extensions.Logging;
using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Contracts.Services;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;

namespace QuickSign.Application.Services;

/// <summary>
/// Application service implementing digital certificate issuance, lifecycle management, and revocation workflows.
/// Implements resilient compensating transactions to delete orphaned keys in Vault upon DB failure.
/// </summary>
public sealed class CertificateAppService : ICertificateAppService
{
    private readonly ICertificateManager _certificateManager;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICryptoEngineGrpcService _cryptoEngineGrpcService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CertificateAppService> _logger;

    public CertificateAppService(
        ICertificateManager certificateManager,
        ICertificateRepository certificateRepository,
        ICryptoEngineGrpcService cryptoEngineGrpcService,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CertificateAppService> logger)
    {
        _certificateManager = certificateManager;
        _certificateRepository = certificateRepository;
        _cryptoEngineGrpcService = cryptoEngineGrpcService;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CertificateResponseDto> IssueCertificateAsync(
        IssueCertificateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var certId = Guid.NewGuid();
        var validityDays = request.ValidityDays > 0 ? request.ValidityDays : 365;
        var validFrom = DateTimeOffset.UtcNow;
        var expiresAt = validFrom.AddDays(validityDays);

        // 1. Generate asymmetric key pair via Go Crypto Engine (Stores Private Key in Vault)
        var keyMaterial = await _cryptoEngineGrpcService.GenerateCertificateKeyPairAsync(
            certId,
            userId,
            request.SubjectName,
            request.Algorithm.ToString(),
            validFrom,
            expiresAt,
            cancellationToken);

        // 2. Validate domain invariants and construct Certificate Aggregate Root
        var certificate = await _certificateManager.CreateAsync(
            certId,
            userId,
            request.SubjectName,
            keyMaterial.SerialNumber,
            request.Algorithm,
            keyMaterial.PublicKeyPem,
            keyMaterial.VaultKeyPath,
            expiresAt,
            cancellationToken);

        // 3. Persist to PostgreSQL database with Compensating Transaction rollback safety
        try
        {
            await _certificateRepository.InsertAsync(certificate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully issued Certificate {CertId} for User {UserId}.", certId, userId);
            return _mapper.Map<CertificateResponseDto>(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist Certificate {CertId} to PostgreSQL. Triggering Compensating Transaction to clean Vault key.", certId);

            // Execute compensating transaction: Delete the private key from Vault to prevent orphan keys
            await _cryptoEngineGrpcService.DeleteCertificateKeyPairAsync(keyMaterial.VaultKeyPath, CancellationToken.None);

            throw new BusinessException(
                QuickSignDomainErrorCodes.DatabaseError,
                "Failed to save certificate metadata. Cryptographic keys have been securely purged. Please try again.",
                500,
                innerException: ex);
        }
    }

    public async Task RevokeCertificateAsync(
        Guid certificateId,
        RevokeCertificateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var certificate = await _certificateRepository.GetByIdAsync(certificateId, cancellationToken)
            ?? throw new BusinessException(
                QuickSignDomainErrorCodes.CertNotFound,
                "Certificate not found.",
                404);

        // Check ownership or Admin role
        if (certificate.UserId != userId && !_currentUserService.IsInRole("Admin"))
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.UnauthorizedCertificateAccess,
                "You are not authorized to revoke this certificate.",
                403);
        }

        // Apply domain business logic for revocation
        certificate.Revoke(request.InvalidatedFrom ?? DateTimeOffset.UtcNow, request.Reason);

        _certificateRepository.Update(certificate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate Redis cache entry if present
        await _cacheService.RemoveAsync($"cert:{certificateId}", cancellationToken);

        _logger.LogInformation("Certificate {CertId} successfully revoked by User {UserId}.", certificateId, userId);
    }

    public async Task<IReadOnlyList<CertificateResponseDto>> GetMyCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var certificates = await _certificateRepository.GetListByUserIdAsync(userId, cancellationToken);
        return _mapper.Map<IReadOnlyList<CertificateResponseDto>>(certificates);
    }

    public async Task<CertificateResponseDto> GetByIdAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var certificate = await _certificateRepository.GetByIdAsync(certificateId, cancellationToken)
            ?? throw new BusinessException(
                QuickSignDomainErrorCodes.CertNotFound,
                "Certificate not found.",
                404);

        if (certificate.UserId != userId && !_currentUserService.IsInRole("Admin"))
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.UnauthorizedCertificateAccess,
                "You are not authorized to access this certificate.",
                403);
        }

        return _mapper.Map<CertificateResponseDto>(certificate);
    }
}
