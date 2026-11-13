# Limpeza do card e ordem por utilização

1. [concluído] Remover textos explicativos dentro da área de filas e ordenar a visão global do gerente por utilização atual (espera + atendimento), maior primeiro.
2. [concluído] Validar ordenação, cobertura incompleta, detalhes e layout em navegador.
3. [em andamento] Commit/push main, publicar somente painel e verificar saúde/hash; converter em atividade.

Contrato: manter `—` no total sem cobertura e diagnóstico nas opções/alerta externo já existente; tirar o aviso longo e a contagem histórica do corpo do card. Preservar informações de cada fila, agrupamento, GUID/gestão e limite de exibição. Desempate: espera, estado atual, nome e identidade. Visão de empresa mantém ordem anterior. Sem alterações/reinício no Google. Referência: captura e pedido do usuário, DESIGN.md/SUI existentes; ajuste local.

Reproduzido por regressão: ordenação anterior falhou ao priorizar fila com seis chamadas sobre fila com uma chamada. Implementada soma atual de espera+atendimento; empate pela espera, estado atual e nome. Mensagem de cobertura transferida para tooltip do total; diagnóstico externo preservado.

Validação: regressão falhou antes e passou depois; 1.494 checks passaram. Navegador queue-summary passou, conferindo maior uso antes do nome, manutenção da visão por empresa, texto ausente/tooltip de cobertura presente, GUID/gestão/canais e filtros; desktop/mobile sem overflow inspecionados. Detector sem achados; aviso NU1903 preexistente.
