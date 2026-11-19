package integration_test

import (
	"context"
	"net"
	"os"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/test/bufconn"

	grpcAdapter "quicksign/crypto-engine/internal/adapters/inbound/grpc"
	vaultAdapter "quicksign/crypto-engine/internal/adapters/outbound/vault"
	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/services"
	"quicksign/crypto-engine/pb"
)

const bufconnSize = 1024 * 1024

func setupRealVaultGrpcServer(t *testing.T) (pb.CryptoEngineServiceClient, *vaultAdapter.HashiCorpVaultAdapter, func()) {
	client, mount := getVaultClient(t)

	vaultStore, err := vaultAdapter.NewHashiCorpVaultAdapter(client, mount)
	require.NoError(t, err)

	prefix := os.Getenv("VAULT_PATH_PREFIX")
	if prefix == "" {
		prefix = "quicksign-test/users"
	}

	keyGenSvc, err := services.NewKeyGenService(vaultStore, prefix)
	require.NoError(t, err)

	lis := bufconn.Listen(bufconnSize)
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

	grpcClient := pb.NewCryptoEngineServiceClient(conn)
	cleanup := func() {
		conn.Close()
		grpcServer.Stop()
		lis.Close()
	}

	return grpcClient, vaultStore, cleanup
}

func TestGrpcServer_GenerateKeyPair_RSA2048_Integration(t *testing.T) {
	client, vaultStore, cleanup := setupRealVaultGrpcServer(t)
	defer cleanup()

	ctx := context.Background()
	now := time.Now().UTC()
	req := &pb.GenerateKeyRequest{
		CertificateId: "int-cert-rsa-001",
		UserId:        "int-user-001",
		SubjectName:   "Integration RSA Organization",
		Algorithm:     string(domain.AlgorithmRSA2048),
		ValidFromUnix: now.Unix(),
		ExpiresAtUnix: now.Add(90 * 24 * time.Hour).Unix(),
	}

	resp, err := client.GenerateCertificateKeyPair(ctx, req)
	require.NoError(t, err)
	require.NotNil(t, resp)

	assert.True(t, resp.Success)
	assert.NotEmpty(t, resp.SerialNumber)
	assert.NotEmpty(t, resp.PublicKeyPem)
	assert.NotEmpty(t, resp.VaultKeyPath)

	// Verify key actually exists in real Vault
	privPEM, err := vaultStore.GetPrivateKey(ctx, resp.VaultKeyPath)
	require.NoError(t, err, "Private key must be successfully stored in Vault")
	assert.Contains(t, string(privPEM), "-----BEGIN PRIVATE KEY-----")

	// Clean up from Vault
	delResp, err := client.DeleteCertificateKeyPair(ctx, &pb.DeleteKeyRequest{
		VaultKeyPath: resp.VaultKeyPath,
	})
	require.NoError(t, err)
	assert.True(t, delResp.Success)
}

func TestGrpcServer_GenerateKeyPair_ECDSAP256_Integration(t *testing.T) {
	client, vaultStore, cleanup := setupRealVaultGrpcServer(t)
	defer cleanup()

	ctx := context.Background()
	now := time.Now().UTC()
	req := &pb.GenerateKeyRequest{
		CertificateId: "int-cert-ecdsa-002",
		UserId:        "int-user-002",
		SubjectName:   "Integration ECDSA Organization",
		Algorithm:     string(domain.AlgorithmECDSAP256),
		ValidFromUnix: now.Unix(),
		ExpiresAtUnix: now.Add(60 * 24 * time.Hour).Unix(),
	}

	resp, err := client.GenerateCertificateKeyPair(ctx, req)
	require.NoError(t, err)
	require.NotNil(t, resp)

	assert.True(t, resp.Success)
	assert.NotEmpty(t, resp.SerialNumber)
	assert.NotEmpty(t, resp.PublicKeyPem)

	// Clean up
	_ = vaultStore.DeletePrivateKey(ctx, resp.VaultKeyPath)
}

func TestGrpcServer_CompensatingTransaction_Flow(t *testing.T) {
	client, vaultStore, cleanup := setupRealVaultGrpcServer(t)
	defer cleanup()

	ctx := context.Background()
	now := time.Now().UTC()
	certId := "int-cert-compensating-003"

	// 1. Generate key material (stores in Vault)
	resp, err := client.GenerateCertificateKeyPair(ctx, &pb.GenerateKeyRequest{
		CertificateId: certId,
		UserId:        "int-user-003",
		SubjectName:   "Rollback Safety Test",
		Algorithm:     string(domain.AlgorithmRSA2048),
		ValidFromUnix: now.Unix(),
		ExpiresAtUnix: now.Add(24 * time.Hour).Unix(),
	})
	require.NoError(t, err)
	require.True(t, resp.Success)

	// 2. Simulate downstream database persistence failure -> Trigger compensating transaction
	delResp, err := client.DeleteCertificateKeyPair(ctx, &pb.DeleteKeyRequest{
		VaultKeyPath: resp.VaultKeyPath,
	})
	require.NoError(t, err)
	assert.True(t, delResp.Success)

	// 3. Confirm Vault no longer retains the private key (no orphan keys left)
	_, err = vaultStore.GetPrivateKey(ctx, resp.VaultKeyPath)
	assert.Error(t, err, "Key must not be reachable after compensating transaction")
}
