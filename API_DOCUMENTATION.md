# AutoWash Pro - Authentication API Documentation

## Base URL

```
http://localhost:5000/api/auth
https://localhost:5443/api/auth (HTTPS)
```

---

## Endpoints

### 1. Register New Member

Register a new customer account with phone and password.

**Endpoint**: `POST /api/auth/register`

**Authentication**: Not required

**Request Headers**:

```
Content-Type: application/json
```

**Request Body**:

```json
{
  "fullName": "string (2-100 chars, required)",
  "phone": "string (format: 0XXXXXXXXX, required, unique)",
  "password": "string (min 6 chars, required)",
  "confirmPassword": "string (must match password, required)"
}
```

**Request Example**:

```json
{
  "fullName": "Nguyễn Văn Anh",
  "phone": "0912345678",
  "password": "SecurePass123",
  "confirmPassword": "SecurePass123"
}
```

**Success Response** (HTTP 201 Created):

```json
{
  "customerId": 11,
  "fullName": "Nguyễn Văn Anh",
  "phone": "0912345678",
  "tier": "Member",
  "createdAt": "2025-06-08T10:30:45.123Z"
}
```

**Error Responses**:

| Status | Error Code            | Message                         |
| ------ | --------------------- | ------------------------------- |
| 400    | PHONE_ALREADY_EXISTS  | Số điện thoại đã được đăng ký   |
| 400    | VALIDATION_FAILED     | Invalid input format            |
| 500    | INTERNAL_SERVER_ERROR | Đã xảy ra lỗi, vui lòng thử lại |

**Error Example** (HTTP 400):

```json
{
  "error": "PHONE_ALREADY_EXISTS",
  "message": "Số điện thoại đã được đăng ký"
}
```

**Business Rules**:

- BR-03: Phone must be unique across all customers
- BR-05: Reject registration if phone already exists
- BR-06: Password must be hashed with Bcrypt before storage
- New customer automatically assigned `TierID=1` (Member)
- `LoyaltyAccount` created automatically with `TotalPoints=0`

**Notes**:

- Phone number format: Starts with 0, followed by 9 digits (10 digits total)
- No spaces or special characters in phone
- Password is case-sensitive
- Customer immediately eligible for login after registration

---

### 2. Login Member

Authenticate with phone and password to receive a JWT token.

**Endpoint**: `POST /api/auth/login`

**Authentication**: Not required

**Request Headers**:

```
Content-Type: application/json
```

**Request Body**:

```json
{
  "phone": "string (format: 0XXXXXXXXX, required)",
  "password": "string (required)"
}
```

**Request Example**:

```json
{
  "phone": "0912345678",
  "password": "SecurePass123"
}
```

**Success Response** (HTTP 200 OK):

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMiIsIm5hbWUiOiJOZ3V54bq_biDDiMOpIiwiZXhwIjoxNjIzMDMzMDAwfQ.abcd1234...",
  "customerId": 12,
  "fullName": "Nguyễn Văn Anh",
  "phone": "0912345678",
  "tier": "Member",
  "isLocked": false,
  "suspendedUntil": null,
  "createdAt": "2025-06-08T10:30:45.123Z"
}
```

**Error Responses**:

| Status | Error Code            | Message                                      |
| ------ | --------------------- | -------------------------------------------- |
| 400    | INVALID_CREDENTIALS   | Số điện thoại hoặc mật khẩu không đúng       |
| 400    | VALIDATION_FAILED     | Invalid input format                         |
| 403    | ACCOUNT_LOCKED        | Tài khoản đã bị khóa, vui lòng liên hệ Admin |
| 500    | INTERNAL_SERVER_ERROR | Đã xảy ra lỗi, vui lòng thử lại              |

**Error Example** (HTTP 400):

```json
{
  "error": "INVALID_CREDENTIALS",
  "message": "Số điện thoại hoặc mật khẩu không đúng"
}
```

**Error Example** (HTTP 403):

```json
{
  "error": "ACCOUNT_LOCKED",
  "message": "Tài khoản đã bị khóa, vui lòng liên hệ Admin"
}
```

**Business Rules**:

- BR-13: Block login if `IsLocked = true` (returns 403)
- BR-66: Check `SuspendedUntil` at booking layer, not login
- Password verified against Bcrypt hash in database
- Non-existent customer treated same as invalid password (security)

**JWT Token Details**:

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "12",
    "name": "Nguyễn Văn Anh",
    "phone": "0912345678",
    "tier": "Member",
    "role": "Member",
    "exp": 1623033000,
    "iat": 1622946600
  }
}
```

**Token Expiration**: 24 hours (1440 minutes)

**Token Usage**:
Use token in subsequent requests:

```bash
Authorization: Bearer <token>
```

---

## Data Models

### RegisterRequest

```csharp
public class RegisterRequest
{
  [Required]
  [StringLength(100, MinimumLength = 2)]
  public string FullName { get; set; }

  [Required]
  [RegularExpression(@"^0[0-9]{9}$")]
  public string Phone { get; set; }

  [Required]
  [StringLength(255, MinimumLength = 6)]
  public string Password { get; set; }

  [Required]
  [Compare("Password")]
  public string ConfirmPassword { get; set; }
}
```

