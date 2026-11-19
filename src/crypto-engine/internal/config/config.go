package config

import (
	"fmt"
	"os"
	"strconv"
	"strings"
)

// Config represents runtime configuration loaded from environment variables.
type Config struct {
	GRPCPort        int
	VaultAddress    string
	VaultToken      string
	VaultKVMount    string
	VaultPathPrefix string
}

// LoadConfig reads configuration from environment variables with sensible defaults.
func LoadConfig() (*Config, error) {
	portStr := getEnv("GRPC_PORT", "50051")
	port, err := strconv.Atoi(portStr)
	if err != nil {
		return nil, fmt.Errorf("invalid GRPC_PORT '%s': %w", portStr, err)
	}

	cfg := &Config{
		GRPCPort:        port,
		VaultAddress:    getEnv("VAULT_ADDR", "http://127.0.0.1:8200"),
		VaultToken:      getEnv("VAULT_TOKEN", ""),
		VaultKVMount:    getEnv("VAULT_KV_MOUNT", "secret"),
		VaultPathPrefix: getEnv("VAULT_PATH_PREFIX", "quicksign/users"),
	}

	return cfg, nil
}

func getEnv(key, defaultVal string) string {
	val := os.Getenv(key)
	if strings.TrimSpace(val) == "" {
		return defaultVal
	}
	return strings.TrimSpace(val)
}
