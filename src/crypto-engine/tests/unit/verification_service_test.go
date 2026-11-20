package unit_test

import (
	"context"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/services"
)

// MockAuthCacheAdapter implements ports.AuthCachePort for testing.
type MockAuthCacheAdapter struct {
	Cache map[string]*domain.ApiKeyCacheItem
}

func NewMockAuthCacheAdapter() *MockAuthCacheAdapter {
	return &MockAuthCacheAdapter{
		Cache: make(map[string]*domain.ApiKeyCacheItem),
	}
}

func (m *MockAuthCacheAdapter) GetApiKey(ctx context.Context, secretHash string) (*domain.ApiKeyCacheItem, error) {
	if item, ok := m.Cache[secretHash]; ok {
		return item, nil
	}
	return nil, nil
}

func (m *MockAuthCacheAdapter) SetApiKey(ctx context.Context, secretHash string, item *domain.ApiKeyCacheItem, ttl time.Duration) error {
	m.Cache[secretHash] = item
	return nil
}

func (m *MockAuthCacheAdapter) CheckAndRecordVerifyRateLimit(ctx context.Context, docCode, ip string) (bool, error) {
	return true, nil
}

func TestVerificationService_VerifyByCode_SecurityIllusionWarning(t *testing.T) {
	mockRepo := NewMockDocumentRepository()
	mockCache := NewMockAuthCacheAdapter()
	mockPdf := &MockPdfCryptoPort{}
	mockJws := &MockJwsCryptoPort{}

	const docCode = "DOC-2026-QRTEST"
	const certId = "cert-qr-001"

	mockRepo.Certificates[certId] = &domain.CertificateEntity{
		ID:          certId,
		SubjectName: "Official Company Issuer",
	}

	mockRepo.SignedDocuments[docCode] = &domain.SignedDocumentEntity{
		ID:            "doc-uuid-001",
		DocumentCode:  docCode,
		DocumentName:  "Invoice.pdf",
		CertificateID: certId,
		SignedHash:    "dummy-signed-hash-1234",
		CreatedAt:     time.Now().UTC(),
	}

	svc, err := services.NewVerificationService(mockRepo, mockCache, mockPdf, mockJws)
	require.NoError(t, err)

	// Act: QR Code lookup
	result, err := svc.VerifyByCode(context.Background(), docCode, "127.0.0.1", "Mobile Scanner/1.0")
	require.NoError(t, err)

	// Assert: ADR 0001 §6 - Must return CODE_EXISTS_UNVERIFIED_CONTENT with warning
	assert.True(t, result.IsValid)
	assert.Equal(t, domain.StatusCodeExistsUnverified, result.ValidationStatus)
	assert.Equal(t, docCode, result.DocumentCode)
	assert.NotEmpty(t, result.VerificationWarning)
	assert.False(t, result.RevocationChecked)

	// Assert audit log was recorded
	assert.Equal(t, 1, len(mockRepo.VerificationLogs))
	assert.Equal(t, "WEB_CODE", mockRepo.VerificationLogs[0].VerificationMethod)
}

func TestVerificationService_VerifyByCode_NotFound(t *testing.T) {
	mockRepo := NewMockDocumentRepository()
	mockCache := NewMockAuthCacheAdapter()

	svc, err := services.NewVerificationService(mockRepo, mockCache, &MockPdfCryptoPort{}, &MockJwsCryptoPort{})
	require.NoError(t, err)

	_, err = svc.VerifyByCode(context.Background(), "DOC-NON-EXISTENT", "127.0.0.1", "Browser")
	assert.ErrorIs(t, err, domain.ErrDocumentNotFound)
}
