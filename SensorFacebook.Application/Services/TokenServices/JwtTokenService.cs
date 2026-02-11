using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SensorFacebook.Application.Services.AuthServices;


namespace SensorFacebook.Application.Services.TokenServices
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        private readonly SymmetricSecurityKey _key;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _opt = options.Value;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        }

        public (string accessToken, DateTimeOffset accessExpires, string jti)
            CreateAccessToken(Guid userId, string email, string role)
        {
            var now = DateTimeOffset.UtcNow;
            var expires = now.AddMinutes(_opt.AccessTokenMinutes);
            var jti = Guid.NewGuid().ToString("N");

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.Role, role)
        };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims,
                notBefore: now.UtcDateTime, expires: expires.UtcDateTime, signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires, jti);
        }

        public (string refreshToken, DateTimeOffset refreshExpires) CreateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(bytes);
            return (token, DateTimeOffset.UtcNow.AddDays(_opt.RefreshTokenDays));
        }
    }
}
