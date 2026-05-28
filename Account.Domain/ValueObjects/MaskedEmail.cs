namespace Account.Domain.ValueObjects;

public sealed record MaskedEmail // sealed + record = идеальный Value Object
{
    public string Value { get; }

    private MaskedEmail(string value) => Value = value;

    public static MaskedEmail Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            throw new ArgumentException("Invalid email format for masking.");

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];

        string maskedName = name.Length <= 2 
            ? "**" 
            : name.Substring(0, 2) + "****";
            
        return new MaskedEmail($"{maskedName}@{domain}");
    }

    public override string ToString() => Value;

    public static implicit operator string(MaskedEmail maskedEmail) => maskedEmail.Value;
}