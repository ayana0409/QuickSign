package integration_test

import (
	"bytes"
	"context"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/json"
	"encoding/pem"
	"fmt"
	"math/big"
	"mime/multipart"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	httpInbound "quicksign/crypto-engine/internal/adapters/inbound/http"
	"quicksign/crypto-engine/internal/adapters/outbound/jws_crypto"
	"quicksign/crypto-engine/internal/adapters/outbound/pdf_pades"
	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/services"
	"quicksign/crypto-engine/tests/fixtures"
)

// In-Memory Repository for HTTP Integration Tests
type inMemoryIntegrationRepo struct {
	certs    map[string]*domain.CertificateEntity
	docs     map[string]*domain.SignedDocumentEntity
	apiKeys  map[string]*domain.ApiKeyCacheItem
	logs     []*domain.VerificationLogEntity
}

func newInMemoryIntegrationRepo() *inMemoryIntegrationRepo {
	return &inMemoryIntegrationRepo{
		certs:   make(map[string]*domain.CertificateEntity),
		docs:    make(map[string]*domain.SignedDocumentEntity),
		apiKeys: make(map[string]*domain.ApiKeyCacheItem),
	}
}

func (r *inMemoryIntegrationRepo) GetActiveCertificate(ctx context.Context, userId string, certId *string) (*domain.CertificateEntity, error) {
	if certId != nil && *certId != "" {
		if cert, ok := r.certs[*certId]; ok {
			return cert, nil
		}
		return nil, domain.ErrCertNotFound
	}
	for _, cert := range r.certs {
		if cert.UserID == userId && cert.Status == "ACTIVE" {
			return cert, nil
		}
	}
	return nil, domain.ErrCertNotFound
}

func (r *inMemoryIntegrationRepo) GetCertificateById(ctx context.Context, certId string) (*domain.CertificateEntity, error) {
	if cert, ok := r.certs[certId]; ok {
		return cert, nil
	}
	return nil, domain.ErrCertNotFound
}

func (r *inMemoryIntegrationRepo) SaveSignedDocument(ctx context.Context, doc *domain.SignedDocumentEntity) error {
	r.docs[doc.DocumentCode] = doc
	return nil
}

func (r *inMemoryIntegrationRepo) GetSignedDocumentByCode(ctx context.Context, code string) (*domain.SignedDocumentEntity, error) {
	if doc, ok := r.docs[code]; ok {
		return doc, nil
	}
	return nil, domain.ErrDocumentNotFound
}

func (r *inMemoryIntegrationRepo) SaveVerificationLog(ctx context.Context, log *domain.VerificationLogEntity) error {
	r.logs = append(r.logs, log)
	return nil
}

func (r *inMemoryIntegrationRepo) IncrementVerifyCount(ctx context.Context, docId string, isUnique bool, isTampered bool) error {
	for _, doc := range r.docs {
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

func (r *inMemoryIntegrationRepo) GetApiKeyBySecretHash(ctx context.Context, secretHash string) (*domain.ApiKeyCacheItem, error) {
	if item, ok := r.apiKeys[secretHash]; ok {
		return item, nil
	}
	return nil, nil
}

// In-Memory Vault Store for Integration Tests
type inMemoryVaultStore struct {
	secrets map[string][]byte
}

func newInMemoryVaultStore() *inMemoryVaultStore {
	return &inMemoryVaultStore{secrets: make(map[string][]byte)}
}

func (v *inMemoryVaultStore) StorePrivateKey(ctx context.Context, path string, pemBytes []byte) error {
	v.secrets[path] = pemBytes
	return nil
}

func (v *inMemoryVaultStore) DeletePrivateKey(ctx context.Context, path string) error {
	delete(v.secrets, path)
	return nil
}

func (v *inMemoryVaultStore) GetPrivateKey(ctx context.Context, path string) ([]byte, error) {
	if data, ok := v.secrets[path]; ok {
		return data, nil
	}
	return nil, domain.ErrKeyNotFound
}

// Helper: generate test RSA key and certificate
func createTestCertificatePair(t *testing.T, userId, certId string) (string, string) {
	privKey, err := rsa.GenerateKey(rand.Reader, 2048)
	require.NoError(t, err)

	template := x509.Certificate{
		SerialNumber: big.NewInt(2026),
		Subject:      pkix.Name{CommonName: "Integration Test Issuer", Country: []string{"VN"}},
		NotBefore:    time.Now().Add(-1 * time.Hour),
		NotAfter:     time.Now().Add(365 * 24 * time.Hour),
		KeyUsage:     x509.KeyUsageDigitalSignature | x509.KeyUsageKeyEncipherment,
		ExtKeyUsage: []x509.ExtKeyUsage{
			x509.ExtKeyUsageClientAuth,
			x509.ExtKeyUsageEmailProtection,
		},
	}

	certDER, err := x509.CreateCertificate(rand.Reader, &template, &template, &privKey.PublicKey, privKey)
	require.NoError(t, err)

	certPEM := pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: certDER})
	keyDER := x509.MarshalPKCS1PrivateKey(privKey)
	keyPEM := pem.EncodeToMemory(&pem.Block{Type: "RSA PRIVATE KEY", Bytes: keyDER})

	return string(certPEM), string(keyPEM)
}

