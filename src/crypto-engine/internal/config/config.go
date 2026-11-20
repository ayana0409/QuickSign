package config

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strconv"
	"strings"
)

// Config represents runtime configuration loaded from environment variables or .env file.
type Config struct {
	GRPCPort        int
	HTTPPort        int
	AppEnv          string // "development" | "production" — controls Swagger UI
	VaultAddress    string
	VaultToken      string
	VaultKVMount    string
	VaultPathPrefix string
	DatabaseURL     string
	RedisAddr       string
	RedisPassword   string
	SSOAuthority    string
	AppBaseURL      string
}

// LoadConfig reads configuration from .env (if present) and environment variables.
func LoadConfig() (*Config, error) {
	loadDotEnv()

	grpcPortStr := getEnv("GRPC_PORT", "50051")
	grpcPort, err := strconv.Atoi(grpcPortStr)
	if err != nil {
		return nil, fmt.Errorf("invalid GRPC_PORT '%s': %w", grpcPortStr, err)
	}

	httpPortStr := getEnv("HTTP_PORT", "50052")
	httpPort, err := strconv.Atoi(httpPortStr)
	if err != nil {
		return nil, fmt.Errorf("invalid HTTP_PORT '%s': %w", httpPortStr, err)
	}

	cfg := &Config{
		GRPCPort:        grpcPort,
		HTTPPort:        httpPort,
		AppEnv:          strings.ToLower(getEnv("APP_ENV", "development")),
		VaultAddress:    getEnv("VAULT_ADDR", "http://127.0.0.1:8200"),
		VaultToken:      getEnv("VAULT_TOKEN", ""),
		VaultKVMount:    getEnv("VAULT_KV_MOUNT", "secret"),
		VaultPathPrefix: getEnv("VAULT_PATH_PREFIX", "quicksign/users"),
		DatabaseURL:     getEnv("DATABASE_URL", ""),
		RedisAddr:       getEnv("REDIS_ADDR", "127.0.0.1:6379"),
		RedisPassword:   getEnv("REDIS_PASSWORD", ""),
		SSOAuthority:    getEnv("SSO_AUTHORITY", ""),
		AppBaseURL:      getEnv("APP_BASE_URL", "http://localhost:50052"),
	}

	return cfg, nil
}

func loadDotEnv() {
	searchPaths := []string{".env", "../.env", "../../.env"}

	for _, candidate := range searchPaths {
		file, err := os.Open(candidate)
		if err != nil {
			continue
		}
		defer file.Close()

		log.Printf("[INFO] Discovered and loading configuration from '%s'", candidate)
		scanner := bufio.NewScanner(file)
		for scanner.Scan() {
			line := strings.TrimSpace(scanner.Text())
			if line == "" || strings.HasPrefix(line, "#") {
				continue
			}

			parts := strings.SplitN(line, "=", 2)
			if len(parts) == 2 {
				key := strings.TrimSpace(parts[0])
				val := strings.TrimSpace(parts[1])
				val = strings.Trim(val, `"'`)

				if os.Getenv(key) == "" {
					_ = os.Setenv(key, val)
				}
			}
		}
		return
	}
}

func getEnv(key, defaultVal string) string {
	val := os.Getenv(key)
	if strings.TrimSpace(val) == "" {
		return defaultVal
	}
	return strings.TrimSpace(val)
}
