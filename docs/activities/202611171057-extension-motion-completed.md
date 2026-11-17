# Correção de animação somente em extensões e erro da mesa

Pedido: corrigir animação aplicada aos troncos e investigar erro UnauthorizedAccessException da captura (referência 01a1e9be6055736698048e5fdf882c7e, 2026-11-17 13:42:10 UTC). Continuidade da alteração/publicação desta sessão.

1. CONCLUÍDO — Identificar origem da exceção e evidência dos estados/correlação dos ramais; reproduzir os defeitos.
2. CONCLUÍDO — Restringir movimento às extensões, corrigir falhas comprovadas e validar com testes de regressão/navegador.
3. CONCLUÍDO — Commit/push, publicar somente painel em eveo-apps e conferir saúde/assets; registrar a entrega parcial em atividade.

Preservar permissões e dados de clientes; não suprimir negação de autorização. Manter referências visuais já pesquisadas, DESIGN.md e tokens SUI. Sem redesign. Não inferir estado do ramal pelo estado de um tronco remoto. Conferência de chamada real não pode ser substituída por fixture.

Logs confirmaram token expirado 13:42:08.8854384 UTC, seguido de sessão removida e exceção OperationsPool.Get. Não causado pela perda de sessão no deploy. Regressão de navegador reproduzida: tronco com Ring animava indevidamente. Snapshot privado agregado: 22 canais de extensões Down/uso não informado; 11 canais Ring em tronco. Não inferir ringing a partir desses troncos. Investigar eventos de atualização antes de afirmar correção dos dados.

4. CONCLUÍDO — Aplicação do filtro Newstate no Google autorizada nesta sessão e validada em produção. Regra de janela de referência: 202611121254-publish-panel-queue-details.md. Evidências em sufficit-ami-events/docs/activities/202611171057-extension-channel-states.md. Aceitação da projeção real de extensões concluída.

Correções locais: CSS condicionado a PEER; tronco da fixture recebe Ring/Ringing para provar ausência de movimento; recuperação Unauthorized oferece /login com retorno para URL/filtros. Enum AMI DialingOffhook/PreRing também reconhecido. 1.530 checks PASS e publish Release PASS. card-call-motion.browser.cjs e error-diagnostics.browser.cjs PASS; regressão dos troncos falhou antes do CSS corrigido.

Evidências da correção/publicação: [atividade](202611171052-extension-motion-panel-fix.md). Plano convertido em atividade após aplicação e validação do filtro no Google.

Mantenedor autorizou a aplicação no Google nesta sessão. Executar filtro Newstate, manager reload e reconexão do observador se necessária, sem reiniciar Asterisk.

Filtro aplicado e validado no Google: 5 cards de extensões ringing e 2 com canais Up na projeção real, eventos NewState restaurados. Painel não reiniciado; somente observador reconectado. Encerramento documental concluído com conversão dos planos.

Detalhes da aplicação/rollback no observador: [atividade](../../../sufficit-ami-events/docs/activities/202611171057-extension-channel-states.md). Asterisk PID6601, painel PID2574156 e legado PID409848 preservados; somente observador mudou PID410178 → 2577787. Health público 200, API anônima 401. Não foi usada sessão autenticada de cliente nem originada chamada; estados reais verificados com probe read-only no UDS e regras reais de classificação.
