using Microsoft.AspNetCore.Authorization;

namespace AccountApi.Authorization;

public static class AuthPolicies
{
    public const string PreAuthOnly = "MfaPreAuthOnlyPolicy";
    public const string MfaRequired = "MfaRequiredPolicy";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizePreAuthOnlyAttribute : AuthorizeAttribute
{
    public AuthorizePreAuthOnlyAttribute() : base(AuthPolicies.PreAuthOnly)
    {
    }
}

// Атрибут для полноценных бизнес-эндпоинтов (требуется 2FA)
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeMfaRequiredAttribute : AuthorizeAttribute
{
    public AuthorizeMfaRequiredAttribute() : base(AuthPolicies.MfaRequired)
    {
    }
}