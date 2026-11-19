package unit_test

import (
	"context"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"encoding/pem"
	"errors"
	"math/big"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/services"
	"quicksign/crypto-engine/pkg/memguard"
)

// mockVaultKeyStore implements ports.VaultKeyStorePort for in-memory unit testing.
type mockVaultKeyStore struct {
	storedKeys map[string][]byte
	storeErr   error
	deleteErr  error
}

func newMockVaultKeyStore() *mockVaultKeyStore {
	return &mockVaultKeyStore{
		storedKeys: make(map[string][]byte),
	}
}

func (m *mockVaultKeyStore) StorePrivateKey(ctx context.Context, path string, pemBytes []byte) error {
	if m.storeErr != nil {
		return m.storeErr
	}
	cp := make([]byte, len(pemBytes))
	copy(cp, pemBytes)
	m.storedKeys[path] = cp
	return nil
}

func (m *mockVaultKeyStore) DeletePrivateKey(ctx context.Context, path string) error {
	if m.deleteErr != nil {
		return m.deleteErr
	}
	delete(m.storedKeys, path)
	return nil
}

func (m *mockVaultKeyStore) GetPrivateKey(ctx context.Context, path string) ([]byte, error) {
	data, exists := m.storedKeys[path]
	if !exists {
		return nil, domain.ErrKeyNotFound
	}
	return data, nil
}

func TestGenerateKeyPair_RSA2048_Success(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	svc, err := services.NewKeyGenService(mockVault, "quicksign/users")
	require.NoError(t, err)

	now := time.Now().UTC().Truncate(time.Second)
	meta := domain.CertificateMetadata{
		CertificateID: "cert-rsa-001",
		UserID:        "user-123",
		SubjectName:   "Acme Corp RSA Signer",
		Algorithm:     domain.AlgorithmRSA2048,
		ValidFrom:     now,
		ExpiresAt:     now.Add(365 * 24 * time.Hour),
	}

	result, err := svc.GenerateKeyPair(context.Background(), meta)
	require.NoError(t, err)
	require.NotNil(t, result)

	assert.NotEmpty(t, result.SerialNumber)
	assert.NotEmpty(t, result.PublicKeyPEM)
	assert.Equal(t, "quicksign/users/user-123/keys/cert-rsa-001", result.VaultKeyPath)

	// Verify the private key was stored in Vault
	storedPEM, exists := mockVault.storedKeys[result.VaultKeyPath]
	require.True(t, exists, "Private key must be saved in Vault")
	assert.Contains(t, string(storedPEM), "-----BEGIN PRIVATE KEY-----")

	// Decode and parse the returned public certificate
	block, _ := pem.Decode([]byte(result.PublicKeyPEM))
	require.NotNil(t, block)
	cert, err := x509.ParseCertificate(block.Bytes)
	require.NoError(t, err)

	rsaPub, ok := cert.PublicKey.(*rsa.PublicKey)
	require.True(t, ok, "Expected RSA public key in certificate")
	assert.Equal(t, 2048, rsaPub.N.BitLen(), "Expected 2048-bit RSA key")
	assert.Equal(t, "Acme Corp RSA Signer", cert.Subject.CommonName)
}

func TestGenerateKeyPair_ECDSAP256_Success(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	svc, err := services.NewKeyGenService(mockVault, "quicksign/users")
	require.NoError(t, err)

	now := time.Now().UTC().Truncate(time.Second)
	meta := domain.CertificateMetadata{
		CertificateID: "cert-ecdsa-002",
		UserID:        "user-456",
		SubjectName:   "QuickSign ECDSA Device",
		Algorithm:     domain.AlgorithmECDSAP256,
		ValidFrom:     now,
		ExpiresAt:     now.Add(180 * 24 * time.Hour),
	}

	result, err := svc.GenerateKeyPair(context.Background(), meta)
	require.NoError(t, err)
	require.NotNil(t, result)

	assert.NotEmpty(t, result.SerialNumber)
	assert.NotEmpty(t, result.PublicKeyPEM)
	assert.Equal(t, "quicksign/users/user-456/keys/cert-ecdsa-002", result.VaultKeyPath)

	// Verify private key in Vault
	storedPEM, exists := mockVault.storedKeys[result.VaultKeyPath]
	require.True(t, exists)
	assert.Contains(t, string(storedPEM), "-----BEGIN PRIVATE KEY-----")

	// Decode and parse public certificate
	block, _ := pem.Decode([]byte(result.PublicKeyPEM))
	require.NotNil(t, block)
	cert, err := x509.ParseCertificate(block.Bytes)
	require.NoError(t, err)

	ecdsaPub, ok := cert.PublicKey.(*ecdsa.PublicKey)
	require.True(t, ok, "Expected ECDSA public key in certificate")
	assert.Equal(t, elliptic.P256(), ecdsaPub.Curve, "Expected P-256 curve")
	assert.Equal(t, "QuickSign ECDSA Device", cert.Subject.CommonName)
}

