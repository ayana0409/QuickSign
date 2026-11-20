package pdf_pades_test

import (
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"math/big"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"quicksign/crypto-engine/internal/adapters/outbound/pdf_pades"
	"quicksign/crypto-engine/tests/fixtures"
)

func TestPadesPdfAdapter_SignAndVerify(t *testing.T) {
	pdfBytes := fixtures.SamplePDF
	require.NotEmpty(t, pdfBytes)

	privKey, err := rsa.GenerateKey(rand.Reader, 2048)
	require.NoError(t, err)

	template := x509.Certificate{
		SerialNumber: big.NewInt(2026),
		Subject:      pkix.Name{CommonName: "QuickSign Test CA", Organization: []string{"QuickSign"}},
		NotBefore:    time.Now().Add(-1 * time.Hour),
		NotAfter:     time.Now().Add(24 * time.Hour),
		KeyUsage:  x509.KeyUsageDigitalSignature,
		ExtKeyUsage: []x509.ExtKeyUsage{
			x509.ExtKeyUsageClientAuth,
			x509.ExtKeyUsageEmailProtection,
		},
	}

	certDER, err := x509.CreateCertificate(rand.Reader, &template, &template, &privKey.PublicKey, privKey)
	require.NoError(t, err)

	adapter := pdf_pades.NewPadesPdfAdapter()

	docCode := "DOC-2026-TESTPDF1"
	qrUrl := "http://localhost:50052/api/v1/verify/code/" + docCode

	signedPdf, sha256Hex, err := adapter.SignPdfPades(pdfBytes, privKey, certDER, docCode, qrUrl, "Agreement approval", "Hanoi, Vietnam")
	require.NoError(t, err)
	assert.NotEmpty(t, signedPdf)
	assert.NotEmpty(t, sha256Hex)
	assert.Greater(t, len(signedPdf), len(pdfBytes))

	// Verify the signed PDF
	sigData, err := adapter.VerifyPdfPades(signedPdf)
	require.NoError(t, err)
	require.NotNil(t, sigData)
	assert.True(t, sigData.IsSignatureValid)
	assert.Equal(t, docCode, sigData.DocumentCode)
	assert.Equal(t, "QuickSign Test CA", sigData.SignerSubject)
}

func TestPadesPdfAdapter_VerifyInvalidPdf(t *testing.T) {
	adapter := pdf_pades.NewPadesPdfAdapter()
	_, err := adapter.VerifyPdfPades([]byte("not a valid pdf"))
	assert.Error(t, err)
}
