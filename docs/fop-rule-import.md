# Catálogo FOP incluído na mesa

Importação pontual autorizada em 10/09/2026. Fonte: método `writeTrunks` de
`sufficit-endpoints/src/Controllers/Telephony/FlashOperatorPanelController.cs`.
Catálogo editável/versionável: [fop-board-rules.json](../deploy/fop-board-rules.json).

Foram incluídos 14 grupos, na ordem original: IN-GTGI, IN-DATORA, IN-TVN, IN-TIP,
IN-BRDID, IN-DIRECTCALL, EXT-FLUX, MIXED-ALGAR, WHATSAPP, INBOUND, ROTABRASIL,
MIXED, OUTBOUND e GATEWAYS. Total: 130 padrões (INBOUND possui 45).

## Adaptações explícitas

- SIP, PJSIP e IAX2 preservados; nenhum alias equivalente foi inventado.
- Marcadores de fallback `SIP/in-` e `SIP/out` tornados prefixos explícitos
  `SIP/in-*` e `SIP/out*`; demais identificadores são exatos no novo motor.
- Duplicata de `SIP/in-fonetalk` dentro de INBOUND removida sem mudar cobertura.
- `SIP/mixed-marvin-upbytes` consta em INBOUND e MIXED na fonte. Ambas as listas
  foram preservadas, mas no motor da mesa a primeira regra vence: INBOUND.
  Isto não é uma reprodução da eventual exibição duplicada do FOP.
- A capacidade passou de 32 para 64 padrões por regra, para não truncar INBOUND.
  Limite global permanece 512. IDs/revisão UUIDv7 sem hífens.
- Nenhum usuário, senha, contexto de discagem, fila dinâmica ou ramal de cliente
  foi importado. Esses itens não são regras estáticas de classificação de tronco.
- Mostrar recursos sem regra permanece ligado. Nenhuma regra de ocultação foi
  inventada. Todos os grupos se aplicam aos servidores com eventos correspondentes.

Use Configurar mesa para alterar o catálogo ativo. Os grupos só aparecem quando
existem recursos autorizados correspondentes; criar a regra não fabrica eventos.

## Ferramenta administrativa

`tools/BoardRules` extrai somente a região de troncos, valida com o contrato do
painel, mescla preservando regras anteriores e recusa conflitos de nome/conteúdo.
Reimportação do mesmo catálogo é idempotente. `verify` relê via BoardRuleStore.

```sh
dotnet run --project tools/BoardRules -- extract /caminho/FlashOperatorPanelController.cs
dotnet run --project tools/BoardRules -- verify /caminho/board-rules.json
```

`apply <catalog> <absolute-target> <expected-revision|absent>` é **offline**:
parar somente o serviço do painel antes de usá-lo e reiniciá-lo mesmo em erro.
Não aplicar contra um writer ativo: o store possui cache em memória. No deploy
feito, o preflight confirmou arquivo ausente, e `absent` foi exigido novamente na
gravação. Destino privado fora dos releases:
`/var/lib/sufficit-telephony-panel/board-rules.json`, dono sufftelpanel, modo0600.

Não voltar simplesmente ao binário 0.7.0: o catálogo INBOUND não é válido no
limite antigo de 32 padrões. Qualquer rollback deve preservar suporte a 64 ou ter
um plano explícito de restauração da configuração. Não remover regras do usuário
silenciosamente para voltar um binário.
