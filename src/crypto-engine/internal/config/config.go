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
	VaultAddress    string
	VaultToken      string
	VaultKVMount    string
	VaultPathPrefix string
}

// LoadConfig reads configuration from .env (if present) and environment variables.
func LoadConfig() (*Config, error) {
	// Attempt loading environment variables from .env if present
	loadDotEnv()

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

// loadDotEnv searches for a .env file in the current directory and up to 2 parent levels.
// It sets any environment variables that are not already defined in the OS environment.
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
				// Strip surrounding double or single quotes
				val = strings.Trim(val, `"'`)

				// Only set if not already defined in OS environment (allows CLI overrides)
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
