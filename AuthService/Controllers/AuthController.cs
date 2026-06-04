using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Đăng ký tài khoản mới</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new AuthResponse(false, "Vui lòng điền đầy đủ thông tin."));

        if (request.Password.Length < 6)
            return BadRequest(new AuthResponse(false, "Mật khẩu phải có ít nhất 6 ký tự."));

        var result = await _authService.RegisterAsync(request);
        return result.Success ? Ok(result) : Conflict(result);
    }

    /// <summary>Admin tạo tài khoản User/Staff</summary>
    [HttpPost("admin/register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterByAdmin([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new AuthResponse(false, "Vui lòng điền đầy đủ thông tin."));

        if (request.Password.Length < 6)
            return BadRequest(new AuthResponse(false, "Mật khẩu phải có ít nhất 6 ký tự."));

        var result = await _authService.RegisterByAdminAsync(request);
        return result.Success ? Ok(result) : Conflict(result);
    }

    /// <summary>Đăng nhập</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new AuthResponse(false, "Email và mật khẩu không được trống."));

        var result = await _authService.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Đổi mật khẩu (cần JWT)</summary>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new AuthResponse(false, "Không xác định được user."));

        var result = await _authService.ChangePasswordAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Validate token - dùng nội bộ giữa các service</summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromHeader(Name = "Authorization")] string? authHeader)
    {
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Ok(new TokenValidationResponse(false, null, null, null, null));

        var token = authHeader.Substring("Bearer ".Length);
        var result = await _authService.ValidateTokenAsync(token);
        return Ok(result);
    }

    /// <summary>Lấy thông tin user hiện tại từ token</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        var email = User.FindFirstValue(ClaimTypes.Email)
                 ?? User.FindFirstValue("email");
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new UserInfo(userId!, username!, email!, role!));
    }

    /// <summary>Danh sách tài khoản (Admin)</summary>
    [HttpGet("admin/accounts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminAccounts()
    {
        var accounts = await _authService.GetAdminAccountsAsync();
        return Ok(accounts);
    }

    /// <summary>Cập nhật tài khoản đăng nhập (Admin)</summary>
    [HttpPut("admin/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserByAdmin(string userId, [FromBody] UpdateAuthUserRequest request)
    {
        var result = await _authService.UpdateUserByAdminAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Khóa / mở khóa tài khoản (Admin)</summary>
    [HttpPatch("admin/{userId}/lock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetUserLocked(string userId, [FromBody] LockAccountRequest request)
    {
        var result = await _authService.SetUserLockedAsync(userId, request.Locked);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Vô hiệu hóa user (Admin only)</summary>
    [HttpDelete("{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(string userId)
    {
        var success = await _authService.DeactivateUserAsync(userId);
        return success
            ? Ok(new { success = true, message = "User đã bị vô hiệu hóa." })
            : NotFound(new { success = false, message = "Không tìm thấy user." });
    }
}
