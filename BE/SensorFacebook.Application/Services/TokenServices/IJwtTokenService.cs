using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.TokenServices
{
    public interface IJwtTokenService
    {
        (string accessToken, DateTimeOffset accessExpires, string jti)
            CreateAccessToken(Guid userId, string email, string role);

        (string refreshToken, DateTimeOffset refreshExpires)
            CreateRefreshToken();
    }
}
