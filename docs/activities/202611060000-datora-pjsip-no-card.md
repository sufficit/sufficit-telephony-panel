# Interconexões PJSIP nos cards de tronco (Datora, Flux, Rota Brasil)

Pedido do dono (05/11/2026): as linhas registradas da Datora migraram para PJSIP; incluir no card do tronco.

## Avaliação

- As linhas viraram registros de interconexão no google-voip: 584 `xre-<id>` para `sip:187.60.60.161:5060`, todos `Registered`
  na avaliação (06/11 ~00:00). Os registros não têm endpoint (`line=false`); as chamadas são identificadas pelo IP.
- Os IPs 187.60.60.67, .124, .161 e .189 estão no identify `xid-915b9618e42ef111aa58f26e44c3a574`, que aponta para o endpoint
  de interconexão `xep-d64e8b18e42ef111aa58f26e44c3a574` (contexto `from-datora`).
- CDR: a primeira entrada por `PJSIP/xep-d64e8b18…` foi em 05/11 22:08 UTC; até então entrava ~4 mil/dia por `SIP/in-dtr`.
  Depois disso, 66 entradas no google-voip e 8 no eveo-voip pelo endpoint novo. Os peers `in-dtr-*` e `outdtr0*` não existem mais
  no chan_sip; a última saída por `SIP/outdtr01` foi às 22:30 UTC, sem saída nova até a avaliação. Restam 2 registros SIP
  (chan_sip) para 187.60.60.161 no google-voip, ainda `Registered`.

## Mudança

O motor da mesa compara o endpoint exato (sem o sufixo da chamada), então `SIP/in-dtr*` não cobria o canal novo e ele caía
como recurso sem regra. A regra IN-DATORA passou a ter `PJSIP/xep-d64e8b18e42ef111aa58f26e44c3a574`, mantendo `SIP/in-dtr*`
enquanto houver tráfego antigo. Saídas pelo mesmo host de interconexão também caem nesse card.

- Produção (eveo-apps): `/var/lib/sufficit-telephony-panel/board-rules.json`, revisão `01a1aec7e0d4713f82e235b15f00e6bc`,
  validado com `tools/BoardRules verify` antes de trocar; backup em `/root/board-rules.before-datora-pjsip-20261106.json`.
  O catálogo fica em memória, então o serviço foi reiniciado (05/11 23:55 BRT); `/health` e `/board` responderam 200.
- Repositório: o mesmo padrão em `deploy/fop-board-rules.json`.
- A supervisão contínua de tronco compartilhado deriva o prefixo das chamadas observadas, então passa a oferecer
  `PJSIP/xep-d64e8b18e42ef111aa58f26e44c3a574-` sem mudança de código.

Pendente: conferir o card com chamada real da Datora na mesa; decidir se os 2 registros SIP restantes e os padrões
`SIP/outdtr0*` (OUTBOUND) podem sair.

## Complemento (06/11): todas as interconexões PJSIP

O dono viu `PJSIP/xep-e6518b18…` sem card. Cada host de interconexão vira um endpoint `xep-<id do host>`
(`telp_interconnection_hosts`; o id no nome é o Guid do host em formato N). Mapeamento aplicado:

| Card | Interconexão (host) | Endpoint |
|---|---|---|
| IN-DATORA | in-datora (pvd-datora, entrada e saída) | `xep-d64e8b18…` |
| IN-DATORA | out-datora-cli-internacional / -newcontract / -oldcontract / out-datora-tdm | `xep-5e508b18…`, `xep-96508b18…`, `xep-cc508b18…`, `xep-03518b18…` |
| EXT-FLUX | out-flux, out-flux-01, out-flux-02, icx flux-01/02 | `xep-74518b18…`, `xep-ac518b18…`, `xep-e6518b18…`, `xep-514e8b18…`, `xep-984e8b18…` |
| ROTABRASIL | rotabrasil-01 | `xep-c8528b18…` |

Todos terminam em `e42ef111aa58f26e44c3a574` e existem nos 3 nós de voz. Revisão nova validada com `tools/BoardRules verify`,
backup em `/root/board-rules.before-pjsip-carriers-20261106.json`, serviço reiniciado com `/health` e `/board` 200.
Novas interconexões PJSIP também precisam entrar no card: o motor compara o endpoint exato.
