using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.PasswordServices
{
    public sealed class BcryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        public bool Verify(string hash, string password) => BCrypt.Net.BCrypt.Verify(password, hash);
    }

}
