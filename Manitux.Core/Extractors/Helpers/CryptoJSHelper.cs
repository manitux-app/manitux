using System.Security.Cryptography;
using System.Text;

namespace Manitux.Core.Extractors.Helpers;

public static class CryptoJSHelper
{
    private const int KeySizeBytes = 32;
    private const int IvSizeBytes = 16;
    private const int SaltSizeBytes = 8;
    private const string SaltPrefix = "Salted__";

    public static string? Encrypt(string password, string plainText)
    {
        try
        {
            var salt = GenerateSalt(SaltSizeBytes);
            var keyIv = AesHelper.GenerateKeyAndIv(
                Encoding.UTF8.GetBytes(password),
                salt,
                keyLength: KeySizeBytes,
                ivLength: IvSizeBytes,
                saltLength: SaltSizeBytes);

            if (keyIv is null) return null;

            var cipherText = TransformAes(
                Encoding.UTF8.GetBytes(plainText),
                keyIv.Value.Key,
                keyIv.Value.Iv,
                encrypt: true);

            var prefix = Encoding.ASCII.GetBytes(SaltPrefix);
            var encrypted = new byte[prefix.Length + salt.Length + cipherText.Length];
            Buffer.BlockCopy(prefix, 0, encrypted, 0, prefix.Length);
            Buffer.BlockCopy(salt, 0, encrypted, prefix.Length, salt.Length);
            Buffer.BlockCopy(cipherText, 0, encrypted, prefix.Length + salt.Length, cipherText.Length);

            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return null;
        }
    }

    public static string? Decrypt(string password, string cipherText)
    {
        try
        {
            var encrypted = Convert.FromBase64String(cipherText);
            if (encrypted.Length <= SaltPrefix.Length + SaltSizeBytes) return null;

            var prefix = Encoding.ASCII.GetString(encrypted, 0, SaltPrefix.Length);
            if (!string.Equals(prefix, SaltPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            var salt = encrypted.Skip(SaltPrefix.Length).Take(SaltSizeBytes).ToArray();
            var cipherBytes = encrypted.Skip(SaltPrefix.Length + SaltSizeBytes).ToArray();
            var keyIv = AesHelper.GenerateKeyAndIv(
                Encoding.UTF8.GetBytes(password),
                salt,
                keyLength: KeySizeBytes,
                ivLength: IvSizeBytes,
                saltLength: SaltSizeBytes);

            if (keyIv is null) return null;

            var plainBytes = TransformAes(cipherBytes, keyIv.Value.Key, keyIv.Value.Iv, encrypt: false);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] TransformAes(byte[] data, byte[] key, byte[] iv, bool encrypt)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
        return transform.TransformFinalBlock(data, 0, data.Length);
    }

    private static byte[] GenerateSalt(int length)
    {
        var salt = new byte[length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }
}
