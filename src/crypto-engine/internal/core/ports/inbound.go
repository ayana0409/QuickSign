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
