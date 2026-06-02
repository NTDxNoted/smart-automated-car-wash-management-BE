# Skill: qa-review-skill

## Purpose
Systematically perform a high-fidelity Quality Assurance (QA) review of codebase changes (Frontend and Backend) to ensure perfect alignment with target functional requirements (FR), strict error handling, compile-time validations, and zero warnings.

## Inputs
- Modified/added files path lists
- Specific FR Specification file (`doc/FR-xxx-spec.md`)
- PR template checklist parameters

## Steps

### 1. Verification of Compilation & Warnings
- Ensure both Frontend and Backend compile with **zero warnings** and **zero errors**.
- **Backend check**: Run `mvn clean compile` to check for Lombok `@Builder` annotations, unused imports, or deprecation warnings.
- **Frontend check**: Run `npm run build` to verify there are no TypeScript syntax errors or compiler flags issues (`tsconfig` settings check).

### 2. Validation & Business Rule Audit
- Compare code implementation directly against **Business Rules & Constraints** of the target FR.
- Verify client-side validations (input field formats, phone/email regex, required values checks) in forms before submission.
- Verify server-side validations (JPA/Hibernate annotations like `@NotBlank`, `@Size`, `@Email` and validation logic).
- Ensure primary keys and unique identifiers are enforced correctly (e.g., unique business codes check).

### 3. Null-Safety & Security Review
- Verify that path variables, query parameters, and database entity fields have appropriate null-safety checks.
- **Java checks**: Ensure parameter-level `@NonNull` annotations are defined for JpaRepository calls to prevent raw unchecked type errors.
- **TypeScript checks**: Ensure strict types (no `any` types where avoidable) are defined.
- **RBAC Audit**: Verify roles matches the permission levels in the spec. Ensure unauthorized endpoints respond with `403 Forbidden`.

### 4. Exception Handling Review
- Verify that REST controllers return standard JSON error envelopes rather than raw Java stack traces.
- Verify HTTP status codes match the REST contract:
  - `400 Bad Request` for validation failures.
  - `403 Forbidden` for role mismatch.
  - `404 Not Found` for missing resources.
  - `409 Conflict` for duplicate entries.

### 5. Generate QA Review Report
Compile all findings into a structured QA Review Report containing:
- **Build Status**: Frontend and Backend compile outcomes.
- **Validation Checklist**: Status of business rule compliance.
- **Null Safety & RBAC Audit**: Verification of annotations and security checks.
- **Exceptions Mapping**: REST error codes validations.
- **Verdict**: `APPROVED` or `REQUEST_CHANGES` (with explicit actions).

## Output
- A committed `qa_review_report.md` summarizing the QA audit.
- Clean, error-free branch ready for Pull Request creation.
