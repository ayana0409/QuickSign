package ports

import "context"

// VaultKeyStorePort defines the outbound port for interacting with HashiCorp Vault Key Management Service.
type VaultKeyStorePort interface {
	// StorePrivateKey persists the PEM-encoded private key at the designated path in Vault KV v2.
	StorePrivateKey(ctx context.Context, path string, pemBytes []byte) error

	// DeletePrivateKey permanently removes the secret and its metadata from Vault.
	DeletePrivateKey(ctx context.Context, path string) error

	// GetPrivateKey retrieves the PEM-encoded private key from Vault.
	GetPrivateKey(ctx context.Context, path string) ([]byte, error)
}
