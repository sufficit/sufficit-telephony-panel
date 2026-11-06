using System.Net;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>Static pairing screen for a monitoring display: a large code and a poll that signs the display in once staff pair it.</summary>
public static class DisplayPage
{
    public static string Html(string code, string pathBase)
    {
        var safeCode = WebUtility.HtmlEncode(code);
        var basePath = WebUtility.HtmlEncode(pathBase);
        var minutes = (int)DisplayPairing.CodeLifetime.TotalMinutes;
        return $$"""
<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex">
<title>Conectar tela · Painel de telefonia Sufficit</title>
<style>
  :root { color-scheme: dark; }
  body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #0f1720; color: #e8eef5;
         font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; text-align: center; }
  main { padding: 4vh 6vw; max-width: 1100px; }
  h1 { font-size: clamp(24px, 3.2vw, 44px); font-weight: 600; margin: 0 0 2vh; }
  .code { font-size: clamp(64px, 12vw, 180px); font-weight: 700; letter-spacing: .08em; margin: 2vh 0;
          font-variant-numeric: tabular-nums; color: #7cc4ff; }
  p { font-size: clamp(18px, 2vw, 28px); line-height: 1.5; margin: 1vh 0; color: #b8c4d1; }
  .status { margin-top: 4vh; font-size: clamp(16px, 1.6vw, 22px); color: #8a99a8; }
</style>
</head>
<body>
<main>
  <h1>Conectar esta tela de monitoramento ao painel de telefonia</h1>
  <div class="code" aria-label="Código de pareamento">{{safeCode}}</div>
  <p>Peça à equipe Sufficit para informar este código em <strong>Painel de telefonia → Adicionar tela</strong>.</p>
  <p>O código vale por {{minutes}} minutos.</p>
  <div class="status" id="status" role="status">Aguardando o pareamento…</div>
</main>
<script>
  (function () {
    var base = "{{basePath}}";
    var status = document.getElementById("status");
    function poll() {
      fetch(base + "/display/status", { credentials: "same-origin", cache: "no-store" })
        .then(function (r) { return r.ok ? r.json() : { status: "error" }; })
        .then(function (data) {
          if (data.status === "paired") { status.textContent = "Tela conectada. Abrindo o painel…"; location.replace(base + "/display/complete"); return; }
          if (data.status === "expired") { location.replace(base + "/display?new=1"); return; }
          setTimeout(poll, 3000);
        })
        .catch(function () { status.textContent = "Sem conexão. Tentando de novo…"; setTimeout(poll, 10000); });
    }
    setTimeout(poll, 3000);
  })();
</script>
</body>
</html>
""";
    }
}
