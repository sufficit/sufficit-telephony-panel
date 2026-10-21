# Sessões do painel sobrevivendo a deploy (etapa 9)

Aprovado pelo usuário em 21/10 ("sim"). Motivação: o journal mostrou o `teste@` entrando 5 vezes em
1 hora, uma por deploy, porque `PanelSessions` guardava tickets só em memória.

- `Security/SessionFileStore.cs`: um arquivo por sessão (`<handle de 32 hex>.session`), cifrado com o
  key ring persistido do DataProtection (propósito `panel-session-ticket-v1`), escrita atômica, diretório
  0700 e arquivos 0600, limite de 200 sessões, expiração do próprio ticket e corte absoluto de 9 h.
  O handle nunca vira caminho sem validar `^[0-9a-f]{32}$`.
- `PanelSessions`: memória continua sendo o primeiro cache; o arquivo restaura a sessão uma vez depois de
  reiniciar. Sair, expirar ou perder a permissão apaga o arquivo. A releitura da permissão no Identity a
  cada 60 s continua igual.
- Ativo só com `Panel:KeyDirectory`; em produção grava em `/var/lib/sufficit-telephony-panel/sessions`.
- Testes: 16 checks novos (`SessionStoreChecks`). Cobrem reinício, cifragem, permissão do arquivo,
  remoção, adulteração, expiração, handle malicioso, limite e key ring diferente. Total: 1022 PASS.

Publicado como `sessions-20261021-56671bd` (anterior `customer-peers-20261021-acfd344` preservado), v0.15.0,
appsettings idêntico, health 200, diretório criado `sufftelpanel` 0700, sem warnings. Este deploy ainda
derrubou as sessões em memória; a partir dele, os próximos preservam.

Rollback: release anterior; o diretório `sessions` pode ser apagado com segurança (só derruba sessões).