func setupTestHttpServer(t *testing.T) (http.Handler, *inMemoryIntegrationRepo, *inMemoryVaultStore, string, string) {
	const userId = "test-integration-user"
	const certId = "cert-integration-001"
	vaultPath := fmt.Sprintf("quicksign/users/%s/keys/%s", userId, certId)

	certPEM, keyPEM := createTestCertificatePair(t, userId, certId)

	vaultStore := newInMemoryVaultStore()
	_ = vaultStore.StorePrivateKey(context.Background(), vaultPath, []byte(keyPEM))

	repo := newInMemoryIntegrationRepo()
	repo.certs[certId] = &domain.CertificateEntity{
		ID:           certId,
		UserID:       userId,
		SubjectName:  "Integration Test Issuer",
		SerialNumber: "2026",
		Algorithm:    domain.AlgorithmRSA2048,
		PublicKeyPEM: certPEM,
		VaultKeyPath: vaultPath,
		Status:       "ACTIVE",
		ExpiresAt:    time.Now().Add(365 * 24 * time.Hour),
	}

	padesAdapter := pdf_pades.NewPadesPdfAdapter()
	jwsAdapter := jws_crypto.NewJwsAdapter()

	signingSvc, err := services.NewSigningService(vaultStore, repo, padesAdapter, jwsAdapter, "http://localhost:3000")
	require.NoError(t, err)

	verifySvc, err := services.NewVerificationService(repo, nil, padesAdapter, jwsAdapter)
	require.NoError(t, err)

	authMw := httpInbound.NewAuthMiddleware(repo, nil, "")
	signHandler := httpInbound.NewSignHandler(signingSvc)
	verifyHandler := httpInbound.NewVerifyHandler(verifySvc)

	router := httpInbound.NewRouter(httpInbound.RouterConfig{
		AuthMiddleware: authMw,
		SignHandler:    signHandler,
		VerifyHandler:  verifyHandler,
	})

	return router, repo, vaultStore, userId, certId
}

func TestHttpServer_HealthCheck(t *testing.T) {
	router, _, _, _, _ := setupTestHttpServer(t)

	req := httptest.NewRequest(http.MethodGet, "/health", nil)
	rec := httptest.NewRecorder()

	router.ServeHTTP(rec, req)

	assert.Equal(t, http.StatusOK, rec.Code)
	assert.Contains(t, rec.Body.String(), `"status":"healthy"`)
}

