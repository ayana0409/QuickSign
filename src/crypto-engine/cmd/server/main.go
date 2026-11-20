package main

import (
	"context"
	"fmt"
	"log"
	"net"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	vaultapi "github.com/hashicorp/vault/api"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"

	grpcAdapter "quicksign/crypto-engine/internal/adapters/inbound/grpc"
	httpAdapter "quicksign/crypto-engine/internal/adapters/inbound/http"
	"quicksign/crypto-engine/internal/adapters/outbound/jws_crypto"
	"quicksign/crypto-engine/internal/adapters/outbound/pdf_pades"
	"quicksign/crypto-engine/internal/adapters/outbound/postgres"
	"quicksign/crypto-engine/internal/adapters/outbound/redis"
	vaultAdapter "quicksign/crypto-engine/internal/adapters/outbound/vault"
	"quicksign/crypto-engine/internal/config"
	"quicksign/crypto-engine/internal/core/services"
	"quicksign/crypto-engine/pb"

	// Import generated Swagger docs — only compiled in when docs/ package exists.
	_ "quicksign/crypto-engine/docs"
)

// @title           QuickSign Crypto Engine API
// @version         1.0
// @description     Go Crypto Engine — HTTP REST API for PDF/JSON signing (PAdES & JWS) and document verification.
// @description     Supports dual authentication: JWT Bearer (Web Portal) and X-API-Key (third-party integrations).

// @contact.name    QuickSign Engineering
// @contact.url     https://github.com/quicksign

// @license.name    MIT
// @license.url     https://opensource.org/licenses/MIT

// @host     localhost:50052
// @BasePath /

// @securityDefinitions.apikey  BearerAuth
// @in                          header
// @name                        Authorization
// @description                 JWT Bearer token from QuickBite Identity — format: "Bearer {token}"

// @securityDefinitions.apikey  ApiKeyAuth
// @in                          header
// @name                        X-API-Key
// @description                 QuickSign API Key — format: "qs_live_{32-hex-bytes}"

// @tag.name         Signing
// @tag.description  Endpoints for signing PDF and JSON documents

// @tag.name         Verification
// @tag.description  Endpoints for verifying document integrity and QR code lookup

// @tag.name         Health
// @tag.description  Service health probe

