package services

import (
	"context"
	"crypto/ecdsa"
	"crypto/rsa"
	"crypto/sha256"
	"crypto/x509"
	"encoding/hex"
	"encoding/pem"
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/google/uuid"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/ports"
	"quicksign/crypto-engine/pkg/memguard"
)

// SigningService implements ports.DocumentSigningUseCase.
type SigningService struct {
	vaultPort  ports.VaultKeyStorePort
	repoPort   ports.DocumentRepositoryPort
	pdfPort    ports.PdfCryptoPort
	jwsPort    ports.JwsCryptoPort
	appBaseURL string
}

// NewSigningService constructs a new SigningService instance.
func NewSigningService(
	vaultPort ports.VaultKeyStorePort,
	repoPort ports.DocumentRepositoryPort,
	pdfPort ports.PdfCryptoPort,
	jwsPort ports.JwsCryptoPort,
	appBaseURL string,
) (*SigningService, error) {
	if vaultPort == nil {
		return nil, errors.New("vaultPort cannot be nil")
	}
	if repoPort == nil {
		return nil, errors.New("repoPort cannot be nil")
	}
	if pdfPort == nil {
		return nil, errors.New("pdfPort cannot be nil")
	}
	if jwsPort == nil {
		return nil, errors.New("jwsPort cannot be nil")
	}

	return &SigningService{
		vaultPort:  vaultPort,
		repoPort:   repoPort,
		pdfPort:    pdfPort,
		jwsPort:    jwsPort,
		appBaseURL: strings.TrimRight(appBaseURL, "/"),
	}, nil
}

// SignPdf signs an uploaded PDF document using PAdES-BES standard with visual QR code and PKCS#9 attributes.
func (s *SigningService) SignPdf(ctx context.Context, input domain.SignPdfInput) (*domain.SignPdfOutput, error) {
	if err := input.Validate(); err != nil {
		return nil, err
	}

	// 1. Fetch active certificate for user
	var certIdPtr *string
	if strings.TrimSpace(input.CertificateID) != "" {
		certIdPtr = &input.CertificateID
	}

	cert, err := s.repoPort.GetActiveCertificate(ctx, input.UserID, certIdPtr)
	if err != nil {
		return nil, err
	}

	// Check certificate invariants
	if cert.Status != "ACTIVE" || (cert.RevokedAt != nil && !cert.RevokedAt.IsZero()) {
		return nil, domain.ErrCertRevoked
	}
	if time.Now().UTC().After(cert.ExpiresAt) {
		return nil, domain.ErrCertExpired
	}

	// 2. Fetch Private Key from HashiCorp Vault
	privKeyPEM, err := s.vaultPort.GetPrivateKey(ctx, cert.VaultKeyPath)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve signing key from vault: %w", err)
	}
	defer memguard.ZeroBytes(privKeyPEM)

	privKey, err := parsePrivateKey(privKeyPEM)
	if err != nil {
		return nil, fmt.Errorf("failed to decode private key: %w", err)
	}
	defer memguard.ZeroPrivateKey(privKey)

	// Decode Certificate DER from PEM
	certDER, err := decodePemToDer(cert.PublicKeyPEM)
	if err != nil {
		return nil, fmt.Errorf("failed to decode public certificate: %w", err)
	}

	// 3. Generate unique document_code: DOC-YYYY-XXXXXXXX
	docCode := generateDocumentCode()
	qrUrl := fmt.Sprintf("%s/api/v1/verify/code/%s", s.appBaseURL, docCode)

	// 4. Perform PAdES signing and QR code embedding
	signedBytes, signedHash, err := s.pdfPort.SignPdfPades(
		input.FileContent,
		privKey,
		certDER,
		docCode,
		qrUrl,
		input.Reason,
		input.Location,
	)
	if err != nil {
		return nil, fmt.Errorf("PAdES signing operation failed: %w", err)
	}

	// Calculate original file SHA-256
	origSum := sha256.Sum256(input.FileContent)
	origHash := hex.EncodeToString(origSum[:])

	// 5. Save signed document record to PostgreSQL
	now := time.Now().UTC()
	docEntity := &domain.SignedDocumentEntity{
		ID:             uuid.New().String(),
		UserID:         input.UserID,
		CertificateID:  cert.ID,
		DocumentCode:   docCode,
		DocumentName:   input.FileName,
		DocType:        domain.DocumentTypePdf,
		OriginalHash:   origHash,
		SignedHash:     signedHash,
		SignatureValue: signedHash,
		CreatedAt:      now,
	}

	if err := s.repoPort.SaveSignedDocument(ctx, docEntity); err != nil {
		return nil, fmt.Errorf("failed to save signed document record: %w", err)
	}

	return &domain.SignPdfOutput{
		SignedBytes:    signedBytes,
		DocumentCode:   docCode,
		DocumentName:   input.FileName,
		FileHash:       signedHash,
		SignatureValue: signedHash,
		CreatedAt:      now,
	}, nil
}

