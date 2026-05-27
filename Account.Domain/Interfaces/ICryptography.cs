namespace Account.Domain.Interfaces;

public interface ICryptography
{
    string Hash(string value);
    bool VerifyHash(string value, string hash);
    
}