package ports

import (
	"context"

	"quicksign/crypto-engine/internal/core/domain"
)

// KeyGenerationUseCase defines the inbound port for asymmetric key lifecycle management.
type KeyGenerationUseCase interface {
	// GenerateKeyPair generates an asymmetric key pair, creates an X.509 certificate,
	// securely persists the private key into Vault, and returns public certificate metadata.
	GenerateKeyPair(ctx context.Context, meta domain.CertificateMetadata) (*domain.GeneratedKeyPairResult, error)

	// DeleteKeyPair executes a compensating transaction to delete an orphaned key from Vault
	// if downstream database persistence fails in the management layer.
	DeleteKeyPair(ctx context.Context, vaultKeyPath string) error
}

// DocumentSigningUseCase defines the inbound port for signing PDFs and structured JSON.
type DocumentSigningUseCase interface {
	// SignPdf signs a PDF document using PAdES-BES standard with visual QR code and PKCS#9 attributes.
	SignPdf(ctx context.Context, input domain.SignPdfInput) (*domain.SignPdfOutput, error)

	// SignJson signs a JSON payload producing a standardized RFC 7515 JWS token.
	SignJson(ctx context.Context, input domain.SignJsonInput) (*domain.SignJsonOutput, error)
}

// DocumentVerificationUseCase defines the inbound port for document integrity verification and QR lookup.
type DocumentVerificationUseCase interface {
	// VerifyFile verifies the cryptographic signature of an uploaded PDF or JWS token.
	VerifyFile(ctx context.Context, input domain.VerifyFileInput) (*domain.VerifyResult, error)

	// VerifyByCode performs a public QR code lookup while preventing Security Illusion.
	VerifyByCode(ctx context.Context, documentCode string, verifierIP string, userAgent string) (*domain.VerifyResult, error)
}
