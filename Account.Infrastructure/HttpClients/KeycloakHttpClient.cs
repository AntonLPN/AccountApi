using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Models;
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

            var response = await _httpClient.PostAsync(
                CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/token"),
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get admin token: {response.StatusCode}");
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

            var response = await _httpClient.PostAsync(
                CombineUrls(options.BaseUrl, "realms", options.Realm, "protocol/openid-connect/token"),
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Login failed for user {userName}: {response.StatusCode}");
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

    public async Task<bool> RegisterUserAsync(string userName, string email,
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
                _logger.LogInformation($"User {userName} registered successfully");
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Failed to register user {userName}: {response.StatusCode} - {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during user registration for {userName}");
            return false;
        }
    }

    private string CombineUrls(string baseUrl, params string[] segments)
    {
        var uri = new Uri(baseUrl.TrimEnd('/') + '/');
        foreach (var segment in segments)
        {
            uri = new Uri(uri, segment.TrimStart('/'));
        }

        return uri.ToString();
    }
}