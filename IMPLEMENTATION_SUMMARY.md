# ISSUE-01: Member Registration & Login Implementation

## Overview

This document summarizes the implementation of Issue-01: Authentication (Member Registration & Login) for the AutoWash Pro system.

## Implementation Status

✅ **COMPLETED**

---

## Components Implemented

### 1. **Domain Entities** (Already Exists)

- **Customer.cs**: Updated to include `IsLocked` and `SuspendedUntil` fields
- **LoyaltyAccount.cs**: Links Customer loyalty points to their account

### 2. **Application Layer**

#### DTOs Created:

- **RegisterRequest.cs**:
  - `FullName` - Customer name (2-100 chars)
  - `Phone` - Unique phone number (format: 0XXXXXXXXX)
  - `Password` - Password (min 6 chars)
  - `ConfirmPassword` - Password confirmation

- **LoginRequest.cs**:
  - `Phone` - Phone number for login
  - `Password` - Account password

- **AuthResponse.cs**:
  - Authentication response with JWT token
  - Customer details and lock status
- **RegisterResponse.cs**:
  - Registration response with customer details
  - No JWT token (redirect to login)

- **ErrorResponse.cs**:
  - Standardized error responses
  - Error code and message

#### Interfaces:

- **IAuthService.cs**:
  - `RegisterAsync()` - Register new customer
  - `LoginAsync()` - Authenticate and generate JWT
  - `ValidateTokenAsync()` - Verify JWT token
  - `GetCustomerIdFromToken()` - Extract customer ID from token

#### Services:

- **AuthService.cs**:
  - Full implementation of IAuthService
  - Bcrypt password hashing (secure password storage)
  - JWT token generation and validation
  - Business rule enforcement:
    - **BR-03**: Phone is unique username
    - **BR-05**: Reject duplicate phone
    - **BR-06**: Bcrypt password hashing
    - **BR-13**: Block login if `IsLocked = true`
    - **BR-66**: Check `SuspendedUntil` at booking layer (not login)

### 3. **API Layer**

#### AuthController.cs:

**Endpoints:**

1. **POST /api/auth/register**
   - Request: `RegisterRequest`
   - Response: `201 Created` with `RegisterResponse`
   - Error Cases:
     - `400 Bad Request`: Phone already exists or validation failed
     - `500 Internal Server Error`: Server error

2. **POST /api/auth/login**
   - Request: `LoginRequest`
   - Response: `200 OK` with `AuthResponse` (includes JWT token)
   - Error Cases:
     - `400 Bad Request`: Invalid credentials or validation failed
     - `403 Forbidden`: Account locked
     - `500 Internal Server Error`: Server error

#### Middleware:

- **JwtMiddleware.cs**:
  - Validates JWT tokens from Authorization header
  - Attaches user claims to HTTP context
  - Integrated into request pipeline

### 4. **Configuration**

#### appsettings.json:

```json
{
  "Jwt": {
    "SecretKey": "AutoWashPro_SecretKey_2025_MustBe32CharsLong!!",
    "Issuer": "AutoWashAPI",
    "Audience": "AutoWashClient",
    "ExpiryMinutes": 1440
  }
}
```

#### Program.cs:

- Registered JWT authentication
- Registered IAuthService → AuthService
- Added JwtMiddleware to pipeline
- Configured JWT bearer token validation

### 5. **Unit Tests**

#### AuthServiceTests.cs:

Comprehensive test coverage with 9 test cases:

1. **RegisterAsync_WithValidData_ShouldCreateCustomerAndLoyaltyAccount**
   - Verifies successful registration
   - Confirms Customer and LoyaltyAccount creation

2. **RegisterAsync_WithDuplicatePhone_ShouldThrowInvalidOperationException**
   - Tests duplicate phone rejection
   - Validates BR-05

3. **LoginAsync_WithValidCredentials_ShouldReturnAuthResponse**
   - Tests successful login
   - Verifies JWT token generation

4. **LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException**
   - Tests incorrect password handling

5. **LoginAsync_WithLockedAccount_ShouldThrowInvalidOperationException**
   - Tests locked account blocking
   - Validates BR-13

6. **LoginAsync_WithNonExistentPhone_ShouldThrowUnauthorizedAccessException**
   - Tests non-existent customer handling

7. **GetCustomerIdFromToken_WithValidToken_ShouldReturnCustomerId**
   - Tests token claim extraction

8. **GetCustomerIdFromToken_WithInvalidToken_ShouldReturnNull**
   - Tests invalid token handling

9. **ValidateTokenAsync_WithValidToken_ShouldReturnTrue**
   - Tests token validation
   - Also tests invalid token (returns false)

---

## API Response Examples

### Register Success (201 Created)

```json
{
  "customerId": 11,
  "fullName": "Nguyễn Văn X",
  "phone": "0901111011",
  "tier": "Member",
  "createdAt": "2025-06-01T08:00:00Z"
}
```

