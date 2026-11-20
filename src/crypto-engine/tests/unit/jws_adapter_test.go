package unit_test

import (
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/rsa"
	"strings"
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"quicksign/crypto-engine/internal/adapters/outbound/jws_crypto"
	"quicksign/crypto-engine/internal/core/domain"
)

func TestJwsAdapter_SignAndVerify_RSA2048(t *testing.T) {
	adapter := jws_crypto.NewJwsAdapter()
	privKey, err := rsa.GenerateKey(rand.Reader, 2048)
	require.NoError(t, err)

	payload := []byte(`{"invoiceId":"INV-2026-001","amount":500000}`)
	docCode := "DOC-2026-RSA001"

	// Sign JWS
	token, err := adapter.SignJws(payload, privKey, domain.AlgorithmRSA2048, docCode)
	require.NoError(t, err)
	assert.NotEmpty(t, token)
	assert.Equal(t, 3, len(strings.Split(token, ".")))

	// Verify JWS with Public Key
	sigData, err := adapter.VerifyJws(token, &privKey.PublicKey)
	require.NoError(t, err)
	assert.True(t, sigData.IsSignatureValid)
	assert.Equal(t, docCode, sigData.DocumentCode)
	assert.Equal(t, "RS256", sigData.Algorithm)
	assert.JSONEq(t, string(payload), string(sigData.Payload))
}

func TestJwsAdapter_SignAndVerify_ECDSAP256(t *testing.T) {
	adapter := jws_crypto.NewJwsAdapter()
	privKey, err := ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
	require.NoError(t, err)

	payload := []byte(`{"certificateId":"CERT-123","student":"Nguyen Van A"}`)
	docCode := "DOC-2026-ECDSA01"

	// Sign JWS
	token, err := adapter.SignJws(payload, privKey, domain.AlgorithmECDSAP256, docCode)
	require.NoError(t, err)
	assert.NotEmpty(t, token)

	// Verify JWS
	sigData, err := adapter.VerifyJws(token, &privKey.PublicKey)
	require.NoError(t, err)
	assert.True(t, sigData.IsSignatureValid)
	assert.Equal(t, docCode, sigData.DocumentCode)
	assert.Equal(t, "ES256", sigData.Algorithm)
	assert.JSONEq(t, string(payload), string(sigData.Payload))
}

func TestJwsAdapter_TamperedPayload_FailsVerification(t *testing.T) {
	adapter := jws_crypto.NewJwsAdapter()
	privKey, err := rsa.GenerateKey(rand.Reader, 2048)
	require.NoError(t, err)

	payload := []byte(`{"amount":100}`)
	token, err := adapter.SignJws(payload, privKey, domain.AlgorithmRSA2048, "DOC-123")
	require.NoError(t, err)

	// Tamper token payload segment
	parts := strings.Split(token, ".")
	tamperedToken := parts[0] + ".eyJhbW91bnQiOjk5OTk5OX0." + parts[2]

	sigData, err := adapter.VerifyJws(tamperedToken, &privKey.PublicKey)
	require.NoError(t, err)
	assert.False(t, sigData.IsSignatureValid, "Tampered JWS must fail verification")
}

func TestJwsAdapter_InvalidFormat_ReturnsError(t *testing.T) {
	adapter := jws_crypto.NewJwsAdapter()
	privKey, _ := rsa.GenerateKey(rand.Reader, 2048)

	_, err := adapter.VerifyJws("not-a-valid-token", &privKey.PublicKey)
	assert.Error(t, err)
}
