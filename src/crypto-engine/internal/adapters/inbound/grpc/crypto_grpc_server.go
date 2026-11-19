package grpc

import (
	"context"
	"fmt"
	"time"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/ports"
	"quicksign/crypto-engine/pb"
)

// CryptoGrpcServer implements the gRPC interface for CryptoEngineService.
type CryptoGrpcServer struct {
	pb.UnimplementedCryptoEngineServiceServer
	keyGenUseCase ports.KeyGenerationUseCase
}

// NewCryptoGrpcServer constructs a new CryptoGrpcServer.
func NewCryptoGrpcServer(keyGenUseCase ports.KeyGenerationUseCase) *CryptoGrpcServer {
	return &CryptoGrpcServer{
		keyGenUseCase: keyGenUseCase,
	}
}

// GenerateCertificateKeyPair generates an asymmetric key pair, stores private key in Vault, and returns certificate metadata.
func (s *CryptoGrpcServer) GenerateCertificateKeyPair(ctx context.Context, req *pb.GenerateKeyRequest) (*pb.GenerateKeyResponse, error) {
	if req == nil {
		return &pb.GenerateKeyResponse{
			Success:      false,
			ErrorMessage: "request cannot be nil",
		}, nil
	}

	algo, err := domain.ParseAlgorithm(req.Algorithm)
	if err != nil {
		return &pb.GenerateKeyResponse{
			Success:      false,
			ErrorMessage: fmt.Sprintf("invalid algorithm '%s': %v", req.Algorithm, err),
		}, nil
	}

	meta := domain.CertificateMetadata{
		CertificateID: req.CertificateId,
		UserID:        req.UserId,
		SubjectName:   req.SubjectName,
		Algorithm:     algo,
		ValidFrom:     time.Unix(req.ValidFromUnix, 0).UTC(),
		ExpiresAt:     time.Unix(req.ExpiresAtUnix, 0).UTC(),
	}

	result, err := s.keyGenUseCase.GenerateKeyPair(ctx, meta)
	if err != nil {
		return &pb.GenerateKeyResponse{
			Success:      false,
			ErrorMessage: err.Error(),
		}, nil
	}

	return &pb.GenerateKeyResponse{
		Success:      true,
		SerialNumber: result.SerialNumber,
		PublicKeyPem: result.PublicKeyPEM,
		VaultKeyPath: result.VaultKeyPath,
	}, nil
}

// DeleteCertificateKeyPair handles compensating transaction requests to remove orphaned keys from Vault.
func (s *CryptoGrpcServer) DeleteCertificateKeyPair(ctx context.Context, req *pb.DeleteKeyRequest) (*pb.DeleteKeyResponse, error) {
	if req == nil || req.VaultKeyPath == "" {
		return &pb.DeleteKeyResponse{
			Success: false,
			Message: "vault_key_path cannot be empty",
		}, nil
	}

	err := s.keyGenUseCase.DeleteKeyPair(ctx, req.VaultKeyPath)
	if err != nil {
		return &pb.DeleteKeyResponse{
			Success: false,
			Message: fmt.Sprintf("failed to delete key: %v", err),
		}, nil
	}

	return &pb.DeleteKeyResponse{
		Success: true,
		Message: "cryptographic key successfully purged from Vault",
	}, nil
}

// Ensure interface compliance at compile time.
var _ pb.CryptoEngineServiceServer = (*CryptoGrpcServer)(nil)
