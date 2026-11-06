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
        : null, sp.GetRequiredService<ILogger<PanelSessions>>()));
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
builder.Services.AddSingleton<DisplayPairing>();
builder.Services.AddSingleton<PanelImpersonation>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Sufficit.Blazor.UI.Services.ISUIToast, Sufficit.Blazor.UI.Services.SUIToastService>();
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = "panel";
    o.DefaultChallengeScheme = "oidc";
}).AddCookie("panel", o =>
{
    // __Host-: host-only, Path=/, no Domain. A stale same-name cookie from an older deployment (domain-wide or
    // another path) was sent first and hid the fresh session, so sign-in looped (2026-11-06).
    o.Cookie.Name = "__Host-Sufficit.Telephony.Panel";
    o.Cookie.Path = "/";
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
{
    var display = app.Services.GetRequiredService<DisplayPairing>();
    app.Services.GetRequiredService<PanelSessions>().Refresher = display.RefreshAsync;
}
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
app.Use(async (ctx, next) =>
{
    if (AutoSignIn.Applies(ctx.Request, ctx.User.Identity?.IsAuthenticated == true))
    {
        // A sign-in that just finished and still lands here unauthenticated would loop until Identity rate-limits
        // (2026-11-06). Stop at the landing page instead and keep the evidence in the log.
        if (ctx.Request.Cookies.ContainsKey(AutoSignIn.AttemptCookie))
        {
            var sent = ctx.Request.Headers.Cookie.SelectMany(h => (h ?? "").Split(';')).Select(c => c.Trim())
                .Where(c => c.StartsWith("__Host-Sufficit.Telephony.Panel", StringComparison.Ordinal)
                    || c.StartsWith("__Secure-Sufficit.Telephony.Panel", StringComparison.Ordinal))
                .Select(c => c.Split('=')[0]).ToArray();
            ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Panel.Login")
                .LogWarning("Automatic sign-in stopped: the previous attempt did not produce a usable session (panel cookies sent: {Cookies})",
                    sent.Length == 0 ? "none" : string.Join(", ", sent));
            ctx.Response.Redirect(pathBase + "/?login=failed");
            return;
        }
        ctx.Response.Cookies.Append(AutoSignIn.AttemptCookie, "1", new CookieOptions
        { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromSeconds(30), Path = pathBase.Length > 0 ? pathBase : "/" });
        ctx.Response.Redirect(AutoSignIn.LoginPath(ctx.Request, pathBase)); return;
    }
    // A request that is authenticated clears the attempt marker, so the next expiry can sign in again.
    if (ctx.User.Identity?.IsAuthenticated == true && ctx.Request.Cookies.ContainsKey(AutoSignIn.AttemptCookie))
        ctx.Response.Cookies.Delete(AutoSignIn.AttemptCookie, new CookieOptions { Path = pathBase.Length > 0 ? pathBase : "/", Secure = true });
    await next();
});
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
    // ?signedout=1 keeps the landing page instead of starting a new automatic sign-in.
    return Results.LocalRedirect(pathBase + "/?signedout=1");
});
app.MapGet("/api/fleet", async (HttpContext ctx, PanelSessions sessions, FleetMetrics metrics) =>
    await sessions.IsManagerAsync(ctx.User, ctx.RequestAborted)
        ? Results.Ok(await metrics.ReadAsync(ctx.RequestAborted)) : Results.StatusCode(403))
    .RequireAuthorization("manager");
