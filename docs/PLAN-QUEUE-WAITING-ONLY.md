# Fila representa apenas chamadas antes do atendimento

1. [concluído] Aplicar esclarecimento do mantenedor: cabeçalho, atividade, cartões, detalhes e ordem de filas consideram somente chamadas ainda não atendidas. Preservar chamadas nos ramais/troncos.
2. [concluído] Validar transição espera → atendimento com regressão e navegador, agrupamento/filtros.
3. [em andamento] Commit/push main, publicar somente painel, conferir saúde/hash e projeção de dados reais sem ações de telefonia; converter em atividade.

Decisão vigente: a última instrução substitui o pedido anterior, interrompido, de soma espera+atendimento. Chamando atendente/saudação/anúncio ainda pertencem à fila; connected/answered saem da apresentação da fila. Metadados originais do controlador e diagnóstico não são adulterados. Ordenação global por quantidade ainda aguardando. Sem deploy/reinício Google/controladores. Referências visuais existentes e DESIGN.md/SUI; ajuste local sem redesign.

Regressão antes da mudança falhou: “Answered calls no longer constitute queue activity”. Após mudança,1.509 checks passaram, incluindo filas nativas e legadas: chamada atendida sai de atividade/fluxos/canais da fila e continua no ramal; metadados originais são preservados.

Navegador: queue-board (atendida sem canal na fila, preservada no ramal e excluída do filtro ativo), queue-summary (agrupamento/gestão/filtros), queue-call-flow (legado) e board-supervision passaram. Capturas desktop/mobile inspecionadas, detector sem achados. Nenhuma chamada real efetuada.
