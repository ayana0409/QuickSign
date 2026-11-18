using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuickSign.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scopes = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    public_key_pem = table.Column<string>(type: "text", nullable: false),
                    vault_key_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    invalidated_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "signed_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    certificate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    doc_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    document_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    original_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    signed_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    signature_value = table.Column<string>(type: "text", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_verify_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    unique_verify_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tampered_alert_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signed_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_signed_documents_certificates_certificate_id",
                        column: x => x.certificate_id,
                        principalTable: "certificates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "verification_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    signed_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    verification_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    api_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verifier_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false),
                    validation_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_verification_logs_signed_documents_signed_document_id",
                        column: x => x.signed_document_id,
                        principalTable: "signed_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_key_hash",
                table: "api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_user_id",
                table: "api_keys",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_serial_number",
                table: "certificates",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificates_user_id",
                table: "certificates",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_signed_documents_api_key_id",
                table: "signed_documents",
                column: "api_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_signed_documents_certificate_id",
                table: "signed_documents",
                column: "certificate_id");

            migrationBuilder.CreateIndex(
                name: "IX_signed_documents_document_code",
                table: "signed_documents",
                column: "document_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signed_documents_original_hash",
                table: "signed_documents",
                column: "original_hash");

            migrationBuilder.CreateIndex(
                name: "IX_signed_documents_signed_hash",
                table: "signed_documents",
                column: "signed_hash");

            migrationBuilder.CreateIndex(
                name: "IX_signed_documents_user_id",
                table: "signed_documents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_logs_signed_document_id",
                table: "verification_logs",
                column: "signed_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_logs_verified_at",
                table: "verification_logs",
                column: "verified_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "verification_logs");

            migrationBuilder.DropTable(
                name: "signed_documents");

            migrationBuilder.DropTable(
                name: "certificates");
        }
    }
}
