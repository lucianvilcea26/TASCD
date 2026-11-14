namespace Universitate.Shared;

/// <summary>
/// The "Keycloak" section of appsettings.json. Each app is one OIDC client in the realm.
/// The same confidential client is used for user login (authorization code + PKCE)
/// and for the app's own service account (client credentials).
/// </summary>
public sealed class KeycloakOptions
{
    public const string Section = "Keycloak";

    /// <summary>Realm URL, e.g. http://localhost:8080/realms/universitate</summary>
    public string Authority { get; set; } = "";

    public string ClientId { get; set; } = "";

    /// <summary>Demo value only. In real projects keep it in user-secrets or a vault.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>False only because the lab runs Keycloak on plain HTTP.</summary>
    public bool RequireHttpsMetadata { get; set; }

    /// <summary>Extra scopes requested at login, on top of openid, profile and email.</summary>
    public string[] Scopes { get; set; } = [];

    /// <summary>
    /// Scopes requested for the app's own service-account token (client credentials).
    /// The Secretariat asks for "kc-admin-api": since Keycloak 26.6 an Admin API token must
    /// carry that scope's audience, not just the realm-management roles.
    /// </summary>
    public string[] ServiceAccountScopes { get; set; } = [];

    public string TokenEndpoint => $"{Authority.TrimEnd('/')}/protocol/openid-connect/token";
}
