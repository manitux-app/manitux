using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Manitux.Core.Extractors.Helpers;

public static class AesHelper
{
    private const string DefaultHashAlgorithm = "MD5";

    public static string? CryptoAesHandler(
        string data,
        byte[] pass,
        bool encrypt = true,
        string padding = "AES/CBC/PKCS5PADDING")
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<AesData>(data);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Ct)) return null;

            var salt = HexToByteArray(parsed.S);
            var ivLength = parsed.Iv.Length / 2;
            var saltLength = parsed.S.Length / 2;
            var keyIv = GenerateKeyAndIv(pass, salt, ivLength: ivLength, saltLength: saltLength);
            if (keyIv is null) return null;

            using var aes = Aes.Create();
            aes.Key = keyIv.Value.Key;
            aes.IV = keyIv.Value.Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = GetPaddingMode(padding);

            if (!encrypt)
            {
                var encryptedBytes = Convert.FromBase64String(parsed.Ct);
                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }

            var plainBytes = Encoding.UTF8.GetBytes(parsed.Ct);
            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return null;
        }
    }

    public static (byte[] Key, byte[] Iv)? GenerateKeyAndIv(
        byte[] password,
        byte[] salt,
        string hashAlgorithm = DefaultHashAlgorithm,
        int keyLength = 32,
        int ivLength = 16,
        int saltLength = 8,
        int iterations = 1)
    {
        try
        {
            using var md = CreateHashAlgorithm(hashAlgorithm);
            if (md is null) return null;

            var digestLength = md.HashSize / 8;
            var targetKeySize = keyLength + ivLength;
            var requiredLength = ((targetKeySize + digestLength - 1) / digestLength) * digestLength;
            var generatedData = new byte[requiredLength];
            var generatedLength = 0;
            var safeSaltLength = Math.Min(saltLength, salt.Length);

            while (generatedLength < targetKeySize)
            {
                var inputLength = (generatedLength > 0 ? digestLength : 0) + password.Length + safeSaltLength;
                var input = new byte[inputLength];
                var offset = 0;
                if (generatedLength > 0)
                {
                    Buffer.BlockCopy(generatedData, generatedLength - digestLength, input, offset, digestLength);
                    offset += digestLength;
                }

                Buffer.BlockCopy(password, 0, input, offset, password.Length);
                offset += password.Length;
                Buffer.BlockCopy(salt, 0, input, offset, safeSaltLength);

                var digest = md.ComputeHash(input);

                Buffer.BlockCopy(digest, 0, generatedData, generatedLength, digestLength);

                for (var i = 1; i < iterations; i++)
                {
                    digest = md.ComputeHash(digest);
                    Buffer.BlockCopy(digest, 0, generatedData, generatedLength, digestLength);
                }

                generatedLength += digestLength;
            }

            var key = generatedData.Take(keyLength).ToArray();
            var iv = generatedData.Skip(keyLength).Take(ivLength).ToArray();
            return (key, iv);
        }
        catch
        {
            return null;
        }
    }

    private static HashAlgorithm? CreateHashAlgorithm(string hashAlgorithm)
    {
        return hashAlgorithm.ToUpperInvariant() switch
        {
            "MD5" => MD5.Create(),
            "SHA1" => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            _ => null
        };
    }

    public static byte[] HexToByteArray(string value)
    {
        if (value.Length % 2 != 0)
        {
            throw new ArgumentException("Must have an even length.", nameof(value));
        }

        var bytes = new byte[value.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    private static PaddingMode GetPaddingMode(string padding)
    {
        return padding.Contains("NOPADDING", StringComparison.OrdinalIgnoreCase)
            ? PaddingMode.None
            : PaddingMode.PKCS7;
    }

    private sealed class AesData
    {
        [JsonPropertyName("ct")]
        public string Ct { get; set; } = string.Empty;

        [JsonPropertyName("iv")]
        public string Iv { get; set; } = string.Empty;

        [JsonPropertyName("s")]
        public string S { get; set; } = string.Empty;
    }
}
