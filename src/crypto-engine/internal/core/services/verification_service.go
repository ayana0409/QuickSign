package services

import (
	"context"
	"crypto/sha256"
	"crypto/x509"
	"encoding/hex"
	"encoding/pem"
	"errors"
	"strings"
	"time"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/ports"
)

// VerificationService implements ports.DocumentVerificationUseCase.
type VerificationService struct {
	repoPort      ports.DocumentRepositoryPort
	authCachePort ports.AuthCachePort
	pdfPort       ports.PdfCryptoPort
	jwsPort       ports.JwsCryptoPort
}

// NewVerificationService constructs a new VerificationService instance.
func NewVerificationService(
	repoPort ports.DocumentRepositoryPort,
	authCachePort ports.AuthCachePort,
	pdfPort ports.PdfCryptoPort,
	jwsPort ports.JwsCryptoPort,
) (*VerificationService, error) {
	if repoPort == nil {
		return nil, errors.New("repoPort cannot be nil")
	}
	if pdfPort == nil {
		return nil, errors.New("pdfPort cannot be nil")
	}
	if jwsPort == nil {
		return nil, errors.New("jwsPort cannot be nil")
	}

	return &VerificationService{
		repoPort:      repoPort,
		authCachePort: authCachePort,
		pdfPort:       pdfPort,
		jwsPort:       jwsPort,
	}, nil
}

// VerifyFile verifies the cryptographic signature of an uploaded PDF or JWS token.
func (s *VerificationService) VerifyFile(ctx context.Context, input domain.VerifyFileInput) (*domain.VerifyResult, error) {
	if len(input.FileBytes) == 0 {
		return nil, domain.ErrEmptyVerifyPayload
	}

	method := "WEB_UPLOAD"
	if input.ApiKeyID != nil && *input.ApiKeyID != "" {
		method = "API"
	}

	// Detect if input is PDF or JWS
	isPdf := len(input.FileBytes) >= 5 && string(input.FileBytes[:5]) == "%PDF-"

	if isPdf {
		return s.verifyPdf(ctx, input, method)
	}
	return s.verifyJws(ctx, input, method)
}

func (s *VerificationService) verifyPdf(ctx context.Context, input domain.VerifyFileInput, method string) (*domain.VerifyResult, error) {
	sigData, err := s.pdfPort.VerifyPdfPades(input.FileBytes)
	if err != nil {
		// Log failed attempt with unknown signature
		_ = s.repoPort.SaveVerificationLog(ctx, &domain.VerificationLogEntity{
			SubmittedHash:      hex.EncodeToString(input.FileBytes[:min(32, len(input.FileBytes))]),
			VerificationMethod: method,
			ApiKeyID:           input.ApiKeyID,
			VerifierIP:         input.VerifierIP,
			UserAgent:          &input.UserAgent,
			IsValid:            false,
			ValidationStatus:   domain.StatusUnknownSignature,
			VerifiedAt:         time.Now().UTC(),
		})

		return &domain.VerifyResult{
			IsValid:          false,
			ValidationStatus: domain.StatusUnknownSignature,
			RevocationChecked: false,
		}, nil
	}

	currentHashHex := hex.EncodeToString(sigData.ContentDigest)

	// Lookup document by document_code embedded in signature contact
	doc, err := s.repoPort.GetSignedDocumentByCode(ctx, sigData.DocumentCode)
	if err != nil {
		// If code not in database, it's an unrecognized or external signature
		_ = s.repoPort.SaveVerificationLog(ctx, &domain.VerificationLogEntity{
			SubmittedHash:      currentHashHex,
			VerificationMethod: method,
			ApiKeyID:           input.ApiKeyID,
			VerifierIP:         input.VerifierIP,
			UserAgent:          &input.UserAgent,
			IsValid:            false,
			ValidationStatus:   domain.StatusUnknownSignature,
			VerifiedAt:         time.Now().UTC(),
		})

		return &domain.VerifyResult{
			IsValid:          false,
			ValidationStatus: domain.StatusUnknownSignature,
			DocumentCode:     sigData.DocumentCode,
			SignerSubject:    sigData.SignerSubject,
			SigningTime:      &sigData.SigningTime,
			Sha256Hash:       currentHashHex,
		}, nil
	}

	// Check integrity: compare current signed hash against stored original signed_hash
	isTampered := !sigData.IsSignatureValid || currentHashHex != doc.SignedHash
	var valStatus domain.ValidationStatus
	var isValid bool

	if isTampered {
		valStatus = domain.StatusTamperedContent
		isValid = false
	} else {
		// Signature is mathematically valid. Check certificate status at signing time
		cert, certErr := s.repoPort.GetCertificateById(ctx, doc.CertificateID)
		if certErr != nil {
			valStatus = domain.StatusUnknownSignature
			isValid = false
		} else if cert.InvalidatedFrom != nil && sigData.SigningTime.After(*cert.InvalidatedFrom) {
			valStatus = domain.StatusCertRevoked
			isValid = false
		} else if sigData.SigningTime.After(cert.ExpiresAt) {
			valStatus = domain.StatusCertExpiredAtSigning
			isValid = false
		} else {
			valStatus = domain.StatusValid
			isValid = true
		}
	}

	// Anti-spam and unique verification check
	isUnique := true
	if s.authCachePort != nil {
		isUnique, _ = s.authCachePort.CheckAndRecordVerifyRateLimit(ctx, doc.DocumentCode, input.VerifierIP)
	}

	// Atomically increment verify counters
	_ = s.repoPort.IncrementVerifyCount(ctx, doc.ID, isUnique, isTampered)

	// Save verification log
	_ = s.repoPort.SaveVerificationLog(ctx, &domain.VerificationLogEntity{
		SignedDocumentID:   &doc.ID,
		SubmittedHash:      currentHashHex,
		VerificationMethod: method,
		ApiKeyID:           input.ApiKeyID,
		VerifierIP:         input.VerifierIP,
		UserAgent:          &input.UserAgent,
		IsValid:            isValid,
		ValidationStatus:   valStatus,
		VerifiedAt:         time.Now().UTC(),
	})

	return &domain.VerifyResult{
		IsValid:           isValid,
		ValidationStatus:  valStatus,
		DocumentCode:      doc.DocumentCode,
		DocumentName:      doc.DocumentName,
		SignerSubject:     sigData.SignerSubject,
		SigningTime:       &sigData.SigningTime,
		Sha256Hash:        currentHashHex,
		RevocationChecked: true,
	}, nil
}

