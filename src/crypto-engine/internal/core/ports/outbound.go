package ports

import (
	"context"
	"time"

	"quicksign/crypto-engine/internal/core/domain"
)

// VaultKeyStorePort defines the outbound port for interacting with HashiCorp Vault Key Management Service.
type VaultKeyStorePort interface {
	// StorePrivateKey persists the PEM-encoded private key at the designated path in Vault KV v2.
	StorePrivateKey(ctx context.Context, path string, pemBytes []byte) error

	// DeletePrivateKey permanently removes the secret and its metadata from Vault.
	DeletePrivateKey(ctx context.Context, path string) error

	// GetPrivateKey retrieves the PEM-encoded private key from Vault.
	GetPrivateKey(ctx context.Context, path string) ([]byte, error)
}

// PdfCryptoPort defines the outbound port for PDF PAdES signing and signature extraction.
type PdfCryptoPort interface {
	// SignPdfPades signs an existing PDF with detached CMS PKCS#7 signature and embeds a QR code.
	SignPdfPades(pdfBytes []byte, privKey any, certDER []byte, docCode string, qrUrl string, reason, location string) ([]byte, string, error)

	// VerifyPdfPades parses and validates the PAdES signature embedded in the PDF byte stream.
	VerifyPdfPades(pdfBytes []byte) (*domain.PdfSignatureData, error)
}

// JwsCryptoPort defines the outbound port for JWS signing and verification (RFC 7515).
type JwsCryptoPort interface {
	// SignJws signs a JSON payload producing a Compact JWS string (header.payload.signature).
	SignJws(payload []byte, privKey any, algo domain.KeyAlgorithm, docCode string) (string, error)

	// VerifyJws parses and verifies the JWS compact token against the public key.
	VerifyJws(jwsString string, pubKey any) (*domain.JwsSignatureData, error)
}

// DocumentRepositoryPort defines the outbound port for PostgreSQL persistence.
type DocumentRepositoryPort interface {
	// GetActiveCertificate retrieves an active certificate for the user, or by specific ID.
	GetActiveCertificate(ctx context.Context, userId string, certId *string) (*domain.CertificateEntity, error)

	// GetCertificateById retrieves any certificate by its unique GUID.
	GetCertificateById(ctx context.Context, certId string) (*domain.CertificateEntity, error)

	// SaveSignedDocument records a newly signed document into PostgreSQL.
	SaveSignedDocument(ctx context.Context, doc *domain.SignedDocumentEntity) error

	// GetSignedDocumentByCode retrieves a signed document record using its unique document_code.
	GetSignedDocumentByCode(ctx context.Context, code string) (*domain.SignedDocumentEntity, error)

	// SaveVerificationLog appends an audit record to verification_logs.
	SaveVerificationLog(ctx context.Context, log *domain.VerificationLogEntity) error

	// IncrementVerifyCount atomically updates verify metrics on the signed document.
	IncrementVerifyCount(ctx context.Context, docId string, isUnique bool, isTampered bool) error

	// GetApiKeyBySecretHash retrieves an API key entity using its SHA-256 hashed secret.
	GetApiKeyBySecretHash(ctx context.Context, secretHash string) (*domain.ApiKeyCacheItem, error)
}

// AuthCachePort defines the outbound port for Redis caching and rate-limiting.
type AuthCachePort interface {
	// GetApiKey retrieves cached API key details from Redis.
	GetApiKey(ctx context.Context, secretHash string) (*domain.ApiKeyCacheItem, error)

	// SetApiKey stores API key details into Redis cache with a TTL.
	SetApiKey(ctx context.Context, secretHash string, item *domain.ApiKeyCacheItem, ttl time.Duration) error

	// CheckAndRecordVerifyRateLimit returns whether this verification attempt from the given IP is unique within the window.
	CheckAndRecordVerifyRateLimit(ctx context.Context, docCode, ip string) (bool, error)
}
