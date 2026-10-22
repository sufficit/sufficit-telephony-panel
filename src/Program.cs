using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Sufficit.Blazor.UI;
using Sufficit.Identity.Authorization;
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
// Sessions survive restarts only when the DataProtection key ring is persisted; otherwise memory only.
builder.Services.AddSingleton(sp => new PanelSessions(sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<TimeProvider>(),
    builder.Configuration["Panel:KeyDirectory"] is { Length: > 0 } keyDirectory
        ? new SessionFileStore(builder.Configuration["Panel:SessionDirectory"] is { Length: > 0 } sessions ? sessions
            : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(keyDirectory).TrimEnd('/'))!, "sessions"),
            sp.GetRequiredService<IDataProtectionProvider>(), sp.GetRequiredService<TimeProvider>())
        : null));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient("identity", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.MaxResponseContentBufferSize = 128 * 1024;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("identity-authorization", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.MaxResponseContentBufferSize = 256 * 1024;
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
builder.Services.AddSingleton<PanelDiagnostics>();
builder.Services.AddSingleton<SharedTelemetry>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SharedTelemetry>());
builder.Services.AddSingleton<OperationsPool>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OperationsPool>());
builder.Services.AddScoped<OperationsAccess>();
builder.Services.AddHttpClient("queue-board", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
    client.MaxResponseContentBufferSize = 4 * 1024 * 1024;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton(sp => new QueueBoardReader(builder.Configuration.GetSection("Panel:QueueSources")
    .Get<QueueBoardSource[]>() ?? [], sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<PanelDiagnostics>()));
builder.Services.AddSingleton<QueueDiagnosticsReader>();
builder.Services.AddScoped<QueueBoardAccess>();
builder.Services.AddSingleton<ControllerQueueDistribution>();
builder.Services.AddScoped<QueueDistributionAccess>();
builder.Services.AddSingleton(new SupervisionPrefixPolicy(builder.Configuration
    .GetSection("Panel:SupervisionPrefixes").Get<SupervisionPrefixRule[]>() ?? []));
builder.Services.AddSingleton(new BoardRuleStore(builder.Configuration["Panel:BoardRulesFile"]
    ?? "/var/lib/sufficit-telephony-panel/board-rules.json"));
builder.Services.AddScoped<BoardConfigurationAccess>();
builder.Services.AddSingleton(new BoardInventoryStore(builder.Configuration["Panel:BoardInventoryFile"]
    ?? "/var/lib/sufficit-telephony-panel/known-resources.json"));
builder.Services.AddScoped<BoardInventoryAccess>();
builder.Services.AddSingleton<SupportChat>();
builder.Services.AddScoped<Sufficit.Blazor.UI.Services.ISUIToast, Sufficit.Blazor.UI.Services.SUIToastService>();
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
        if (!(await ctx.HttpContext.RequestServices.GetRequiredService<PanelSessions>()
            .AuthorizationAsync(ctx.Principal!, ctx.HttpContext.RequestAborted)).PanelAllowed) ctx.RejectPrincipal();
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
    // The already-provisioned entitlements scope supplies the legacy Endpoints
    // audience. It does not authorize Panel access: these two permission values
    // are excluded by Identity and resolved separately through current UserInfo.
    foreach (var scope in new[] { "openid", "profile", "roles", "entitlements" }) o.Scope.Add(scope);
    o.ClaimActions.MapUniqueJsonKey("role", "role");
    o.TokenValidationParameters.NameClaimType = "name";
    o.TokenValidationParameters.RoleClaimType = "role";
    o.Events.OnRedirectToIdentityProvider = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        { ctx.HandleResponse(); ctx.Response.StatusCode = 401; }
        return Task.CompletedTask;
    };
    o.Events.OnTokenValidated = async ctx =>
    {
        var log = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Panel.Login");
        var subject = ctx.Principal?.FindFirst("sub")?.Value;
        var token = ctx.TokenEndpointResponse?.AccessToken;
        // Identity authenticated the person; the panel still needs its own permission. Deny here,
        // before creating a session, so the user sees "no access" instead of a failed sign-in.
        var permissions = subject is null || string.IsNullOrEmpty(token) ? CurrentAuthorization.None
            : await ctx.HttpContext.RequestServices.GetRequiredService<PanelSessions>().PermissionsAsync(subject, token, ctx.HttpContext.RequestAborted);
        if (!permissions.PanelAllowed)
        {
            log.LogWarning("Sign-in denied: subject {Subject} has no panel permission", subject ?? "unknown");
            // Keep only what support needs (no tokens) for 10 minutes, so the denial page can open the chat.
            var support = ctx.HttpContext.RequestServices.GetRequiredService<SupportChat>();
            ctx.Response.Cookies.Append(SupportChat.DeniedCookie, support.ProtectDenied(SupportChat.Denied(ctx.Principal, permissions)), new CookieOptions
            { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromMinutes(10), Path = pathBase.Length > 0 ? pathBase : "/" });
            ctx.HandleResponse();
            ctx.Response.Redirect(pathBase + "/?login=denied");
            return;
        }
        log.LogWarning("Sign-in completed for subject {Subject} (manager: {Manager})", subject, permissions.Manager);
    };
    o.Events.OnRemoteFailure = async ctx =>
    {
        ctx.HandleResponse();
        // A failed callback (stale/duplicate state, missing correlation cookie) does not
        // sign anyone out: a session already present in this browser stays active.
        // Tell the user which account remains instead of a misleading "failed" over live data.
        var existing = await ctx.HttpContext.AuthenticateAsync("panel");
        var subject = existing.Succeeded ? existing.Principal?.FindFirst("sub")?.Value : null;
        ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Panel.Login")
            .LogWarning("Sign-in callback failed ({Failure}); existing session kept for subject {Subject}",
                ctx.Failure?.GetType().Name ?? "unknown", subject ?? "none");
        ctx.Response.Redirect(pathBase + (subject is null ? "/?login=failed" : "/?login=kept"));
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
// Support chat context for a denied sign-in: read from the short-lived protected cookie, never from query input.
app.MapGet("/support/denied", (HttpContext ctx, SupportChat support) =>
{
    if (!support.Enabled) return Results.NotFound();
    var request = support.UnprotectDenied(ctx.Request.Cookies[SupportChat.DeniedCookie]);
    return request is null ? Results.NotFound()
        : Results.Ok(support.Payload(request, ctx.Request.Query["locale"] == "pt-BR" ? "pt-BR" : "en", ctx.Request.Query["dark"] == "true"));
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
