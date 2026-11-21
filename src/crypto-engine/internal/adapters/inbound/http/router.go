package http

import (
	"encoding/json"
	"net/http"
	"time"

	httpSwagger "github.com/swaggo/http-swagger"

	// Register generated OpenAPI/Swagger specification for the HTTP router.
	_ "quicksign/crypto-engine/docs"
)

// RouterConfig contains dependencies for building the HTTP server router.
type RouterConfig struct {
	AuthMiddleware *AuthMiddleware
	SignHandler    *SignHandler
	VerifyHandler  *VerifyHandler
	// DevMode enables the Swagger UI at /swagger/ when true (APP_ENV=development).
	DevMode bool
}

// NewRouter sets up the HTTP mux with CORS, routes, and optional Swagger UI in dev mode.
func NewRouter(cfg RouterConfig) http.Handler {
	mux := http.NewServeMux()

	// Health check
	mux.HandleFunc("GET /health", handleHealth)

	// Signing Endpoints (Require 'sign' scope / authentication)
	mux.HandleFunc("POST /api/v1/sign/pdf", cfg.AuthMiddleware.RequireAuth("sign", cfg.SignHandler.HandleSignPdf))
	mux.HandleFunc("POST /api/v1/sign/json", cfg.AuthMiddleware.RequireAuth("sign", cfg.SignHandler.HandleSignJson))

	// Verification Endpoints
	mux.HandleFunc("POST /api/v1/verify/file", cfg.AuthMiddleware.OptionalAuth(cfg.VerifyHandler.HandleVerifyFile))
	mux.HandleFunc("GET /api/v1/verify/code/{documentCode}", cfg.VerifyHandler.HandleVerifyByCode)

	// Swagger UI — only available in development mode
	if cfg.DevMode {
		// Redirect /swagger → /swagger/index.html for convenience
		mux.HandleFunc("GET /swagger", func(w http.ResponseWriter, r *http.Request) {
			http.Redirect(w, r, "/swagger/index.html", http.StatusMovedPermanently)
		})
		// Mount Swagger UI at /swagger/
		mux.Handle("/swagger/", httpSwagger.Handler(
			httpSwagger.URL("/swagger/doc.json"),
			httpSwagger.DeepLinking(true),
			httpSwagger.DocExpansion("list"),
			httpSwagger.DomID("swagger-ui"),
		))
	}

	return corsMiddleware(mux)
}

// handleHealth godoc
//
//	@Summary     Health check
//	@Description Returns service health status and timestamp
//	@Tags        Health
//	@Produce     json
//	@Success     200  {object}  map[string]any  "healthy"
//	@Router      /health [get]
func handleHealth(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	_ = json.NewEncoder(w).Encode(map[string]any{
		"status":    "healthy",
		"timestamp": time.Now().UTC().Format(time.RFC3339),
		"service":   "quicksign-crypto-engine",
	})
}

func corsMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization, X-API-Key, X-Document-Code")
		w.Header().Set("Access-Control-Expose-Headers", "Content-Disposition, X-Document-Code")

		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}

		next.ServeHTTP(w, r)
	})
}
