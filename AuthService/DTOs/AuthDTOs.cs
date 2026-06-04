namespace AuthService.DTOs;

// ─── Request DTOs ───────────────────────────────────────────────
public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string Role = "User"
);

public record LoginRequest(
    string Email,
    string Password
);

public record ChangePasswordRequest(
    string OldPassword,
    string NewPassword
);

public record UpdateAuthUserRequest(
    string? Username,
    string? Email,
    string? Role
);

public record LockAccountRequest(bool Locked);

// ─── Response DTOs ──────────────────────────────────────────────
public record AuthResponse(
    bool Success,
    string Message,
    string? Token = null,
    UserInfo? User = null
);

public record UserInfo(
    string Id,
    string Username,
    string Email,
    string Role,
    bool IsLocked = false
);

public record AdminAccountResponse(
    string Id,
    string Username,
    string Email,
    string Role,
    bool IsLocked
);

public record TokenValidationResponse(
    bool IsValid,
    string? UserId,
    string? Username,
    string? Email,
    string? Role
);
