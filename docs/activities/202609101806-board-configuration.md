# Configurar mesa — 0.7.0

## Objetivo e terreno

Pedido: tela para escolher o que aparece como tronco e o que deve ser filtrado,
usando como referência FlashOperatorPanelController.cs em sufficit-endpoints.
O exemplo agrupa aliases de canais em troncos, mas sua configuração é código fixo.
A mesa 0.6.0 tinha somente classificação vinda dos cartões do portal.

## Alterações

- Nova aba Configurar mesa: lista ordenada, editor inline SUI, prévia, rascunho,
  cancelamento, remoção confirmada e salvamento explícito compartilhado.
- Tipos tronco/ramal/ocultar, aliases exatos ou prefixo final explícito, servidor
  opcional, ativar/desativar e prioridade com primeira correspondência vencedora.
- Regras após autorização, sem ampliar recursos ou alterar outras abas. Agrupam
  aliases do mesmo nó; deduplicam eventos; separam parte de cartões antigos.
- Armazenamento privado no StateDirectory, revisão UUIDv7, gravação atômica,
  comparação otimista e limites. Acesso revalidado via sessão manager existente.
- Exemplo WhatsApp somente como formulário em rascunho, sem importar catálogo ou
  credenciais do FOP. Identidade, Asterisk, coletores e endpoints não modificados.
- Reuso SUI e revisão visual pelas skills sufficit-frontend/Impeccable; estrutura
  de execução e relatório pela skill software-development.

## Verificação

- Build e publish Release: concluídos. Warning de UnixCreateMode multiplataforma
  corrigido com guarda explícita; testes seguintes sem warning.
- `dotnet run --project tests/Panel.Tests.csproj`: 105 verificações aprovadas.
- `NODE_PATH=/tmp/k8sui-edit/node_modules node tests/board-settings.browser.cjs`:
  validação, editar/cancelar, salvar, aliases, ocultar, canais preservados,
  remover/descartar/recarregar, temas e móvel aprovados.
- Suítes operator-board.browser.cjs e call-grouping.browser.cjs aprovadas.
- Ajustes no harness: esperar renderização Blazor e acionar checkbox SUI pelo
  teclado (o elemento visual do checkbox cobre o input nativo).
- Capturas `.impeccable/review/settings-{desktop,dark,mobile}.png` abertas e
  verificadas; reviewer independente retornou **ship**, sem material fixes.
- Detector único: advisories tipográficos já existentes/compactos, sem bloquear.
- Persistência exercitada em diretório temporário: releitura, 0600, colisão de
  revisões, dois writers, corrupção não sobrescrita, anonimato negado.

## Publicação

Eveo: `/opt/sufficit-telephony-panel/releases/settings-20260910-2110`, promovido
por symlink; versão 0.7.0. Configurações privadas preservadas somente no host.
Reiniciado apenas sufficit-telephony-panel: PID 2668813, ativo, NRestarts=0.
Memória após início observada em 78.839.808 bytes.

- DLL: `c8d2a7c0a0af02493a1a183c04e0e9ca66e3d59f18c9bd2818c6cc221384c775`.
- CSS local/publicado/HTTPS idêntico:
  `522a74b96c678a9665b687b9c9959a4ac93d8534ef3d7cba55a64426f2683b6a`.
- Health 200 e fleet anônimo 401; gRPC reconectado ao socket privado.
- Observador PID 2618869 e AMI legado PID 2619544 preservados.
- StateDirectory 0700 e gravável por sufftelpanel. Arquivo de regras ainda ausente,
  como esperado antes do primeiro salvamento. Nenhuma regra real criada no deploy.
- Rollback: release anterior `board-20260910-2045`, reiniciando somente painel.
  Nenhum backup adicional ou commit/push; worktree preexistente preservado.

## Referências e limites

[Como configurar](../board-configuration.md), [Mesa de operação](../operator-board.md),
[Publicação](../../deploy/README.md). Persistência single-host Eveo; expansão para
vários writers requer coordenação distribuída. Não é cadastro de troncos do PBX.
Filtros visuais não são controles de acesso. Navegador usou fixture local; health,
assets e socket não comprovam login ou gravação com uma sessão humana em produção.
