package unit_test

import (
	"context"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/pem"
	"math/big"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/services"
)

// MockDocumentRepository implements ports.DocumentRepositoryPort for unit testing.
type MockDocumentRepository struct {
	Certificates    map[string]*domain.CertificateEntity
	SignedDocuments map[string]*domain.SignedDocumentEntity
	VerificationLogs []*domain.VerificationLogEntity
	ApiKeys         map[string]*domain.ApiKeyCacheItem
}

func NewMockDocumentRepository() *MockDocumentRepository {
	return &MockDocumentRepository{
		Certificates:    make(map[string]*domain.CertificateEntity),
		SignedDocuments: make(map[string]*domain.SignedDocumentEntity),
		ApiKeys:         make(map[string]*domain.ApiKeyCacheItem),
	}
}

func (m *MockDocumentRepository) GetActiveCertificate(ctx context.Context, userId string, certId *string) (*domain.CertificateEntity, error) {
	if certId != nil && *certId != "" {
		if cert, ok := m.Certificates[*certId]; ok {
			return cert, nil
		}
		return nil, domain.ErrCertNotFound
	}
	for _, cert := range m.Certificates {
		if cert.UserID == userId && cert.Status == "ACTIVE" {
			return cert, nil
		}
	}
	return nil, domain.ErrCertNotFound
}

func (m *MockDocumentRepository) GetCertificateById(ctx context.Context, certId string) (*domain.CertificateEntity, error) {
	if cert, ok := m.Certificates[certId]; ok {
		return cert, nil
	}
	return nil, domain.ErrCertNotFound
}

func (m *MockDocumentRepository) SaveSignedDocument(ctx context.Context, doc *domain.SignedDocumentEntity) error {
	m.SignedDocuments[doc.DocumentCode] = doc
	return nil
}

func (m *MockDocumentRepository) GetSignedDocumentByCode(ctx context.Context, code string) (*domain.SignedDocumentEntity, error) {
	if doc, ok := m.SignedDocuments[code]; ok {
		return doc, nil
	}
	return nil, domain.ErrDocumentNotFound
}

func (m *MockDocumentRepository) SaveVerificationLog(ctx context.Context, log *domain.VerificationLogEntity) error {
	m.VerificationLogs = append(m.VerificationLogs, log)
	return nil
}

func (m *MockDocumentRepository) IncrementVerifyCount(ctx context.Context, docId string, isUnique bool, isTampered bool) error {
	for _, doc := range m.SignedDocuments {
		if doc.ID == docId {
			doc.TotalVerifyCount++
			if isUnique {
				doc.UniqueVerifyCount++
			}
			if isTampered {
				doc.TamperedAlertCount++
			}
			return nil
		}
	}
	return nil
}

func (m *MockDocumentRepository) GetApiKeyBySecretHash(ctx context.Context, secretHash string) (*domain.ApiKeyCacheItem, error) {
	if item, ok := m.ApiKeys[secretHash]; ok {
		return item, nil
	}
	return nil, nil
}

// MockPdfCryptoPort implements ports.PdfCryptoPort.
type MockPdfCryptoPort struct {
	SignedBytesToReturn []byte
	HashToReturn        string
	ShouldFail          bool
}

func (m *MockPdfCryptoPort) SignPdfPades(pdfBytes []byte, privKey any, certDER []byte, docCode string, qrUrl string, reason, location string) ([]byte, string, error) {
	if m.ShouldFail {
		return nil, "", domain.ErrSigningFailed
	}
	res := append(pdfBytes, []byte("\n%%EOF_SIGNED")...)
	return res, "dummy-signed-hash-1234", nil
}

func (m *MockPdfCryptoPort) VerifyPdfPades(pdfBytes []byte) (*domain.PdfSignatureData, error) {
	if m.ShouldFail {
		return nil, domain.ErrUnknownSignature
	}
	return &domain.PdfSignatureData{
		DocumentCode:     "DOC-2026-TEST01",
		SignerSubject:    "Test Signer",
		SigningTime:      time.Now().UTC(),
		ContentDigest:    []byte("dummy-digest"),
		IsSignatureValid: true,
	}, nil
}

// MockJwsCryptoPort implements ports.JwsCryptoPort.
type MockJwsCryptoPort struct{}

func (m *MockJwsCryptoPort) SignJws(payload []byte, privKey any, algo domain.KeyAlgorithm, docCode string) (string, error) {
	return "dummy.header.payload.signature", nil
}

func (m *MockJwsCryptoPort) VerifyJws(jwsString string, pubKey any) (*domain.JwsSignatureData, error) {
	return &domain.JwsSignatureData{
		DocumentCode:     "DOC-2026-TEST01",
		Algorithm:        "RS256",
		Payload:          []byte(`{"test":"data"}`),
		PayloadDigest:    []byte("dummy-payload-digest"),
		IsSignatureValid: true,
	}, nil
}

// Helper: generate test self-signed RSA certificate & private key PEM
func generateTestCertAndKey(t *testing.T) (string, string) {
	privKey, err := rsa.GenerateKey(rand.Reader, 2048)
	require.NoError(t, err)

	template := x509.Certificate{
		SerialNumber: big.NewInt(1001),
		Subject:      pkix.Name{CommonName: "Unit Test Signer"},
		NotBefore:    time.Now().Add(-1 * time.Hour),
		NotAfter:     time.Now().Add(24 * time.Hour),
	}

	certDER, err := x509.CreateCertificate(rand.Reader, &template, &template, &privKey.PublicKey, privKey)
	require.NoError(t, err)

	certPEM := pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: certDER})
	privKeyDER := x509.MarshalPKCS1PrivateKey(privKey)
	keyPEM := pem.EncodeToMemory(&pem.Block{Type: "RSA PRIVATE KEY", Bytes: privKeyDER})

	return string(certPEM), string(keyPEM)
}

