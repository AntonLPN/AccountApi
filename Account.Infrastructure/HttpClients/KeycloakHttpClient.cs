using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Account.Domain.Models;
using Account.Domain.ValueObjects;
using Account.Infrastructure.Configuration;
using Ardalis.Result;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.HttpClients;

public class KeycloakHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakHttpClient> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public KeycloakHttpClient(HttpClient httpClient, ILogger<KeycloakHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<string>> RegisterUserAsync(string userName, string email,
        string? password,
        string adminToken,
        KeycloakAdminOptions options, bool useCredentials = true)
    {
        try
        {
            object newUser;
            if (useCredentials)
            {
                newUser = new
                {
                    username = userName,
                    email,
                    enabled = true,
                    emailVerified = true,
                    credentials = new[]
                    {
                        new { type = "password", value = password, temporary = false }
                    }
                };
            }
            else
            {
                newUser = new
                {
                    username = userName,
                    email,
                    enabled = true,
                    emailVerified = true
                };
            }


            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                CombineUrls(options.BaseUrl, "admin/realms", options.Realm, "users"))
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
                Content = new StringContent(JsonSerializer.Serialize(newUser), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var locationHeader = response.Headers.Location?.ToString();
                var userId = locationHeader?.Split('/').Last();

                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation($"User {userName} registered successfully with ID: {userId}");
                    return Result<string>.Success(userId);
                }

                return Result<string>.Error("User created but failed to retrieve user ID");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return Result<string>.Error($"Failed to register user: {response.StatusCode} - {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during user registration for {userName}");
            throw; //throw to middleware handle exception
        }
    }

    public async Task<TokenResponse?> GetAdminTokenAsync(KeycloakAdminOptions options)
    {
        try
        {
            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ]);
            var tokenUrl = CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/token");
            _logger.LogInformation($"--- Trying to get token from: {tokenUrl} ---");
            var response = await _httpClient.PostAsync(tokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var failedUrl = response.RequestMessage?.RequestUri?.ToString();

                _logger.LogError($"--- KEYCLOAK 404 ERROR ---");
                _logger.LogError($"URL: {failedUrl}");
                _logger.LogError($"Body: {errorBody}");

                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining admin token");
            throw; //throw to middleware handle exception
        }
    }

    public async Task<TokenResponse?> LoginAsync(string userName, string password, KeycloakAdminOptions options)
    {
        try
        {
            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", userName),
                new KeyValuePair<string, string>("password", password)
            ]);

            var url = CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/token");
            var response = await _httpClient.PostAsync(
                url,
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed for user : {ResponseStatusCode} - {ErrorBody}", response.StatusCode,
                    errorBody);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during login for user {userName}");
            throw; //throw to middleware handle exception
        }
    }


    public async Task<bool> LogoutAsync(string refreshToken, KeycloakAdminOptions options)
    {
        try
        {
            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            ]);

            var url = CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/logout");
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
                return true;

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Logout failed: {StatusCode} - {Body}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw; //throw to middleware handle exception
        }
    }


    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, KeycloakAdminOptions options)
    {
        try
        {
            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            ]);

            var tokenUrl = CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/token");

            _logger.LogInformation("Trying to refresh token: {TokenUrl} ", tokenUrl);
            var response = await _httpClient.PostAsync(tokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                //if token expired
                _logger.LogError("KEYCLOAK REFRESH ERROR ({ResponseStatusCode})", response.StatusCode);
                _logger.LogError("Body: {ErrorBody}", errorBody);

                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            throw; //throw to middleware handle exception
        }
    }

    public async Task<string?> GetUserIdByEmailAsync(string email, KeycloakAdminOptions options)
    {
        var adminToken = await GetAdminTokenAsync(options);
        var url = CombineUrls(options.BaseUrl, "admin/realms", options.Realm, "users") + $"?email={email}&exact=true";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.AccessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("GetUserIdByEmailAsync failed: {Error}", error);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var firstUser = root[0];
            if (firstUser.TryGetProperty("id", out var idElement))
            {
                return idElement.GetString();
            }
        }

        return null;
    }

    public async Task<TokenResponse?> LoginAsync(string email, KeycloakAdminOptions options)
    {
        try
        {
            var userId = await GetUserIdByEmailAsync(email, options);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("LoginByEmailWithoutPassword: user not found for email {Email}",
                    MaskedEmail.Create(email));
                return null;
            }

            var adminToken = await GetAdminTokenAsync(options);
            if (adminToken is null or { AccessToken: null })
            {
                _logger.LogError("LoginByEmailWithoutPassword: failed to get admin token");
                return null;
            }

            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"),
                new KeyValuePair<string, string>("requested_subject", userId),
                new KeyValuePair<string, string>("subject_token", adminToken.AccessToken),
                new KeyValuePair<string, string>("subject_token_type", "urn:ietf:params:oauth:token-type:access_token")
            ]);

            var tokenUrl = CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/token");

            _logger.LogInformation("LoginByEmailWithoutPassword: exchanging token for userId {UserId}", userId);
            var response = await _httpClient.PostAsync(tokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("LoginByEmailWithoutPassword: token exchange failed {StatusCode} - {Body}",
                    response.StatusCode, errorBody);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during passwordless login for email {Email}", email);
            throw;
        }
    }

    private async Task<Result> DeleteUserAsync(string userId, string adminToken, KeycloakAdminOptions options)
    {
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete,
                CombineUrls(options.BaseUrl, "admin/realms", options.Realm, "users", userId))
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) }
            };

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("User {UserId} deleted successfully", userId);
                return Result.Success();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to delete user {UserId}: {StatusCode} - {Error}",
                userId, response.StatusCode, errorContent);

            return Result.Error($"Failed to delete user: {response.StatusCode} - {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user deletion for {UserId}", userId);
            return Result.Error("An error occurred while trying to delete the user");
        }
    }

    public async Task<Result> DeleteUserByEmailAsync(string email, KeycloakAdminOptions options)
    {
        var adminToken = await GetAdminTokenAsync(options);
        if (adminToken is null or { AccessToken: null })
            return Result.Error("Failed to obtain admin token");

        var userId = await GetUserIdByEmailAsync(email, options);
        if (string.IsNullOrEmpty(userId))
            return Result.Error($"User with email {email} not found");

        return await DeleteUserAsync(userId, adminToken.AccessToken, options);
    }

    private string CombineUrls(string baseUrl, params string[] segments)
    {
        var parts = new List<string> { baseUrl.TrimEnd('/') };
        parts.AddRange(segments.Select(s => s.Trim('/')));

        return string.Join("/", parts);
    }
}