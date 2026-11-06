using System.Net;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>Plain answer when Identity refuses "Entrar como": what happened and how to fix it.</summary>
public static class ImpersonationPage
{
    public static string Refused(string refusal, string pathBase)
    {
        var lower = refusal.ToLowerInvariant();
        var hint = lower.Contains("second factor") ? "Entre de novo no painel usando o segundo fator e tente outra vez."
            : lower.Contains("sign in again") ? "Seu login é antigo demais para isso. Saia, entre de novo e tente outra vez."
            : lower.Contains("may not impersonate") ? "Sua conta não tem a permissão identity.users.impersonate."
            : lower.Contains("cannot be impersonated") ? "Administradores, gestores e contas protegidas não podem ser usados."
            : lower.Contains("not found") || lower.Contains("cannot sign in") ? "A pessoa não existe ou está bloqueada para entrar."
            : lower.Contains("not allowed to impersonate") || lower.Contains("not available") ? "O recurso ainda não está habilitado para o painel no Identity."
            : "O Identity recusou o pedido.";
        return $$"""
<!doctype html>
<html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Entrar como · Painel de telefonia</title>
<style>body{margin:0;min-height:100vh;display:grid;place-items:center;background:#0f1720;color:#e8eef5;font-family:system-ui,sans-serif}
main{max-width:640px;padding:32px}h1{font-size:24px}p{color:#b8c4d1;line-height:1.5}code{color:#8a99a8;font-size:13px}a{color:#7cc4ff}</style></head>
<body><main><h1>Não foi possível entrar como essa pessoa</h1><p>{{WebUtility.HtmlEncode(hint)}}</p>
<p><code>{{WebUtility.HtmlEncode(refusal)}}</code></p><p><a href="{{WebUtility.HtmlEncode(pathBase)}}/">Voltar ao painel</a></p></main></body></html>
""";
    }
}
