# 2026-09-10 — Chamadas correlacionadas e canais, versão 0.5.1

## Pedido e causa
O usuário apontou que a tabela representava canais da mesma chamada como chamadas
independentes. Confirmado na projeção/aba: a chave era canal, e o contador rotulava
cada recurso kind=call como chamada. A projeção já preservava Linkedid.

## Alterações
- `ObservedCallGroup`: agrupamento por nó/Linkedid apenas sobre snapshot autorizado,
  sem heurística por número e sem correlação entre nós. Valores ausentes/brancos
  ficam separados e não contam como chamadas correlacionadas.
- `LiveOperations`: abas Chamadas/Canais, contadores distintos, grupos expansíveis
  por teclado, estados por canal, filtros que preservam os demais membros do grupo,
  duração explicitamente observada e supervisão pelo canal escolhido.
- Agrupamento calculado ao receber snapshot; detalhes limitados a 100 canais por
  grupo com aviso e acesso à aba Canais para consultar todos. Paginação mantida.
- Fixture com SIP e duas pernas Local no mesmo grupo, mais um canal sem correlação.
- Reutilizados SUI, details/summary e os tokens/estilos existentes. Skills de
  desenvolvimento e frontend orientaram plano, testes, nomenclatura e validação;
  Impeccable clarify orientou distinção de termos, sem reformulação estética.

## Validação
- Build e publicação Release: sucesso; build Debug sem erros ou avisos.
- 66 verificações .NET aprovadas: 56 anteriores + 10 de correlação, filtros,
  isolamento/autorização e Hangup de uma perna/final.
- Playwright local: contagem 1 chamada/4 canais/1 sem correlação; expansão por Enter,
  três canais no detalhe; confirmar escuta aponta para o canal selecionado; não
  executada a ação. Filtros, vazio, claro/escuro, desktop 1440px/móvel 390px aprovados.
  Sem overflow da página ou erros JavaScript. Capturas locais de fixture em
  `.impeccable/review/call-groups-{desktop,dark,mobile}.png`.
- Detector visual indicou avisos de tamanhos tipográficos preexistentes em CSS;
  não alterado o design system para silenciar avisos fora do escopo.
- `git diff --check`: sucesso.

## Publicação
Eveo Apps: `/opt/sufficit-telephony-panel/current` aponta para
`releases/callgroups-20260910-2030`, versão 0.5.1.
SHA256 DLL: `16549ea38ccacadd2735707a14f184b5dd1196f9b5e4c10ace1e4a7707472b4a`.
SHA256 pacote: `fa17f7b5c30dc9f2b19927d529cf7eb4e819a1fa7430515a0beeef930a041b2f`.
Configuração host-only preservada. Reiniciado somente `sufficit-telephony-panel`,
PID 2636911, ActiveState active, NRestarts 0. Saúde pública HTTP 200 e fleet anônimo
401; conexão Unix gRPC presente. Coletores mantiveram PIDs 2618869 e 2619544.
Nenhum Asterisk/Identity reiniciado ou recarregado; nenhuma chamada real criada.

## Entrega e limites
[Contrato](../call-channel-correlation.md). Correlação é por evidência observada,
não histórico completo de atendimento nem deduplicação global. A validação visual
usou fixture; não houve login humano autenticado em produção nesta etapa. A sessão
do navegador pode precisar de atualização/login após reiniciar o painel.
Alterações anteriores preservadas; não foi feito commit/push. Rollback: apontar
current para `releases/grpc-20260910-1958` e reiniciar somente o painel.
