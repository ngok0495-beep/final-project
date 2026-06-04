using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Services;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetByAuthUserIdAsync(string authUserId);
    Task<UserProfileResponse?> GetByIdAsync(string id);
    Task<PagedResult<UserProfileResponse>> GetAllAsync(int page, int pageSize, string? search);
    Task<UserProfileResponse> CreateAsync(CreateProfileRequest request);
    Task<UserProfileResponse?> UpdateAsync(string authUserId, UpdateProfileRequest request);
    Task<bool> DeleteAsync(string authUserId);
}

public class UserProfileService : IUserProfileService
{
    private readonly IMongoCollection<UserProfile> _profiles;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IOptions<MongoDbSettings> mongoSettings,
        ILogger<UserProfileService> logger)
    {
        var client = new MongoClient(mongoSettings.Value.ConnectionString);
        var db = client.GetDatabase(mongoSettings.Value.DatabaseName);
        _profiles = db.GetCollection<UserProfile>("user_profiles");

        // Index theo authUserId (unique)
        var indexKeys = Builders<UserProfile>.IndexKeys.Ascending(p => p.AuthUserId);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _profiles.Indexes.CreateOne(new CreateIndexModel<UserProfile>(indexKeys, indexOptions));

        _logger = logger;
    }

    public async Task<UserProfileResponse?> GetByAuthUserIdAsync(string authUserId)
    {
        var profile = await _profiles
            .Find(p => p.AuthUserId == authUserId)
            .FirstOrDefaultAsync();

        return profile is null ? null : ToResponse(profile);
    }

    public async Task<UserProfileResponse?> GetByIdAsync(string id)
    {
        var profile = await _profiles.Find(p => p.Id == id).FirstOrDefaultAsync();
        return profile is null ? null : ToResponse(profile);
    }

    public async Task<PagedResult<UserProfileResponse>> GetAllAsync(int page, int pageSize, string? search)
    {
        var filter = Builders<UserProfile>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filter = Builders<UserProfile>.Filter.Or(
                Builders<UserProfile>.Filter.Regex(p => p.FullName,
                    new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<UserProfile>.Filter.Regex(p => p.Email,
                    new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<UserProfile>.Filter.Regex(p => p.JobTitle,
                    new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<UserProfile>.Filter.Regex(p => p.Phone,
                    new MongoDB.Bson.BsonRegularExpression(search, "i"))
            );
        }

        var totalCount = (int)await _profiles.CountDocumentsAsync(filter);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var profiles = await _profiles
            .Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();

        return new PagedResult<UserProfileResponse>(
            profiles.Select(ToResponse).ToList(),
            totalCount, page, pageSize, totalPages
        );
    }

    public async Task<UserProfileResponse> CreateAsync(CreateProfileRequest request)
    {
        var profile = new UserProfile
        {
            AuthUserId = request.AuthUserId,
            FullName = request.FullName,
            Email = request.Email,
            JobTitle = request.JobTitle,
            Phone = request.Phone,
            Gender = request.Gender,
            Department = request.Department,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address is null ? null : new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                Province = request.Address.Province,
                ZipCode = request.Address.ZipCode
            },
            Role = request.Role is "Staff" or "User" ? request.Role : "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _profiles.InsertOneAsync(profile);
        _logger.LogInformation("Created profile for AuthUserId: {Id}", request.AuthUserId);
        return ToResponse(profile);
    }

    public async Task<UserProfileResponse?> UpdateAsync(string authUserId, UpdateProfileRequest request)
    {
        var profile = await _profiles
            .Find(p => p.AuthUserId == authUserId)
            .FirstOrDefaultAsync();

        if (profile is null) return null;

        var updateDef = Builders<UserProfile>.Update
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        if (request.FullName is not null)
            updateDef = updateDef.Set(p => p.FullName, request.FullName);
        if (request.Email is not null)
            updateDef = updateDef.Set(p => p.Email, request.Email);
        if (request.JobTitle is not null)
            updateDef = updateDef.Set(p => p.JobTitle, request.JobTitle);
        if (request.Phone is not null)
            updateDef = updateDef.Set(p => p.Phone, request.Phone);
        if (request.AvatarUrl is not null)
            updateDef = updateDef.Set(p => p.AvatarUrl, request.AvatarUrl);
        if (request.Gender is not null)
            updateDef = updateDef.Set(p => p.Gender, request.Gender);
        if (request.DateOfBirth is not null)
            updateDef = updateDef.Set(p => p.DateOfBirth, request.DateOfBirth);
        if (request.Department is not null)
            updateDef = updateDef.Set(p => p.Department, request.Department);
        if (request.Address is not null)
        {
            updateDef = updateDef.Set(p => p.Address, new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                Province = request.Address.Province,
                ZipCode = request.Address.ZipCode
            });
        }

        await _profiles.UpdateOneAsync(p => p.AuthUserId == authUserId, updateDef);

        var updated = await _profiles.Find(p => p.AuthUserId == authUserId).FirstOrDefaultAsync();
        return updated is null ? null : ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(string authUserId)
    {
        var result = await _profiles.DeleteOneAsync(p => p.AuthUserId == authUserId);
        return result.DeletedCount > 0;
    }

    // ─── Mapper ─────────────────────────────────────────────────
    private static UserProfileResponse ToResponse(UserProfile p) => new(
        p.Id!,
        p.AuthUserId,
        p.FullName,
        p.Email,
        p.JobTitle,
        p.Phone,
        p.Address is null ? null : new AddressDto(
            p.Address.Street, p.Address.City,
            p.Address.Province, p.Address.ZipCode),
        p.AvatarUrl,
        p.Gender,
        p.DateOfBirth,
        p.Department,
        string.IsNullOrWhiteSpace(p.Role) ? "User" : p.Role,
        p.CreatedAt,
        p.UpdatedAt
    );
}
