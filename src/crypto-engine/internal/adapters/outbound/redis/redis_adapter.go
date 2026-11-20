package redis

import (
	"context"
	"encoding/json"
	"fmt"
	"time"

	goredis "github.com/redis/go-redis/v9"

	"quicksign/crypto-engine/internal/core/domain"
)

// RedisAuthCacheAdapter implements ports.AuthCachePort using Redis.
type RedisAuthCacheAdapter struct {
	client *goredis.Client
}

// NewRedisAuthCacheAdapter creates a new Redis client and checks connection health.
func NewRedisAuthCacheAdapter(addr, password string) (*RedisAuthCacheAdapter, error) {
	rdb := goredis.NewClient(&goredis.Options{
		Addr:     addr,
		Password: password,
		DB:       0,
	})

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	if err := rdb.Ping(ctx).Err(); err != nil {
		// Log warning but don't crash entirely so services can operate even if cache is temporarily offline
		return &RedisAuthCacheAdapter{client: rdb}, fmt.Errorf("warning: redis ping failed at %s: %w", addr, err)
	}

	return &RedisAuthCacheAdapter{client: rdb}, nil
}

// GetApiKey fetches cached API Key metadata from Redis.
func (r *RedisAuthCacheAdapter) GetApiKey(ctx context.Context, secretHash string) (*domain.ApiKeyCacheItem, error) {
	if r.client == nil {
		return nil, nil
	}

	key := "quicksign:apikey:" + secretHash
	val, err := r.client.Get(ctx, key).Result()
	if err != nil {
		if err == goredis.Nil {
			return nil, nil // Cache miss
		}
		return nil, err
	}

	var item domain.ApiKeyCacheItem
	if err := json.Unmarshal([]byte(val), &item); err != nil {
		return nil, err
	}
	return &item, nil
}

// SetApiKey stores API Key metadata into Redis with the given expiration duration.
func (r *RedisAuthCacheAdapter) SetApiKey(ctx context.Context, secretHash string, item *domain.ApiKeyCacheItem, ttl time.Duration) error {
	if r.client == nil || item == nil {
		return nil
	}

	key := "quicksign:apikey:" + secretHash
	bytes, err := json.Marshal(item)
	if err != nil {
		return err
	}

	return r.client.Set(ctx, key, bytes, ttl).Err()
}

// CheckAndRecordVerifyRateLimit sets a 10-minute anti-spam key (SET NX) to determine if a verification is unique.
func (r *RedisAuthCacheAdapter) CheckAndRecordVerifyRateLimit(ctx context.Context, docCode, ip string) (bool, error) {
	if r.client == nil {
		return true, nil // Fallback to unique if cache is disabled
	}

	key := fmt.Sprintf("quicksign:ratelimit:%s:%s", docCode, ip)
	wasSet, err := r.client.SetNX(ctx, key, "1", 10*time.Minute).Result()
	if err != nil {
		// If Redis returns error, gracefully return true so user verification is never blocked
		return true, nil
	}
	return wasSet, nil
}