func TestGenerateKeyPair_VaultError_ReturnsError(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	mockVault.storeErr = errors.New("vault connection timeout")

	svc, err := services.NewKeyGenService(mockVault, "quicksign/users")
	require.NoError(t, err)

	meta := domain.CertificateMetadata{
		CertificateID: "cert-fail-001",
		UserID:        "user-789",
		SubjectName:   "Fail Test",
		Algorithm:     domain.AlgorithmRSA2048,
		ValidFrom:     time.Now().UTC(),
		ExpiresAt:     time.Now().UTC().Add(24 * time.Hour),
	}

	result, err := svc.GenerateKeyPair(context.Background(), meta)
	assert.Error(t, err)
	assert.Nil(t, result)
	assert.Contains(t, err.Error(), "failed to store private key in vault")
}

func TestDeleteKeyPair_Success(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	keyPath := "quicksign/users/user-123/keys/cert-del-001"
	mockVault.storedKeys[keyPath] = []byte("dummy-key-data")

	svc, err := services.NewKeyGenService(mockVault, "quicksign/users")
	require.NoError(t, err)

	err = svc.DeleteKeyPair(context.Background(), keyPath)
	assert.NoError(t, err)

	_, exists := mockVault.storedKeys[keyPath]
	assert.False(t, exists, "Key must be purged upon compensating transaction deletion")
}

func TestMemoryZeroing_PrivateKeyCleared(t *testing.T) {
	// Test ZeroBytes
	data := []byte{0xDE, 0xAD, 0xBE, 0xEF, 0x42}
	memguard.ZeroBytes(data)
	for i, b := range data {
		assert.Equal(t, byte(0), b, "Byte at index %d should be 0", i)
	}

	// Test ZeroBigInt
	bigVal := big.NewInt(123456789)
	memguard.ZeroBigInt(bigVal)
	assert.Equal(t, int64(0), bigVal.Int64(), "BigInt value should be reset to 0")

	// Test ZeroECDSAPrivateKey
	curve := elliptic.P256()
	privScalar, err := rand.Int(rand.Reader, curve.Params().N)
	require.NoError(t, err)
	ecdsaKey := &ecdsa.PrivateKey{
		D: privScalar,
	}
	memguard.ZeroECDSAPrivateKey(ecdsaKey)
	assert.Equal(t, int64(0), ecdsaKey.D.Int64(), "ECDSA scalar D should be zeroed")
}

func TestGenerateKeyPair_InvalidAlgorithm_ReturnsError(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	svc, err := services.NewKeyGenService(mockVault, "quicksign/users")
	require.NoError(t, err)

	meta := domain.CertificateMetadata{
		CertificateID: "cert-invalid-algo",
		UserID:        "user-001",
		SubjectName:   "Invalid Algo",
		Algorithm:     domain.KeyAlgorithm("UNKNOWN_ALGO"),
		ValidFrom:     time.Now().UTC(),
		ExpiresAt:     time.Now().UTC().Add(24 * time.Hour),
	}

	result, err := svc.GenerateKeyPair(context.Background(), meta)
	assert.Error(t, err)
	assert.Nil(t, result)
	assert.Contains(t, err.Error(), "unsupported key algorithm")
}

func TestX509Certificate_ValidMetadata(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	svc, err := services.NewKeyGenService(mockVault, "quicksign/users")
	require.NoError(t, err)

	from := time.Date(2026, 11, 1, 0, 0, 0, 0, time.UTC)
	to := time.Date(2027, 11, 1, 0, 0, 0, 0, time.UTC)

	meta := domain.CertificateMetadata{
		CertificateID: "cert-meta-check",
		UserID:        "user-meta",
		SubjectName:   "Metadata Verification Corp",
		Algorithm:     domain.AlgorithmRSA2048,
		ValidFrom:     from,
		ExpiresAt:     to,
	}

	result, err := svc.GenerateKeyPair(context.Background(), meta)
	require.NoError(t, err)

	block, _ := pem.Decode([]byte(result.PublicKeyPEM))
	require.NotNil(t, block)
	cert, err := x509.ParseCertificate(block.Bytes)
	require.NoError(t, err)

	assert.Equal(t, "Metadata Verification Corp", cert.Subject.CommonName)
	assert.Equal(t, from, cert.NotBefore)
	assert.Equal(t, to, cert.NotAfter)
	assert.True(t, cert.SerialNumber.Sign() > 0, "Serial number must be a positive integer")
	assert.Equal(t, x509.KeyUsageDigitalSignature|x509.KeyUsageKeyEncipherment, cert.KeyUsage)
}
