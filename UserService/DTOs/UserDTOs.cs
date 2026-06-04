namespace UserService.DTOs;

// ─── Request DTOs ───────────────────────────────────────────────
public record CreateProfileRequest(
    string AuthUserId,
    string FullName,
    string? Email = null,
    string? JobTitle = null,
    string? Phone = null,
    string? Gender = null,
    string? Department = null,
    string Role = "User",
    AddressDto? Address = null,
    DateTime? DateOfBirth = null
);

public record UpdateProfileRequest(
    string? FullName,
    string? Email,
    string? JobTitle,
    string? Phone,
    AddressDto? Address,
    string? AvatarUrl,
    string? Gender,
    DateTime? DateOfBirth,
    string? Department
);

public record AddressDto(
    string? Street,
    string? City,
    string? Province,
    string? ZipCode
);

// ─── Response DTOs ──────────────────────────────────────────────
public record UserProfileResponse(
    string Id,
    string AuthUserId,
    string FullName,
    string? Email,
    string? JobTitle,
    string? Phone,
    AddressDto? Address,
    string? AvatarUrl,
    string? Gender,
    DateTime? DateOfBirth,
    string? Department,
    string Role,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// ─── Internal (dùng khi call AuthService) ──────────────────────
public record AuthValidationResponse(
    bool IsValid,
    string? UserId,
    string? Username,
    string? Email,
    string? Role
);
