using Microsoft.AspNetCore.Authorization;

namespace AccountApi.Authorization;

public static class AuthPolicies
{
    public const string PreAuthOnly = "MfaPreAuthOnlyPolicy";
    public const string MfaRequired = "MfaRequiredPolicy";
    public const string ApiKeyOnly = "ApiKeyOnlyPolicy";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PreAuthOnlyAttribute : AuthorizeAttribute
{
    public PreAuthOnlyAttribute() : base(AuthPolicies.PreAuthOnly)
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeMfaRequiredAttribute : AuthorizeAttribute
{
    public AuthorizeMfaRequiredAttribute() : base(AuthPolicies.MfaRequired)
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeApiKeyOnlyAttribute: AuthorizeAttribute
{
    public AuthorizeApiKeyOnlyAttribute() : base(AuthPolicies.ApiKeyOnly)
    {
    }
}