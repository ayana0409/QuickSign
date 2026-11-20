package domain

import (
	"errors"
	"time"
)

var (
	ErrEmptyVerifyPayload  = errors.New("verification payload cannot be empty")
	ErrDocumentNotFound    = errors.New("document record not found in system")
	ErrUnknownSignature    = errors.New("unable to parse or detect signature in supplied file")
)

// ValidationStatus represents the detailed outcome of cryptographic verification.
type ValidationStatus string

const (
	StatusValid                     ValidationStatus = "VALID"
	StatusTamperedContent           ValidationStatus = "TAMPERED_CONTENT"
	StatusCertRevoked               ValidationStatus = "CERT_REVOKED"
	StatusCertExpiredAtSigning      ValidationStatus = "CERT_EXPIRED_AT_SIGNING"
	StatusUnknownSignature          ValidationStatus = "UNKNOWN_SIGNATURE"
	StatusCodeExistsUnverified      ValidationStatus = "CODE_EXISTS_UNVERIFIED_CONTENT"
)

// VerifyFileInput holds input parameters for verifying a PDF file or JWS token.
type VerifyFileInput struct {
	FileBytes      []byte
	FileName       string
	VerifierIP     string
	UserAgent      string
	ApiKeyID       *string
}

// VerifyResult represents the unified verification response across PDF, JWS, and QR lookup.
type VerifyResult struct {
	IsValid            bool             `json:"isValid"`
	ValidationStatus   ValidationStatus `json:"validationStatus"`
	DocumentCode       string           `json:"documentCode,omitempty"`
	DocumentName       string           `json:"documentName,omitempty"`
	SignerSubject      string           `json:"signerSubject,omitempty"`
	SigningTime        *time.Time       `json:"signingTime,omitempty"`
	Sha256Hash         string           `json:"sha256Hash,omitempty"`
	RevocationChecked  bool             `json:"revocationChecked"`
	VerificationWarning string          `json:"verificationWarning,omitempty"`
}

// PdfSignatureData encapsulates raw cryptographic data extracted from a signed PDF.
type PdfSignatureData struct {
	DocumentCode     string
	SignerSubject    string
	SigningTime      time.Time
	ContentDigest    []byte
	RawSignature     []byte
	CertificateDER   []byte
	IsSignatureValid bool
}

// JwsSignatureData encapsulates raw data extracted from a JWS compact token.
type JwsSignatureData struct {
	DocumentCode     string
	Algorithm        string
	Payload          []byte
	PayloadDigest    []byte
	RawSignature     []byte
	KeyID            string
	IsSignatureValid bool
}

// CertificateEntity represents a certificate row from PostgreSQL.
type CertificateEntity struct {
	ID               string
	UserID           string
	SubjectName      string
	SerialNumber     string
	Algorithm        KeyAlgorithm
	PublicKeyPEM     string
	VaultKeyPath     string
	Status           string // ACTIVE, REVOKED, EXPIRED
	ExpiresAt        time.Time
	RevokedAt        *time.Time
	InvalidatedFrom  *time.Time
	RevocationReason *string
}

// SignedDocumentEntity represents a signed_documents row in PostgreSQL.
type SignedDocumentEntity struct {
	ID                 string
	UserID             string
	CertificateID      string
	ApiKeyID           *string
	DocumentCode       string
	DocumentName       string
	DocType            DocumentType
	OriginalHash       string
	SignedHash         string
	SignatureValue     string
	StoragePath        *string
	TotalVerifyCount   int
	UniqueVerifyCount  int
	TamperedAlertCount int
	LastVerifiedAt     *time.Time
	CreatedAt          time.Time
}

// VerificationLogEntity represents a record written to verification_logs in PostgreSQL.
type VerificationLogEntity struct {
	ID                 int64
	SignedDocumentID   *string
	SubmittedHash      string
	VerificationMethod string // WEB_UPLOAD, WEB_CODE, API
	ApiKeyID           *string
	VerifierIP         string
	UserAgent          *string
	IsValid            bool
	ValidationStatus   ValidationStatus
	VerifiedAt         time.Time
}

// ApiKeyCacheItem represents cached API Key authorization details.
type ApiKeyCacheItem struct {
	ID       string
	UserID   string
	Scopes   string // e.g. "sign,verify"
	IsActive bool
}

// HasScope checks whether the API key possesses the required permission scope.
func (k *ApiKeyCacheItem) HasScope(scope string) bool {
	if !k.IsActive {
		return false
	}
	parts := splitScopes(k.Scopes)
	for _, s := range parts {
		if s == scope || s == "*" {
			return true
		}
	}
	return false
}

func splitScopes(scopes string) []string {
	raw := []rune(scopes)
	var res []string
	var cur []rune
	for _, r := range raw {
		if r == ',' || r == ' ' {
			if len(cur) > 0 {
				res = append(res, string(cur))
				cur = nil
			}
		} else {
			cur = append(cur, r)
		}
	}
	if len(cur) > 0 {
		res = append(res, string(cur))
	}
	return res
}