### Login Success (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "customerId": 11,
  "fullName": "Nguyễn Văn X",
  "phone": "0901111011",
  "tier": "Member",
  "isLocked": false,
  "suspendedUntil": null,
  "createdAt": "2025-06-01T08:00:00Z"
}
```

### Phone Already Exists (400 Bad Request)

```json
{
  "error": "PHONE_ALREADY_EXISTS",
  "message": "Số điện thoại đã được đăng ký"
}
```

### Account Locked (403 Forbidden)

```json
{
  "error": "ACCOUNT_LOCKED",
  "message": "Tài khoản đã bị khóa, vui lòng liên hệ Admin"
}
```

### Invalid Credentials (400 Bad Request)

```json
{
  "error": "INVALID_CREDENTIALS",
  "message": "Số điện thoại hoặc mật khẩu không đúng"
}
```

---

## File Structure

```
Issue1---Login-and-Register/src/
├── AutoWash.Application/
│   ├── DTOs/
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── AuthResponse.cs
│   ├── Interfaces/
│   │   └── IAuthService.cs
│   └── Services/
│       └── AuthService.cs
├── AutoWash.Domain/
│   └── Entities/
│       ├── Customer.cs (existing)
│       └── LoyaltyAccount.cs (existing)
├── AutoWash.Infrastructure/
│   └── Data/
│       └── ApplicationDbContext.cs (existing)
├── AutoWashPro.API/
│   ├── Controllers/
│   │   └── AuthController.cs
│   ├── Middleware/
│   │   └── JwtMiddleware.cs (updated)
│   ├── Program.cs (updated)
│   └── appsettings.json (updated)
└── AutoWash.Tests/
    └── AuthServiceTests.cs
```

---

## Security Features Implemented

1. **Password Hashing**: Bcrypt.Net-Core for secure password storage
2. **JWT Tokens**: Secure token-based authentication
3. **Token Validation**: Signature and expiry verification
4. **Account Locking**: `IsLocked` field prevents unauthorized access
5. **Input Validation**: Phone format and password requirements
6. **Unique Constraints**: Database-level phone uniqueness

---

## Business Rules Validated

- **BR-03**: ✅ Phone is unique username
- **BR-05**: ✅ Reject if phone already exists
- **BR-06**: ✅ Bcrypt password hashing
- **BR-13**: ✅ Block login if `IsLocked = true`
- **BR-66**: ⚠️ `SuspendedUntil` check done at booking layer (not here)

---

## Testing & Validation

### Build Status

✅ Project builds successfully with no errors

### Test Framework

- xUnit for unit testing
- Moq for mocking dependencies
- In-memory database for isolated tests

### How to Run Tests

```bash
cd src/AutoWash.Tests
dotnet test
```

---

## Integration Notes

### Register New Customer Flow:

1. Client sends POST /api/auth/register with full details
2. AuthService validates phone uniqueness
3. Password is hashed with Bcrypt
4. Customer record created with TierID=1 (Member)
5. LoyaltyAccount created automatically
6. Response returned with customer details

### Login Flow:

1. Client sends POST /api/auth/login with phone + password
2. AuthService finds customer by phone
3. Checks if account is locked (BR-13)
4. Verifies password against Bcrypt hash
5. Generates JWT token with claims:
   - CustomerId
   - FullName
   - Phone
   - Tier
   - Role (Member)
6. Returns token + customer details

### Protected Endpoints:

Use `[Authorize]` attribute on endpoints that require authentication:

```csharp
[Authorize]
[HttpGet("profile")]
public async Task<IActionResult> GetProfile()
{
    var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ...
}
```

---

## Future Enhancements

1. **Refresh Token Implementation**: Long-lived refresh tokens
2. **Two-Factor Authentication**: SMS/Email verification
3. **Password Reset**: Forgot password flow
4. **Social Login**: Google, Facebook integration
5. **Rate Limiting**: Prevent brute force attacks
6. **Audit Logging**: Track login attempts
7. **Admin Account Lock/Unlock**: Admin panel for account management

---

## Dependencies Added

### Application Layer:

- BCrypt.Net-Core (1.6.0) - Password hashing
- System.IdentityModel.Tokens.Jwt (8.0.2) - JWT handling
- Microsoft.IdentityModel.Tokens (8.0.2) - Token validation
- Microsoft.Extensions.Configuration.Abstractions (8.0.0) - Config access

### Test Project:

- xunit (2.6.6) - Unit testing framework
- Moq (4.20.70) - Mocking library
- Microsoft.EntityFrameworkCore.InMemory (8.0.10) - In-memory DB

---

## Notes

- Password validation requires minimum 6 characters in practice
- Phone format validated as: 0XXXXXXXXX (10 digits starting with 0)
- JWT tokens expire after 24 hours (1440 minutes)
- All timestamps use UTC for consistency
- Validation errors are returned with HTTP 400 status
- Server errors are returned with HTTP 500 status with generic message

---

**Implementation Date**: June 8, 2025  
**Status**: ✅ Ready for Testing & Deployment
