using System.Security.Cryptography;
using System.Text;
using Account.Domain.Interfaces;

namespace Account.Infrastructure.Cryptography;

public class CryptographService:ICryptography
{
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
}