### LoginRequest

```csharp
public class LoginRequest
{
  [Required]
  [RegularExpression(@"^0[0-9]{9}$")]
  public string Phone { get; set; }

  [Required]
  public string Password { get; set; }
}
```

### AuthResponse

```csharp
public class AuthResponse
{
  public int CustomerId { get; set; }
  public string FullName { get; set; }
  public string Phone { get; set; }
  public string Tier { get; set; }
  public string Token { get; set; }
  public bool IsLocked { get; set; }
  public DateTime? SuspendedUntil { get; set; }
  public DateTime CreatedAt { get; set; }
}
```

### RegisterResponse

```csharp
public class RegisterResponse
{
  public int CustomerId { get; set; }
  public string FullName { get; set; }
  public string Phone { get; set; }
  public string Tier { get; set; }
  public DateTime CreatedAt { get; set; }
}
```

### ErrorResponse

```csharp
public class ErrorResponse
{
  public string Error { get; set; }
  public string Message { get; set; }
}
```

---

## Authentication & Authorization

### JWT Bearer Token

Use the token obtained from login for authenticated requests:

```bash
GET /api/profile HTTP/1.1
Host: localhost:5000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Claims in Token

Extract claims from the JWT token:

```json
{
  "sub": "CustomerId",
  "name": "FullName",
  "phone": "PhoneNumber",
  "tier": "TierName",
  "role": "Member"
}
```

### Securing Endpoints

Mark endpoints as `[Authorize]` to require JWT:

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

## Validation Rules

### Phone Validation

- **Format**: `0XXXXXXXXX` (10 digits, starts with 0)
- **Example**: `0912345678`, `0901111001`
- **Invalid**: `912345678`, `+84912345678`, `0912-345-678`

### Password Validation

- **Minimum Length**: 6 characters
- **Maximum Length**: 255 characters
- **Case Sensitive**: Yes
- **Special Characters**: Allowed
- **Requirements**: None (no complexity rules enforced)

### Full Name Validation

- **Minimum Length**: 2 characters
- **Maximum Length**: 100 characters
- **Characters**: Letters, spaces, Vietnamese diacritics allowed
- **Example**: `Nguyễn Văn Anh`, `John Smith`

---

## HTTP Status Codes

| Code | Meaning               | Usage                                                        |
| ---- | --------------------- | ------------------------------------------------------------ |
| 200  | OK                    | Login successful, token returned                             |
| 201  | Created               | Registration successful                                      |
| 400  | Bad Request           | Validation failed, invalid input, or business rule violation |
| 403  | Forbidden             | Account locked or suspended                                  |
| 500  | Internal Server Error | Unexpected server error                                      |

---

## Rate Limiting

No rate limiting currently implemented. Production should add:

- Max 5 login attempts per IP/minute
- Max 3 registration attempts per IP/hour
- Account lockout after 5 failed login attempts

---

## Security Best Practices

### For Clients

1. ✅ Always use HTTPS in production
2. ✅ Store JWT token securely (httpOnly cookies, secure storage)
3. ✅ Never share tokens
4. ✅ Include token in `Authorization` header as `Bearer <token>`
5. ✅ Handle token expiration gracefully (redirect to login)

### For Developers

1. ✅ Never log sensitive data (passwords, tokens)
2. ✅ Validate all input server-side
3. ✅ Use HTTPS for all communications
4. ✅ Rotate JWT secret key periodically
5. ✅ Monitor failed login attempts

---

## Example Workflow

### 1. Register

```bash
POST /api/auth/register
{
  "fullName": "Nguyễn Văn A",
  "phone": "0912345678",
  "password": "Password123",
  "confirmPassword": "Password123"
}
→ 201 Created
{
  "customerId": 1,
  "fullName": "Nguyễn Văn A",
  "phone": "0912345678",
  "tier": "Member",
  "createdAt": "2025-06-08T10:30:45Z"
}
```

### 2. Login

```bash
POST /api/auth/login
{
  "phone": "0912345678",
  "password": "Password123"
}
→ 200 OK
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "customerId": 1,
  "fullName": "Nguyễn Văn A",
  "phone": "0912345678",
  "tier": "Member",
  "isLocked": false,
  "suspendedUntil": null,
  "createdAt": "2025-06-08T10:30:45Z"
}
```

### 3. Use Token (in another endpoint)

```bash
GET /api/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
→ 200 OK
{
  "customerId": 1,
  "fullName": "Nguyễn Văn A",
  "phone": "0912345678",
  "tier": "Member",
  "totalPoints": 0,
  "totalSpending": 0,
  ...
}
```

---

## Changelog

### Version 1.0.0 (2025-06-08)

- ✅ Initial implementation
- ✅ Register endpoint
- ✅ Login endpoint
- ✅ JWT token generation
- ✅ Bcrypt password hashing
- ✅ Account locking support

---

## Support

For issues or questions, contact: development@autowash.local
