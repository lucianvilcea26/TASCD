using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Universitate.Shared;

public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Browser login through Keycloak: OpenID Connect (authorization code + PKCE) for the
    /// sign-in itself, then an app cookie that holds the tokens.
    /// </summary>
    public static AuthenticationBuilder AddKeycloakLogin(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(KeycloakOptions.Section);
        var kc = section.Get<KeycloakOptions>()
                 ?? throw new InvalidOperationException("Missing 'Keycloak' section in appsettings.json.");
        services.Configure<KeycloakOptions>(section);
        services.AddHttpClient(TokenRefresher.HttpClientName);
        services.AddSingleton<TokenRefresher>();
        services.AddSingleton<EndedSessions>();

        return services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                // Both apps run on "localhost". Browsers do not separate cookies by port,
                // so each app needs its own cookie name.
                options.Cookie.Name = $"{kc.ClientId}.session";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // lab runs on http
                options.AccessDeniedPath = "/AccessDenied";
                options.Events.OnValidatePrincipal = context =>
                    context.HttpContext.RequestServices.GetRequiredService<TokenRefresher>().ValidateAsync(context);
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = kc.Authority;
                options.ClientId = kc.ClientId;
                options.ClientSecret = kc.ClientSecret;
                options.RequireHttpsMetadata = kc.RequireHttpsMetadata;

                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.UsePkce = true;
                options.SaveTokens = true;                       // keep id/access/refresh tokens in the cookie
                options.GetClaimsFromUserInfoEndpoint = false;   // the ID token already has our claims
                options.MapInboundClaims = false;                // keep Keycloak's claim names
                options.ClaimActions.Clear();                    // keep iat, exp, aud, ... (same claims as after a refresh)

                options.Scope.Clear();
                foreach (var scope in new[] { "openid", "profile", "email" }.Concat(kc.Scopes))
                    options.Scope.Add(scope);

                options.TokenValidationParameters.NameClaimType = TokenClaims.NameClaim;
                options.TokenValidationParameters.RoleClaimType = TokenClaims.RoleClaim;

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                // Use the same claim mapping at login as on every later refresh.
                options.Events.OnTokenValidated = context =>
                {
                    if (context.TokenEndpointResponse?.IdToken is { } idToken)
                        context.Principal = TokenClaims.PrincipalFromIdToken(idToken);
                    return Task.CompletedTask;
                };
            });
    }

    /// <summary>GET /account/login, POST /account/logout and POST /backchannel-logout.</summary>
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapBackChannelLogout();

        app.MapGet("/account/login", (string? returnUrl) =>
                Results.Challenge(
                    new AuthenticationProperties { RedirectUri = LocalOnly(returnUrl) },
                    [OpenIdConnectDefaults.AuthenticationScheme]))
            .AllowAnonymous();

        // Signs out of the app (cookie) AND ends the Keycloak session (OIDC RP-initiated logout).
        app.MapPost("/account/logout", () =>
                Results.SignOut(
                    new AuthenticationProperties { RedirectUri = "/" },
                    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .WithMetadata(new RequireAntiforgeryTokenAttribute());

        return app;
    }

    private static string LocalOnly(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\")
            ? url
            : "/";
}
