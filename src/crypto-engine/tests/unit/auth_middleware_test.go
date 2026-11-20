package unit_test

import (
	"crypto/sha256"
	"encoding/hex"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/stretchr/testify/assert"

	httpInbound "quicksign/crypto-engine/internal/adapters/inbound/http"
	"quicksign/crypto-engine/internal/core/domain"
)

func TestAuthMiddleware_ApiKey_ValidScope_Success(t *testing.T) {
	mockRepo := NewMockDocumentRepository()
	mockCache := NewMockAuthCacheAdapter()

	apiKeySecret := "qs_live_prefix_validsecret123"
	hash := sha256.Sum256([]byte(apiKeySecret))
	hashHex := hex.EncodeToString(hash[:])

	mockRepo.ApiKeys[hashHex] = &domain.ApiKeyCacheItem{
		ID:       "key-001",
		UserID:   "user-api-01",
		Scopes:   "sign,verify",
		IsActive: true,
	}

	mw := httpInbound.NewAuthMiddleware(mockRepo, mockCache, "")

	var reachedHandler bool
	testHandler := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		reachedHandler = true
		userId := r.Context().Value(httpInbound.UserIDContextKey).(string)
		assert.Equal(t, "user-api-01", userId)
		w.WriteHeader(http.StatusOK)
	})

	req := httptest.NewRequest(http.MethodPost, "/api/v1/sign/pdf", nil)
	req.Header.Set("X-API-Key", apiKeySecret)
	rec := httptest.NewRecorder()

	mw.RequireAuth("sign", testHandler)(rec, req)

	assert.True(t, reachedHandler)
	assert.Equal(t, http.StatusOK, rec.Code)
}

func TestAuthMiddleware_ApiKey_InsufficientScope_ReturnsForbidden(t *testing.T) {
	mockRepo := NewMockDocumentRepository()
	mockCache := NewMockAuthCacheAdapter()

	apiKeySecret := "qs_live_prefix_verifyonly"
	hash := sha256.Sum256([]byte(apiKeySecret))
	hashHex := hex.EncodeToString(hash[:])

	mockRepo.ApiKeys[hashHex] = &domain.ApiKeyCacheItem{
		ID:       "key-002",
		UserID:   "user-verify-only",
		Scopes:   "verify", // lacks "sign"
		IsActive: true,
	}

	mw := httpInbound.NewAuthMiddleware(mockRepo, mockCache, "")

	testHandler := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		t.Fatal("handler should not have been called")
	})

	req := httptest.NewRequest(http.MethodPost, "/api/v1/sign/pdf", nil)
	req.Header.Set("X-API-Key", apiKeySecret)
	rec := httptest.NewRecorder()

	mw.RequireAuth("sign", testHandler)(rec, req)

	assert.Equal(t, http.StatusForbidden, rec.Code)
	assert.Contains(t, rec.Body.String(), "API_KEY_SCOPE_INSUFFICIENT")
}

func TestAuthMiddleware_NoAuthHeaders_ReturnsUnauthorized(t *testing.T) {
	mockRepo := NewMockDocumentRepository()
	mockCache := NewMockAuthCacheAdapter()
	mw := httpInbound.NewAuthMiddleware(mockRepo, mockCache, "")

	testHandler := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		t.Fatal("handler should not have been called")
	})

	req := httptest.NewRequest(http.MethodPost, "/api/v1/sign/pdf", nil)
	rec := httptest.NewRecorder()

	mw.RequireAuth("sign", testHandler)(rec, req)

	assert.Equal(t, http.StatusUnauthorized, rec.Code)
	assert.Contains(t, rec.Body.String(), "UNAUTHORIZED")
}