func TestSigningService_SignPdf_Success(t *testing.T) {
	certPEM, keyPEM := generateTestCertAndKey(t)
	mockVault := newMockVaultKeyStore()
	mockRepo := NewMockDocumentRepository()
	mockPdf := &MockPdfCryptoPort{}
	mockJws := &MockJwsCryptoPort{}

	const userId = "test-user-001"
	const certId = "cert-001"
	vaultPath := "quicksign/users/" + userId + "/keys/" + certId

	_ = mockVault.StorePrivateKey(context.Background(), vaultPath, []byte(keyPEM))

	mockRepo.Certificates[certId] = &domain.CertificateEntity{
		ID:           certId,
		UserID:       userId,
		SubjectName:  "Unit Test Signer",
		SerialNumber: "1001",
		Algorithm:    domain.AlgorithmRSA2048,
		PublicKeyPEM: certPEM,
		VaultKeyPath: vaultPath,
		Status:       "ACTIVE",
		ExpiresAt:    time.Now().Add(24 * time.Hour),
	}

	svc, err := services.NewSigningService(mockVault, mockRepo, mockPdf, mockJws, "http://localhost:50052")
	require.NoError(t, err)

	input := domain.SignPdfInput{
		FileContent:   []byte("%PDF-1.4\n1 0 obj\n<<>>\nendobj\n"),
		FileName:      "contract.pdf",
		UserID:        userId,
		CertificateID: certId,
		Reason:        "Unit Test Approval",
		Location:      "Hanoi",
	}

	output, err := svc.SignPdf(context.Background(), input)
	require.NoError(t, err)
	assert.NotEmpty(t, output.DocumentCode)
	assert.Contains(t, output.DocumentCode, "DOC-")
	assert.NotEmpty(t, output.FileHash)
	assert.True(t, len(output.SignedBytes) > len(input.FileContent))

	// Verify persistence in repository
	doc, err := mockRepo.GetSignedDocumentByCode(context.Background(), output.DocumentCode)
	require.NoError(t, err)
	assert.Equal(t, input.FileName, doc.DocumentName)
	assert.Equal(t, domain.DocumentTypePdf, doc.DocType)
}

func TestSigningService_SignPdf_RevokedCert_ReturnsError(t *testing.T) {
	certPEM, keyPEM := generateTestCertAndKey(t)
	mockVault := newMockVaultKeyStore()
	mockRepo := NewMockDocumentRepository()

	const userId = "test-user-revoked"
	const certId = "cert-revoked"
	vaultPath := "quicksign/users/" + userId + "/keys/" + certId

	_ = mockVault.StorePrivateKey(context.Background(), vaultPath, []byte(keyPEM))

	revokedAt := time.Now().Add(-10 * time.Minute)
	mockRepo.Certificates[certId] = &domain.CertificateEntity{
		ID:           certId,
		UserID:       userId,
		SubjectName:  "Revoked Signer",
		PublicKeyPEM: certPEM,
		VaultKeyPath: vaultPath,
		Status:       "REVOKED",
		ExpiresAt:    time.Now().Add(24 * time.Hour),
		RevokedAt:    &revokedAt,
	}

	svc, err := services.NewSigningService(mockVault, mockRepo, &MockPdfCryptoPort{}, &MockJwsCryptoPort{}, "http://localhost")
	require.NoError(t, err)

	input := domain.SignPdfInput{
		FileContent:   []byte("%PDF-1.5 test"),
		FileName:      "doc.pdf",
		UserID:        userId,
		CertificateID: certId,
	}

	_, err = svc.SignPdf(context.Background(), input)
	assert.ErrorIs(t, err, domain.ErrCertRevoked)
}

func TestSigningService_SignJson_Success(t *testing.T) {
	certPEM, keyPEM := generateTestCertAndKey(t)
	mockVault := newMockVaultKeyStore()
	mockRepo := NewMockDocumentRepository()

	const userId = "test-user-json"
	const certId = "cert-json"
	vaultPath := "quicksign/users/" + userId + "/keys/" + certId

	_ = mockVault.StorePrivateKey(context.Background(), vaultPath, []byte(keyPEM))

	mockRepo.Certificates[certId] = &domain.CertificateEntity{
		ID:           certId,
		UserID:       userId,
		SubjectName:  "JSON Signer",
		Algorithm:    domain.AlgorithmRSA2048,
		PublicKeyPEM: certPEM,
		VaultKeyPath: vaultPath,
		Status:       "ACTIVE",
		ExpiresAt:    time.Now().Add(24 * time.Hour),
	}

	svc, err := services.NewSigningService(mockVault, mockRepo, &MockPdfCryptoPort{}, &MockJwsCryptoPort{}, "http://localhost")
	require.NoError(t, err)

	input := domain.SignJsonInput{
		Payload:       []byte(`{"invoice":123,"total":500}`),
		UserID:        userId,
		CertificateID: certId,
	}

	output, err := svc.SignJson(context.Background(), input)
	require.NoError(t, err)
	assert.NotEmpty(t, output.JwsToken)
	assert.NotEmpty(t, output.DocumentCode)
}
