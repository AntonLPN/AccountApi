using System.Text;

namespace Account.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cant be empty.");

        var trimmed = value.Trim();

        var normalized = trimmed.Normalize(NormalizationForm.FormC);

        if (!IsValidFormat(normalized))
            throw new ArgumentException($"'{value}' — invalid format email.");

        var parts = normalized.Split('@');
        var localPart = parts[0];
        var domain = parts[1].ToLowerInvariant(); // Domain part is case-insensitive, so we normalize it to lower case.

        localPart = localPart.ToLowerInvariant();

        localPart = ApplyProviderSpecificNormalization(localPart, domain);

        return new Email($"{localPart}@{domain}");
    }

    private static bool IsValidFormat(string value)
    {
        
        var addr = new System.Net.Mail.MailAddress(value);
        return addr.Address == value;
        
    }

    private static string ApplyProviderSpecificNormalization(string localPart, string domain)
    {
     
        if (domain is "gmail.com" or "googlemail.com")
        {
            localPart = localPart.Replace(".", string.Empty);
            
            var plusIndex = localPart.IndexOf('+');
            if (plusIndex >= 0)
                localPart = localPart[..plusIndex];
        }
        else
        {
            var plusIndex = localPart.IndexOf('+');
            if (plusIndex >= 0)
                localPart = localPart[..plusIndex];
        }

        return localPart;
    }

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Email);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}