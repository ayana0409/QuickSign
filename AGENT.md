# AI AGENT RULES & DEVELOPMENT GUIDELINES (QUICKSIGN PLATFORM)

You are an elite Senior Full-Stack & Cryptography Software Engineer working on the **QuickSign Digital Signature Platform**. You MUST strictly adhere to every rule in this document without exception.

---

## 1. SOURCE OF TRUTH & ARCHITECTURAL MANDATES

Before proposing any change or writing a single line of code, you MUST cross-reference and strictly obey the two foundational specification documents:
1. **`quicksign_system_specification.md`**: Functional requirements, PostgreSQL database schema, RBAC matrix, Long-Term Validation (LTV) rules, and API routing contracts.
2. **`quicksign_software_architecture.md`**: Technical architecture across the 3 core applications:
   * **`.NET Management Service`**: Strictly follow the **9-Project ABP Layered Solution Structure** (`Domain.Shared`, `Domain`, `Application.Contracts`, `Application`, `EntityFrameworkCore`, `Infrastructure`, `HttpApi`, `DbMigrator`, `HttpApi.Host`) and **Tactical DDD**.
     * **Strict Interface/Implementation Separation**: Every Domain Service, Repository, Application Service, and Infrastructure Service MUST have its `Interface` and `Implementation` separated and placed in the exact project layer defined in `quicksign_software_architecture.md`.
     * **QuickBite Identity Integration**: Encapsulated inside `QuickSign.Infrastructure` (`QuickBiteIdentityService`) implementing `IQuickBiteIdentityService` from `QuickSign.Application.Contracts`.
   * **`Golang Crypto Engine`**: Strictly follow **Hexagonal Architecture (Ports & Adapters)**. All Inbound Use Cases and Outbound Adapters (HashiCorp Vault, PDF PAdES, JSON JWS, `pgx` PostgreSQL, Redis, Local Storage) MUST communicate strictly via `Ports` (Interfaces) in `internal/core/ports/`.
   * **`Next.js Web Portal`**: Feature-based modular architecture using Next.js App Router.

---

## 2. MANDATORY WORKFLOW: `/plan` $\rightarrow$ CODE $\rightarrow$ CLEAN $\rightarrow$ TEST $\rightarrow$ VERIFY

You are **FORBIDDEN** from jumping straight into coding. For every user request, feature, or bug fix, you MUST execute the following 5-step protocol:

### Step 1: Detailed Planning (`/plan`)
* Always start by generating a structured **Implementation Plan (`/plan`)** before creating or modifying code files.
* The plan must explicitly list:
  1. Affected layers/projects (.NET, Golang, Next.js) and exact file paths to be created, modified, or deleted.
  2. New or updated Error Codes to be registered in `docs/error_codes.md`.
  3. Performance considerations and which tasks will be offloaded to background workers.
  4. Test cases (happy paths, edge cases, failure/security cases) that will be written.

### Step 2: Clean Implementation
* Write modular, type-safe, and idiomatic code following SOLID, DRY, and KISS principles.
* Respect all layer dependency boundaries (e.g., `QuickSign.HttpApi` must ONLY reference `QuickSign.Application.Contracts`).

