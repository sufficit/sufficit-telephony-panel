# Cartão único por fila

Objetivo: reunir chamadas de todos os servidores em um cartão por fila, preservando identidade, filtros, detalhes e autorização.

1. [concluído] Definir agrupamento seguro e integração com seleção/detalhes/supervisão; registrar contrato e regressão.
2. [concluído] Implementar agrupamento por empresa/GUID, somar estados atuais e misturar chamadas; validar testes e navegador desktop/mobile.
3. [em andamento] Commit/push main, publicar somente painel, verificar saúde/hash e converter plano em atividade.

Restrições: não alterar/reiniciar Google nem outros controladores; não unir clientes ou números sem identidade comprovada; manter histórico e alerta de cobertura incompleta. Referência visual: pedido explícito do usuário, DESIGN.md e composição SUI existente; alteração local sem redesign. Servidor permanece nos dados técnicos das chamadas e controles de supervisão, sem separar cartões de fila.

Contrato: agrupar após autorização e filtro explícito de servidor, antes de busca/estado; seleção estável por empresa/GUID. Preservar linhas/canais originais e somar snapshots atuais uma vez por nó, sem trazer contagem stale. Números sem identidade comprovada permanecem separados; não disparar milhares de lookups. Regressão antes da implementação falhou: “Same company/GUID must produce one queue card across servers”.

Validação: 1.489 checks passaram, incluindo soma por nó, deduplicação, histórico/stale, isolamento de empresa, seleção estável e prefixos protegidos por nó. Navegador: queue-summary, queue-board, queue-resource-details e board-supervision passaram; capturas desktop/mobile inspecionadas; detector sem achados. Warning NU1903 preexistente. Fixtures locais, sem chamadas reais ou sessão autenticada de produção.