func (s *VerificationService) verifyJws(ctx context.Context, input domain.VerifyFileInput, method string) (*domain.VerifyResult, error) {
	jwsStr := strings.TrimSpace(string(input.FileBytes))
	parts := strings.Split(jwsStr, ".")
	if len(parts) != 3 {
		return &domain.VerifyResult{
			IsValid:          false,
			ValidationStatus: domain.StatusUnknownSignature,
		}, nil
	}

	// Preliminary decode of JWS to read document code from JTI
	initialData, err := s.jwsPort.VerifyJws(jwsStr, nil)
	if initialData == nil || initialData.DocumentCode == "" {
		return &domain.VerifyResult{
			IsValid:          false,
			ValidationStatus: domain.StatusUnknownSignature,
		}, nil
	}

	doc, err := s.repoPort.GetSignedDocumentByCode(ctx, initialData.DocumentCode)
	if err != nil {
		return &domain.VerifyResult{
			IsValid:          false,
			ValidationStatus: domain.StatusUnknownSignature,
		}, nil
	}

	cert, err := s.repoPort.GetCertificateById(ctx, doc.CertificateID)
	if err != nil {
		return &domain.VerifyResult{
			IsValid:          false,
			ValidationStatus: domain.StatusUnknownSignature,
		}, nil
	}

	pubKey, err := parsePublicKeyPem(cert.PublicKeyPEM)
	if err != nil {
		return &domain.VerifyResult{
			IsValid:          false,
			ValidationStatus: domain.StatusUnknownSignature,
		}, nil
	}

	verifiedData, err := s.jwsPort.VerifyJws(jwsStr, pubKey)
	isTampered := err != nil || verifiedData == nil || !verifiedData.IsSignatureValid

	var payloadHashHex string
	if verifiedData != nil {
		payloadHashHex = hex.EncodeToString(verifiedData.PayloadDigest)
		if payloadHashHex != doc.OriginalHash {
			isTampered = true
		}
	} else {
		payloadHashHex = hex.EncodeToString(sha256.New().Sum(nil))
	}

	var valStatus domain.ValidationStatus
	var isValid bool

	if isTampered {
		valStatus = domain.StatusTamperedContent
		isValid = false
	} else if cert.InvalidatedFrom != nil && doc.CreatedAt.After(*cert.InvalidatedFrom) {
		valStatus = domain.StatusCertRevoked
		isValid = false
	} else if doc.CreatedAt.After(cert.ExpiresAt) {
		valStatus = domain.StatusCertExpiredAtSigning
		isValid = false
	} else {
		valStatus = domain.StatusValid
		isValid = true
	}

	isUnique := true
	if s.authCachePort != nil {
		isUnique, _ = s.authCachePort.CheckAndRecordVerifyRateLimit(ctx, doc.DocumentCode, input.VerifierIP)
	}

	_ = s.repoPort.IncrementVerifyCount(ctx, doc.ID, isUnique, isTampered)
	_ = s.repoPort.SaveVerificationLog(ctx, &domain.VerificationLogEntity{
		SignedDocumentID:   &doc.ID,
		SubmittedHash:      payloadHashHex,
		VerificationMethod: method,
		ApiKeyID:           input.ApiKeyID,
		VerifierIP:         input.VerifierIP,
		UserAgent:          &input.UserAgent,
		IsValid:            isValid,
		ValidationStatus:   valStatus,
		VerifiedAt:         time.Now().UTC(),
	})

	return &domain.VerifyResult{
		IsValid:           isValid,
		ValidationStatus:  valStatus,
		DocumentCode:      doc.DocumentCode,
		DocumentName:      doc.DocumentName,
		SignerSubject:     cert.SubjectName,
		SigningTime:       &doc.CreatedAt,
		Sha256Hash:        payloadHashHex,
		RevocationChecked: true,
	}, nil
}