// SignJson signs a JSON payload producing a standardized RFC 7515 JWS token.
func (s *SigningService) SignJson(ctx context.Context, input domain.SignJsonInput) (*domain.SignJsonOutput, error) {
	if err := input.Validate(); err != nil {
		return nil, err
	}

	var certIdPtr *string
	if strings.TrimSpace(input.CertificateID) != "" {
		certIdPtr = &input.CertificateID
	}

	cert, err := s.repoPort.GetActiveCertificate(ctx, input.UserID, certIdPtr)
	if err != nil {
		return nil, err
	}

	if cert.Status != "ACTIVE" || (cert.RevokedAt != nil && !cert.RevokedAt.IsZero()) {
		return nil, domain.ErrCertRevoked
	}
	if time.Now().UTC().After(cert.ExpiresAt) {
		return nil, domain.ErrCertExpired
	}

	privKeyPEM, err := s.vaultPort.GetPrivateKey(ctx, cert.VaultKeyPath)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve signing key from vault: %w", err)
	}
	defer memguard.ZeroBytes(privKeyPEM)

	privKey, err := parsePrivateKey(privKeyPEM)
	if err != nil {
		return nil, fmt.Errorf("failed to decode private key: %w", err)
	}
	defer memguard.ZeroPrivateKey(privKey)

	docCode := generateDocumentCode()

	jwsToken, err := s.jwsPort.SignJws(input.Payload, privKey, cert.Algorithm, docCode)
	if err != nil {
		return nil, fmt.Errorf("JWS signing failed: %w", err)
	}

	payloadSum := sha256.Sum256(input.Payload)
	payloadHash := hex.EncodeToString(payloadSum[:])

	now := time.Now().UTC()
	docEntity := &domain.SignedDocumentEntity{
		ID:             uuid.New().String(),
		UserID:         input.UserID,
		CertificateID:  cert.ID,
		DocumentCode:   docCode,
		DocumentName:   "payload.json",
		DocType:        domain.DocumentTypeJson,
		OriginalHash:   payloadHash,
		SignedHash:     payloadHash,
		SignatureValue: jwsToken,
		CreatedAt:      now,
	}

	if err := s.repoPort.SaveSignedDocument(ctx, docEntity); err != nil {
		return nil, fmt.Errorf("failed to persist signed document metadata: %w", err)
	}

	return &domain.SignJsonOutput{
		JwsToken:     jwsToken,
		DocumentCode: docCode,
		PayloadHash:  payloadHash,
		CreatedAt:    now,
	}, nil
}

func generateDocumentCode() string {
	rawUUID := strings.ToUpper(strings.ReplaceAll(uuid.New().String(), "-", ""))
	return fmt.Sprintf("DOC-%s-%s", time.Now().UTC().Format("2006"), rawUUID[:8])
}

func parsePrivateKey(pemBytes []byte) (any, error) {
	block, _ := pem.Decode(pemBytes)
	if block == nil {
		return nil, errors.New("no PEM data found in private key")
	}

	// Try PKCS#8 first
	if key, err := x509.ParsePKCS8PrivateKey(block.Bytes); err == nil {
		switch k := key.(type) {
		case *rsa.PrivateKey, *ecdsa.PrivateKey:
			return k, nil
		default:
			return nil, fmt.Errorf("unsupported PKCS#8 key type %T", key)
		}
	}

	// Try PKCS#1 RSA
	if rsaKey, err := x509.ParsePKCS1PrivateKey(block.Bytes); err == nil {
		return rsaKey, nil
	}

	// Try EC private key
	if ecKey, err := x509.ParseECPrivateKey(block.Bytes); err == nil {
		return ecKey, nil
	}

	return nil, errors.New("failed to parse private key as PKCS#8, PKCS#1, or SEC1")
}

func decodePemToDer(pemStr string) ([]byte, error) {
	block, _ := pem.Decode([]byte(pemStr))
	if block == nil {
		return nil, errors.New("failed to decode PEM block for certificate")
	}
	return block.Bytes, nil
}
