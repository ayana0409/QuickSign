package vault

import (
	"context"
	"errors"
	"fmt"
	"strings"

	vaultapi "github.com/hashicorp/vault/api"
	"quicksign/crypto-engine/internal/core/ports"
)

var (
	ErrNilClient        = errors.New("vault client cannot be nil")
	ErrEmptyPath        = errors.New("vault key path cannot be empty")
	ErrKeyDataNotFound  = errors.New("private key data field not found in vault secret")
)

const (
	DefaultPrivateKeyField = "private_key_pem"
)

// HashiCorpVaultAdapter implements ports.VaultKeyStorePort using the official Vault KV v2 client.
type HashiCorpVaultAdapter struct {
	client    *vaultapi.Client
	mountPath string
}

// NewHashiCorpVaultAdapter constructs a new HashiCorpVaultAdapter.
func NewHashiCorpVaultAdapter(client *vaultapi.Client, mountPath string) (*HashiCorpVaultAdapter, error) {
	if client == nil {
		return nil, ErrNilClient
	}
	if strings.TrimSpace(mountPath) == "" {
		mountPath = "secret"
	}
	return &HashiCorpVaultAdapter{
		client:    client,
		mountPath: strings.Trim(mountPath, "/"),
	}, nil
}

// StorePrivateKey persists the PEM-encoded private key at the designated path in Vault KV v2.
func (a *HashiCorpVaultAdapter) StorePrivateKey(ctx context.Context, path string, pemBytes []byte) error {
	cleanPath := strings.Trim(path, "/")
	if cleanPath == "" {
		return ErrEmptyPath
	}

	data := map[string]interface{}{
		DefaultPrivateKeyField: string(pemBytes),
	}

	_, err := a.client.KVv2(a.mountPath).Put(ctx, cleanPath, data)
	if err != nil {
		return fmt.Errorf("failed to write secret to vault at '%s/%s': %w", a.mountPath, cleanPath, err)
	}

	return nil
}

// DeletePrivateKey permanently deletes the secret and its metadata from Vault KV v2.
func (a *HashiCorpVaultAdapter) DeletePrivateKey(ctx context.Context, path string) error {
	cleanPath := strings.Trim(path, "/")
	if cleanPath == "" {
		return ErrEmptyPath
	}

	// DeleteMetadata removes all versions and the metadata itself, leaving no orphan keys
	err := a.client.KVv2(a.mountPath).DeleteMetadata(ctx, cleanPath)
	if err != nil {
		return fmt.Errorf("failed to purge secret metadata from vault at '%s/%s': %w", a.mountPath, cleanPath, err)
	}

	return nil
}

// GetPrivateKey retrieves the PEM-encoded private key from Vault KV v2.
func (a *HashiCorpVaultAdapter) GetPrivateKey(ctx context.Context, path string) ([]byte, error) {
	cleanPath := strings.Trim(path, "/")
	if cleanPath == "" {
		return nil, ErrEmptyPath
	}

	secret, err := a.client.KVv2(a.mountPath).Get(ctx, cleanPath)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve secret from vault at '%s/%s': %w", a.mountPath, cleanPath, err)
	}
	if secret == nil || secret.Data == nil {
		return nil, fmt.Errorf("no secret data returned from vault at '%s/%s'", a.mountPath, cleanPath)
	}

	val, exists := secret.Data[DefaultPrivateKeyField]
	if !exists {
		return nil, ErrKeyDataNotFound
	}

	pemStr, ok := val.(string)
	if !ok {
		return nil, fmt.Errorf("vault secret field '%s' is not a string", DefaultPrivateKeyField)
	}

	return []byte(pemStr), nil
}

// Ensure interface compliance at compile time.
var _ ports.VaultKeyStorePort = (*HashiCorpVaultAdapter)(nil)
