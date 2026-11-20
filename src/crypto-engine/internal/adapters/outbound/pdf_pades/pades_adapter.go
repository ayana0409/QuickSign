package pdf_pades

import (
	"bytes"
	"crypto"
	"crypto/sha256"
	"crypto/x509"
	"encoding/hex"
	"fmt"
	"time"

	"github.com/digitorus/pdfsign"
	"github.com/digitorus/pdfsign/images"
	"github.com/skip2/go-qrcode"

	"quicksign/crypto-engine/internal/core/domain"
)

// PadesPdfAdapter implements ports.PdfCryptoPort using digitorus/pdfsign.
type PadesPdfAdapter struct{}

// NewPadesPdfAdapter constructs a new PadesPdfAdapter.
func NewPadesPdfAdapter() *PadesPdfAdapter {
	return &PadesPdfAdapter{}
}

// SignPdfPades signs an existing PDF with detached CMS PKCS#7 signature and embeds a visual QR code and metadata.
func (a *PadesPdfAdapter) SignPdfPades(
	pdfBytes []byte,
	privKey any,
	certDER []byte,
	docCode string,
	qrUrl string,
	reason, location string,
) ([]byte, string, error) {
	if len(pdfBytes) == 0 {
		return nil, "", domain.ErrEmptyFileContent
	}

	cert, err := x509.ParseCertificate(certDER)
	if err != nil {
		return nil, "", fmt.Errorf("failed to parse certificate for PDF signing: %w", err)
	}

	signer, ok := privKey.(crypto.Signer)
	if !ok {
		return nil, "", fmt.Errorf("private key does not implement crypto.Signer interface (type: %T)", privKey)
	}

	doc, err := pdfsign.Open(bytes.NewReader(pdfBytes), int64(len(pdfBytes)))
	if err != nil {
		return nil, "", fmt.Errorf("failed to parse PDF document structure: %w", err)
	}

	// Generate QR Code PNG bytes
	qrPNG, err := qrcode.Encode(qrUrl, qrcode.Medium, 128)
	if err != nil {
		return nil, "", fmt.Errorf("failed to generate QR code image: %w", err)
	}

	// Build visual appearance on the signature field (bottom-right)
	signBuilder := doc.Sign(signer, cert)
	signBuilder.Reason(reason)
	signBuilder.Location(location)
	signBuilder.Contact(docCode)
	signBuilder.SignerName(cert.Subject.CommonName)
	signBuilder.Digest(crypto.SHA256)

	appearance := pdfsign.NewAppearance(200, 60)
	appearance.Image(&images.Image{
		Name: "qr",
		Data: qrPNG,
	}).Rect(5, 5, 50, 50)

	signText := fmt.Sprintf("Digitally Signed by:\n%s\nDate: %s\nCode: %s",
		cert.Subject.CommonName,
		time.Now().UTC().Format("2006-01-02 15:04:05 UTC"),
		docCode,
	)
	appearance.Text(signText).
		Position(60, 45).
		Font(pdfsign.StandardFont(pdfsign.Helvetica), 7)

	signBuilder.Appearance(appearance, 350, 40)

	var output bytes.Buffer
	_, err = doc.Write(&output)
	if err != nil {
		return nil, "", fmt.Errorf("failed to write PAdES signed PDF: %w", err)
	}

	signedBytes := output.Bytes()
	hash := sha256.Sum256(signedBytes)
	fileHash := hex.EncodeToString(hash[:])

	return signedBytes, fileHash, nil
}

// VerifyPdfPades parses and validates the PAdES signature embedded in the PDF byte stream.
func (a *PadesPdfAdapter) VerifyPdfPades(pdfBytes []byte) (*domain.PdfSignatureData, error) {
	if len(pdfBytes) == 0 {
		return nil, domain.ErrEmptyVerifyPayload
	}

	doc, err := pdfsign.Open(bytes.NewReader(pdfBytes), int64(len(pdfBytes)))
	if err != nil {
		return nil, fmt.Errorf("invalid PDF structure: %w", err)
	}

	// Execute signature verification
	verifyResults := doc.Verify().TrustSelfSigned(true).SkipRevocationCheck(true).Signatures()
	if len(verifyResults) == 0 {
		return nil, domain.ErrUnknownSignature
	}

	var foundSig *domain.PdfSignatureData
	for _, res := range verifyResults {
		sigData := &domain.PdfSignatureData{
			DocumentCode:     res.Contact, // Contact stores our unique document_code
			SignerSubject:    res.SignerName,
			SigningTime:      res.SigningTime,
			IsSignatureValid: res.Valid,
		}

		if res.Certificate != nil {
			sigData.CertificateDER = res.Certificate.Raw
			if sigData.SignerSubject == "" {
				sigData.SignerSubject = res.Certificate.Subject.CommonName
			}
		}

		foundSig = sigData
		break // Return primary signature
	}

	if foundSig == nil {
		return nil, domain.ErrUnknownSignature
	}

	hash := sha256.Sum256(pdfBytes)
	foundSig.ContentDigest = hash[:]

	return foundSig, nil
}
