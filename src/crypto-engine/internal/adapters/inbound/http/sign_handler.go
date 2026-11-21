package http

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"strings"

	"quicksign/crypto-engine/internal/core/domain"
	"quicksign/crypto-engine/internal/core/ports"
)

// SignHandler handles HTTP endpoints for signing PDFs and structured JSON payloads.
type SignHandler struct {
	signingUseCase ports.DocumentSigningUseCase
}

// NewSignHandler constructs a new SignHandler.
func NewSignHandler(signingUseCase ports.DocumentSigningUseCase) *SignHandler {
	return &SignHandler{
		signingUseCase: signingUseCase,
	}
}

// HandleSignPdf processes POST /api/v1/sign/pdf.
//
//	@Summary      Sign a PDF document
//	@Description  Upload a PDF file for digital signing using PAdES-BES (ISO 32000-1). A visual QR code
//	@Description  appearance is embedded at the bottom of the first page, and the document code is stored
//	@Description  in PKCS#9 Signed Attributes for later verification.
//	@Tags         Signing
//	@Accept       multipart/form-data
//	@Produce      application/pdf
//	@Param        file           formData  file    true   "PDF file to sign (max 25 MB)"
//	@Param        certificateId  formData  string  false  "Certificate UUID to use. If omitted, the user's active certificate is selected automatically."
//	@Param        reason         formData  string  false  "Reason for signing (embedded in the signature dictionary)"
//	@Param        location       formData  string  false  "Location of signing (embedded in the signature dictionary)"
//	@Success      200  {file}    binary   "Signed PDF file"
//	@Header       200  {string}  X-Document-Code  "Generated document tracking code (e.g. DOC-2026-6AB19FD9)"
//	@Failure      400  {object}  ErrorResponse    "Invalid PDF or missing required fields"
//	@Failure      401  {object}  ErrorResponse    "Authentication required"
//	@Failure      404  {object}  ErrorResponse    "Active signing certificate not found"
//	@Failure      422  {object}  ErrorResponse    "Certificate revoked or expired at time of signing"
//	@Failure      500  {object}  ErrorResponse    "Internal signing error"
//	@Security     BearerAuth
//	@Security     ApiKeyAuth
//	@Router       /api/v1/sign/pdf [post]
func (h *SignHandler) HandleSignPdf(w http.ResponseWriter, r *http.Request) {
	userId, _ := r.Context().Value(UserIDContextKey).(string)
	if strings.TrimSpace(userId) == "" {
		WriteError(w, http.StatusUnauthorized, "UNAUTHORIZED", "Missing user context.", nil)
		return
	}

	// 25MB max memory for multipart file upload
	if err := r.ParseMultipartForm(25 << 20); err != nil {
		WriteError(w, http.StatusBadRequest, "INVALID_PDF_FORMAT", "Failed to parse multipart/form-data payload.", nil)
		return
	}

	file, header, err := r.FormFile("file")
	if err != nil {
		WriteError(w, http.StatusBadRequest, "INVALID_PDF_FORMAT", "Multipart field 'file' is required.", nil)
		return
	}
	defer file.Close()

	fileBytes, err := io.ReadAll(file)
	if err != nil {
		WriteError(w, http.StatusBadRequest, "INVALID_PDF_FORMAT", "Unable to read uploaded file stream.", nil)
		return
	}

	certId := strings.TrimSpace(r.FormValue("certificateId"))
	reason := strings.TrimSpace(r.FormValue("reason"))
	location := strings.TrimSpace(r.FormValue("location"))

	input := domain.SignPdfInput{
		FileContent:   fileBytes,
		FileName:      header.Filename,
		UserID:        userId,
		CertificateID: certId,
		Reason:        reason,
		Location:      location,
	}

	output, err := h.signingUseCase.SignPdf(r.Context(), input)
	if err != nil {
		h.mapSigningError(w, err)
		return
	}

	// Prepare attachment filename
	baseName := strings.TrimSuffix(header.Filename, ".pdf")
	if baseName == "" {
		baseName = "document"
	}
	signedFileName := fmt.Sprintf("%s_signed.pdf", baseName)

	w.Header().Set("Content-Type", "application/pdf")
	w.Header().Set("Content-Disposition", fmt.Sprintf("attachment; filename=\"%s\"", signedFileName))
	w.Header().Set("X-Document-Code", output.DocumentCode)
	w.WriteHeader(http.StatusOK)
	_, _ = w.Write(output.SignedBytes)
}

