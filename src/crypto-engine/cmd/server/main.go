package main

import (
	"fmt"
	"log"
	"net"
	"os"
	"os/signal"
	"syscall"

	vaultapi "github.com/hashicorp/vault/api"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"

	grpcAdapter "quicksign/crypto-engine/internal/adapters/inbound/grpc"
	vaultAdapter "quicksign/crypto-engine/internal/adapters/outbound/vault"
	"quicksign/crypto-engine/internal/config"
	"quicksign/crypto-engine/internal/core/services"
	"quicksign/crypto-engine/pb"
)

func main() {
	log.Println("[INFO] Starting QuickSign Crypto Engine Service...")

	cfg, err := config.LoadConfig()
	if err != nil {
		log.Fatalf("[FATAL] Failed to load configuration: %v", err)
	}

	// Initialize HashiCorp Vault client
	vaultConfig := vaultapi.DefaultConfig()
	vaultConfig.Address = cfg.VaultAddress
	vaultClient, err := vaultapi.NewClient(vaultConfig)
	if err != nil {
		log.Fatalf("[FATAL] Failed to initialize Vault client: %v", err)
	}

	if cfg.VaultToken != "" {
		vaultClient.SetToken(cfg.VaultToken)
	}

	// Hexagonal Architecture Wiring: Adapters -> Services -> Inbound Handlers
	vaultStore, err := vaultAdapter.NewHashiCorpVaultAdapter(vaultClient, cfg.VaultKVMount)
	if err != nil {
		log.Fatalf("[FATAL] Failed to initialize Vault adapter: %v", err)
	}

	keyGenService, err := services.NewKeyGenService(vaultStore, cfg.VaultPathPrefix)
	if err != nil {
		log.Fatalf("[FATAL] Failed to initialize KeyGenService: %v", err)
	}

	cryptoGrpcServer := grpcAdapter.NewCryptoGrpcServer(keyGenService)

	// Listen on specified gRPC TCP port
	listener, err := net.Listen("tcp", fmt.Sprintf(":%d", cfg.GRPCPort))
	if err != nil {
		log.Fatalf("[FATAL] Failed to listen on gRPC port %d: %v", cfg.GRPCPort, err)
	}

	server := grpc.NewServer()
	pb.RegisterCryptoEngineServiceServer(server, cryptoGrpcServer)
	reflection.Register(server)

	// Graceful shutdown handling
	stopChan := make(chan os.Signal, 1)
	signal.Notify(stopChan, os.Interrupt, syscall.SIGTERM)

	go func() {
		log.Printf("[INFO] QuickSign Crypto Engine gRPC server listening on :%d", cfg.GRPCPort)
		if err := server.Serve(listener); err != nil {
			log.Fatalf("[FATAL] gRPC server terminated with error: %v", err)
		}
	}()

	<-stopChan
	log.Println("[INFO] Shutting down QuickSign Crypto Engine gRPC server gracefully...")
	server.GracefulStop()
	log.Println("[INFO] QuickSign Crypto Engine server stopped cleanly.")
}
