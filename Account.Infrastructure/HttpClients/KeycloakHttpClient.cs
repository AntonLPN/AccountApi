using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Account.Domain.Models;
using Account.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.HttpClients;

public class KeycloakHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakHttpClient> _logger;

    public KeycloakHttpClient(HttpClient httpClient, ILogger<KeycloakHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?>  RegisterUserAsync(string userName, string email,
        string password,
        string adminToken,
        KeycloakAdminOptions options)
    {
        try
        {
            var newUser = new
            {
                username = userName,
                email = email,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new { type = "password", value = password, temporary = false }
                }
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                CombineUrls(options.BaseUrl, "admin/realms", options.Realm, "users"))
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", adminToken) },
                Content = new StringContent(JsonSerializer.Serialize(newUser), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                // http://localhost:8080/admin/realms/account-realm/users/uuid
                var locationHeader = response.Headers.Location?.ToString();
                var userId = locationHeader?.Split('/').Last();

                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation($"User {userName} registered successfully with ID: {userId}");
                    return userId;
                }

                _logger.LogWarning($"User {userName} created but could not extract ID from Location header");
                return null;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Failed to register user {userName}: {response.StatusCode} - {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during user registration for {userName}");
            return null;
        }
    }
    public async Task<string?> GetAdminTokenAsync(KeycloakAdminOptions options)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });
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
            return tokenResponse?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining admin token");
            return null;
        }
    }

    public async Task<TokenResponse?> LoginAsync(string userName, string password, KeycloakAdminOptions options)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", userName),
                new KeyValuePair<string, string>("password", password)
            });

            var url = CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/token");
            var response = await _httpClient.PostAsync(
                url,
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Login failed for user {userName}: {response.StatusCode} - {errorBody}");
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during login for user {userName}");
            return null;
        }
    }
    
    
    private string CombineUrls(string baseUrl, params string[] segments)
    {
        // Берем базовый URL без слэша на конце
        var parts = new List<string> { baseUrl.TrimEnd('/') };
    
        // Очищаем каждый сегмент от слэшей по краям, чтобы не было двойных
        parts.AddRange(segments.Select(s => s.Trim('/')));
    
        // Склеиваем всё ровно через один слэш
        return string.Join("/", parts);
    }
    
    // private string CombineUrls(string baseUrl, params string[] segments)
    // {
    //     var uri = new Uri(baseUrl.TrimEnd('/') + '/');
    //     foreach (var segment in segments)
    //     {
    //         uri = new Uri(uri, segment.TrimStart('/'));
    //     }
    //
    //     return uri.ToString();
    // }
}