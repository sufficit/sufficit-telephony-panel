using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Sufficit.Blazor.UI;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Security;
using Sufficit.Telephony.Panel.Monitoring;
using Sufficit.Telephony.Panel.Operations;

var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration["Panel:ConfigurationFile"] is { Length: > 0 } config)
    builder.Configuration.AddJsonFile(config, optional: false, reloadOnChange: false);
var pathBase = builder.Configuration["Panel:PathBase"] ?? "";
builder.Services.AddRazorComponents().AddInteractiveServerComponents(o =>
{
    o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(1);
    o.DisconnectedCircuitMaxRetained = 100;
    o.MaxBufferedUnacknowledgedRenderBatches = 5;
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSufficitUI();
builder.Services.AddScoped<Sufficit.Telephony.Panel.Localization.PanelText>();
builder.Services.AddDataProtection().SetApplicationName("sufficit-telephony-panel");
if (builder.Configuration["Panel:KeyDirectory"] is { Length: > 0 } keys)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys));
builder.Services.AddSingleton<PanelSessions>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient("identity", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.MaxResponseContentBufferSize = 128 * 1024;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("metrics", client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
    ConnectCallback = async (context, cancellation) =>
    {
        // Connect through the VPN while keeping the original HTTPS hostname for certificate validation.
        var socket = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse("172.19.2.136"), 443), cancellation);
            return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
        }
        catch { socket.Dispose(); throw; }
    }
});
builder.Services.AddSingleton<FleetMetrics>();
builder.Services.AddScoped<PanelAccess>();
builder.Services.AddHttpClient("operations", client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
    client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<OperationsApi>();
builder.Services.AddSingleton<SharedTelemetry>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SharedTelemetry>());
builder.Services.AddSingleton<OperationsPool>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OperationsPool>());
builder.Services.AddScoped<OperationsAccess>();
builder.Services.AddSingleton(new BoardRuleStore(builder.Configuration["Panel:BoardRulesFile"]
    ?? "/var/lib/sufficit-telephony-panel/board-rules.json"));
builder.Services.AddScoped<BoardConfigurationAccess>();
builder.Services.AddSingleton(new BoardInventoryStore(builder.Configuration["Panel:BoardInventoryFile"]
    ?? "/var/lib/sufficit-telephony-panel/known-resources.json"));
builder.Services.AddScoped<BoardInventoryAccess>();
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = "panel";
    o.DefaultChallengeScheme = "oidc";
}).AddCookie("panel", o =>
{
    o.Cookie.Name = "__Secure-Sufficit.Telephony.Panel";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = false;
    o.Events.OnValidatePrincipal = async ctx =>
    {
        if (!await ctx.HttpContext.RequestServices.GetRequiredService<PanelSessions>()
            .IsManagerAsync(ctx.Principal!, ctx.HttpContext.RequestAborted)) ctx.RejectPrincipal();
    };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
}).AddOpenIdConnect("oidc", o =>
{
    o.Authority = "https://identity.sufficit.com.br/";
    o.ClientId = "sufficit-telephony-panel";
    o.ClientSecret = builder.Configuration["Identity:ClientSecret"];
    o.SignInScheme = "panel";
    o.ResponseType = "code";
    o.UsePkce = true;
    // Keep the already registered callback while the dedicated UI moves to root.
    if (pathBase.Length == 0) o.CallbackPath = "/telephony-panel/signin-oidc";
    // The current Identity manifest grants authorization/token but not PAR.
    // Explicitly use the supported authorization-code + PKCE flow; do not request
    // unrelated administrative permissions or modify the shared Identity service.
    o.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    o.SaveTokens = true;
    o.MapInboundClaims = false;
    o.GetClaimsFromUserInfoEndpoint = true;
    o.Scope.Clear();
    foreach (var scope in new[] { "openid", "profile", "roles" }) o.Scope.Add(scope);
    // Enable only after the dedicated client manifest has been applied in Identity.
    var requestEntitlements = builder.Configuration.GetValue<bool>("Panel:RequestEntitlements");
    if (requestEntitlements) o.Scope.Add("entitlements");
    o.Events.OnTokenResponseReceived = ctx =>
    {
        var scopes = ctx.TokenEndpointResponse?.Scope;
        ctx.Properties!.Items["panel_entitlements"] = (scopes is null ? requestEntitlements
            : scopes.Split(' ').Any(s => s is "entitlements" or "directives")) ? "true" : "false";
        return Task.CompletedTask;
    };
    o.ClaimActions.MapUniqueJsonKey("role", "role");
    o.TokenValidationParameters.NameClaimType = "name";
    o.TokenValidationParameters.RoleClaimType = "role";
    o.Events.OnRedirectToIdentityProvider = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        { ctx.HandleResponse(); ctx.Response.StatusCode = 401; }
        return Task.CompletedTask;
    };
    o.Events.OnRemoteFailure = ctx =>
    {
        ctx.HandleResponse();
        ctx.Response.Redirect(pathBase + "/?login=failed");
        return Task.CompletedTask;
    };
});
builder.Services.AddOptions<CookieAuthenticationOptions>("panel")
    .Configure<PanelSessions>((o, store) => o.SessionStore = store);
builder.Services.AddAuthorization(o => o.AddPolicy("manager", p => p.RequireAuthenticatedUser().RequireRole("manager")));
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    o.KnownProxies.Add(IPAddress.Loopback);
});
if (builder.Configuration["Panel:Socket"] is { Length: > 0 } socket)
    builder.WebHost.ConfigureKestrel(o => o.ListenUnixSocket(socket));
var app = builder.Build();
app.UseForwardedHeaders();
if (pathBase.Length > 0) app.UsePathBase(pathBase);
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Cache-Control"] = "no-store";
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
// Keep bookmarked Portuguese routes compatible without using them as canonical URLs.
app.MapGet("/mesa", (HttpContext ctx) => Results.Redirect(ctx.Request.PathBase + "/board" + ctx.Request.QueryString, permanent: true));
app.MapGet("/health", () => Results.Ok(new { service = "sufficit-telephony-panel", status = "running" }));
app.MapGet("/login", async (HttpContext ctx) =>
{
    try
    {
        var returnUrl = ctx.Request.Query["returnUrl"].ToString();
        var safeReturn = PanelReturnUrl.Validate(returnUrl, pathBase);
        await ctx.ChallengeAsync("oidc", new AuthenticationProperties { RedirectUri = safeReturn });
    }
    catch (Exception error) when (!ctx.Response.HasStarted && !ctx.RequestAborted.IsCancellationRequested)
    {
        // Discovery/PAR can fail before a remote callback (e.g. an unprovisioned
        // client). Fail closed with a useful screen, not an unhandled blank 500.
        ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Panel.Login")
            .LogWarning("Identity challenge unavailable ({ErrorType})", error.GetType().Name);
        ctx.Response.Clear();
        ctx.Response.Headers["Cache-Control"] = "no-store";
        ctx.Response.Redirect(pathBase + "/?login=failed");
    }
});
app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(ctx);
    await ctx.SignOutAsync("panel");
    return Results.LocalRedirect(pathBase + "/");
});
app.MapGet("/api/fleet", async (HttpContext ctx, PanelSessions sessions, FleetMetrics metrics) =>
    await sessions.IsManagerAsync(ctx.User, ctx.RequestAborted)
        ? Results.Ok(await metrics.ReadAsync(ctx.RequestAborted)) : Results.StatusCode(403))
    .RequireAuthorization("manager");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
