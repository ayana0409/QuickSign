package jws_crypto

import (
	"crypto"
	"crypto/ecdsa"
	"crypto/rand"
	"crypto/rsa"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"errors"
	"fmt"
	"math/big"
	"strings"

	"quicksign/crypto-engine/internal/core/domain"
)

// JwsAdapter implements ports.JwsCryptoPort for signing and verifying RFC 7515 JSON Web Signatures.
type JwsAdapter struct{}

// NewJwsAdapter constructs a new JwsAdapter.
func NewJwsAdapter() *JwsAdapter {
	return &JwsAdapter{}
}

type jwsHeader struct {
	Algorithm string `json:"alg"`
	Type      string `json:"typ"`
	JWTID     string `json:"jti,omitempty"`
}

// SignJws signs a JSON payload producing a Compact JWS (header.payload.signature).
func (a *JwsAdapter) SignJws(payload []byte, privKey any, algo domain.KeyAlgorithm, docCode string) (string, error) {
	var jwsAlg string
	switch algo {
	case domain.AlgorithmRSA2048:
		jwsAlg = "RS256"
	case domain.AlgorithmECDSAP256:
		jwsAlg = "ES256"
	default:
		return "", fmt.Errorf("unsupported JWS algorithm: %s", algo)
	}

	hdr := jwsHeader{
		Algorithm: jwsAlg,
		Type:      "JOSE",
		JWTID:     docCode,
	}
	hdrBytes, err := json.Marshal(hdr)
	if err != nil {
		return "", fmt.Errorf("failed to marshal JWS header: %w", err)
	}

	encodedHeader := base64.RawURLEncoding.EncodeToString(hdrBytes)
	encodedPayload := base64.RawURLEncoding.EncodeToString(payload)
	signingInput := encodedHeader + "." + encodedPayload

	digest := sha256.Sum256([]byte(signingInput))

	var rawSignature []byte
	switch key := privKey.(type) {
	case *rsa.PrivateKey:
		if jwsAlg != "RS256" {
			return "", errors.New("key type *rsa.PrivateKey requires RS256 algorithm")
		}
		rawSignature, err = rsa.SignPKCS1v15(rand.Reader, key, crypto.SHA256, digest[:])
		if err != nil {
			return "", fmt.Errorf("RSA signing failed: %w", err)
		}

	case *ecdsa.PrivateKey:
		if jwsAlg != "ES256" {
			return "", errors.New("key type *ecdsa.PrivateKey requires ES256 algorithm")
		}
		r, s, err := ecdsa.Sign(rand.Reader, key, digest[:])
		if err != nil {
			return "", fmt.Errorf("ECDSA signing failed: %w", err)
		}
		// In JWS ES256 (RFC 7515), signature is raw R || S, 32 bytes each
		rawSignature = make([]byte, 64)
		rBytes := r.Bytes()
		sBytes := s.Bytes()
		copy(rawSignature[32-len(rBytes):32], rBytes)
		copy(rawSignature[64-len(sBytes):64], sBytes)

	default:
		return "", fmt.Errorf("unsupported private key type for JWS: %T", privKey)
	}

	encodedSignature := base64.RawURLEncoding.EncodeToString(rawSignature)
	return signingInput + "." + encodedSignature, nil
}

// VerifyJws parses a compact JWS token, validates the cryptographic signature, and extracts payload metadata.
func (a *JwsAdapter) VerifyJws(jwsString string, pubKey any) (*domain.JwsSignatureData, error) {
	parts := strings.Split(jwsString, ".")
	if len(parts) != 3 {
		return nil, errors.New("invalid compact JWS format: must contain exactly 3 dot-delimited segments")
	}

	headerBytes, err := base64.RawURLEncoding.DecodeString(parts[0])
	if err != nil {
		return nil, fmt.Errorf("failed to base64url decode JWS header: %w", err)
	}

	var hdr jwsHeader
	if err := json.Unmarshal(headerBytes, &hdr); err != nil {
		return nil, fmt.Errorf("failed to unmarshal JWS header: %w", err)
	}

	payloadBytes, err := base64.RawURLEncoding.DecodeString(parts[1])
	if err != nil {
		return nil, fmt.Errorf("failed to base64url decode JWS payload: %w", err)
	}

	signatureBytes, err := base64.RawURLEncoding.DecodeString(parts[2])
	if err != nil {
		return nil, fmt.Errorf("failed to base64url decode JWS signature: %w", err)
	}

	signingInput := parts[0] + "." + parts[1]
	digest := sha256.Sum256([]byte(signingInput))
	payloadDigest := sha256.Sum256(payloadBytes)

	var isValid bool
	if pubKey == nil {
		isValid = false
	} else {
		switch key := pubKey.(type) {
		case *rsa.PublicKey:
			if hdr.Algorithm != "RS256" {
				return nil, fmt.Errorf("algorithm mismatch: public key is RSA but JWS header specifies '%s'", hdr.Algorithm)
			}
			verifyErr := rsa.VerifyPKCS1v15(key, crypto.SHA256, digest[:], signatureBytes)
			isValid = verifyErr == nil

		case *ecdsa.PublicKey:
			if hdr.Algorithm != "ES256" {
				return nil, fmt.Errorf("algorithm mismatch: public key is ECDSA but JWS header specifies '%s'", hdr.Algorithm)
			}
			if len(signatureBytes) != 64 {
				return nil, fmt.Errorf("invalid ES256 signature length %d (expected 64 bytes)", len(signatureBytes))
			}
			r := new(big.Int).SetBytes(signatureBytes[:32])
			s := new(big.Int).SetBytes(signatureBytes[32:])
			isValid = ecdsa.Verify(key, digest[:], r, s)

		default:
			return nil, fmt.Errorf("unsupported public key type %T", pubKey)
		}
	}

	return &domain.JwsSignatureData{
		DocumentCode:     hdr.JWTID,
		Algorithm:        hdr.Algorithm,
		Payload:          payloadBytes,
		PayloadDigest:    payloadDigest[:],
		RawSignature:     signatureBytes,
		IsSignatureValid: isValid,
	}, nil
}
