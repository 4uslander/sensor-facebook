using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Security
{
    public interface ICookieCryptoService
    {
        string Encrypt(string plaintext);
        string Decrypt(string ciphertext);
    }
}
