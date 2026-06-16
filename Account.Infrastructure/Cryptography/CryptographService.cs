using System.Security.Cryptography;
using System.Text;
using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable InconsistentNaming

namespace Account.Infrastructure.Cryptography;

public class CryptographService(IOptions<CryptoOptions> options) : ICryptography
{
    private const int KEY_SIZE = 32;
    private const int NONCE_SIZE = 12;
    private const int TAG_SIZE = 16;

    // This method is simple only for show as example. In production use more secure method.
    public string Hash(string value)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyHash(string input, string hash)
        => Hash(input) == hash;

    public string Encrypt(string plainText)
    {
        var key = Convert.FromBase64String(options.Value.Key);
        if (key.Length != KEY_SIZE)
            throw new ArgumentException("Key must be 32 bytes (256 bits).");

        byte[] nonce = RandomNumberGenerator.GetBytes(NONCE_SIZE);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);

        byte[] cipherText = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TAG_SIZE];

        using var aes = new AesGcm(key, TAG_SIZE);
        aes.Encrypt(
            nonce,
            plaintextBytes,
            cipherText,
            tag);

        // nonce + tag + ciphertext
        byte[] result = new byte[NONCE_SIZE + TAG_SIZE + cipherText.Length];

        Buffer.BlockCopy(nonce, 0, result, 0, NONCE_SIZE);
        Buffer.BlockCopy(tag, 0, result, NONCE_SIZE, TAG_SIZE);
        Buffer.BlockCopy(cipherText, 0, result, NONCE_SIZE + TAG_SIZE, cipherText.Length);

        return Convert.ToBase64String(result);
    }
    
    public string Decrypt(string encryptedText)
    {
        var key = Convert.FromBase64String(options.Value.Key);
        if (key.Length != KEY_SIZE)
            throw new ArgumentException("Key must be 32 bytes (256 bits).");

        byte[] data = Convert.FromBase64String(encryptedText);

        byte[] nonce = data[..NONCE_SIZE];
        byte[] tag = data[NONCE_SIZE..(NONCE_SIZE + TAG_SIZE)];
        byte[] cipherText = data[(NONCE_SIZE + TAG_SIZE)..];

        byte[] plaintext = new byte[cipherText.Length];

        using var aes = new AesGcm(key, TAG_SIZE);
        aes.Decrypt(
            nonce,
            cipherText,
            tag,
            plaintext);

        return Encoding.UTF8.GetString(plaintext);    }
}