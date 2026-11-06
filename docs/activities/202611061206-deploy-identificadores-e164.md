# Deploy — normalização 0800 e prefixo +

Autorização explícita do usuário: deploy. Publicado em 06/11/2026 às 12:06 BRT,
exclusivamente no serviço sufficit-telephony-panel do eveo-apps.

- Código: main 8c165fa60cdcff930289fac26197efe3b79784c5.
- Release nova: /opt/sufficit-telephony-panel/releases/e164-20261106-8c165fa.
- Anterior preservada: /opt/sufficit-telephony-panel/releases/totals-20261031-08fd6a9.
- Artefato criado com dotnet publish -c Release -m:1 a partir de src; não utilizado
  o diretório legado published. Configurações privadas e estado externos preservados.
- Transferência rsync conferida por checksum, sem diferenças; troca atômica via
  deploy/release-panel.py, com trava local e verificação da release anterior esperada.
- Manifesto de backup/rollback:
  /var/backups/sufficit-telephony-panel/e164-20261106-8c165fa/release.json.
- SHA-256 do DLL publicado e ativo:
  325b7a3296743a1fc19eb84cbbf141519543baa477b13be21f7477d271af1ad2.

Validação: suite em Release passou 1285 checks (aviso NU1903 preexistente em
SignalR.Common 10.0.11). Serviço active/running, PID 1857414, NRestarts=0,
início 12:06:15 -03. Health no socket local e em https://panel.sufficit.com.br/health
retornou status running; /board público retornou HTTP 200. Hash ativo idêntico ao
artefato. Normalização e originais já validados com fixture desktop/móvel antes
da publicação; não foi realizada chamada real nem sessão autenticada de operador
na produção nesta etapa. Apenas o painel foi reiniciado.

Rollback: usar o helper oficial com a release anterior como destino e
`e164-20261106-8c165fa` como versão atual esperada; preserva configuração e estado.
O alias SSH curto eveo-apps não existe nesta estação; utilizado o FQDN oficial
com porta 26492 e chave Sufficit.
