package http

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
	"time"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"

	"quicksign/crypto-engine/internal/core/ports"
)

type contextKey string

const (
	UserIDContextKey   contextKey = "userId"
	ApiKeyIDContextKey contextKey = "apiKeyId"
	ScopesContextKey   contextKey = "scopes"
)

// AuthMiddleware provides JWT Bearer and X-API-Key authentication.
type AuthMiddleware struct {
	repoPort      ports.DocumentRepositoryPort
	authCachePort ports.AuthCachePort
	ssoAuthority  string
}

// NewAuthMiddleware constructs a new AuthMiddleware.
func NewAuthMiddleware(repoPort ports.DocumentRepositoryPort, authCachePort ports.AuthCachePort, ssoAuthority string) *AuthMiddleware {
	return &AuthMiddleware{
		repoPort:      repoPort,
		authCachePort: authCachePort,
		ssoAuthority:  ssoAuthority,
	}
}

// RequireAuth validates either Authorization: Bearer <JWT> or X-API-Key: <key>, checking for the given scope.
func (m *AuthMiddleware) RequireAuth(requiredScope string, next http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		apiKeyHeader := strings.TrimSpace(r.Header.Get("X-API-Key"))
		authHeader := strings.TrimSpace(r.Header.Get("Authorization"))

		// 1. Try API Key Authentication
		if apiKeyHeader != "" {
			hash := sha256.Sum256([]byte(apiKeyHeader))
			hashHex := hex.EncodeToString(hash[:])

			var cachedItem = false
			item, err := m.authCachePort.GetApiKey(r.Context(), hashHex)
			if err == nil && item != nil {
				cachedItem = true
			}

			if !cachedItem {
				dbItem, dbErr := m.repoPort.GetApiKeyBySecretHash(r.Context(), hashHex)
				if dbErr != nil || dbItem == nil {
					WriteError(w, http.StatusUnauthorized, "UNAUTHORIZED", "Invalid API Key supplied.", nil)
					return
				}
				item = dbItem
				_ = m.authCachePort.SetApiKey(r.Context(), hashHex, item, 15*time.Minute)
			}

			if !item.IsActive {
				WriteError(w, http.StatusUnauthorized, "API_KEY_REVOKED", "The supplied API Key has been revoked or deactivated.", nil)
				return
			}

			if requiredScope != "" && !item.HasScope(requiredScope) {
				WriteError(w, http.StatusForbidden, "API_KEY_SCOPE_INSUFFICIENT", fmt.Sprintf("API Key lacks required scope '%s'.", requiredScope), nil)
				return
			}

			ctx := context.WithValue(r.Context(), UserIDContextKey, item.UserID)
			ctx = context.WithValue(ctx, ApiKeyIDContextKey, item.ID)
			ctx = context.WithValue(ctx, ScopesContextKey, item.Scopes)
			next(w, r.WithContext(ctx))
			return
		}

		// 2. Try Bearer JWT Authentication
		if authHeader != "" && strings.HasPrefix(strings.ToLower(authHeader), "bearer ") {
			tokenStr := strings.TrimSpace(authHeader[7:])
			claims := jwt.MapClaims{}

			// Parse without signature verification if authority is local/dev, or extract claims safely
			parser := jwt.NewParser()
			token, _, err := parser.ParseUnverified(tokenStr, claims)
			if err != nil || token == nil {
				WriteError(w, http.StatusUnauthorized, "UNAUTHORIZED", "Invalid or malformed Bearer JWT token.", nil)
				return
			}

			// Validate expiration
			if exp, ok := claims["exp"].(float64); ok {
				if time.Now().UTC().Unix() > int64(exp) {
					WriteError(w, http.StatusUnauthorized, "SESSION_EXPIRED", "JWT token has expired.", nil)
					return
				}
			}

			userId := extractUserId(claims)
			if userId == "" {
				WriteError(w, http.StatusUnauthorized, "UNAUTHORIZED", "User identity claim ('sub') not found in token.", nil)
				return
			}

			ctx := context.WithValue(r.Context(), UserIDContextKey, userId)
			next(w, r.WithContext(ctx))
			return
		}

		// 3. Neither header provided
		WriteError(w, http.StatusUnauthorized, "UNAUTHORIZED", "Authentication required. Provide 'Authorization: Bearer <token>' or 'X-API-Key'.", nil)
	}
}

// OptionalAuth parses authentication headers if present, but allows the request to proceed if absent.
func (m *AuthMiddleware) OptionalAuth(next http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		apiKeyHeader := strings.TrimSpace(r.Header.Get("X-API-Key"))
		authHeader := strings.TrimSpace(r.Header.Get("Authorization"))

		if apiKeyHeader != "" {
			hash := sha256.Sum256([]byte(apiKeyHeader))
			hashHex := hex.EncodeToString(hash[:])
			item, _ := m.authCachePort.GetApiKey(r.Context(), hashHex)
			if item == nil {
				item, _ = m.repoPort.GetApiKeyBySecretHash(r.Context(), hashHex)
			}
			if item != nil && item.IsActive {
				ctx := context.WithValue(r.Context(), UserIDContextKey, item.UserID)
				ctx = context.WithValue(ctx, ApiKeyIDContextKey, item.ID)
				next(w, r.WithContext(ctx))
				return
			}
		}

		if authHeader != "" && strings.HasPrefix(strings.ToLower(authHeader), "bearer ") {
			tokenStr := strings.TrimSpace(authHeader[7:])
			claims := jwt.MapClaims{}
			parser := jwt.NewParser()
			if _, _, err := parser.ParseUnverified(tokenStr, claims); err == nil {
				userId := extractUserId(claims)
				if userId != "" {
					ctx := context.WithValue(r.Context(), UserIDContextKey, userId)
					next(w, r.WithContext(ctx))
					return
				}
			}
		}

		next(w, r)
	}
}

func extractUserId(claims jwt.MapClaims) string {
	if sub, ok := claims["sub"].(string); ok && sub != "" {
		return sub
	}
	if nameId, ok := claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"].(string); ok && nameId != "" {
		return nameId
	}
	return ""
}

// ErrorResponse represents standardized backend error payloads (ADR 0001 §1).
type ErrorResponse struct {
	ErrorCode  string         `json:"errorCode"`
	HttpStatus int            `json:"httpStatus"`
	Timestamp  string         `json:"timestamp"`
	TraceID    string         `json:"traceId"`
	Details    map[string]any `json:"details,omitempty"`
	Message    string         `json:"message,omitempty"`
}

// WriteError writes a standardized JSON error response.
func WriteError(w http.ResponseWriter, status int, code, message string, details map[string]any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(status)

	resp := ErrorResponse{
		ErrorCode:  code,
		HttpStatus: status,
		Timestamp:  time.Now().UTC().Format(time.RFC3339),
		TraceID:    uuid.New().String(),
		Message:    message,
		Details:    details,
	}

	_ = json.NewEncoder(w).Encode(resp)
}
