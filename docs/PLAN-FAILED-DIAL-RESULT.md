# Resultado temporário de discagem inválida no card

Objetivo: informar por alguns segundos, exclusivamente na extensão que tentou discar, uma falha explicitamente reportada pelo PBX (número inválido/inexistente ou falta de rota). Não confundir chamada não atendida/cancelada/ocupada com número inválido. Comentários de código/eventos em inglês.

1. CONCLUÍDO — Verificar eventos/campos de falha e definir retenção curta com isolamento por nó/conta/canal.
2. CONCLUÍDO — Implementar resultado transitório sem manter chamada ativa, com rótulo de motivo e regressões de expiração/autorização/nova tentativa.
3. EM ANDAMENTO — Validar navegador/pacote, publicar componentes necessários dentro da autorização existente e conferir saúde; registrar evidências, converter plano, commit/push main.

Referências visuais: DESIGN.md, captura e pesquisa desta sessão; manter cards/tokens existentes. Proposta funcional: aviso textual de erro por 10 segundos; tentativa seguinte remove o aviso. Sem animação de toque para falha nem estado inválido inferido apenas de Down/Hangup normal. Caso PBX não reporte causa explícita, registrar limitação em vez de inventar resultado.

Regressão comprovada com o pipeline HEAD anterior: perdeu o resultado no Hangup e falhou em DialFailureChecks (nenhum elemento). Implementação passou 1.598 checks; streaming passou 42 checks após restaurar dependências (primeira execução --no-restore usava assets antigos com projeto renomeado). Navegador dial-failure PASS: motivo explícito, sem rota, encerramento normal, expiração, nova tentativa, escopo de extensões e mobile/escuro/reduced-motion.
