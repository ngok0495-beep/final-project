using AuthService.Configuration;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AuthService.Services;

public interface IAuthService
{
    Task EnsureDefaultAdminAsync();
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RegisterByAdminAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<TokenValidationResponse> ValidateTokenAsync(string token);
    Task<bool> DeactivateUserAsync(string userId);
    Task<List<AdminAccountResponse>> GetAdminAccountsAsync();
    Task<AuthResponse> UpdateUserByAdminAsync(string userId, UpdateAuthUserRequest request);
    Task<AuthResponse> SetUserLockedAsync(string userId, bool locked);
}

public class AuthServiceImpl : IAuthService
{
    public const string DefaultAdminEmail = "admin@clothing.com";
    public const string DefaultAdminPassword = "Admin@123";

    private readonly IMongoCollection<AuthUser> _users;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthServiceImpl> _logger;

    public AuthServiceImpl(
        IOptions<MongoDbSettings> mongoSettings,
        IJwtService jwtService,
        ILogger<AuthServiceImpl> logger)
    {
        var client = new MongoClient(mongoSettings.Value.ConnectionString);
        var db = client.GetDatabase(mongoSettings.Value.DatabaseName);
        _users = db.GetCollection<AuthUser>("auth_users");

        // Index email duy nhất
        var indexKeys = Builders<AuthUser>.IndexKeys.Ascending(u => u.Email);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _users.Indexes.CreateOne(new CreateIndexModel<AuthUser>(indexKeys, indexOptions));

        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task EnsureDefaultAdminAsync()
    {
        var existing = await _users
            .Find(u => u.Email == DefaultAdminEmail)
            .FirstOrDefaultAsync();

        if (existing is not null)
            return;

        var admin = new AuthUser
        {
            Username = "admin",
            Email = DefaultAdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword),
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _users.InsertOneAsync(admin);
        _logger.LogInformation("Default admin account created: {Email}", DefaultAdminEmail);
    }

    public Task<AuthResponse> RegisterAsync(RegisterRequest request) =>
        CreateUserAsync(request with { Role = "User" }, allowAdminRole: false);

    public Task<AuthResponse> RegisterByAdminAsync(RegisterRequest request)
    {
        var role = request.Role is "Staff" or "User" ? request.Role : "User";
        return CreateUserAsync(request with { Role = role }, allowAdminRole: false);
    }

    private async Task<AuthResponse> CreateUserAsync(RegisterRequest request, bool allowAdminRole)
    {
        if (!allowAdminRole && request.Role == "Admin")
            return new AuthResponse(false, "Không thể tạo tài khoản Admin qua đăng ký.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (!email.Contains('@') || !email.Contains('.'))
            return new AuthResponse(false, "Email không hợp lệ.");

        var existing = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (existing != null)
            return new AuthResponse(false, "Email đã được sử dụng.");

        var username = request.Username.Trim();
        var existingUsername = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        if (existingUsername != null)
            return new AuthResponse(false, "Username đã được sử dụng.");

        var user = new AuthUser
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _users.InsertOneAsync(user);
        _logger.LogInformation("User registered: {Email} ({Role})", email, user.Role);

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(true, "Đăng ký thành công.", token,
            new UserInfo(user.Id!, user.Username, user.Email, user.Role));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users
            .Find(u => u.Email == email && u.IsActive)
            .FirstOrDefaultAsync();

        if (user == null)
            return new AuthResponse(false, "Email hoặc mật khẩu không đúng.");

        if (user.IsLocked)
            return new AuthResponse(false, "Tài khoản đã bị khóa. Liên hệ quản trị viên.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new AuthResponse(false, "Email hoặc mật khẩu không đúng.");

        // Cập nhật lastLogin
        var update = Builders<AuthUser>.Update
            .Set(u => u.LastLoginAt, DateTime.UtcNow);
        await _users.UpdateOneAsync(u => u.Id == user.Id, update);

        _logger.LogInformation("User logged in: {Email}", request.Email);
        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(true, "Đăng nhập thành công.", token,
            new UserInfo(user.Id!, user.Username, user.Email, user.Role));
    }

    public async Task<AuthResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
            return new AuthResponse(false, "Không tìm thấy user.");

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return new AuthResponse(false, "Mật khẩu cũ không đúng.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        var update = Builders<AuthUser>.Update.Set(u => u.PasswordHash, newHash);
        await _users.UpdateOneAsync(u => u.Id == userId, update);

        return new AuthResponse(true, "Đổi mật khẩu thành công.");
    }

    public async Task<TokenValidationResponse> ValidateTokenAsync(string token)
    {
        var principal = _jwtService.ValidateToken(token);
        if (principal == null)
            return new TokenValidationResponse(false, null, null, null, null);

        var userId = GetClaim(principal,
            System.Security.Claims.ClaimTypes.NameIdentifier,
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            "sub");
        var email = GetClaim(principal,
            System.Security.Claims.ClaimTypes.Email,
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email,
            "email");
        var username = GetClaim(principal,
            System.Security.Claims.ClaimTypes.Name,
            "unique_name",
            "name");
        var role = GetClaim(principal,
            System.Security.Claims.ClaimTypes.Role,
            "role");

        if (string.IsNullOrEmpty(userId))
            return new TokenValidationResponse(false, null, null, null, null);

        var user = await _users.Find(u => u.Id == userId && u.IsActive).FirstOrDefaultAsync();
        if (user is null || user.IsLocked)
            return new TokenValidationResponse(false, null, null, null, null);

        return new TokenValidationResponse(true, userId, username ?? user.Username, email ?? user.Email, role ?? user.Role);
    }

    private static string? GetClaim(System.Security.Claims.ClaimsPrincipal principal, params string[] types)
    {
        foreach (var type in types)
        {
            var value = principal.FindFirst(type)?.Value;
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }

    public async Task<List<AdminAccountResponse>> GetAdminAccountsAsync()
    {
        var users = await _users.Find(_ => true)
            .SortByDescending(u => u.CreatedAt)
            .ToListAsync();

        return users.Select(u => new AdminAccountResponse(
            u.Id!, u.Username, u.Email, u.Role, u.IsLocked)).ToList();
    }

    public async Task<AuthResponse> UpdateUserByAdminAsync(string userId, UpdateAuthUserRequest request)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user is null)
            return new AuthResponse(false, "Không tìm thấy tài khoản.");

        if (user.Role == "Admin")
            return new AuthResponse(false, "Không thể sửa tài khoản Admin.");

        var updates = new List<UpdateDefinition<AuthUser>>();

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim();
            var dup = await _users.Find(u => u.Username == username && u.Id != userId).FirstOrDefaultAsync();
            if (dup is not null)
                return new AuthResponse(false, "Username đã được sử dụng.");
            updates.Add(Builders<AuthUser>.Update.Set(u => u.Username, username));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (!email.Contains('@'))
                return new AuthResponse(false, "Email không hợp lệ.");
            var dup = await _users.Find(u => u.Email == email && u.Id != userId).FirstOrDefaultAsync();
            if (dup is not null)
                return new AuthResponse(false, "Email đã được sử dụng.");
            updates.Add(Builders<AuthUser>.Update.Set(u => u.Email, email));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role is "Staff" or "User" ? request.Role : user.Role;
            updates.Add(Builders<AuthUser>.Update.Set(u => u.Role, role));
        }

        if (updates.Count == 0)
            return new AuthResponse(false, "Không có thông tin để cập nhật.");

        var combined = Builders<AuthUser>.Update.Combine(updates);
        await _users.UpdateOneAsync(u => u.Id == userId, combined);

        var updated = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        return new AuthResponse(true, "Đã cập nhật tài khoản.",
            null, new UserInfo(updated!.Id!, updated.Username, updated.Email, updated.Role, updated.IsLocked));
    }

    public async Task<AuthResponse> SetUserLockedAsync(string userId, bool locked)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user is null)
            return new AuthResponse(false, "Không tìm thấy tài khoản.");

        if (user.Role == "Admin")
            return new AuthResponse(false, "Không thể khóa tài khoản Admin.");

        await _users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<AuthUser>.Update.Set(u => u.IsLocked, locked));

        return new AuthResponse(true, locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.");
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        var update = Builders<AuthUser>.Update.Set(u => u.IsActive, false);
        var result = await _users.UpdateOneAsync(u => u.Id == userId, update);
        return result.ModifiedCount > 0;
    }
}