func main() {
	log.Println("[INFO] Starting QuickSign Crypto Engine Services (gRPC & HTTP)...")

	cfg, err := config.LoadConfig()
	if err != nil {
		log.Fatalf("[FATAL] Failed to load configuration: %v", err)
	}

	// 1. Initialize HashiCorp Vault Client & Adapter
	vaultConfig := vaultapi.DefaultConfig()
	vaultConfig.Address = cfg.VaultAddress
	vaultClient, err := vaultapi.NewClient(vaultConfig)
	if err != nil {
		log.Fatalf("[FATAL] Failed to initialize Vault client: %v", err)
	}

	if cfg.VaultToken != "" {
		vaultClient.SetToken(cfg.VaultToken)
	}

	vaultStore, err := vaultAdapter.NewHashiCorpVaultAdapter(vaultClient, cfg.VaultKVMount)
	if err != nil {
		log.Fatalf("[FATAL] Failed to initialize Vault adapter: %v", err)
	}

	// 2. Initialize PostgreSQL Repository
	ctx := context.Background()
	var pgRepo *postgres.PgxDocumentRepository
	if cfg.DatabaseURL != "" {
		repo, err := postgres.NewPgxDocumentRepository(ctx, cfg.DatabaseURL)
		if err != nil {
			log.Printf("[WARN] Failed to connect to PostgreSQL database: %v. Running in degraded mode.", err)
		} else {
			pgRepo = repo
			log.Println("[INFO] PostgreSQL connection pool initialized successfully.")
		}
	} else {
		log.Println("[WARN] DATABASE_URL not set in environment or .env. Running without database persistence.")
	}

	// 3. Initialize Redis Auth Cache Adapter
	var redisCache *redis.RedisAuthCacheAdapter
	if cfg.RedisAddr != "" {
		rCache, err := redis.NewRedisAuthCacheAdapter(cfg.RedisAddr, cfg.RedisPassword)
		if err != nil {
			log.Printf("[WARN] Redis ping failed: %v. Running with fallback in-memory/direct DB cache.", err)
		} else {
			redisCache = rCache
			log.Println("[INFO] Redis auth cache connected successfully.")
		}
	}

	// 4. Initialize Cryptographic Adapters
	padesAdapter := pdf_pades.NewPadesPdfAdapter()
	jwsAdapter := jws_crypto.NewJwsAdapter()

	// 5. Initialize Core Application Services
	keyGenService, err := services.NewKeyGenService(vaultStore, cfg.VaultPathPrefix)
	if err != nil {
		log.Fatalf("[FATAL] Failed to initialize KeyGenService: %v", err)
	}

	// 6. Setup gRPC Server (Port :50051)
	cryptoGrpcServer := grpcAdapter.NewCryptoGrpcServer(keyGenService)
	grpcListener, err := net.Listen("tcp", fmt.Sprintf(":%d", cfg.GRPCPort))
	if err != nil {
		log.Fatalf("[FATAL] Failed to listen on gRPC port %d: %v", cfg.GRPCPort, err)
	}

	grpcServer := grpc.NewServer()
	pb.RegisterCryptoEngineServiceServer(grpcServer, cryptoGrpcServer)
	reflection.Register(grpcServer)

	// 7. Setup HTTP REST Server (Port :50052)
	var httpServer *http.Server
	if pgRepo != nil {
		signingService, err := services.NewSigningService(vaultStore, pgRepo, padesAdapter, jwsAdapter, cfg.AppBaseURL)
		if err != nil {
			log.Fatalf("[FATAL] Failed to initialize SigningService: %v", err)
		}

		verifyService, err := services.NewVerificationService(pgRepo, redisCache, padesAdapter, jwsAdapter)
		if err != nil {
			log.Fatalf("[FATAL] Failed to initialize VerificationService: %v", err)
		}

		authMiddleware := httpAdapter.NewAuthMiddleware(pgRepo, redisCache, cfg.SSOAuthority)
		signHandler := httpAdapter.NewSignHandler(signingService)
		verifyHandler := httpAdapter.NewVerifyHandler(verifyService)

		router := httpAdapter.NewRouter(httpAdapter.RouterConfig{
			AuthMiddleware: authMiddleware,
			SignHandler:    signHandler,
			VerifyHandler:  verifyHandler,
			DevMode:        cfg.AppEnv == "development",
		})

		httpServer = &http.Server{
			Addr:              fmt.Sprintf(":%d", cfg.HTTPPort),
			Handler:           router,
			ReadHeaderTimeout: 10 * time.Second,
			WriteTimeout:      60 * time.Second,
			IdleTimeout:       120 * time.Second,
		}
	}

	// 8. Launch servers concurrently
	stopChan := make(chan os.Signal, 1)
	signal.Notify(stopChan, os.Interrupt, syscall.SIGTERM)

	go func() {
		log.Printf("[INFO] QuickSign Crypto Engine gRPC server listening on :%d", cfg.GRPCPort)
		if err := grpcServer.Serve(grpcListener); err != nil && err != grpc.ErrServerStopped {
			log.Fatalf("[FATAL] gRPC server terminated with error: %v", err)
		}
	}()

	if httpServer != nil {
		go func() {
			log.Printf("[INFO] QuickSign Crypto Engine REST HTTP server listening on :%d", cfg.HTTPPort)
			if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
				log.Fatalf("[FATAL] HTTP REST server terminated with error: %v", err)
			}
		}()
	}

	// 9. Graceful Shutdown
	<-stopChan
	log.Println("[INFO] Shutting down QuickSign Crypto Engine services gracefully...")

	shutdownCtx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	grpcServer.GracefulStop()

	if httpServer != nil {
		if err := httpServer.Shutdown(shutdownCtx); err != nil {
			log.Printf("[ERROR] HTTP server shutdown error: %v", err)
		}
	}

	if pgRepo != nil {
		pgRepo.Close()
	}

	log.Println("[INFO] QuickSign Crypto Engine services stopped cleanly.")
}
