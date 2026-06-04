using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserProfileService profileService, ILogger<UserController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>Health check</summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", service = "UserService" });

    /// <summary>Lấy profile của chính mình</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var authUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(authUserId))
            return Unauthorized();

        var profile = await _profileService.GetByAuthUserIdAsync(authUserId);
        if (profile is null)
            return NotFound(new { message = "Chưa có profile. Vui lòng tạo profile." });

        return Ok(profile);
    }

    /// <summary>Tạo profile lần đầu (tự động gọi sau khi register)</summary>
    [HttpPost("me")]
    public async Task<IActionResult> CreateMyProfile([FromBody] CreateProfileRequest request)
    {
        var authUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(authUserId))
            return Unauthorized();

        // Gán lại authUserId + role từ token (không tin client gửi Admin)
        var roleFromToken = User.FindFirstValue(ClaimTypes.Role) ?? "User";
        var profileRole = roleFromToken is "Staff" or "User" ? roleFromToken : "User";
        var safeRequest = request with
        {
            AuthUserId = authUserId,
            Role = profileRole
        };

        // Kiểm tra đã có profile chưa
        var existing = await _profileService.GetByAuthUserIdAsync(authUserId);
        if (existing is not null)
            return Conflict(new { message = "Profile đã tồn tại. Dùng PUT để cập nhật." });

        var created = await _profileService.CreateAsync(safeRequest);
        return CreatedAtAction(nameof(GetMyProfile), created);
    }

    /// <summary>Cập nhật profile của chính mình</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var authUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(authUserId))
            return Unauthorized();

        var updated = await _profileService.UpdateAsync(authUserId, request);
        if (updated is null)
            return NotFound(new { message = "Không tìm thấy profile." });

        return Ok(updated);
    }

    // ─── Admin endpoints ─────────────────────────────────────────

    /// <summary>Lấy danh sách tất cả user (Admin only)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != "Admin")
            return Forbid();

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _profileService.GetAllAsync(page, pageSize, search);
        return Ok(result);
    }

    /// <summary>Lấy profile theo authUserId (Admin / internal)</summary>
    [HttpGet("{authUserId}")]
    public async Task<IActionResult> GetByAuthUserId(string authUserId)
    {
        var callerRole = User.FindFirstValue(ClaimTypes.Role);
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Chỉ Admin hoặc chính user đó mới được xem
        if (callerRole != "Admin" && callerId != authUserId)
            return Forbid();

        var profile = await _profileService.GetByAuthUserIdAsync(authUserId);
        if (profile is null)
            return NotFound(new { message = "Không tìm thấy profile." });

        return Ok(profile);
    }

    /// <summary>Admin cập nhật hồ sơ user</summary>
    [HttpPut("{authUserId}")]
    public async Task<IActionResult> AdminUpdateProfile(
        string authUserId,
        [FromBody] UpdateProfileRequest request)
    {
        if (User.FindFirstValue(ClaimTypes.Role) != "Admin")
            return Forbid();

        var updated = await _profileService.UpdateAsync(authUserId, request);
        if (updated is null)
            return NotFound(new { message = "Không tìm thấy hồ sơ." });

        return Ok(updated);
    }

    /// <summary>Xóa profile theo authUserId (Admin only)</summary>
    [HttpDelete("{authUserId}")]
    public async Task<IActionResult> Delete(string authUserId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != "Admin")
            return Forbid();

        var success = await _profileService.DeleteAsync(authUserId);
        if (!success)
            return NotFound(new { message = "Không tìm thấy profile." });

        return Ok(new { message = "Đã xóa profile thành công." });
    }
}
