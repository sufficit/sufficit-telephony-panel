# Deploy da normalização de origem nacional

Autorização explícita: “publique”. Publicado em 06/11/2026 às 12:15:48 BRT,
exclusivamente no serviço sufficit-telephony-panel do eveo-apps.

- Código: main 46686bcfe145ab5595cf84b4a07bba14808d7ffd.
- Release ativa: /opt/sufficit-telephony-panel/releases/national-20261106-46686bc.
- Release anterior preservada: /opt/sufficit-telephony-panel/releases/e164-20261106-8c165fa.
- Artefato novo via dotnet publish src/Sufficit.Telephony.Panel.csproj -c Release
  -o artifacts/national-20261106-46686bc -m:1.
- Transferência rsync conferida por checksum, sem diferenças.
- Ativação pelo deploy/release-panel.py sob flock no servidor, com release
  anterior esperada; configurações privadas e estado externos preservados.
- Manifesto: /var/backups/sufficit-telephony-panel/national-20261106-46686bc/release.json.
- SHA-256 local/ativo: 42cede65ba216614fe39cc7149c3a1098fe96e143f45ec5079f83ee517e57911.

Validação: suíte Release passou 1310 checks, com aviso NU1903 preexistente em
SignalR.Common 10.0.11. Serviço active/running, PID 1868412, NRestarts=0;
health por socket e https://panel.sufficit.com.br/health retornaram running;
/board público retornou HTTP 200. Hash da DLL ativa idêntico ao artefato.
Sem chamada real ou sessão autenticada de operador nesta validação.

Rollback: usar deploy/release-panel.py sob a mesma trava com destino
`e164-20261106-8c165fa` e atual esperada `national-20261106-46686bc`.
Somente o painel foi reiniciado. Registro enviado à main após verificação.
