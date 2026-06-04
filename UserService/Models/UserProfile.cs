using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserService.Models;

public class UserProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Trùng với AuthUser.Id bên AuthService</summary>
    [BsonElement("authUserId")]
    public string AuthUserId { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("jobTitle")]
    public string? JobTitle { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("address")]
    public Address? Address { get; set; }

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [BsonElement("gender")]
    public string? Gender { get; set; } // Male | Female | Other

    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [BsonElement("department")]
    public string? Department { get; set; } // Phòng ban (nếu là staff)

    [BsonElement("role")]
    public string Role { get; set; } = "User"; // User | Staff (đồng bộ AuthService)

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Address
{
    [BsonElement("street")]
    public string? Street { get; set; }

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("province")]
    public string? Province { get; set; }

    [BsonElement("zipCode")]
    public string? ZipCode { get; set; }
}
