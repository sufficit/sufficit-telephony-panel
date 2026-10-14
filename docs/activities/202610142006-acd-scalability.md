# ACD: implementação e validação para escala

Data local: 2026-10-14T20:06:49.195789-03:00
Branch isolada: feat/acd-scale-diagnostics. Implementação autorizada pelo usuário para seis melhorias; nenhuma alteração nova instalada em produção. Checkouts originais e modificações de outras tarefas preservados.

Contrato: journal opcional com canal/lotes limitados e descarte explícito, histórico sob demanda/paginado e caches compartilhados; presença com idade/registro/alcance/ocupação sem propriedade persistida de offline; ordenação atômica com revisão; associação DID por CAS e ingress comprovado; SLA por visitas e tarifação/débito persistidos separados; recuperação não libera reserva incerta.

Validação: ACD integração SQL/Redis descartáveis reais e ARI fake passou emnet10 e net7/7.0.20. Espera recupera ID/prazo original; bridge viva continua visível, conserva answer_at e completa/libera apenas reserva comprovada. Fencing/falha Redis11/11cenários. Journal25000eventos/98lotes≤256, overflow/falha/shutdownobserváveis. Proxy3testes, ingress5testes, presençaPython aprovada. Provision/Nodebuildsnet7/net10aprovados. Panel901checks; Blazor105ACD +132regressões obrigatórias; EndPoints8checks com MariaDB descartável incluindo CAS,foreigncontext/node e débito real persistido. Builds aprovados; warnings preexistentes não foram corrigidos nesta tarefa.

Última carga: calls=5000 elapsed-ms=5095.31 cps=981.29 p50-ms=0.92 p95-ms=1.39 p99-ms=2.13 sql-statements=16324 ari-commands=5000 snapshot-ms=2.42 snapshot-bytes=65208 cpu-ms=2549.99 allocated-bytes=97117216 managed-bytes=34482328.5000visitas/100filas retidas;0eventos perdidos. ARI simulado: não mede SIP, despacho de agentes, áudio RTP nem capacidade de produção. Default100preservado.

UI: Chrome/Playwright com componentes reais do Panel e fixtureDevelopment: chamadas/linha do tempo/SLA e estado não correlacionável;1440/390px,claro/escuro,semoverflow. Blazor: interação nos testesbUnit; revisão visual do markup real+CSSscoped compilado em1440/800/390px,claro/escuro. Revisor inspecionou capturas. Uso de shellvisual do Panel para revisão dos componentes Blazor NÃO valida shellprodutivo/auth/integraçãoentrehosts. Artefatos locais em /tmp/acd-scale-review, logs /tmp/acd-scale-*.log.

Ledger visual: DESIGN.md e SUI mantidos; AcdMonitor/Registration/LiveOperations como referência de diagnóstico; QueueEditor/Members e Subir/Descer deBoardSettings como referência de gestão. Filas/ramais primeiro; histórico sob demanda e detalhe selecionado; config/DID/ingress exibidos separadamente. Campos/ações seguem espaçamento existente emQueueEditor; tabela empilha emcelular. Sem nova estética/dependência/timer por chamada. Evidência desconhecida não vira cobrança zero nem dispositivo disponível.

Publicação pendente: PRs coordenados, revisão/merge por ação e migração008/backup/drain/health conforme DEPLOY-ACD-DIAGNOSTICS noACD. Estado de produção não pode ser declarado por build/captura. Main deBlazor também recebeu alterações externas durante a tarefa; atualizar branch sem sobrescrevê-las antes de entrega.
