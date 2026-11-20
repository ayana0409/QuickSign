package http

import (
	"encoding/json"
	"errors"
	"io"
	"net/http"
	"strings"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/ports"
)

// verifyJwsRequest is the JSON body for POST /api/v1/verify/file when sending a JWS token.
type verifyJwsRequest struct {
	// JWS compact serialization token (header.payload.signature)
	JWS string `json:"jws" example:"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpPU0UiLCJqdGkiOiJET0MtMjAyNi02QUIxOUZEOSJ9.eyJpbnZvaWNlTnVtYmVyIjoiSU5WLTIwMjYtOTk5OSIsImFtb3VudCI6MTI1MDAwMDB9.signature"`
}

// VerifyHandler handles HTTP verification and public QR lookup endpoints.
type VerifyHandler struct {
	verifyUseCase ports.DocumentVerificationUseCase
}

// NewVerifyHandler constructs a new VerifyHandler.
func NewVerifyHandler(verifyUseCase ports.DocumentVerificationUseCase) *VerifyHandler {
	return &VerifyHandler{
		verifyUseCase: verifyUseCase,
	}
}

// HandleVerifyFile processes POST /api/v1/verify/file.
//
//	@Summary      Verify a signed document
//	@Description  Verifies the cryptographic integrity of an uploaded PDF (PAdES) or JWS token.
//	@Description  Accepts two content types:
//	@Description  - **multipart/form-data** with field `file` for PDF files.
//	@Description  - **application/json** with field `jws` containing a compact JWS string.
//	@Description
//	@Description  The service extracts the embedded signature, re-computes the hash, compares against
//	@Description  the stored original hash, and checks the certificate validity at signing time.
//	@Description  Any content modification (even 1 byte) returns `TAMPERED_CONTENT`.
//	@Tags         Verification
//	@Accept       multipart/form-data
//	@Accept       json
//	@Produce      json
//	@Param        file  formData  file                  false  "Signed PDF file (multipart/form-data)"
//	@Param        body  body      verifyJwsRequest      false  "JWS token wrapper (application/json)"
//	@Success      200   {object}  domain.VerifyResult   "Verification result"
//	@Failure      400   {object}  ErrorResponse         "Empty or unreadable payload"
//	@Failure      500   {object}  ErrorResponse         "Internal verification error"
//	@Security     BearerAuth
//	@Security     ApiKeyAuth
//	@Router       /api/v1/verify/file [post]
func (h *VerifyHandler) HandleVerifyFile(w http.ResponseWriter, r *http.Request) {
	apiKeyId, _ := r.Context().Value(ApiKeyIDContextKey).(string)
	var apiKeyIdPtr *string
	if apiKeyId != "" {
		apiKeyIdPtr = &apiKeyId
	}

	verifierIP := extractClientIP(r)
	userAgent := r.UserAgent()

	var fileBytes []byte
	var fileName string

	contentType := r.Header.Get("Content-Type")
	if strings.HasPrefix(contentType, "multipart/form-data") {
		if err := r.ParseMultipartForm(25 << 20); err != nil {
			WriteError(w, http.StatusBadRequest, "INVALID_ARGUMENT", "Failed to parse multipart verification payload.", nil)
			return
		}
		file, header, err := r.FormFile("file")
		if err != nil {
			WriteError(w, http.StatusBadRequest, "INVALID_ARGUMENT", "Multipart field 'file' is required.", nil)
			return
		}
		defer file.Close()
		bytes, readErr := io.ReadAll(file)
		if readErr != nil {
			WriteError(w, http.StatusBadRequest, "INVALID_ARGUMENT", "Unable to read uploaded file.", nil)
			return
		}
		fileBytes = bytes
		fileName = header.Filename
	} else {
		// Read raw body (e.g. text/plain or application/json containing JWS)
		bodyBytes, err := io.ReadAll(r.Body)
		if err != nil || len(bodyBytes) == 0 {
			WriteError(w, http.StatusBadRequest, "INVALID_ARGUMENT", "Verification payload cannot be empty.", nil)
			return
		}

		// Check if JSON containing "jws" field
		var jwsJson struct {
			JWS string `json:"jws"`
		}
		if json.Unmarshal(bodyBytes, &jwsJson) == nil && jwsJson.JWS != "" {
			fileBytes = []byte(jwsJson.JWS)
		} else {
			fileBytes = bodyBytes
		}
		fileName = "verification_payload"
	}

	input := domain.VerifyFileInput{
		FileBytes:  fileBytes,
		FileName:   fileName,
		VerifierIP: verifierIP,
		UserAgent:  userAgent,
		ApiKeyID:   apiKeyIdPtr,
	}

	result, err := h.verifyUseCase.VerifyFile(r.Context(), input)
	if err != nil {
		WriteError(w, http.StatusBadRequest, "INVALID_ARGUMENT", err.Error(), nil)
		return
	}

	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(http.StatusOK)
	_ = json.NewEncoder(w).Encode(result)
}

// HandleVerifyByCode processes public GET /api/v1/verify/code/{documentCode}.
//
//	@Summary      Public QR code lookup
//	@Description  Looks up a document by its tracking code (scanned from the QR printed on the document).
//	@Description  Returns `CODE_EXISTS_UNVERIFIED_CONTENT` per ADR 0001 §6 (Security Illusion Prevention):
//	@Description  the caller is informed the code exists but is **not** guaranteed the content is intact.
//	@Description  The user must upload the original file to `/api/v1/verify/file` for full content verification.
//	@Tags         Verification
//	@Produce      json
//	@Param        documentCode  path      string               true  "Document code embedded in the QR (e.g. DOC-2026-6AB19FD9)"
//	@Success      200           {object}  domain.VerifyResult  "Code exists — verification warning included"
//	@Failure      400           {object}  ErrorResponse        "Missing or empty document code"
//	@Failure      404           {object}  ErrorResponse        "Document code not found in the system"
//	@Router       /api/v1/verify/code/{documentCode} [get]
func (h *VerifyHandler) HandleVerifyByCode(w http.ResponseWriter, r *http.Request) {
	documentCode := r.PathValue("documentCode")
	if strings.TrimSpace(documentCode) == "" {
		WriteError(w, http.StatusBadRequest, "INVALID_ARGUMENT", "Document code path parameter is required.", nil)
		return
	}

	verifierIP := extractClientIP(r)
	userAgent := r.UserAgent()

	result, err := h.verifyUseCase.VerifyByCode(r.Context(), documentCode, verifierIP, userAgent)
	if err != nil {
		if errors.Is(err, domain.ErrDocumentNotFound) {
			WriteError(w, http.StatusNotFound, "DOCUMENT_NOT_FOUND", "Document not found for the supplied code.", nil)
			return
		}
		WriteError(w, http.StatusInternalServerError, "INTERNAL_ERROR", err.Error(), nil)
		return
	}

	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(http.StatusOK)
	_ = json.NewEncoder(w).Encode(result)
}

func extractClientIP(r *http.Request) string {
	if xff := r.Header.Get("X-Forwarded-For"); xff != "" {
		parts := strings.Split(xff, ",")
		if len(parts) > 0 && strings.TrimSpace(parts[0]) != "" {
			return strings.TrimSpace(parts[0])
		}
	}
	if xri := r.Header.Get("X-Real-IP"); xri != "" {
		return strings.TrimSpace(xri)
	}
	idx := strings.LastIndex(r.RemoteAddr, ":")
	if idx > 0 {
		return r.RemoteAddr[:idx]
	}
	return r.RemoteAddr
}
