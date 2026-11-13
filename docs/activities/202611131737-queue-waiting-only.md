# Fila representa apenas chamadas antes do atendimento

1. [concluído] Aplicar esclarecimento do mantenedor: cabeçalho, atividade, cartões, detalhes e ordem de filas consideram somente chamadas ainda não atendidas. Preservar chamadas nos ramais/troncos.
2. [concluído] Validar transição espera → atendimento com regressão e navegador, agrupamento/filtros.
3. [concluído] Commit/push main, publicar somente painel, conferir saúde/hash e projeção de dados reais sem ações de telefonia; converter em atividade.

Decisão vigente: a última instrução substitui o pedido anterior, interrompido, de soma espera+atendimento. Chamando atendente/saudação/anúncio ainda pertencem à fila; connected/answered saem da apresentação da fila. Metadados originais do controlador e diagnóstico não são adulterados. Ordenação global por quantidade ainda aguardando. Sem deploy/reinício Google/controladores. Referências visuais existentes e DESIGN.md/SUI; ajuste local sem redesign.

Regressão antes da mudança falhou: “Answered calls no longer constitute queue activity”. Após mudança,1.509 checks passaram, incluindo filas nativas e legadas: chamada atendida sai de atividade/fluxos/canais da fila e continua no ramal; metadados originais são preservados.

Navegador: queue-board (atendida sem canal na fila, preservada no ramal e excluída do filtro ativo), queue-summary (agrupamento/gestão/filtros), queue-call-flow (legado) e board-supervision passaram. Capturas desktop/mobile inspecionadas, detector sem achados. Nenhuma chamada real efetuada.

## Implementação e entrega

A visão de fila usa somente pending/visitas não answered/connected. Atendidas
não geram atividade, fluxos nem canais de detalhes/supervisão no cartão da fila,
mas permanecem disponíveis nos cartões de ramal e tronco. A correlação da fila
usa a própria visita aguardando, sem absorver a fase atendida de outra visita
relacionada. Ordenação global usa apenas a quantidade aguardando.

O cartão mostra “N aguardando”, sem contador de atendimento. A fila configurada
pode continuar aparecendo vazia; a chamada é que sai dela. O cabeçalho continua
contando espera. Campos originais connected/visitas do controlador permanecem
intactos para diagnóstico e demais telas. Não houve mudança no runtime de
telefonia, roteamento ou autorização.

Validações:1.509 checks passaram; regressão antes/depois comprovada.
`queue-board.browser.cjs`, `queue-summary.browser.cjs`,
`queue-call-flow.browser.cjs` e `board-supervision.browser.cjs` passaram, com
fases anteriores ao atendimento, transição connected, preservação no ramal,
filtros, agrupamento, gestão e supervisão sem chamadas reais. Capturas
`.impeccable/review/queue-summary-desktop.png` e `queue-board-mobile.png`
inspecionadas; detector sem achados. Tema escuro coberto pela supervisão.
Warning NU1903 SignalR.Common10.0.11 preexistente. Referências: pedido explícito
do mantenedor, capturas da sessão e DESIGN.md/SUI; sem novo estilo/dependência.

Fonte `690c3b9`, main publicada. Release no eveo-apps:
`/opt/sufficit-telephony-panel/releases/queue-waiting-20261113-690c3b9`.
Anterior preservada `google-source-20261113-ea041a3`; configuração copiada no
servidor. Drop-in de fonte global separada permanece. Reiniciado somente o painel
com helper atômico existente. DLL instalada igual ao pacote local, SHA256:
`f8cea98e3db6525c1828f29d1430e58f87aca7a4b112657b7f0b414f88f85b6a`.

Prova pós-ativação com DLL instalada, ambiente efetivo e usuário sufftelpanel,
em20:36:44UTC: Google464 contextos/1.161 filas,2 aguardando e2 connected no
controlador; E/A463 contextos/1.159 filas cada, zero atividade. Nenhuma falha de
cobertura. Projeção do quadro:1.161 filas distintas, total2 aguardando,2 filas
ativas nas primeiras40. Snapshot por empresa preservou2 agentes na fachada18190.
Valores observados naquele instante, não constantes do produto.

Health interno/socket e público HTTP200/running, serviço ativo, `/api/fleet`
anônimo HTTP401, hash instalado conferido e coletor sufficit-ami-events manteve
MainPID409848. Nenhuma alteração ou reinício Google/controladores. Navegador de
produção autenticado do usuário não foi usado: a prova real cobre leitor,
projeção e ordenação com pacote/configuração instalados; interação visual usa
fixtures locais. Todos os checkpoints autorizados concluídos.

Contrato permanente em [operator-board.md](../operator-board.md#atendimento-encerra-a-participação-na-fila).