func TestHttpServer_SignJsonAndVerify_EndToEnd(t *testing.T) {
	router, repo, _, _, certId := setupTestHttpServer(t)

	// Step 1: Call POST /api/v1/sign/json with fake Bearer token containing 'sub' claim
	// Create minimal JWT payload with "sub": userId
	fakeJwt := "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ0ZXN0LWludGVncmF0aW9uLXVzZXIiLCJleHAiOjE5OTk5OTk5OTl9."

	signReqBody := map[string]any{
		"certificateId": certId,
		"payload": map[string]any{
			"invoiceNumber": "INV-2026-9999",
			"customer":      "Acme Corp",
			"amount":        12500000,
		},
	}
	bodyBytes, err := json.Marshal(signReqBody)
	require.NoError(t, err)

	signReq := httptest.NewRequest(http.MethodPost, "/api/v1/sign/json", bytes.NewReader(bodyBytes))
	signReq.Header.Set("Authorization", "Bearer "+fakeJwt)
	signReq.Header.Set("Content-Type", "application/json")
	signRec := httptest.NewRecorder()

	router.ServeHTTP(signRec, signReq)

	require.Equal(t, http.StatusOK, signRec.Code, "Sign JSON must succeed with 200 OK")

	var signResp struct {
		JWS          string `json:"jws"`
		DocumentCode string `json:"documentCode"`
		PayloadHash  string `json:"payloadHash"`
	}
	err = json.Unmarshal(signRec.Body.Bytes(), &signResp)
	require.NoError(t, err)
	assert.NotEmpty(t, signResp.JWS)
	assert.NotEmpty(t, signResp.DocumentCode)
	assert.Contains(t, signResp.DocumentCode, "DOC-")

	// Step 2: Call POST /api/v1/verify/file with genuine JWS token
	verifyReqBody := map[string]string{"jws": signResp.JWS}
	vBytes, _ := json.Marshal(verifyReqBody)
	verifyReq := httptest.NewRequest(http.MethodPost, "/api/v1/verify/file", bytes.NewReader(vBytes))
	verifyReq.Header.Set("Content-Type", "application/json")
	verifyRec := httptest.NewRecorder()

	router.ServeHTTP(verifyRec, verifyReq)

	require.Equal(t, http.StatusOK, verifyRec.Code)
	var verifyResp domain.VerifyResult
	err = json.Unmarshal(verifyRec.Body.Bytes(), &verifyResp)
	require.NoError(t, err)

	assert.True(t, verifyResp.IsValid, "Genuine signed JWS must verify as valid")
	assert.Equal(t, domain.StatusValid, verifyResp.ValidationStatus)
	assert.Equal(t, signResp.DocumentCode, verifyResp.DocumentCode)

	// Step 3: Call POST /api/v1/verify/file with TAMPERED JWS token
	jwsParts := strings.Split(signResp.JWS, ".")
	tamperedJws := jwsParts[0] + ".eyJhbW91bnQiOjk5OTk5OX0." + jwsParts[2]
	tBytes, _ := json.Marshal(map[string]string{"jws": tamperedJws})

	tamperReq := httptest.NewRequest(http.MethodPost, "/api/v1/verify/file", bytes.NewReader(tBytes))
	tamperReq.Header.Set("Content-Type", "application/json")
	tamperRec := httptest.NewRecorder()

	router.ServeHTTP(tamperRec, tamperReq)

	require.Equal(t, http.StatusOK, tamperRec.Code)
	var tamperResp domain.VerifyResult
	err = json.Unmarshal(tamperRec.Body.Bytes(), &tamperResp)
	require.NoError(t, err)

	assert.False(t, tamperResp.IsValid, "Tampered payload must be flagged as invalid")
	assert.Equal(t, domain.StatusTamperedContent, tamperResp.ValidationStatus)

	// Step 4: Call public GET /api/v1/verify/code/{documentCode} (QR Lookup)
	qrReq := httptest.NewRequest(http.MethodGet, "/api/v1/verify/code/"+signResp.DocumentCode, nil)
	qrRec := httptest.NewRecorder()

	router.ServeHTTP(qrRec, qrReq)

	require.Equal(t, http.StatusOK, qrRec.Code)
	var qrResp domain.VerifyResult
	err = json.Unmarshal(qrRec.Body.Bytes(), &qrResp)
	require.NoError(t, err)

	// Verify ADR 0001 §6 Security Illusion compliance
	assert.True(t, qrResp.IsValid)
	assert.Equal(t, domain.StatusCodeExistsUnverified, qrResp.ValidationStatus)
	assert.NotEmpty(t, qrResp.VerificationWarning)
	assert.Contains(t, qrResp.VerificationWarning, "tải tệp gốc lên để xác minh")

	// Step 5: Verify atomic counter and logs in repository
	doc, err := repo.GetSignedDocumentByCode(context.Background(), signResp.DocumentCode)
	require.NoError(t, err)
	assert.Equal(t, 3, doc.TotalVerifyCount, "Must have recorded all 3 verification calls")
	assert.Equal(t, 1, doc.TamperedAlertCount, "Must have recorded exactly 1 tampered alert")
}

