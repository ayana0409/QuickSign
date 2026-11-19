package domain

import (
	"errors"
	"strings"
	"time"
)

// KeyAlgorithm represents supported asymmetric cryptographic algorithms.
type KeyAlgorithm string

const (
	AlgorithmRSA2048   KeyAlgorithm = "RSA2048"
	AlgorithmECDSAP256 KeyAlgorithm = "ECDSAP256"
)

var (
	ErrInvalidAlgorithm   = errors.New("unsupported key algorithm: must be RSA2048 or ECDSAP256")
	ErrEmptyCertificateID = errors.New("certificate ID cannot be empty")
	ErrEmptyUserID        = errors.New("user ID cannot be empty")
	ErrEmptySubjectName   = errors.New("subject name cannot be empty")
	ErrInvalidValidity    = errors.New("expires_at must be strictly after valid_from")
	ErrKeyNotFound        = errors.New("cryptographic key was not found")
)

// ParseAlgorithm parses and normalizes a string into a supported KeyAlgorithm.
// Accepts canonical and hyphenated values (e.g. "RSA-2048", "RSA2048", "ECDSA-P256", "ECDSAP256").
func ParseAlgorithm(raw string) (KeyAlgorithm, error) {
	normalized := strings.ToUpper(strings.ReplaceAll(strings.TrimSpace(raw), "-", ""))
	switch KeyAlgorithm(normalized) {
	case AlgorithmRSA2048:
		return AlgorithmRSA2048, nil
	case AlgorithmECDSAP256:
		return AlgorithmECDSAP256, nil
	default:
		return "", ErrInvalidAlgorithm
	}
}

// CertificateMetadata holds metadata for generating an X.509 certificate.
type CertificateMetadata struct {
	CertificateID string
	UserID        string
	SubjectName   string
	Algorithm     KeyAlgorithm
	ValidFrom     time.Time
	ExpiresAt     time.Time
}

// Validate ensures all certificate generation parameters satisfy business and security requirements.
func (c *CertificateMetadata) Validate() error {
	if strings.TrimSpace(c.CertificateID) == "" {
		return ErrEmptyCertificateID
	}
	if strings.TrimSpace(c.UserID) == "" {
		return ErrEmptyUserID
	}
	if strings.TrimSpace(c.SubjectName) == "" {
		return ErrEmptySubjectName
	}
	if c.Algorithm != AlgorithmRSA2048 && c.Algorithm != AlgorithmECDSAP256 {
		return ErrInvalidAlgorithm
	}
	if !c.ExpiresAt.After(c.ValidFrom) {
		return ErrInvalidValidity
	}
	return nil
}

// GeneratedKeyPairResult represents the outcome of asymmetric key generation.
type GeneratedKeyPairResult struct {
	SerialNumber string
	PublicKeyPEM string
	VaultKeyPath string
}
