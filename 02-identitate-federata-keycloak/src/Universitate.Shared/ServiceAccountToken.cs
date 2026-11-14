using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Universitate.Shared;

/// <summary>
/// Gets and caches an access token for the app's own service account
/// (OAuth 2.0 client credentials grant). No user is involved: the token says
/// "this is the Secretariat application" or "this is the Biblioteca application".
/// </summary>
public sealed class ServiceAccountTokenCache(IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> options)
{
    public const string HttpClientName = "keycloak-client-credentials";
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAsync(CancellationToken ct)
    {
        if (_token is not null && _expiresAt - DateTimeOffset.UtcNow > TimeSpan.FromSeconds(30))
            return _token;

        await _lock.WaitAsync(ct);
        try
        {
            if (_token is not null && _expiresAt - DateTimeOffset.UtcNow > TimeSpan.FromSeconds(30))
                return _token;

            var kc = options.Value;
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = kc.ClientId,
                ["client_secret"] = kc.ClientSecret,
            };
            if (kc.ServiceAccountScopes.Length > 0)
                form["scope"] = string.Join(' ', kc.ServiceAccountScopes);

            using var response = await httpClientFactory.CreateClient(HttpClientName).PostAsync(
                kc.TokenEndpoint, new FormUrlEncodedContent(form), ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Keycloak refused the client credentials for '{kc.ClientId}' ({(int)response.StatusCode}): " +
                    await response.Content.ReadAsStringAsync(ct));

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            _token = json.GetProperty("access_token").GetString()!;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32());
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _token = null;
}

/// <summary>Adds "Authorization: Bearer &lt;service account token&gt;" to every outgoing request.</summary>
public sealed class ServiceAccountTokenHandler(ServiceAccountTokenCache cache) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await cache.GetAsync(ct));
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            cache.Invalidate(); // next call fetches a fresh token
        return response;
    }
}

public static class ServiceAccountTokenExtensions
{
    /// <summary>Makes this HttpClient call other services as the app's own service account.</summary>
    public static IHttpClientBuilder AddServiceAccountToken(this IHttpClientBuilder builder)
    {
        builder.Services.AddHttpClient(ServiceAccountTokenCache.HttpClientName);
        builder.Services.TryAddSingleton<ServiceAccountTokenCache>();
        builder.Services.TryAddTransient<ServiceAccountTokenHandler>();
        return builder.AddHttpMessageHandler<ServiceAccountTokenHandler>();
    }
}
