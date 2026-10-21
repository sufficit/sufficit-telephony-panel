# Reorganização do front-end (etapas 1, 2, 4 e 6)

Pedido do usuário: avaliar o front-end, instalar skills de front-end e reorganizar
a interface "confusa e bagunçada"; implementar e publicar. Plano:
[PLAN-FRONTEND-REORGANIZACAO](../PLAN-FRONTEND-REORGANIZACAO.md).

## Mudanças
- `Home.razor`: navegação das áreas dentro do cabeçalho (links com `aria-current`),
  rota `/infrastructure`, recarga da área ao navegar no cliente (antes a troca SPA
  para `/queues` podia exigir recarregar a página), chip de demonstração no cabeçalho.
- `LiveOperations.razor`: status do stream ao lado do H1; "Configurar mesa" saiu das
  abas e virou botão; contexto recolhido por padrão (abre com resultados de busca);
  abas antes dos filtros; limites de observação e "Recursos lembrados e limpeza"
  recolhidos abaixo do mosaico; barra "Mesa de operação · Toda a central" removida
  (título só para leitor de tela) e "Tela cheia" na linha de filtros.
- `Pager.razor` novo nas listas de Filas; oculto quando tudo cabe numa página.
- `panel.css`: shell único (gutter 24px, H1 24px, cabeçalho compacto), navegação
  rolável no mobile, abas internas em linha única rolável no mobile.
- `panel.js`: sem preferência salva, tema segue `prefers-color-scheme`.
- `App.razor`: briefing de design virou comentário Razor (não vai mais ao HTML).
- Skills instaladas no usuário: `frontend-design` (plugin oficial), `refero-design`
  (link para o espelho), `web-design-guidelines` (Vercel).

## Validação
- `dotnet run --project tests/Panel.Tests.csproj`: 971 checks PASS.
- 16/16 `tests/*.browser.cjs` PASS com fixture Development (5168/5169). Ajustados
  por mudança intencional: `board-inventory` (abre "Recursos lembrados"),
  `call-grouping` (chip de demonstração), `monitoring-migration` (Infra em `/infrastructure`).
- Capturas desktop/mobile, claro/escuro: sem overflow horizontal nem erros de página.

## Publicação no Eveo Apps
Autorização do usuário: "faça e implemente e deploy". Ativado em 21/10/2026 ≈10:05 -03.

- Release: `/opt/sufficit-telephony-panel/releases/frontend-reorg-20261021-7d99719`
  (commit `7d99719`); anterior preservado: `error-diagnostics-20261021-0918`.
- SHA256 DLL `e9f0c7f4e1e3c725673691ef23d73e8d043bf544c016993c591e1e2000a4c874`;
  pacote `362332e0d94fc4ff9e6267ff28019d2328bf70d6266902e7a1c7640c11586096`, conferido no host.
- `appsettings.json` copiado do release anterior e confirmado idêntico (`cmp`).
- `deploy/release-panel.py` passou o gate de health pelo socket; sem rollback. PID 9877.
- `/health` público 200; `/api/fleet` anônimo 401; CSS novo (`panel.54hp91l9js.css`)
  servido com `.demo-chip`; briefing de design ausente do HTML; sem warnings no journal.
- Não validado em produção com sessão manager (sem login nesta sessão); Nginx,
  Identity, PBXs e coletores não foram tocados.

Rollback: apontar `current` para `error-diagnostics-20261021-0918` e reiniciar só
`sufficit-telephony-panel` (manifesto em `/var/backups/sufficit-telephony-panel/frontend-reorg-20261021-7d99719/`).

## Mesa de operação sempre em área inteira (pedido do usuário, 21/10)
"A mesa de operação é sempre bom usar tela cheia, todo o espaço disponível, nunca aba
de alguma coisa." Mudança:
- A mesa virou área própria no cabeçalho ("Mesa de operação", `/`); "Telefonia ao vivo"
  (`/?tab=extensions`) reúne Ramais, Chamadas, Canais, Filas e Eventos. A aba
  "Mesa de operação" saiu da faixa de abas.
- Na página normal a mesa usa o mesmo modo da mesa dedicada: ocupa toda a altura abaixo
  do cabeçalho, sem título, abas, legenda nem rodapé; filtros, contexto, tela cheia,
  reconectar, abrir mesa dedicada, configurar mesa, recursos lembrados e limites de
  observação ficam no popover do ícone de filtro. Detalhes do recurso abrem em popup.
- `LiveOperations` inicia na aba da URL (antes montava a mesa e desmontava em seguida,
  derrubando o circuito com `board-layout.attach(null)`); `attach` ficou tolerante a nulo.
- Testes ajustados: board-inventory, board-only, board-settings, call-grouping,
  error-diagnostics, operator-board. Resultado: 971 checks + 16/16 navegador PASS,
  zero exceções no log do servidor.

Publicado em 21/10 ≈10:09 -03: release `board-fill-20261021-f7802ac` (anterior preservado
`frontend-reorg-20261021-7d99719`), DLL `80aa4737546ba50029b90a6a00f460145c1ea59113788da804acb7001275800e`,
pacote `9394c9e2f30a578371144d9eb15331c859134d43ef6f8872c76fe9ded79aece7`; appsettings idêntico,
gate de health passou, `/health` 200, `/api/fleet` anônimo 401, CSS novo com `.board-fill`.

## Etapas 3 e 5 parciais (refatoração, sem mudança de comportamento)
Commit `d5a863c`: `LiveOperations.razor.cs` com a lógica; componentes `ContextPicker`,
`RetentionControls`, `ObservationNote`; inspetor inline e CSS morto removidos.
975 checks + 16/16 navegador PASS, zero exceções no servidor. Publicado como
`refactor-20261021-d5a863c` (anterior `iax2-group-20261021-b8d9c5c` preservado), DLL
`c54833afee2ea4a31de514985272c04a83fe0693c8f408c20879b9ef1cbd8c5f`, appsettings idêntico,
health 200, `/api/fleet` anônimo 401, sem warnings no journal.

## Vistas extraídas e CSS da mesa escopado
Commit `32656b1`: `LiveFormat`, `ChannelTable`, `EventsView`; 50 regras da mesa em
`OperatorBoard.razor.css`. Comparação de pixels antes/depois em 11 capturas (mesa embutida
claro/escuro/tablet/mobile, mesa dedicada desktop/mobile/1977px, popup, canais, eventos,
configuração): idênticas salvo timestamps do fixture. 975 checks + 16/16 navegador PASS,
zero exceções. Publicado como `views-css-20261021-32656b1` (anterior `refactor-20261021-d5a863c`
preservado), DLL `be915020916ca982fed4f6400421632a29f03a905e5546912958b8b4361078d5`,
appsettings idêntico, health 200, `/api/fleet` anônimo 401, bundle escopado servido com as regras da mesa.
