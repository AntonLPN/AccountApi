using PhoneNumbers;

namespace Account.Domain.ValueObjects;

public sealed record Phone
{
    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    public static bool TryNormalize(string? raw, out string e164, string defaultRegion = "UA")
    {
        e164 = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        try
        {
            var number = Util.Parse(raw, defaultRegion);
            if (!Util.IsValidNumber(number)) return false;

            e164 = Util.Format(number, PhoneNumberFormat.E164);
            return true;
        }
        catch (NumberParseException)
        {
            return false;
        }
    }
}