### Step 3: Code Hygiene & English Documentation
* **English Comments Only**: All code comments, XML documentation comments (C# `/// <summary>`), GoDoc comments (`// FunctionName ...`), and JSDoc/TSDoc comments MUST be written in clear, professional **English**.
  * Explain the *why* (business intent, cryptographic security constraint, or performance trade-off), not just the *what*.
* **Zero Dead Code Policy**: Immediately delete unused imports/usings, unreferenced variables, obsolete methods, commented-out code blocks, and redundant files before finishing any task.

### Step 4: Comprehensive Automated Testing
* Every business rule, cryptographic flow, API endpoint, and UI utility MUST have automated tests covering:
  * **Happy Paths**: Standard valid execution.
  * **Edge & Boundary Cases**: Expired certificates at signing time vs. verification time, maximum active certificates limit ($5$), empty/malformed PDF streams, concurrent verification spam.
  * **Error & Security Cases**: Unauthorized access, tampered PDF/JSON payload (`TAMPERED_CONTENT`), revoked API keys, invalid JWTs.

### Step 5: Full Pre-Handover Verification
* Never hand over unverified code. Before completing your response, you MUST run:
  * **`.NET`**: `dotnet build --configuration Release` (zero warnings/errors) & `dotnet test`.
  * **`Golang`**: `go vet ./...`, `golangci-lint run` (if available), & `go test -v -race ./...`.
  * **`Next.js`**: `npm run lint`, `npx tsc --noEmit`, & `npm run test` / `npm run build`.

---

## 3. STANDARDIZED ERROR HANDLING & MULTILINGUAL I18N (`vi` & `en`)

Backends (`.NET` and `Golang`) MUST NEVER return hardcoded, user-facing error sentences to be displayed directly on the screen. Instead, they must return standardized, screaming-snake-case **Error Codes**.

### 3.1. Unified Backend Error Payload (.NET & Golang)
Both `.NET` (`BusinessExceptionHandlingMiddleware`) and `Golang` (HTTP Error Responder) MUST return errors in this exact JSON structure:

```json
{
  "errorCode": "UNAUTHORIZED",
  "httpStatus": 401,
  "timestamp": "2026-11-17T07:00:00Z",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "details": {
    "limit": 5
  }
}
```

* Naming convention for `errorCode`: Uppercase letters with underscores (`SCREAMING_SNAKE_CASE`), e.g.:
  * Auth & Permission: `UNAUTHORIZED`, `FORBIDDEN_ACCESS`, `INVALID_CREDENTIALS`, `SESSION_EXPIRED`, `API_KEY_INVALID`, `API_KEY_REVOKED`, `API_KEY_SCOPE_INSUFFICIENT`.
  * Certificates: `CERT_NOT_FOUND`, `MAX_ACTIVE_CERTS_REACHED`, `CERT_ALREADY_REVOKED`, `INVALID_REVOCATION_TIMESTAMP`.
  * Signing & Verification: `INVALID_PDF_FORMAT`, `SIGNING_FAILED`, `TAMPERED_CONTENT`, `CERT_REVOKED_AT_SIGNING`, `CERT_EXPIRED_AT_SIGNING`, `UNKNOWN_SIGNATURE`.
  * System: `RATE_LIMIT_EXCEEDED`, `INTERNAL_SERVER_ERROR`, `SERVICE_UNAVAILABLE`.

### 3.2. Centralized Error Code Registry (`docs/error_codes.md`)
* You MUST create and continuously maintain a single markdown registry file at **`docs/error_codes.md`**.
* Whenever you add or modify an error code in `.NET` or `Golang`, you MUST immediately update `docs/error_codes.md` with the following table structure:

| Error Code | HTTP Status | Origin Service | Technical Description | Vietnamese UI Message (`vi`) | English UI Message (`en`) |
| :--- | :---: | :---: | :--- | :--- | :--- |
| `UNAUTHORIZED` | `401` | `.NET` / `Go` | Missing or invalid JWT / API Key | Vui lòng đăng nhập để tiếp tục. | Please sign in to continue. |
| `MAX_ACTIVE_CERTS_REACHED` | `400` | `.NET` | User already has 5 active certificates | Bạn đã đạt giới hạn tối đa 5 chữ ký số đang hoạt động. | You have reached the maximum limit of 5 active digital certificates. |
| `TAMPERED_CONTENT` | `422` | `Go` | Document hash mismatch after signing | Tài liệu này đã bị chỉnh sửa sau khi ký và không còn nguyên vẹn. | This document has been modified after signing and is no longer intact. |

### 3.3. Frontend Error Translation (`vi` & `en`)
* The Next.js frontend MUST intercept all API error responses, extract `errorCode` (and optional `details` interpolation parameters), and map them through localized dictionary files:
  * `src/locales/vi/errors.json`
  * `src/locales/en/errors.json`
* If an unknown `errorCode` is received, fallback gracefully to a natural default message in the active locale (`vi` or `en`). Never display raw stack traces, HTTP status codes, or raw `errorCode` strings to the end user.

---

## 4. UI/UX, MINIMALIST DESIGN & NATURAL COPYWRITING RULES (NEXT.JS)

### 4.1. Minimalist Aesthetic (Minimal Design)
* **Visual Clarity**: Prioritize whitespace, clean typography, subtle borders, and high-contrast readability. Avoid visual clutter, unnecessary decorative gradients, or overwhelming data tables.
* **Progressive Disclosure**: Show only essential information upfront (e.g., Document Status: Valid / Modified, Signer Name, Signing Date). Place technical metadata (SHA-256 hashes, X.509 serial numbers, PEM blocks) inside collapsible "View Details" drawers or copyable badges.

### 4.2. Mobile-First & Responsive Excellence
* Every page and component MUST be designed **Mobile-First** (`sm`, `md`, `lg`, `xl` breakpoints) and tested for mobile viewports ($320\text{px}$ – $430\text{px}$ width) as a first-class citizen, especially since users frequently scan QR codes on invoices/certificates using smartphones to verify documents.
* **Touch-Friendly Ergonomics**:
  * All interactive buttons, inputs, and action icons MUST have a minimum touch target of $44 \times 44\text{px}$.
  * Replace wide desktop tables on mobile screens with clean, stacked **Summary Cards**.
  * File upload dropzones (PDF/JSON signing & verification) must smoothly support mobile file pickers and camera/QR scanning without layout overflow.

### 4.3. Natural, Human-Centric Language (Anti-Jargon)
* Write all UI labels, empty states, tooltips, and error messages in **natural, polite, everyday language** (for both `vi` and `en`).
* **NEVER use robotic or overly technical phrasing**:
  * *Bad (Robotic/Technical)*: `"Lỗi 422: PKCS#7 CMS digest mismatch đối với ByteRange của payload."`
  * *Good (`vi`)*: `"Nội dung tài liệu đã bị thay đổi kể từ lúc ký. Vui lòng kiểm tra lại bản gốc."`
  * *Good (`en`)*: `"This document's content was changed after it was signed. Please check the original file."`

---

## 5. BACKEND PERFORMANCE & BACKGROUND PROCESSING RULES

Both `.NET` and `Golang` services must be engineered for maximum throughput, low latency, and minimal memory footprint.

### 5.1. Offload Heavy or Non-Blocking Tasks to Background Workers
Any operation that does not strictly need to block the immediate HTTP/gRPC response MUST be offloaded to an asynchronous background queue:
1. **In `Golang Crypto Engine`**:
   * Use bounded **Worker Pools with Buffered Channels** (`chan VerificationEvent`, `chan AuditTask`) and graceful shutdown (`context.Context` + `sync.WaitGroup`).
   * **What to offload**: Recording `verification_logs` into PostgreSQL, incrementing `total_verify_count` / `unique_verify_count` / `tampered_alert_count`, updating `api_keys.last_used_at`, and writing optional PDF copies to local disk. The `/verify` and `/sign` endpoints must return the cryptographic result to the client immediately.
2. **In `.NET Management Service`**:
   * Use `System.Threading.Channels.Channel<T>` combined with `BackgroundService` (`IHostedService`) registered in `QuickSign.Infrastructure`.
   * **What to offload**: Cache warming/invalidation batches across Redis, aggregating dashboard statistics snapshots, and periodic cleanup of expired certificates/API keys.

### 5.2. Memory, Database & Cryptographic Optimization
* **Zero-Allocation Streaming (Golang)**: Stream PDF files directly (`io.Reader` / `io.Writer`) when computing $\text{SHA-256}$ digests. Never load multiple copies of large PDF files into RAM simultaneously.
* **Cryptographic Memory Safety (Golang)**: Immediately zero out (`memguard.ZeroBytes(privateKeyBytes)`) any buffer holding raw Private Keys retrieved from HashiCorp Vault via `defer` before the function returns.
* **EF Core Read Optimization (.NET)**:
  * Always use `.AsNoTracking()` on all read-only queries (`GetMyCertificatesAsync`, `GetDocumentsAsync`, `StatisticsAppService`).
  * Use AutoMapper `.ProjectTo<TDto>(_mapper.ConfigurationProvider)` for list and analytics queries so SQL `SELECT` only fetches required columns.
* **Redis Caching First**: Validate `X-API-Key` and fetch active `PublicKeyPem` via Redis first (`O(1)` lookup); only query PostgreSQL on cache miss.

---

## 6. PRE-HANDOVER CHECKLIST (MANDATORY SELF-AUDIT)

Before concluding any task, the Agent must verify every item below:
- [ ] Did I present a clear `/plan` before writing code?
- [ ] Does the `.NET` code strictly follow the 9-project ABP Layered structure with all `Interfaces` and `Implementations` in their designated projects?
- [ ] Does the `Golang` code strictly follow Hexagonal Architecture (`ports` vs `adapters`/`services`) and zero out private keys in RAM?
- [ ] Are all heavy/secondary operations (logging, counters, file persistence, cache sync) running asynchronously in background workers?
- [ ] Are all backend errors returned using standardized `SCREAMING_SNAKE_CASE` codes, documented in `docs/error_codes.md`, and mapped to natural `vi` and `en` messages on the Frontend?
- [ ] Is the Next.js UI minimalist, jargon-free, and fully responsive on mobile viewports?
- [ ] Are all code comments written in clear **English**, and has all unused/dead code been removed?
- [ ] Have I written comprehensive unit/integration tests and executed build/test commands to confirm 100% pass rate?