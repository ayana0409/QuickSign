package memguard

import (
	"crypto/ecdsa"
	"crypto/rsa"
	"math/big"
)

// ZeroBytes overwrites a byte slice with zeros to prevent sensitive data from persisting in memory.
// Complies with ADR 0001: Standardization, Cryptography & Distributed Integrity.
func ZeroBytes(data []byte) {
	for i := range data {
		data[i] = 0
	}
}

// ZeroBigInt securely wipes the underlying word slice of a math/big.Int.
func ZeroBigInt(n *big.Int) {
	if n == nil {
		return
	}
	words := n.Bits()
	for i := range words {
		words[i] = 0
	}
	n.SetInt64(0)
}

// ZeroRSAPrivateKey wipes all secret components of an RSA private key from RAM.
func ZeroRSAPrivateKey(key *rsa.PrivateKey) {
	if key == nil {
		return
	}

	ZeroBigInt(key.D)
	for _, prime := range key.Primes {
		ZeroBigInt(prime)
	}

	ZeroBigInt(key.Precomputed.Dp)
	ZeroBigInt(key.Precomputed.Dq)
	ZeroBigInt(key.Precomputed.Qinv)

	for _, crt := range key.Precomputed.CRTValues {
		ZeroBigInt(crt.Exp)
		ZeroBigInt(crt.Coeff)
		ZeroBigInt(crt.R)
	}
}

// ZeroECDSAPrivateKey wipes the private scalar D of an ECDSA private key from RAM.
func ZeroECDSAPrivateKey(key *ecdsa.PrivateKey) {
	if key == nil {
		return
	}
	ZeroBigInt(key.D)
}

// ZeroPrivateKey securely wipes any supported private key (*rsa.PrivateKey or *ecdsa.PrivateKey) from RAM.
func ZeroPrivateKey(key any) {
	switch k := key.(type) {
	case *rsa.PrivateKey:
		ZeroRSAPrivateKey(k)
	case *ecdsa.PrivateKey:
		ZeroECDSAPrivateKey(k)
	}
}
