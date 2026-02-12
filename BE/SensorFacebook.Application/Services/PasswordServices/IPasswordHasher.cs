using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.PasswordServices
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string hash, string password);
    }
}
