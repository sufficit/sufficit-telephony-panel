# Resultado temporário de discagem inválida no card

Objetivo: informar por alguns segundos, exclusivamente na extensão que tentou discar, uma falha explicitamente reportada pelo PBX (número inválido/inexistente ou falta de rota). Não confundir chamada não atendida/cancelada/ocupada com número inválido. Comentários de código/eventos em inglês.

1. CONCLUÍDO — Verificar eventos/campos de falha e definir retenção curta com isolamento por nó/conta/canal.
2. CONCLUÍDO — Implementar resultado transitório sem manter chamada ativa, com rótulo de motivo e regressões de expiração/autorização/nova tentativa.
3. CONCLUÍDO — Validar navegador/pacote, publicar componentes necessários dentro da autorização existente e conferir saúde; registrar evidências, converter plano, commit/push main.

Referências visuais: DESIGN.md, captura e pesquisa desta sessão; manter cards/tokens existentes. Proposta funcional: aviso textual de erro por 10 segundos; tentativa seguinte remove o aviso. Sem animação de toque para falha nem estado inválido inferido apenas de Down/Hangup normal. Caso PBX não reporte causa explícita, registrar limitação em vez de inventar resultado.

Regressão comprovada com o pipeline HEAD anterior: perdeu o resultado no Hangup e falhou em DialFailureChecks (nenhum elemento). Implementação passou 1.598 checks; streaming passou 42 checks após restaurar dependências (primeira execução --no-restore usava assets antigos com projeto renomeado). Navegador dial-failure PASS: motivo explícito, sem rota, encerramento normal, expiração, nova tentativa, escopo de extensões e mobile/escuro/reduced-motion.


## Entrega e validação

Fonte b938273/main; observer 5617f2b/main. O resultado fica em memória por 10s,
no endpoint/nó/conta do próprio canal observado; sem caller/destination/linkedid
no resultado. Nova tentativa remove o resultado. Aviso textual estático, sem
pulso de toque. Mantém-se visível no filtro de atividade breve, mas soma zero
chamadas/canais ativos. Clientes só recebem resultados da empresa autorizada;
regras de exibição não substituem essa autorização. Nenhum estado de terceiros
ou de troncos/filas é herdado. Registro/alcance permanecem independentes.

Publicados somente em eveo-apps, sem mudança/reload no Google/Asterisk:
- Observador: dial-failure-20261117-5617f2b, PID2606396/NRestarts0.
- Painel: dial-failure-20261117-b938273, PID2606600/NRestarts0.
- Legado AMIEvents manteve PID409848.
- DLL painel SHA256 0ae9b319425a3b4d72d114d68b868ea9b58640e8f47c7e1aed4357a2e36414bd.
- Manifesto dos 239 arquivos do painel conferido antes/depois da ativação.
- Configuração do painel preservada, SHA256
  a0f5e8fe9674454ab4bb53bb3016fb22f5ae100005f87d36d1e0bd65fefa5e44.
- CSS público Sufficit.Telephony.Panel.1a5i8rp8t7.styles.css idêntico ao pacote,
  SHA256 c730034ac294d2def94cd069c1f2ac016724449c1342990d72019885da20e8cb.
- Health socket e público 200; API fleet anônima 401; preview público sem fixture.
- Journal sem erros na janela de 2min consultada. Health autenticado do observador
  passou como gate do rollout. Rollback automático disponível, não necessário.

1.598 checks C# PASS (transporte snapshot, causas, expiração, nova tentativa,
contagens e autorização), 42 streaming PASS, 28 integração do observador PASS.
Regressão contra pipeline antigo falhou antes da correção; o resultado era
apagado no Hangup. Build/publish Release PASS. dial-failure.browser.cjs PASS
em Development e contra o pacote Release. Verificados motivo explícito, sem
rota, normal clearing, expiração, nova tentativa, ausência de canal vivo,
extensões somente, mobile/escuro/reduced-motion. Captura
.impeccable/review/dial-failure-light.png inspecionada; artefatos visuais locais
são ignorados pelo Git. Servidor local encerrado após a validação.

Probe UDS read-only real observou 13 Hangup com causa numérica (0:4, 1:2,
16:6, 21:1), com geração de resultado temporário; não foram originadas chamadas
nem usadas sessões de cliente. O aviso depende da causa explícita do PBX:
um dialplan que anuncia erro e encerra com causa normal não permite inferir
“número inválido” só pelo encerramento. A documentação oficial de referência
é https://docs.asterisk.org/Configuration/Miscellaneous/Hangup-Cause-Mappings/ .

Rollback do painel: release anterior ringback-20261117-a9c5564 retida,
manifesto privado /var/backups/sufficit-telephony-panel/dial-failure-20261117-b938273/release.json;
conferir paths, reverter current e reiniciar somente painel. Configuração e
inventário persistente não foram substituídos. O pacote do observador tem
rollback independente, sem intervenção em telefonia.

Evidências do fluxo e rollback do observador: [atividade](../../../sufficit-ami-events/docs/activities/202611171128-recent-dial-failure.md).
