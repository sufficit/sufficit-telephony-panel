# Diagnósticos de erros e bloqueios de supervisão

Objetivo: explicar falhas por fonte e por canal, correlacionar exceções com logs sem expor mensagens brutas, tokens ou dados de chamadas. Não flexibilizar autorização, limite de 30 segundos nem iniciar chamadas nos testes.

1. [concluído] Implementar motivos de bloqueio centralizados, detalhes de fontes e referências de erro nos fluxos da tela e supervisão.
2. [concluído] Validar build, regressões de autorização/diagnósticos e interface em navegador com fixture.
3. [concluído] Registrar evidências e limitações, revisar diff e estado final. Implementação local concluída; publicação autorizada posteriormente pelo usuário.

Aceite: botão bloqueado acompanhado de motivo legível no tablet; alerta de fontes mostra nó, contexto autorizado, conexão e última leitura; exceção gera referência visível e mesmo ID nos logs; nenhuma mensagem bruta de exceção no navegador.

Validação: dotnet build src/Sufficit.Telephony.Panel.csproj -m:1; dotnet run --project tests/Panel.Tests.csproj; fixture Development no navegador, larguras 1280 e 768.

Referências/decisões: captura do usuário e DESIGN.md preservam tabela, cores e componentes SUI; detalhes HTML nativos seguem o padrão Context existente; motivo textual adjacente não depende de hover em tablet. Pesquisa proporcional: referência local, sem Refero MCP disponível. Não redesenhar a mesa.

Evidência: build sem erros; 970 verificações passaram; browser diagnostics/call-grouping/monitoring-migration passaram; tablet Lenovo real abriu fixture via ADB reverse sem overflow e exibiu motivos. O teste legado de monitoramento usava seletor .queues removido antes desta tarefa; ajustado para .queue-queues para executar a regressão.

Entrega local documentada em docs/activities/202610210916-error-diagnostics.md e docs/error-diagnostics.md. Fixture e redirecionamento ADB encerrados após validação; nenhuma chamada ou publicação de produção executada.

## Publicação autorizada em 21/10/2026

4. [concluído] Gerar Release em diretório novo, verificar artefato e preservar configuração do release atual no host.
5. [concluído] Transferir e ativar somente no Eveo Apps pelo helper com rollback automático e release anterior esperado.
6. [concluído] Validar saúde pública, acesso anônimo negado e interface autenticada no tablet; registrar release, hash e rollback.

Estado anterior confirmado: queue-capacity-20261015-a60d8a3, PID 4134108, serviço ativo. Não alterar Nginx, Identity, PBXs, coletores ou arquivos de estado.

Publicação concluída: error-diagnostics-20261021-0918, health 200, API anônima 401, tablet autenticado com motivos por canal e quatro fontes atuais. Evidência: docs/activities/202610210922-error-diagnostics-deploy.md. Nenhuma chamada originada.
