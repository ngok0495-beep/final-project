# Clothing Management - Microservices

## Kiến trúc

```
Client (Browser/Postman)
        │
        ├──► AuthService  (port 5001)  →  MongoDB: ClothingAuth
        │         │
        │    validate token (HTTP)
        │         ↓
        └──► UserService  (port 5002)  →  MongoDB: ClothingUsers
```

## Yêu cầu
- .NET 8 SDK
- MongoDB đang chạy ở localhost:27017

## Chạy dự án

### Terminal 1 - AuthService
```bash
cd AuthService
dotnet restore
dotnet run
# Swagger: http://localhost:5001/swagger
```

### Terminal 2 - UserService
```bash
cd UserService
dotnet restore
dotnet run
# Swagger: http://localhost:5002/swagger
```

## Luồng sử dụng

### 1. Đăng ký tài khoản
```
POST http://localhost:5001/api/auth/register
{
  "username": "nguyenvana",
  "email": "a@example.com",
  "password": "123456",
  "role": "User"
}
→ Nhận được token JWT
```

### 2. Đăng nhập
```
POST http://localhost:5001/api/auth/login
{
  "email": "a@example.com",
  "password": "123456"
}
→ Nhận được token JWT
```

### 3. Tạo profile (gọi UserService với token)
```
POST http://localhost:5002/api/users/me
Authorization: Bearer <token>
{
  "authUserId": "...",
  "fullName": "Nguyễn Văn A",
  "phone": "0901234567"
}
```

### 4. Cập nhật profile
```
PUT http://localhost:5002/api/users/me
Authorization: Bearer <token>
{
  "fullName": "Nguyễn Văn A Updated",
  "address": {
    "street": "123 Lê Lợi",
    "city": "TP.HCM"
  }
}
```

## API Endpoints

### AuthService (port 5001)
| Method | Route                      | Auth     | Mô tả                        |
|--------|----------------------------|----------|------------------------------|
| POST   | /api/auth/register         | ❌       | Đăng ký tài khoản            |
| POST   | /api/auth/login            | ❌       | Đăng nhập, nhận JWT          |
| POST   | /api/auth/validate         | ❌       | Validate token (internal)    |
| GET    | /api/auth/me               | ✅ JWT   | Thông tin user từ token      |
| PUT    | /api/auth/change-password  | ✅ JWT   | Đổi mật khẩu                 |
| DELETE | /api/auth/{userId}         | ✅ Admin | Vô hiệu hóa tài khoản       |

### UserService (port 5002)
| Method | Route                    | Auth     | Mô tả                         |
|--------|--------------------------|----------|-------------------------------|
| GET    | /api/users/health        | ❌       | Health check                  |
| GET    | /api/users/me            | ✅ JWT   | Xem profile của mình          |
| POST   | /api/users/me            | ✅ JWT   | Tạo profile lần đầu           |
| PUT    | /api/users/me            | ✅ JWT   | Cập nhật profile              |
| GET    | /api/users               | ✅ Admin | Danh sách tất cả user         |
| GET    | /api/users/{authUserId}  | ✅ JWT   | Xem profile theo authUserId   |
| DELETE | /api/users/{authUserId}  | ✅ Admin | Xóa profile                   |

## Roles
- `User`  - Nhân viên / Khách hàng thông thường
- `Staff` - Nhân viên quản lý
- `Admin` - Toàn quyền