// "Entrar como": a manager opens the panel as a real customer person (Identity impersonation token, 30 minutes).
app.MapGet("/impersonate", async (HttpContext ctx, PanelSessions sessions, PanelImpersonation impersonation) =>
{
    var log = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Panel.Impersonation");
    IResult Explain(string reason, string message)
    {
        log.LogWarning("Impersonation not started for {Actor}: {Reason}", ctx.User.FindFirst("sub")?.Value ?? "anonymous", reason);
        return Results.Content(ImpersonationPage.Refused(message, pathBase), "text/html; charset=utf-8", statusCode: 403);
    }
    if (ctx.User.Identity?.IsAuthenticated != true)
        return Results.Redirect(pathBase + "/login?returnUrl=" + Uri.EscapeDataString(pathBase + ctx.Request.Path + ctx.Request.QueryString));
    if (PanelImpersonation.IsImpersonating(ctx.User))
        return Explain("already_impersonating", "already: Volte para a sua conta antes de entrar como outra pessoa.");
    if (!(await sessions.AuthorizationAsync(ctx.User, ctx.RequestAborted)).Manager)
        return Explain("not_manager", "not_manager: Só gestores da Sufficit podem entrar como outra pessoa.");
    var target = ctx.Request.Query["userid"].ToString();
    if (!Guid.TryParse(target, out _)) return Explain("invalid_target", "invalid_request: Pessoa não informada.");
    if (string.Equals(target, ctx.User.FindFirst("sub")?.Value, StringComparison.OrdinalIgnoreCase))
        return Explain("self", "self: Essa é a sua própria conta. Escolha uma pessoa do cliente.");
    var context = Guid.TryParse(ctx.Request.Query["contextid"], out var c) ? c : Guid.Empty;
    var managerToken = await sessions.GetAccessTokenAsync(ctx.User, ctx.RequestAborted);
    var currentKey = ctx.User.FindFirst("panel_session")?.Value;
    if (managerToken is null || currentKey is null) return Results.Redirect(pathBase + "/login?returnUrl=" + Uri.EscapeDataString(pathBase + ctx.Request.Path + ctx.Request.QueryString));
    var (issued, refusal) = await impersonation.ExchangeAsync(managerToken, target, ctx.RequestAborted);
    if (issued is null)
        return Results.Content(ImpersonationPage.Refused(refusal ?? "", pathBase), "text/html; charset=utf-8", statusCode: 403);
    // Reproduce first, then sign in: a person without panel access would be signed out on the next request.
    var personAccess = await sessions.PermissionsAsync(target, issued.AccessToken, ctx.RequestAborted);
    if (!personAccess.PanelAllowed)
        return Explain("person_without_panel", $"no_panel: {issued.Name ?? "Essa pessoa"} não tem acesso ao painel. É isto que ela vê hoje: acesso negado. Libere o acesso na tela do cliente para que ela possa usar o painel.");
    if (context != Guid.Empty && !personAccess.CanRead(context))
        return Explain("person_without_context", $"no_context: {issued.Name ?? "Essa pessoa"} tem acesso ao painel, mas não a este cliente.");
    // Signing in over the open session reuses its key, so keep a copy of the manager session under its own key
    // for "Voltar para minha conta".
    var current = await sessions.RetrieveAsync(currentKey);
    if (current is null) return Explain("manager_session_missing", "invalid_request: Sua sessão expirou. Entre de novo no painel.");
    var returnKey = await sessions.StoreAsync(new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(
        new System.Security.Claims.ClaimsIdentity(current.Principal.Claims.Where(c => c.Type != "panel_session"),
            current.Principal.Identity?.AuthenticationType, "name", "role")), current.Properties.Clone(), current.AuthenticationScheme));
    var identity = new System.Security.Claims.ClaimsIdentity("impersonation", "name", "role");
    identity.AddClaim(new System.Security.Claims.Claim("sub", target));
    identity.AddClaim(new System.Security.Claims.Claim("name", issued.Name ?? target));
    identity.AddClaim(new System.Security.Claims.Claim(PanelImpersonation.ActorClaim, ctx.User.FindFirst("sub")?.Value ?? ""));
    identity.AddClaim(new System.Security.Claims.Claim(PanelImpersonation.ActorNameClaim, ctx.User.FindFirst("name")?.Value ?? ""));
    identity.AddClaim(new System.Security.Claims.Claim(PanelImpersonation.ReturnSessionClaim, returnKey));
    if (context != Guid.Empty) identity.AddClaim(new System.Security.Claims.Claim(PanelImpersonation.ContextClaim, context.ToString("N")));
    var properties = new AuthenticationProperties { ExpiresUtc = issued.ExpiresAt };
    properties.StoreTokens([
        new AuthenticationToken { Name = "access_token", Value = issued.AccessToken },
        new AuthenticationToken { Name = "expires_at", Value = issued.ExpiresAt.ToString("o", System.Globalization.CultureInfo.InvariantCulture) },
    ]);
    ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Panel.Impersonation")
        .LogWarning("Manager {Actor} entered the panel as {Target}", ctx.User.FindFirst("sub")?.Value, target);
    await ctx.SignInAsync("panel", new System.Security.Claims.ClaimsPrincipal(identity), properties);
    // The person's own permissions decide what loads; a person without panel access lands on the denial screen.
    return Results.Redirect(pathBase + (context != Guid.Empty ? "/board?contextid=" + context.ToString("N") : "/"));
});
app.MapPost("/impersonate/exit", async (HttpContext ctx, PanelSessions sessions) =>
{
    await ctx.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(ctx);
    var returnKey = ctx.User.FindFirst(PanelImpersonation.ReturnSessionClaim)?.Value;
    var manager = returnKey is null ? null : await sessions.RetrieveAsync(returnKey);
    if (manager is null) { await ctx.SignOutAsync("panel"); return Results.LocalRedirect(pathBase + "/?signedout=1"); }
    var identity = new System.Security.Claims.ClaimsIdentity(manager.Principal.Claims.Where(claim => claim.Type != "panel_session"),
        manager.Principal.Identity?.AuthenticationType, "name", "role");
    // Renews the current (impersonated) key with the manager ticket; the saved copy is no longer needed.
    await ctx.SignInAsync("panel", new System.Security.Claims.ClaimsPrincipal(identity), manager.Properties.Clone());
    await sessions.RemoveAsync(returnKey!);
    return Results.LocalRedirect(pathBase + "/");
});
// Monitoring display pairing: /display shows a code, staff pair it from the customer's panel access screen (Blazor), the display signs in.
app.MapGet("/display", async (HttpContext ctx, DisplayPairing display) =>
{
    var cookiePath = pathBase.Length > 0 ? pathBase : "/";
    if (ctx.User.Identity?.IsAuthenticated == true && ctx.Request.Query["new"] != "1")
        return Results.Redirect(pathBase + "/board");
    string id, code;
    try { (id, code) = display.Begin(); }
    catch (InvalidOperationException) { return Results.StatusCode(503); }
    ctx.Response.Cookies.Append(DisplayPairing.Cookie, id, new CookieOptions
    { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, MaxAge = DisplayPairing.CodeLifetime, Path = cookiePath });
    await Task.CompletedTask;
    return Results.Content(DisplayPage.Html(DisplayPairing.Display(code), pathBase), "text/html; charset=utf-8");
}).AllowAnonymous();
app.MapGet("/display/status", (HttpContext ctx, DisplayPairing display) => Results.Ok(new { status = display.Status(ctx.Request.Cookies[DisplayPairing.Cookie]) }))
    .AllowAnonymous();
