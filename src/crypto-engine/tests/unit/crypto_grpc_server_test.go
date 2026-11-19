package unit_test

import (
	"context"
	"net"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/test/bufconn"

	grpcAdapter "quicksign/crypto-engine/internal/adapters/inbound/grpc"
	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/services"
	"quicksign/crypto-engine/pb"
)

const bufSize = 1024 * 1024

func setupBufconnGrpcServer(t *testing.T, vaultStore *mockVaultKeyStore) (pb.CryptoEngineServiceClient, func()) {
	lis := bufconn.Listen(bufSize)
	keyGenSvc, err := services.NewKeyGenService(vaultStore, "quicksign/test-users")
	require.NoError(t, err)

	grpcServer := grpc.NewServer()
	cryptoServer := grpcAdapter.NewCryptoGrpcServer(keyGenSvc)
	pb.RegisterCryptoEngineServiceServer(grpcServer, cryptoServer)

	go func() {
		if err := grpcServer.Serve(lis); err != nil && err != grpc.ErrServerStopped {
			t.Errorf("Server error: %v", err)
		}
	}()

	conn, err := grpc.NewClient("passthrough://bufnet",
		grpc.WithContextDialer(func(context.Context, string) (net.Conn, error) {
			return lis.Dial()
		}),
		grpc.WithTransportCredentials(insecure.NewCredentials()),
	)
	require.NoError(t, err)

	client := pb.NewCryptoEngineServiceClient(conn)
	cleanup := func() {
		conn.Close()
		grpcServer.Stop()
		lis.Close()
	}

	return client, cleanup
}

func TestGrpcServer_GenerateKey_Success(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	client, cleanup := setupBufconnGrpcServer(t, mockVault)
	defer cleanup()

	now := time.Now().UTC()
	req := &pb.GenerateKeyRequest{
		CertificateId: "cert-grpc-001",
		UserId:        "user-grpc-001",
		SubjectName:   "gRPC Test Service",
		Algorithm:     string(domain.AlgorithmRSA2048),
		ValidFromUnix: now.Unix(),
		ExpiresAtUnix: now.Add(30 * 24 * time.Hour).Unix(),
	}

	resp, err := client.GenerateCertificateKeyPair(context.Background(), req)
	require.NoError(t, err)
	require.NotNil(t, resp)

	assert.True(t, resp.Success)
	assert.NotEmpty(t, resp.SerialNumber)
	assert.NotEmpty(t, resp.PublicKeyPem)
	assert.Equal(t, "quicksign/test-users/user-grpc-001/keys/cert-grpc-001", resp.VaultKeyPath)
	assert.Empty(t, resp.ErrorMessage)

	// Verify compensating transaction Delete
	delResp, err := client.DeleteCertificateKeyPair(context.Background(), &pb.DeleteKeyRequest{
		VaultKeyPath: resp.VaultKeyPath,
	})
	require.NoError(t, err)
	assert.True(t, delResp.Success)
	assert.Contains(t, delResp.Message, "purged")
}

func TestGrpcServer_GenerateKey_InvalidAlgorithm(t *testing.T) {
	mockVault := newMockVaultKeyStore()
	client, cleanup := setupBufconnGrpcServer(t, mockVault)
	defer cleanup()

	req := &pb.GenerateKeyRequest{
		CertificateId: "cert-invalid-algo",
		UserId:        "user-002",
		SubjectName:   "Invalid Test",
		Algorithm:     "DSA_UNKNOWN",
		ValidFromUnix: time.Now().Unix(),
		ExpiresAtUnix: time.Now().Add(time.Hour).Unix(),
	}

	resp, err := client.GenerateCertificateKeyPair(context.Background(), req)
	require.NoError(t, err)
	assert.False(t, resp.Success)
	assert.Contains(t, resp.ErrorMessage, "invalid algorithm")
}