func TestHttpServer_SignPdfAndVerify_EndToEnd(t *testing.T) {
	router, _, _, _, certId := setupTestHttpServer(t)

	fakeJwt := "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ0ZXN0LWludGVncmF0aW9uLXVzZXIiLCJleHAiOjE5OTk5OTk5OTl9."

	// Step 1: Create multipart request for POST /api/v1/sign/pdf
	var body bytes.Buffer
	writer := multipart.NewWriter(&body)

	part, err := writer.CreateFormFile("file", "contract.pdf")
	require.NoError(t, err)
	_, err = part.Write(fixtures.SamplePDF)
	require.NoError(t, err)

	_ = writer.WriteField("certificateId", certId)
	_ = writer.WriteField("reason", "Contract Final Sign")
	_ = writer.WriteField("location", "Hanoi, VN")
	err = writer.Close()
	require.NoError(t, err)

	signReq := httptest.NewRequest(http.MethodPost, "/api/v1/sign/pdf", &body)
	signReq.Header.Set("Authorization", "Bearer "+fakeJwt)
	signReq.Header.Set("Content-Type", writer.FormDataContentType())
	signRec := httptest.NewRecorder()

	router.ServeHTTP(signRec, signReq)

	require.Equal(t, http.StatusOK, signRec.Code)
	assert.Equal(t, "application/pdf", signRec.Header().Get("Content-Type"))
	docCode := signRec.Header().Get("X-Document-Code")
	assert.NotEmpty(t, docCode)
	assert.Contains(t, docCode, "DOC-")
	signedPdfBytes := signRec.Body.Bytes()
	assert.Greater(t, len(signedPdfBytes), len(fixtures.SamplePDF))

	// Step 2: Call POST /api/v1/verify/file with the signed PDF in multipart
	var verifyBody bytes.Buffer
	verifyWriter := multipart.NewWriter(&verifyBody)

	vPart, err := verifyWriter.CreateFormFile("file", "contract_signed.pdf")
	require.NoError(t, err)
	_, err = vPart.Write(signedPdfBytes)
	require.NoError(t, err)
	err = verifyWriter.Close()
	require.NoError(t, err)

	verifyReq := httptest.NewRequest(http.MethodPost, "/api/v1/verify/file", &verifyBody)
	verifyReq.Header.Set("Content-Type", verifyWriter.FormDataContentType())
	verifyRec := httptest.NewRecorder()

	router.ServeHTTP(verifyRec, verifyReq)

	require.Equal(t, http.StatusOK, verifyRec.Code)
	var verifyResp domain.VerifyResult
	err = json.Unmarshal(verifyRec.Body.Bytes(), &verifyResp)
	require.NoError(t, err)

	assert.True(t, verifyResp.IsValid, "Signed PDF must verify as mathematically valid")
	assert.Equal(t, domain.StatusValid, verifyResp.ValidationStatus)
	assert.Equal(t, docCode, verifyResp.DocumentCode)
	assert.Equal(t, "Integration Test Issuer", verifyResp.SignerSubject)
}

func TestHttpServer_SwaggerUI_EnabledInDevMode(t *testing.T) {
	authMw := httpInbound.NewAuthMiddleware(newInMemoryIntegrationRepo(), nil, "")
	router := httpInbound.NewRouter(httpInbound.RouterConfig{
		AuthMiddleware: authMw,
		DevMode:        true,
	})

	// 1. /swagger redirects to /swagger/index.html
	reqRedirect := httptest.NewRequest(http.MethodGet, "/swagger", nil)
	recRedirect := httptest.NewRecorder()
	router.ServeHTTP(recRedirect, reqRedirect)
	assert.Equal(t, http.StatusMovedPermanently, recRedirect.Code)
	assert.Equal(t, "/swagger/index.html", recRedirect.Header().Get("Location"))

	// 2. /swagger/index.html serves Swagger UI page
	reqUI := httptest.NewRequest(http.MethodGet, "/swagger/index.html", nil)
	recUI := httptest.NewRecorder()
	router.ServeHTTP(recUI, reqUI)
	assert.Equal(t, http.StatusOK, recUI.Code)
	assert.Contains(t, recUI.Body.String(), "Swagger UI")

	// 3. /swagger/doc.json serves valid OpenAPI JSON spec
	reqDoc := httptest.NewRequest(http.MethodGet, "/swagger/doc.json", nil)
	recDoc := httptest.NewRecorder()
	router.ServeHTTP(recDoc, reqDoc)
	assert.Equal(t, http.StatusOK, recDoc.Code)
	assert.Contains(t, recDoc.Body.String(), "QuickSign Crypto Engine API")
	assert.Contains(t, recDoc.Body.String(), "/api/v1/sign/pdf")
}

func TestHttpServer_SwaggerUI_DisabledInProductionMode(t *testing.T) {
	authMw := httpInbound.NewAuthMiddleware(newInMemoryIntegrationRepo(), nil, "")
	router := httpInbound.NewRouter(httpInbound.RouterConfig{
		AuthMiddleware: authMw,
		DevMode:        false,
	})

	for _, path := range []string{"/swagger", "/swagger/index.html", "/swagger/doc.json"} {
		req := httptest.NewRequest(http.MethodGet, path, nil)
		rec := httptest.NewRecorder()
		router.ServeHTTP(rec, req)
		assert.Equal(t, http.StatusNotFound, rec.Code, "Swagger endpoint %s must return 404 when DevMode is false", path)
	}
}