app.MapGet("/display/complete", async (HttpContext ctx, DisplayPairing display) =>
{
    var paired = display.Take(ctx.Request.Cookies[DisplayPairing.Cookie]);
    ctx.Response.Cookies.Delete(DisplayPairing.Cookie, new CookieOptions { Path = pathBase.Length > 0 ? pathBase : "/", Secure = true });
    if (paired is null) return Results.Redirect(pathBase + "/display");
    var identity = new System.Security.Claims.ClaimsIdentity("display", "name", "role");
    identity.AddClaim(new System.Security.Claims.Claim("sub", paired.Subject));
    identity.AddClaim(new System.Security.Claims.Claim("name", paired.Name));
    identity.AddClaim(new System.Security.Claims.Claim("panel_kind", "display"));
    var properties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.Add(DisplayPairing.SessionLifetime) };
    properties.StoreTokens([
        new AuthenticationToken { Name = "access_token", Value = paired.AccessToken },
        new AuthenticationToken { Name = "refresh_token", Value = paired.RefreshToken },
        new AuthenticationToken { Name = "expires_at", Value = paired.ExpiresAt.ToString("o", System.Globalization.CultureInfo.InvariantCulture) },
    ]);
    await ctx.SignInAsync("panel", new System.Security.Claims.ClaimsPrincipal(identity), properties);
    return Results.Redirect(pathBase + "/board?contextid=" + paired.Context.ToString("N"));
}).AllowAnonymous();
// Called by the Blazor panel access screen (server side). The enrollment code is the authorization.
app.MapPost("/api/display/pair", async (DisplayPairRequest request, DisplayPairing display, CancellationToken ct) =>
    await display.PairAsync(request.Code, request.EnrollmentCode, ct) switch
    {
        DisplayPairing.PairResult.Paired => Results.Ok(new { status = "paired" }),
        DisplayPairing.PairResult.UnknownCode => Results.NotFound(new { status = "unknown_code" }),
        DisplayPairing.PairResult.NotDisplaySafe => Results.UnprocessableEntity(new { status = "not_display_safe" }),
        _ => Results.BadRequest(new { status = "invalid_enrollment" }),
    }).AllowAnonymous().DisableAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public sealed record DisplayPairRequest(string? Code, string? EnrollmentCode);
