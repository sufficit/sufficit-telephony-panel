# Canais IAX2 viravam um card por chamada

Relato do usuário (21/10): a mesa mostrava dezenas de cards `IAX2/gateway-phonevox-<n>`,
`IAX2/dundi-internal-<n>` como recursos diferentes, todos "Já visto · sem estado atual".

## Causa
`BoardRuleEngine.EndpointKey` só removia o sufixo de chamada no formato SIP/PJSIP
(`-` + 8 hex). No Asterisk o canal IAX2 é `IAX2/<peer ou host:porta>-<callno>`, com número
de chamada decimal (1..32767). Cada chamada IAX2 gerava um endpoint novo, e o inventário
de "recursos lembrados" o guardava para sempre.

## Correção (commit `b8d9c5c`)
- Canais (`row.Kind == "call"`) IAX2 perdem o sufixo decimal de 1 a 5 dígitos; o sufixo de 8 hex
  continua aceito. A identidade do peer (`peer`) nunca é truncada (`SIP/in-dtr-67` e
  `IAX2/trunk-2` como peer seguem intactos).
- 4 checks novos; 975 checks PASS; operator-board, board-inventory, board-settings e call-grouping
  no navegador PASS.

## Produção (eveo-apps)
- Release `iax2-group-20261021-b8d9c5c` (anterior `board-fill-20261021-f7802ac` preservado),
  DLL `6c94ff3e00c59eae158c105892e0cef4142f7fe6aed4cdcd5081e78cda3ceef2`, appsettings idêntico, health 200.
- Com o serviço parado, removidas do inventário 75 entradas fantasmas cujas chaves eram todas
  `IAX2/<x>-<dígitos>`: gateway-phonevox 29, `143.208.224.21:26497` 23 e dundi-internal 23.
  Ficaram 2255 entradas. Backup em `/var/backups/sufficit-telephony-panel/known-resources-before-iax2-20261021.json`.
  Restaurar: parar o serviço, copiar o backup de volta (dono `sufftelpanel`, modo 0600) e iniciar.
- Daqui em diante essas chamadas aparecem agrupadas num card por peer (`IAX2/gateway-phonevox`).

Observação: `PJSIP/wss-<hex>` e `PJSIP/xep-<hex>` são nomes reais de endpoint (WebRTC), não
sufixo de chamada; não foram alterados.
