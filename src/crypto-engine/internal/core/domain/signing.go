package domain

import (
	"errors"
	"strings"
	"time"
)

var (
	ErrEmptyFileContent  = errors.New("file content cannot be empty")
	ErrInvalidPdfFormat  = errors.New("invalid PDF format: missing PDF magic header")
	ErrEmptyJsonPayload  = errors.New("JSON payload cannot be empty")
	ErrCertNotFound      = errors.New("active certificate not found for user")
	ErrCertRevoked       = errors.New("certificate was revoked before or at the time of signing")
	ErrCertExpired       = errors.New("certificate was expired at the time of signing")
	ErrSigningFailed     = errors.New("cryptographic signing operation failed")
)

// DocumentType represents the format of signed documents.
type DocumentType string

const (
	DocumentTypePdf  DocumentType = "PDF"
	DocumentTypeJson DocumentType = "JSON"
)

// SignPdfInput holds all input parameters required to digitally sign a PDF document.
type SignPdfInput struct {
	FileContent   []byte
	FileName      string
	UserID        string
	CertificateID string
	Reason        string
	Location      string
}

// Validate checks that the PDF signing input satisfies invariants.
func (i *SignPdfInput) Validate() error {
	if len(i.FileContent) == 0 {
		return ErrEmptyFileContent
	}
	// Check PDF magic header '%PDF-'
	if len(i.FileContent) < 5 || string(i.FileContent[:5]) != "%PDF-" {
		return ErrInvalidPdfFormat
	}
	if strings.TrimSpace(i.UserID) == "" {
		return ErrEmptyUserID
	}
	if strings.TrimSpace(i.Reason) == "" {
		i.Reason = "Digital Signature"
	}
	if strings.TrimSpace(i.Location) == "" {
		i.Location = "Vietnam"
	}
	return nil
}

// SignPdfOutput represents the outcome of a PDF signing operation.
type SignPdfOutput struct {
	SignedBytes    []byte
	DocumentCode   string
	DocumentName   string
	FileHash       string
	SignatureValue string
	CreatedAt      time.Time
}

// SignJsonInput holds parameters for creating a JSON Web Signature (JWS).
type SignJsonInput struct {
	Payload       []byte
	UserID        string
	CertificateID string
}

// Validate checks that the JSON signing input satisfies invariants.
func (i *SignJsonInput) Validate() error {
	if len(i.Payload) == 0 {
		return ErrEmptyJsonPayload
	}
	if strings.TrimSpace(i.UserID) == "" {
		return ErrEmptyUserID
	}
	return nil
}

// SignJsonOutput represents the result of signing structured JSON data into JWS.
type SignJsonOutput struct {
	JwsToken     string
	DocumentCode string
	PayloadHash  string
	CreatedAt    time.Time
}