type signJsonRequest struct {
	Payload       map[string]any `json:"payload"`
	CertificateID string         `json:"certificateId,omitempty"`
}

type signJsonResponse struct {
	Jws          string `json:"jws"`
	DocumentCode string `json:"documentCode"`
	PayloadHash  string `json:"payloadHash"`
	CreatedAt    string `json:"createdAt"`
}

// HandleSignJson processes POST /api/v1/sign/json.
//
//	@Summary      Sign a JSON payload
//	@Description  Signs a structured JSON payload using JWS Compact Serialization (RFC 7515).
//	@Description  Supports RS256 (RSA-2048) and ES256 (ECDSA-P256). The document code is embedded
//	@Description  in the JWT Header `jti` claim for later verification.
//	@Tags         Signing
//	@Accept       json
//	@Produce      json
//	@Param        body  body      signJsonRequest   true  "JSON payload and optional certificate selection"
//	@Success      200   {object}  signJsonResponse  "JWS compact token and document metadata"
//	@Header       200   {string}  X-Document-Code   "Generated document tracking code"
//	@Failure      400   {object}  ErrorResponse     "Invalid request body or empty payload"
//	@Failure      401   {object}  ErrorResponse     "Authentication required"
//	@Failure      404   {object}  ErrorResponse     "Active signing certificate not found"
//	@Failure      422   {object}  ErrorResponse     "Certificate revoked or expired at time of signing"
//	@Failure      500   {object}  ErrorResponse     "Internal signing error"
//	@Security     BearerAuth
//	@Security     ApiKeyAuth
//	@Router       /api/v1/sign/json [post]
func (h *SignHandler) HandleSignJson(w http.ResponseWriter, r *http.Request) {
	userId, _ := r.Context().Value(UserIDContextKey).(string)
	if strings.TrimSpace(userId) == "" {
		WriteError(w, http.StatusUnauthorized, "UNAUTHORIZED", "Missing user context.", nil)
		return
	}

	var req signJsonRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		WriteError(w, http.StatusBadRequest, "INVALID_PAYLOAD", "Invalid JSON request body.", nil)
		return
	}

	if len(req.Payload) == 0 {
		WriteError(w, http.StatusBadRequest, "INVALID_PAYLOAD", "Payload object cannot be empty.", nil)
		return
	}

	payloadBytes, err := json.Marshal(req.Payload)
	if err != nil {
		WriteError(w, http.StatusBadRequest, "INVALID_PAYLOAD", "Failed to serialize payload.", nil)
		return
	}

	input := domain.SignJsonInput{
		Payload:       payloadBytes,
		UserID:        userId,
		CertificateID: req.CertificateID,
	}

	output, err := h.signingUseCase.SignJson(r.Context(), input)
	if err != nil {
		h.mapSigningError(w, err)
		return
	}

	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.Header().Set("X-Document-Code", output.DocumentCode)
	w.WriteHeader(http.StatusOK)

	resp := signJsonResponse{
		Jws:          output.JwsToken,
		DocumentCode: output.DocumentCode,
		PayloadHash:  output.PayloadHash,
		CreatedAt:    output.CreatedAt.Format("2006-01-02T15:04:05Z"),
	}
	_ = json.NewEncoder(w).Encode(resp)
}

func (h *SignHandler) mapSigningError(w http.ResponseWriter, err error) {
	switch {
	case errors.Is(err, domain.ErrInvalidPdfFormat), errors.Is(err, domain.ErrEmptyFileContent):
		WriteError(w, http.StatusBadRequest, "INVALID_PDF_FORMAT", err.Error(), nil)
	case errors.Is(err, domain.ErrCertNotFound):
		WriteError(w, http.StatusNotFound, "CERT_NOT_FOUND", "Active signing certificate not found for user.", nil)
	case errors.Is(err, domain.ErrCertRevoked):
		WriteError(w, http.StatusUnprocessableEntity, "CERT_REVOKED_AT_SIGNING", "The specified certificate was revoked before or at the time of signing.", nil)
	case errors.Is(err, domain.ErrCertExpired):
		WriteError(w, http.StatusUnprocessableEntity, "CERT_EXPIRED_AT_SIGNING", "The specified certificate was expired at the time of signing.", nil)
	default:
		WriteError(w, http.StatusInternalServerError, "SIGNING_FAILED", fmt.Sprintf("Cryptographic signing operation failed: %v", err), nil)
	}
}
