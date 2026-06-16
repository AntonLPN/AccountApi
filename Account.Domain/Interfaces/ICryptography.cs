namespace Account.Domain.Interfaces;

public interface ICryptography
{
    string Hash(string value);
    bool VerifyHash(string value, string hash);

    string Encrypt(string plainText);

    //TODO Delete in production
    /// <summary>
    /// NOW THIS METHOD IMPLEMENTED FOR SHOW EXAMPLE AND DEBUGGING PURPOSES§
    /// This method is never used in production, only for show example, if you want to decrypt
    /// Remember that in production you should delete this method and never use it because if you can decrypt data,
    /// it means that your encryption method is not secure enough and can be easily broken by attackers.
    /// In production, you should use a one-way hashing algorithm for sensitive data like passwords
    /// and never store or transmit the original plaintext.
    /// </summary>
    /// <param name="encryptedText"></param>
    /// <returns></returns>
    string Decrypt(string encryptedText);
}