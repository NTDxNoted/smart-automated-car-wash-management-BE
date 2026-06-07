# Testing Guide for Issue-01: Authentication

## Prerequisites

- .NET 8.0 SDK installed
- PostgreSQL running and accessible
- Database schema initialized with the SQL script

## Running the API

### Start the Application

```bash
cd src/AutoWashPro.API
dotnet run
```

The API will start on: **https://localhost:5000** (or HTTP on port 5001)

Swagger UI available at: **https://localhost:5000/swagger/index.html**

---

## Manual Testing with cURL or Postman

### Test 1: Register a New User (Success)

**Endpoint**: `POST /api/auth/register`

**Request**:

```json
{
  "fullName": "Nguyễn Văn Test",
  "phone": "0912345678",
  "password": "Password123",
  "confirmPassword": "Password123"
}
```

**Expected Response (201 Created)**:

```json
{
  "customerId": 11,
  "fullName": "Nguyễn Văn Test",
  "phone": "0912345678",
  "tier": "Member",
  "createdAt": "2025-06-08T10:30:00Z"
}
```

**cURL Command**:

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Nguyễn Văn Test",
    "phone": "0912345678",
    "password": "Password123",
    "confirmPassword": "Password123"
  }'
```

---

### Test 2: Register Duplicate Phone (Error)

**Endpoint**: `POST /api/auth/register`

**Request**:

```json
{
  "fullName": "Another User",
  "phone": "0912345678",
  "password": "Password123",
  "confirmPassword": "Password123"
}
```

**Expected Response (400 Bad Request)**:

```json
{
  "error": "PHONE_ALREADY_EXISTS",
  "message": "Số điện thoại đã được đăng ký"
}
```

---

### Test 3: Login with Valid Credentials (Success)

**Endpoint**: `POST /api/auth/login`

**Request**:

```json
{
  "phone": "0912345678",
  "password": "Password123"
}
```

**Expected Response (200 OK)**:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik5ndXnDqW4gVsOg biIsImlhdCI6MTUxNjIzOTAyMn0...",
  "customerId": 11,
  "fullName": "Nguyễn Văn Test",
  "phone": "0912345678",
  "tier": "Member",
  "isLocked": false,
  "suspendedUntil": null,
  "createdAt": "2025-06-08T10:30:00Z"
}
```

**cURL Command**:

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "phone": "0912345678",
    "password": "Password123"
  }'
```

---

### Test 4: Login with Invalid Password (Error)

**Endpoint**: `POST /api/auth/login`

**Request**:

```json
{
  "phone": "0912345678",
  "password": "WrongPassword"
}
```

**Expected Response (400 Bad Request)**:

```json
{
  "error": "INVALID_CREDENTIALS",
  "message": "Số điện thoại hoặc mật khẩu không đúng"
}
```

---

### Test 5: Login with Locked Account (Error)

**Prerequisite**: Create an account and set `IsLocked = true` in the database

**Endpoint**: `POST /api/auth/login`

**Request**:

```json
{
  "phone": "0901111001",
  "password": "password123"
}
```

**Expected Response (403 Forbidden)**:

```json
{
  "error": "ACCOUNT_LOCKED",
  "message": "Tài khoản đã bị khóa, vui lòng liên hệ Admin"
}
```

---

### Test 6: Test with Existing Demo Data

The database comes with 10 demo customers:

**Phone**: `0901111001` → `0901111010`

**Password** for all demo accounts: `password123` (need to hash with Bcrypt)

Try logging in with:

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "phone": "0901111001",
    "password": "password123"
  }'
```

---

## Running Unit Tests

### Run All Tests

```bash
cd src/AutoWash.Tests
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "AuthServiceTests"
```

### Run with Verbose Output

```bash
dotnet test --verbosity normal
```

### Expected Test Results

All 9 tests should pass:

- ✅ RegisterAsync_WithValidData_ShouldCreateCustomerAndLoyaltyAccount
- ✅ RegisterAsync_WithDuplicatePhone_ShouldThrowInvalidOperationException
- ✅ LoginAsync_WithValidCredentials_ShouldReturnAuthResponse
- ✅ LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException
- ✅ LoginAsync_WithLockedAccount_ShouldThrowInvalidOperationException
- ✅ LoginAsync_WithNonExistentPhone_ShouldThrowUnauthorizedAccessException
- ✅ GetCustomerIdFromToken_WithValidToken_ShouldReturnCustomerId
- ✅ GetCustomerIdFromToken_WithInvalidToken_ShouldReturnNull
- ✅ ValidateTokenAsync_WithValidToken_ShouldReturnTrue

---

## Using JWT Token in Subsequent Requests

Once you have a token from login, use it to access protected endpoints:

```bash
curl -X GET http://localhost:5000/api/profile \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

The token should be passed in the `Authorization` header as: `Bearer <token>`

---

## Common Issues & Solutions

### Issue 1: Connection String Error

**Error**: `Could not connect to the database`  
**Solution**: Verify PostgreSQL is running and the connection string in `appsettings.json` is correct

### Issue 2: Bcrypt Not Found

**Error**: `BCrypt.Net namespace not found`  
**Solution**: Run `dotnet restore` to install NuGet packages

### Issue 3: JWT Token Invalid

**Error**: `Invalid token` or `Token expired`  
**Solution**:

- Ensure the token is recent (tokens expire after 24 hours)
- Check the `Jwt:SecretKey` is the same in generation and validation
- Verify token is not corrupted in transmission

### Issue 4: Port Already in Use

**Error**: `Address already in use`  
**Solution**:

- Kill existing dotnet process: `Get-Process dotnet | Stop-Process -Force`
- Or specify a different port: `dotnet run --urls="http://localhost:5001"`

---

## Postman Collection

Save this as a `.json` file to import into Postman:

```json
{
  "info": {
    "name": "AutoWash Auth API",
    "version": "1.0"
  },
  "item": [
    {
      "name": "Register",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\"fullName\": \"Nguyễn Văn Test\", \"phone\": \"0912345678\", \"password\": \"Password123\", \"confirmPassword\": \"Password123\"}"
        },
        "url": {
          "raw": "http://localhost:5000/api/auth/register",
          "protocol": "http",
          "host": ["localhost"],
          "port": "5000",
          "path": ["api", "auth", "register"]
        }
      }
    },
    {
      "name": "Login",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\"phone\": \"0912345678\", \"password\": \"Password123\"}"
        },
        "url": {
          "raw": "http://localhost:5000/api/auth/login",
          "protocol": "http",
          "host": ["localhost"],
          "port": "5000",
          "path": ["api", "auth", "login"]
        }
      }
    }
  ]
}
```

---

## Validation Errors (400 Bad Request)

### Invalid Phone Format

**Request**: Phone doesn't match pattern `0XXXXXXXXX`  
**Response**:

```json
{
  "error": "VALIDATION_FAILED",
  "message": "Số điện thoại không hợp lệ"
}
```

### Password Too Short

**Request**: Password less than 6 characters  
**Response**:

```json
{
  "error": "VALIDATION_FAILED",
  "message": "Mật khẩu phải từ 6 ký tự trở lên"
}
```

### Passwords Don't Match

**Request**: `password` ≠ `confirmPassword`  
**Response**:

```json
{
  "error": "VALIDATION_FAILED",
  "message": "Mật khẩu không khớp"
}
```

---

**Happy Testing! 🚀**
