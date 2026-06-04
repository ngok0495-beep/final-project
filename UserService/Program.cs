using UserService.Configuration;
using UserService.Middleware;
using UserService.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuration ───────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// ─── Services ────────────────────────────────────────────────────
builder.Services.AddSingleton<IJwtTokenValidator, JwtTokenValidator>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// ─── CORS ────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("MicroservicePolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5001",  // AuthService
                "http://localhost:3000",   // Frontend
                "http://127.0.0.1:5500",   // Live Server (FE/index.html)
                "http://localhost:5500"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

// ─── Swagger ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UserService API",
        Version = "v1",
        Description = "Microservice quản lý hồ sơ người dùng - Clothing Management System"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT từ AuthService. Nhập: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── App Pipeline ─────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserService v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("MicroservicePolicy");

// Middleware tự validate token bằng cách gọi AuthService
app.UseMiddleware<AuthValidationMiddleware>();

app.MapControllers();
app.Run();
