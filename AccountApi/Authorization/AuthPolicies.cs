using Microsoft.AspNetCore.Authorization;

namespace AccountApi.Authorization;

public static class AuthPolicies
{
    public const string PreAuthOnly = "MfaPreAuthOnlyPolicy";
    public const string MfaRequired = "MfaRequiredPolicy";
    public const string ApiKeyOnly = "ApiKeyOnlyPolicy";
    public const string MasterKeyOnly = "MasterKeyOnlyPolicy";
    public const string TokenOnly = "TokenOnlyPolicy";
    public const string ApiKeyAndMasterKey = "ApiKeyAndMasterKeyPolicy";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeApiKeyAndMasterKeyAttribute : AuthorizeAttribute
{
    public AuthorizeApiKeyAndMasterKeyAttribute() : base(AuthPolicies.ApiKeyAndMasterKey)
    {
    }   
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeTokenOnlyAttribute : AuthorizeAttribute
{
    public AuthorizeTokenOnlyAttribute() : base(AuthPolicies.TokenOnly)
    {
        
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MasterKeyOnlyAttribute : AuthorizeAttribute
{
    public MasterKeyOnlyAttribute() :base(AuthPolicies.MasterKeyOnly)
    {
        
    }
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