// VerifyByCode performs a public QR code lookup while preventing Security Illusion (ADR 0001 §6).
func (s *VerificationService) VerifyByCode(ctx context.Context, documentCode string, verifierIP string, userAgent string) (*domain.VerifyResult, error) {
	if strings.TrimSpace(documentCode) == "" {
		return nil, domain.ErrEmptyVerifyPayload
	}

	doc, err := s.repoPort.GetSignedDocumentByCode(ctx, documentCode)
	if err != nil {
		return nil, domain.ErrDocumentNotFound
	}

	cert, err := s.repoPort.GetCertificateById(ctx, doc.CertificateID)
	signerSubject := "QuickSign Verified Signer"
	if err == nil && cert != nil {
		signerSubject = cert.SubjectName
	}

	// ADR 0001 §6: Return special status CODE_EXISTS_UNVERIFIED_CONTENT
	isUnique := true
	if s.authCachePort != nil {
		isUnique, _ = s.authCachePort.CheckAndRecordVerifyRateLimit(ctx, doc.DocumentCode, verifierIP)
	}

	_ = s.repoPort.IncrementVerifyCount(ctx, doc.ID, isUnique, false)
	_ = s.repoPort.SaveVerificationLog(ctx, &domain.VerificationLogEntity{
		SignedDocumentID:   &doc.ID,
		SubmittedHash:      doc.SignedHash,
		VerificationMethod: "WEB_CODE",
		VerifierIP:         verifierIP,
		UserAgent:          &userAgent,
		IsValid:            true,
		ValidationStatus:   domain.StatusCodeExistsUnverified,
		VerifiedAt:         time.Now().UTC(),
	})

	return &domain.VerifyResult{
		IsValid:             true,
		ValidationStatus:    domain.StatusCodeExistsUnverified,
		DocumentCode:        doc.DocumentCode,
		DocumentName:        doc.DocumentName,
		SignerSubject:       signerSubject,
		SigningTime:         &doc.CreatedAt,
		Sha256Hash:          doc.SignedHash,
		RevocationChecked:   false,
		VerificationWarning: "Mã tài liệu hợp lệ trên hệ thống. Vui lòng tải tệp gốc lên để xác minh nội dung chưa bị chỉnh sửa.",
	}, nil
}

func parsePublicKeyPem(pemStr string) (any, error) {
	block, _ := pem.Decode([]byte(pemStr))
	if block == nil {
		return nil, errors.New("failed to decode public key PEM block")
	}

	if cert, err := x509.ParseCertificate(block.Bytes); err == nil {
		return cert.PublicKey, nil
	}
	if pubKey, err := x509.ParsePKIXPublicKey(block.Bytes); err == nil {
		return pubKey, nil
	}
	return nil, errors.New("unable to parse public key from PEM")
}

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}
