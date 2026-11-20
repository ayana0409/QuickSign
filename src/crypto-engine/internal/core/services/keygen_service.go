package services

import (
	"context"
	"crypto"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/asn1"
	"encoding/pem"
	"errors"
	"fmt"
	"math/big"
	"strings"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/ports"
	"quicksign/crypto-engine/pkg/memguard"
)

var (
	ErrNilVaultPort = errors.New("vault key store port cannot be nil")
)

// KeyGenService implements ports.KeyGenerationUseCase for asymmetric key generation and Vault persistence.
type KeyGenService struct {
	vaultPort       ports.VaultKeyStorePort
	vaultPathPrefix string
}

// NewKeyGenService constructs a new KeyGenService.
func NewKeyGenService(vaultPort ports.VaultKeyStorePort, vaultPathPrefix string) (*KeyGenService, error) {
	if vaultPort == nil {
		return nil, ErrNilVaultPort
	}
	if strings.TrimSpace(vaultPathPrefix) == "" {
		vaultPathPrefix = "quicksign/users"
	}
	return &KeyGenService{
		vaultPort:       vaultPort,
		vaultPathPrefix: strings.Trim(vaultPathPrefix, "/"),
	}, nil
}

// GenerateKeyPair generates a cryptographic asymmetric key pair, produces an X.509 certificate,
// stores the private key into Vault, zeroes private key memory in RAM, and returns public key material.
func (s *KeyGenService) GenerateKeyPair(ctx context.Context, meta domain.CertificateMetadata) (*domain.GeneratedKeyPairResult, error) {
	if err := meta.Validate(); err != nil {
		return nil, fmt.Errorf("invalid certificate metadata: %w", err)
	}

	var (
		signer     crypto.Signer
		publicKey  crypto.PublicKey
		privKeyDER []byte
		err        error
	)

	switch meta.Algorithm {
	case domain.AlgorithmRSA2048:
		rsaPriv, genErr := rsa.GenerateKey(rand.Reader, 2048)
		if genErr != nil {
			return nil, fmt.Errorf("failed to generate RSA-2048 key pair: %w", genErr)
		}
		// Zero the sensitive RSA private key numbers from RAM when function returns (ADR 0001 §3)
		defer memguard.ZeroRSAPrivateKey(rsaPriv)

		signer = rsaPriv
		publicKey = &rsaPriv.PublicKey

		privKeyDER, err = x509.MarshalPKCS8PrivateKey(rsaPriv)
		if err != nil {
			return nil, fmt.Errorf("failed to marshal RSA private key to PKCS#8: %w", err)
		}
		defer memguard.ZeroBytes(privKeyDER)

	case domain.AlgorithmECDSAP256:
		ecdsaPriv, genErr := ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
		if genErr != nil {
			return nil, fmt.Errorf("failed to generate ECDSA-P256 key pair: %w", genErr)
		}
		// Zero the sensitive ECDSA scalar from RAM when function returns (ADR 0001 §3)
		defer memguard.ZeroECDSAPrivateKey(ecdsaPriv)

		signer = ecdsaPriv
		publicKey = &ecdsaPriv.PublicKey

		privKeyDER, err = x509.MarshalPKCS8PrivateKey(ecdsaPriv)
		if err != nil {
			return nil, fmt.Errorf("failed to marshal ECDSA private key to PKCS#8: %w", err)
		}
		defer memguard.ZeroBytes(privKeyDER)

	default:
		return nil, domain.ErrInvalidAlgorithm
	}

	// Generate a 128-bit cryptographically secure random serial number for the certificate
	serialNumberLimit := new(big.Int).Lsh(big.NewInt(1), 128)
	serialNumber, err := rand.Int(rand.Reader, serialNumberLimit)
	if err != nil {
		return nil, fmt.Errorf("failed to generate certificate serial number: %w", err)
	}

	certTemplate := x509.Certificate{
		SerialNumber: serialNumber,
		Subject: pkix.Name{
			CommonName: meta.SubjectName,
		},
		NotBefore:             meta.ValidFrom,
		NotAfter:              meta.ExpiresAt,
		KeyUsage:              x509.KeyUsageDigitalSignature | x509.KeyUsageKeyEncipherment,
		ExtKeyUsage: []x509.ExtKeyUsage{
			x509.ExtKeyUsageClientAuth,
			x509.ExtKeyUsageCodeSigning,
			x509.ExtKeyUsageEmailProtection,
		},
		UnknownExtKeyUsage: []asn1.ObjectIdentifier{
			{1, 3, 6, 1, 5, 5, 7, 3, 36}, // RFC 9336 Document Signing
		},
		BasicConstraintsValid: true,
		IsCA:                  false,
	}

	// Create self-signed X.509 certificate
	certDER, err := x509.CreateCertificate(rand.Reader, &certTemplate, &certTemplate, publicKey, signer)
	if err != nil {
		return nil, fmt.Errorf("failed to create X.509 certificate: %w", err)
	}

	certPEM := pem.EncodeToMemory(&pem.Block{
		Type:  "CERTIFICATE",
		Bytes: certDER,
	})

	privKeyPEM := pem.EncodeToMemory(&pem.Block{
		Type:  "PRIVATE KEY",
		Bytes: privKeyDER,
	})
	defer memguard.ZeroBytes(privKeyPEM)

	// Determine Vault path: e.g. "quicksign/users/{userId}/keys/{certificateId}"
	vaultKeyPath := fmt.Sprintf("%s/%s/keys/%s", s.vaultPathPrefix, meta.UserID, meta.CertificateID)

	// Persist the private key in HashiCorp Vault
	if err := s.vaultPort.StorePrivateKey(ctx, vaultKeyPath, privKeyPEM); err != nil {
		return nil, fmt.Errorf("failed to store private key in vault: %w", err)
	}

	return &domain.GeneratedKeyPairResult{
		SerialNumber: fmt.Sprintf("%X", serialNumber),
		PublicKeyPEM: string(certPEM),
		VaultKeyPath: vaultKeyPath,
	}, nil
}

// DeleteKeyPair permanently removes an orphaned key from Vault upon compensating transaction.
func (s *KeyGenService) DeleteKeyPair(ctx context.Context, vaultKeyPath string) error {
	cleanPath := strings.Trim(vaultKeyPath, "/")
	if cleanPath == "" {
		return domain.ErrKeyNotFound
	}
	return s.vaultPort.DeletePrivateKey(ctx, cleanPath)
}

// Ensure interface compliance at compile time.
var _ ports.KeyGenerationUseCase = (*KeyGenService)(nil)
