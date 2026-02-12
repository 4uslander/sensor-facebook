using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SensorFacebook.Api.Setup;               // AddApiSwagger()
using SensorFacebook.Application.Services.AccountServices.Security;
using SensorFacebook.Application.Services.AuthServices;
using SensorFacebook.Application.Utilities;    // InfrastructureRegister()
using SensorFacebook.Infrastructure.Models;
using SensorFacebook.Shared.Options;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// -------------------- JWT OPTIONS --------------------
var jwtOpt = cfg.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.Configure<JwtOptions>(cfg.GetSection("Jwt"));

// -------------------- REDIS (SAFE) --------------------
var redisConn = cfg["Redis:Configuration"]; // ex: "localhost:6379"
if (!string.IsNullOrWhiteSpace(redisConn))
{
    // cache abstraction (Microsoft.Extensions.Caching.StackExchangeRedis)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = cfg["Redis:InstanceName"]; // ex: "sensor:"
    });

    // multiplexer for your RedisCacheService / others
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var opt = ConfigurationOptions.Parse(redisConn!, true);
        opt.AbortOnConnectFail = false; // ✅ Redis down won't crash API at startup
        return ConnectionMultiplexer.Connect(opt);
    });
}
else
{
    // Nếu bạn muốn bắt buộc có redis thì throw ở đây.
    // Hiện tại để dễ dev: không throw.
}

// -------------------- DB CONTEXT --------------------
builder.Services.AddDbContext<SensorDbContext>(opt =>
{
    opt.UseNpgsql(cfg.GetConnectionString("Default"),
        npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "public"));
    opt.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// -------------------- DI MODULES --------------------
builder.Services.InfrastructureRegister(cfg);

// -------------------- CONTROLLERS + JSON --------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddApiSwagger();

// cookie crypto (nếu bạn dùng)
builder.Services.AddCookieCrypto(cfg);

// -------------------- AUTHN/AUTHZ --------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = jwtOpt.Issuer,
            ValidAudience = jwtOpt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// -------------------- HEALTH CHECKS --------------------
var hc = builder.Services.AddHealthChecks();

if (!string.IsNullOrWhiteSpace(redisConn))
{
    hc.AddRedis(redisConn!, name: "redis");
}

hc.AddRabbitMQ(sp =>
{
    var conn = sp.GetRequiredService<RabbitMQ.Client.IConnection>();
    return conn;
}, name: "rabbit");

// -------------------- CORS --------------------
builder.Services.AddCors(o => o.AddPolicy("dev", p => p
    .WithOrigins("http://localhost:5173", "https://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
));

// -------------------- BUILD APP --------------------
var app = builder.Build();

// -------------------- PIPELINE --------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SensorFacebook API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("dev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ health: includes redis + rabbit (whatever you registered)
app.MapHealthChecks("/health");

app.Run();
