# Retenção de recursos na mesa — 10/09/2026 18:36 -03

## Solicitação e ponto de partida

Manter cartões de ramais, troncos e filas indefinidamente depois de vistos, com
opção ativa por padrão e limpeza explícita. A versão 0.7.1 derivava os cartões
somente da projeção temporária; troncos desapareciam junto com seus canais.

## Entrega 0.8.0

- `BoardInventoryStore`: inventário privado por visão central/empresa, substituição
  atômica, sem TTL. Salva somente identidade e última observação amostrada por minuto.
  Não persiste canais, números de origem/destino ou estados antigos de registro.
- `BoardInventoryAccess`: sessão manager revalidada; empresa reapresenta apenas
  cartões ainda permitidos pela API. Não reutiliza o inventário central em empresas.
- `LiveOperations`: checkbox SUI Manter recursos já vistos ativo por padrão,
  preferência persistida com revisão otimista, limpeza confirmada do contexto e
  mensagens de erro. Filtros pesquisam também chaves e servidores lembrados.
- `OperatorTile`/`BoardRuleEngine`: existência sem evidência atual recebe estado
  neutro; regras FOP ainda classificam/ocultam recursos. Filas possuem seção própria.
- Limpeza preserva recursos atuais e não altera telefonia; barreira temporal impede
  snapshots anteriores de recriarem o inventário. Eventos novos podem recriá-lo.
- As skills de desenvolvimento, SUI e hardening orientaram o contrato dos estados,
  confirmação, isolamento e verificação em desktop/móvel, preservando o visual.

## Validação

- `dotnet run --project tests/Panel.Tests.csproj -m:1 -v:q`: **145 verificações**.
  Incluem reinício do store, ausência de dados de chamadas, toggle, regras, isolamento,
  revogação de cartões, limpeza, novas observações, filas, Local transitório, modo
  0600, negação de anônimos e preservação de arquivo corrompido.
- `NODE_PATH=/tmp/k8sui-edit/node_modules node tests/board-inventory.browser.cjs`
  passou: defaults, desaparecimento de eventos, cartões preservados, canais removidos,
  teclado, detalhes, cancelar/confirmar, desktop e móvel escuro sem overflow.
- Regressões `operator-board.browser.cjs`, `board-settings.browser.cjs` e
  `call-grouping.browser.cjs` passaram. Fixtures identificadas, não dados reais.
- Build/publish concluídos; `git diff --check` limpo.
- Capturas inspecionadas em `.impeccable/review/inventory-desktop.png` e
  `inventory-mobile-confirm.png`.

## Publicação e fronteiras

Eveo: `/opt/sufficit-telephony-panel/releases/inventory-20260910-2140`, versão 0.8.0.
SHA256 do assembly: `289a666d0b6c4ff19bc252cdebfb02ba64b850394a7c7bb5c7a39da2b77e460a`.
Configurações privadas do host preservadas. Somente `sufficit-telephony-panel`
reiniciado: PID 2696165, ativo, NRestarts 0. Memória imediata ~73 MiB (não carga).

Health público **200**, API fleet anônima **401**, socket gRPC presente, sem entradas
de erro do painel na janela inspecionada. Observer PID 2618869 e AMI PID 2619544
inalterados. Nenhuma alteração ou reinício de Asterisk, Identity ou coletores.

Regras FOP não modificadas; SHA256 antes/depois:
`199c3638421e6030fdc14387115b770d91f08830cca01895b910f0ac0a8c4bd4`.
Diretório de estado 0700, gravável por sufftelpanel. Arquivo do inventário aguarda
primeira observação autorizada; não foi criado com dados artificiais em produção.
Login humano e gravação pela sessão real não foram exercitados nesta entrega.

Contrato e limites: [Recursos já vistos](../retained-resources.md). Instância única,
até 100.000 identidades sem expulsão automática; não recupera passado não observado
nem persiste canais Local transitórios. Não houve commit ou push nesta solicitação;
alterações anteriores e planos de outros trabalhos preservados.
