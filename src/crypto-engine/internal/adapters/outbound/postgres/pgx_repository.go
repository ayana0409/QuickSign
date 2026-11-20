package postgres

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"

	"quicksign/crypto-engine/internal/core/domain"
)

// PgxDocumentRepository implements ports.DocumentRepositoryPort using pgx connection pool.
type PgxDocumentRepository struct {
	pool *pgxpool.Pool
}

// NewPgxDocumentRepository initializes a connection pool to PostgreSQL.
func NewPgxDocumentRepository(ctx context.Context, connString string) (*PgxDocumentRepository, error) {
	if strings.TrimSpace(connString) == "" {
		return nil, errors.New("database connection string cannot be empty")
	}

	config, err := pgxpool.ParseConfig(connString)
	if err != nil {
		return nil, fmt.Errorf("failed to parse database connection string: %w", err)
	}

	config.MaxConns = 25
	config.MinConns = 2
	config.MaxConnLifetime = 30 * time.Minute

	pool, err := pgxpool.NewWithConfig(ctx, config)
	if err != nil {
		return nil, fmt.Errorf("failed to create postgres connection pool: %w", err)
	}

	if err := pool.Ping(ctx); err != nil {
		return nil, fmt.Errorf("failed to ping postgres database: %w", err)
	}

	return &PgxDocumentRepository{pool: pool}, nil
}

// Close closes the connection pool.
func (r *PgxDocumentRepository) Close() {
	if r.pool != nil {
		r.pool.Close()
	}
}

// GetActiveCertificate retrieves an active certificate for a user, optionally matching a specific certificateId.
func (r *PgxDocumentRepository) GetActiveCertificate(ctx context.Context, userId string, certId *string) (*domain.CertificateEntity, error) {
	var query string
	var args []any

	if certId != nil && strings.TrimSpace(*certId) != "" {
		query = `
			SELECT id, user_id, subject_name, serial_number, algorithm, public_key_pem, 
			       vault_key_path, status, expires_at, revoked_at, invalidated_from, revoked_reason
			FROM certificates
			WHERE id = $1 AND user_id = $2
		`
		args = []any{*certId, userId}
	} else {
		query = `
			SELECT id, user_id, subject_name, serial_number, algorithm, public_key_pem, 
			       vault_key_path, status, expires_at, revoked_at, invalidated_from, revoked_reason
			FROM certificates
			WHERE user_id = $1 AND status = 'ACTIVE' AND expires_at > NOW()
			ORDER BY created_at DESC
			LIMIT 1
		`
		args = []any{userId}
	}

	var cert domain.CertificateEntity
	var rawAlgo string
	err := r.pool.QueryRow(ctx, query, args...).Scan(
		&cert.ID,
		&cert.UserID,
		&cert.SubjectName,
		&cert.SerialNumber,
		&rawAlgo,
		&cert.PublicKeyPEM,
		&cert.VaultKeyPath,
		&cert.Status,
		&cert.ExpiresAt,
		&cert.RevokedAt,
		&cert.InvalidatedFrom,
		&cert.RevocationReason,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, domain.ErrCertNotFound
		}
		return nil, fmt.Errorf("database query error fetching certificate: %w", err)
	}

	algo, err := domain.ParseAlgorithm(rawAlgo)
	if err != nil {
		return nil, fmt.Errorf("invalid algorithm '%s' stored in certificate record: %w", rawAlgo, err)
	}
	cert.Algorithm = algo

	return &cert, nil
}

// GetCertificateById retrieves any certificate by its ID regardless of user or status.
func (r *PgxDocumentRepository) GetCertificateById(ctx context.Context, certId string) (*domain.CertificateEntity, error) {
	query := `
		SELECT id, user_id, subject_name, serial_number, algorithm, public_key_pem, 
		       vault_key_path, status, expires_at, revoked_at, invalidated_from, revoked_reason
		FROM certificates
		WHERE id = $1
	`

	var cert domain.CertificateEntity
	var rawAlgo string
	err := r.pool.QueryRow(ctx, query, certId).Scan(
		&cert.ID,
		&cert.UserID,
		&cert.SubjectName,
		&cert.SerialNumber,
		&rawAlgo,
		&cert.PublicKeyPEM,
		&cert.VaultKeyPath,
		&cert.Status,
		&cert.ExpiresAt,
		&cert.RevokedAt,
		&cert.InvalidatedFrom,
		&cert.RevocationReason,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, domain.ErrCertNotFound
		}
		return nil, fmt.Errorf("database error fetching certificate by ID: %w", err)
	}

	algo, err := domain.ParseAlgorithm(rawAlgo)
	if err != nil {
		return nil, fmt.Errorf("invalid algorithm '%s' stored in certificate: %w", rawAlgo, err)
	}
	cert.Algorithm = algo

	return &cert, nil
}

