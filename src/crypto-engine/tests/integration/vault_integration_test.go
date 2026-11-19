package integration_test

import (
	"context"
	"os"
	"testing"

	vaultapi "github.com/hashicorp/vault/api"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"quicksign/crypto-engine/internal/adapters/outbound/vault"
)

func getVaultClient(t *testing.T) (*vaultapi.Client, string) {
	vaultAddr := os.Getenv("VAULT_ADDR")
	vaultToken := os.Getenv("VAULT_TOKEN")

	if vaultAddr == "" || vaultToken == "" {
		t.Skip("Skipping Vault integration test: VAULT_ADDR and VAULT_TOKEN environment variables not set")
	}

	cfg := vaultapi.DefaultConfig()
	cfg.Address = vaultAddr
	client, err := vaultapi.NewClient(cfg)
	require.NoError(t, err)
	client.SetToken(vaultToken)

	mountPath := os.Getenv("VAULT_KV_MOUNT")
	if mountPath == "" {
		mountPath = "secret"
	}

	return client, mountPath
}

func TestVaultAdapter_StoreAndDelete_Integration(t *testing.T) {
	client, mount := getVaultClient(t)
	adapter, err := vault.NewHashiCorpVaultAdapter(client, mount)
	require.NoError(t, err)

	ctx := context.Background()
	testPath := "quicksign-test/users/test-user-001/keys/test-cert-integration"
	testPEM := []byte("-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC...\n-----END PRIVATE KEY-----")

	// Store private key
	err = adapter.StorePrivateKey(ctx, testPath, testPEM)
	require.NoError(t, err, "Storing key to real Vault must succeed")

	// Read private key back and verify content matches
	retrievedPEM, err := adapter.GetPrivateKey(ctx, testPath)
	require.NoError(t, err, "Retrieving key from real Vault must succeed")
	assert.Equal(t, testPEM, retrievedPEM, "Retrieved PEM must match stored PEM exactly")

	// Delete private key (clean up)
	err = adapter.DeletePrivateKey(ctx, testPath)
	require.NoError(t, err, "Deleting key from real Vault must succeed")

	// Verify key is truly purged
	_, err = adapter.GetPrivateKey(ctx, testPath)
	assert.Error(t, err, "Accessing purged key must return an error")
}

func TestVaultAdapter_DeleteNonExistent_NoError(t *testing.T) {
	client, mount := getVaultClient(t)
	adapter, err := vault.NewHashiCorpVaultAdapter(client, mount)
	require.NoError(t, err)

	ctx := context.Background()
	err = adapter.DeletePrivateKey(ctx, "quicksign-test/non-existent-path/key")
	assert.NoError(t, err, "Deleting non-existent path in Vault should not return error")
}
