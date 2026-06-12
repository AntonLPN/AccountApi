namespace Account.Domain.Interfaces;

public interface ICryptography
{
    string Hash(string value);
    bool VerifyHash(string value, string hash);
    
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
}