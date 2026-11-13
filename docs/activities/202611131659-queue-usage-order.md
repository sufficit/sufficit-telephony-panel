# Limpeza do card e ordem por utilização

1. [concluído] Remover textos explicativos dentro da área de filas e ordenar a visão global do gerente por utilização atual (espera + atendimento), maior primeiro.
2. [concluído] Validar ordenação, cobertura incompleta, detalhes e layout em navegador.
3. [concluído] Commit/push main, publicar somente painel e verificar saúde/hash; converter em atividade.

Contrato: manter `—` no total sem cobertura e diagnóstico nas opções/alerta externo já existente; tirar o aviso longo e a contagem histórica do corpo do card. Preservar informações de cada fila, agrupamento, GUID/gestão e limite de exibição. Desempate: espera, estado atual, nome e identidade. Visão de empresa mantém ordem anterior. Sem alterações/reinício no Google. Referência: captura e pedido do usuário, DESIGN.md/SUI existentes; ajuste local.

Reproduzido por regressão: ordenação anterior falhou ao priorizar fila com seis chamadas sobre fila com uma chamada. Implementada soma atual de espera+atendimento; empate pela espera, estado atual e nome. Mensagem de cobertura transferida para tooltip do total; diagnóstico externo preservado.

Validação: regressão falhou antes e passou depois; 1.494 checks passaram. Navegador queue-summary passou, conferindo maior uso antes do nome, manutenção da visão por empresa, texto ausente/tooltip de cobertura presente, GUID/gestão/canais e filtros; desktop/mobile sem overflow inspecionados. Detector sem achados; aviso NU1903 preexistente.

## Entrega e limites

Retirados os dois parágrafos do corpo do card: aviso de cobertura e quantidade
de filas sem estado atual. O `—` foi preservado; explicação no tooltip e
diagnóstico nas opções/alertas externos já existentes. Informações de cada fila,
agrupamento e indicação funcional do limite de 40 cartões continuam disponíveis.

Ordenação restrita a gerente sem empresa selecionada: espera + atendimento
atuais, maior primeiro; empate favorece espera e estado atual, depois nome/nó/id
determinísticos. Contagens stale e histórico não aumentam utilização. A visão
por empresa mantém o comportamento anterior. Não há estatística histórica de
uso nesta ordenação: ela representa as observações atuais disponíveis.

A regressão unitária reproduziu o comportamento anterior e falhou antes da
correção, passando depois. Suite final: 1.494 checks. Navegador testou ordem
entre filas com uso diferente, escopo por empresa, texto ausente, tooltip,
agrupamento, filtros, GUID/link, dois canais e ausência de overflow mobile.
Capturas `.impeccable/review/queue-summary-desktop.png` e `queue-summary-mobile.png`
inspecionadas (ignoradas no Git); detector sem achados. Warning NU1903
SignalR.Common10.0.11 preexistente. Fixtures locais, sem chamadas reais nem
sessão autenticada do usuário em produção. Referência visual: captura fornecida,
DESIGN.md/SUI e pedido explícito do mantenedor; sem novos estilos/dependências.

Fonte `80b3d8b`, main publicada. Release no eveo-apps:
`/opt/sufficit-telephony-panel/releases/queue-order-20261113-80b3d8b`.
Anterior preservada `queue-merged-20261113-c1acaba`. Configuração copiada no
servidor, troca atômica pelo helper existente e reinício somente do painel.

DLL SHA256 `5ce140988b986059732db2df720c672c10cd4d131d575c315d9f1666c0e22c32`
igual ao pacote local. Serviço ativo, health interno/socket e público HTTP200,
`/api/fleet` anônimo HTTP401. CSS público SHA256
`8865725c8c020d2c0e81a251524dfeef4e89a64b10dc1277f1bf5b100a64f562`.
Coletor `sufficit-ami-events` manteve MainPID409848. Nenhuma alteração/reinício
no Google ou outros controladores; cobertura parcial permanece uma limitação
separada, já adiada nesta sessão. Todos os checkpoints autorizados concluídos.

Contrato permanente em [operator-board.md](../operator-board.md#contagem-e-cobertura-das-filas).
