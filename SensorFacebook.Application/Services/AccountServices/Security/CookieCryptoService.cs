using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SensorFacebook.Application.Services.AccountServices.Security
{
    public sealed class CookieCryptoService : ICookieCryptoService
    {
        private readonly byte[] _key; // 16/24/32 bytes

        public CookieCryptoService(IConfiguration cfg)
        {
            var keyStr = cfg["Secrets:CookieKey"] ?? throw new InvalidOperationException("Secrets:CookieKey missing");
            // Nếu bạn lưu Base64: _key = Convert.FromBase64String(keyStr);
            _key = Encoding.UTF8.GetBytes(keyStr);
            if (_key.Length is not (16 or 24 or 32))
                throw new InvalidOperationException("CookieKey must be 16/24/32 bytes");
        }

        public string Encrypt(string plaintext)
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var pt = Encoding.UTF8.GetBytes(plaintext);
            var ct = new byte[pt.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(_key);
            aes.Encrypt(nonce, pt, ct, tag);

            // format: base64(nonce|tag|ciphertext)
            var payload = new byte[12 + 16 + ct.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, 12);
            Buffer.BlockCopy(tag, 0, payload, 12, 16);
            Buffer.BlockCopy(ct, 0, payload, 28, ct.Length);
            return Convert.ToBase64String(payload);
        }

        public string Decrypt(string ciphertext)
        {
            var payload = Convert.FromBase64String(ciphertext);
            var nonce = new byte[12];
            var tag = new byte[16];
            var ct = new byte[payload.Length - 28];

            Buffer.BlockCopy(payload, 0, nonce, 0, 12);
            Buffer.BlockCopy(payload, 12, tag, 0, 16);
            Buffer.BlockCopy(payload, 28, ct, 0, ct.Length);

            var pt = new byte[ct.Length];
            using var aes = new AesGcm(_key);
            aes.Decrypt(nonce, ct, tag, pt);
            return Encoding.UTF8.GetString(pt);
        }
    }
}