// SaveSignedDocument records a newly created signed document in PostgreSQL.
func (r *PgxDocumentRepository) SaveSignedDocument(ctx context.Context, doc *domain.SignedDocumentEntity) error {
	query := `
		INSERT INTO signed_documents (
			id, document_code, user_id, certificate_id, api_key_id, 
			doc_type, document_name, original_hash, signed_hash, 
			signature_value, storage_path, total_verify_count, unique_verify_count, 
			tampered_alert_count, last_verified_at, created_at
		) VALUES (
			$1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16
		)
	`

	_, err := r.pool.Exec(ctx, query,
		doc.ID,
		doc.DocumentCode,
		doc.UserID,
		doc.CertificateID,
		doc.ApiKeyID,
		string(doc.DocType),
		doc.DocumentName,
		doc.OriginalHash,
		doc.SignedHash,
		doc.SignatureValue,
		doc.StoragePath,
		doc.TotalVerifyCount,
		doc.UniqueVerifyCount,
		doc.TamperedAlertCount,
		doc.LastVerifiedAt,
		doc.CreatedAt,
	)
	if err != nil {
		return fmt.Errorf("failed to insert signed document record: %w", err)
	}
	return nil
}

// GetSignedDocumentByCode queries a signed document record by its unique document_code.
func (r *PgxDocumentRepository) GetSignedDocumentByCode(ctx context.Context, code string) (*domain.SignedDocumentEntity, error) {
	query := `
		SELECT id, document_code, user_id, certificate_id, api_key_id, 
		       doc_type, document_name, original_hash, signed_hash, 
		       signature_value, storage_path, total_verify_count, unique_verify_count, 
		       tampered_alert_count, last_verified_at, created_at
		FROM signed_documents
		WHERE document_code = $1
	`

	var doc domain.SignedDocumentEntity
	var rawDocType string
	err := r.pool.QueryRow(ctx, query, code).Scan(
		&doc.ID,
		&doc.DocumentCode,
		&doc.UserID,
		&doc.CertificateID,
		&doc.ApiKeyID,
		&rawDocType,
		&doc.DocumentName,
		&doc.OriginalHash,
		&doc.SignedHash,
		&doc.SignatureValue,
		&doc.StoragePath,
		&doc.TotalVerifyCount,
		&doc.UniqueVerifyCount,
		&doc.TamperedAlertCount,
		&doc.LastVerifiedAt,
		&doc.CreatedAt,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, domain.ErrDocumentNotFound
		}
		return nil, fmt.Errorf("failed to query signed document by code: %w", err)
	}

	doc.DocType = domain.DocumentType(rawDocType)
	return &doc, nil
}

// SaveVerificationLog appends an audit record to verification_logs.
func (r *PgxDocumentRepository) SaveVerificationLog(ctx context.Context, log *domain.VerificationLogEntity) error {
	query := `
		INSERT INTO verification_logs (
			signed_document_id, submitted_hash, verification_method, api_key_id, 
			verifier_ip, user_agent, is_valid, validation_status, verified_at
		) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
		RETURNING id
	`

	err := r.pool.QueryRow(ctx, query,
		log.SignedDocumentID,
		log.SubmittedHash,
		log.VerificationMethod,
		log.ApiKeyID,
		log.VerifierIP,
		log.UserAgent,
		log.IsValid,
		string(log.ValidationStatus),
		log.VerifiedAt,
	).Scan(&log.ID)
	if err != nil {
		return fmt.Errorf("failed to insert verification log: %w", err)
	}
	return nil
}

// IncrementVerifyCount atomically updates verify counters on the signed_documents table (ADR 0001 §5).
func (r *PgxDocumentRepository) IncrementVerifyCount(ctx context.Context, docId string, isUnique bool, isTampered bool) error {
	query := `
		UPDATE signed_documents 
		SET total_verify_count = total_verify_count + 1,
		    unique_verify_count = unique_verify_count + CASE WHEN $2 THEN 1 ELSE 0 END,
		    tampered_alert_count = tampered_alert_count + CASE WHEN $3 THEN 1 ELSE 0 END,
		    last_verified_at = NOW()
		WHERE id = $1
	`

	_, err := r.pool.Exec(ctx, query, docId, isUnique, isTampered)
	if err != nil {
		return fmt.Errorf("failed to atomically increment verify count: %w", err)
	}
	return nil
}

// GetApiKeyBySecretHash queries an API key record by its SHA-256 hash.
func (r *PgxDocumentRepository) GetApiKeyBySecretHash(ctx context.Context, secretHash string) (*domain.ApiKeyCacheItem, error) {
	query := `
		SELECT id, user_id, scopes, is_active
		FROM api_keys
		WHERE key_hash = $1
	`

	var item domain.ApiKeyCacheItem
	err := r.pool.QueryRow(ctx, query, secretHash).Scan(
		&item.ID,
		&item.UserID,
		&item.Scopes,
		&item.IsActive,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, nil // Not found
		}
		return nil, fmt.Errorf("failed to query api key: %w", err)
	}

	return &item, nil
}
