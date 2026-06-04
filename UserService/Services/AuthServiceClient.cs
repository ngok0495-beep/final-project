using UserService.DTOs;

namespace UserService.Services;

/// <summary>
/// HTTP Client để UserService gọi AuthService xác thực token
/// </summary>
public interface IAuthServiceClient
{
    Task<AuthValidationResponse?> ValidateTokenAsync(string bearerToken);
}

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthValidationResponse?> ValidateTokenAsync(string bearerToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/validate");
            request.Headers.Add("Authorization", bearerToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AuthService validate returned {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthValidationResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể kết nối AuthService để validate token");
            return null;
        }
    }
}
