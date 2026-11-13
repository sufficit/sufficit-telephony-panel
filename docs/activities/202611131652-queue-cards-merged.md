# Cartão único por fila

Objetivo: reunir chamadas de todos os servidores em um cartão por fila, preservando identidade, filtros, detalhes e autorização.

1. [concluído] Definir agrupamento seguro e integração com seleção/detalhes/supervisão; registrar contrato e regressão.
2. [concluído] Implementar agrupamento por empresa/GUID, somar estados atuais e misturar chamadas; validar testes e navegador desktop/mobile.
3. [concluído] Commit/push main, publicar somente painel, verificar saúde/hash e converter plano em atividade.

Restrições: não alterar/reiniciar Google nem outros controladores; não unir clientes ou números sem identidade comprovada; manter histórico e alerta de cobertura incompleta. Referência visual: pedido explícito do usuário, DESIGN.md e composição SUI existente; alteração local sem redesign. Servidor permanece nos dados técnicos das chamadas e controles de supervisão, sem separar cartões de fila.

Contrato: agrupar após autorização e filtro explícito de servidor, antes de busca/estado; seleção estável por empresa/GUID. Preservar linhas/canais originais e somar snapshots atuais uma vez por nó, sem trazer contagem stale. Números sem identidade comprovada permanecem separados; não disparar milhares de lookups. Regressão antes da implementação falhou: “Same company/GUID must produce one queue card across servers”.

Validação: 1.489 checks passaram, incluindo soma por nó, deduplicação, histórico/stale, isolamento de empresa, seleção estável e prefixos protegidos por nó. Navegador: queue-summary, queue-board, queue-resource-details e board-supervision passaram; capturas desktop/mobile inspecionadas; detector sem achados. Warning NU1903 preexistente. Fixtures locais, sem chamadas reais ou sessão autenticada de produção.

## Implementação e decisões

`QueueCardGrouping` reúne identidades empresa/GUID após autorização e filtro de
servidor. A interface apresenta chamadas de todos os nós no mesmo cartão, sem
linha de servidor, e preserva GUID/link de gestão. Seleção é estável quando muda
o conjunto de nós. Ramais, troncos e inventário persistido permanecem intactos.

Snapshots atuais somam pending/connected/fases uma vez por nó; aliases de
cartão não duplicam contagens. Estado antigo não vira atividade atual; réplicas
históricas se incorporam à fila comprovada. Linhas originais são preservadas e
um resumo separado serve apenas à apresentação. Canais e fluxos preservam
identificador e nó para correlação/supervisão. Prefixos explicitamente protegidos
continuam vinculados aos nós originais observados; nenhuma autorização de envio
foi ampliada. Agrupamento é reutilizado enquanto fonte e filtro de nó não mudam.

Limite deliberado: nome/número sem GUID/empresa comprovados não une clientes.
Registros assim continuam separados; resolução do detalhe funciona como antes.
Não há milhares de consultas de identidade a cada atualização. A cobertura
Google permanece parcial, indicada por `—` e aviso global, conforme entrega
anterior. Essa pendência de controlador segue adiada e fora desta publicação.

## Evidência de validação

A regressão com dois cartões da mesma fila falhou no comportamento anterior
sem agrupamento e passou após a implementação. Suite final: 1.489 checks,
incluindo identidades contraditórias, empresa diferente, aliases por nó, stale,
histórico, idempotência e opções de prefixo por nó. Warning NU1903 preexistente
SignalR.Common10.0.11 não foi tratado neste escopo.

`queue-summary.browser.cjs` conferiu um cartão com duas chamadas de nós
diferentes, soma, dois canais originais no detalhe, GUID/link, fechamento por
Escape, filtro de nó e ausência de overflow. `queue-board.browser.cjs`,
`queue-resource-details.browser.cjs` e `board-supervision.browser.cjs` também
passaram, cobrindo fases, estado indisponível, legado, filtros, teclado, gestão
e supervisão sem chamadas reais. Desktop/mobile inspecionados em
`.impeccable/review/queue-summary-*`; detalhes/supervisão incluíram temas claro
e escuro. Detector mecânico sem achados. Houve uma execução inicial do navegador
com texto antigo porque o processo local ainda usava o build anterior à troca
de tradução; após recompilar/reiniciar somente a fixture, o teste passou.

Navegador usa fixtures locais. Não houve sessão autenticada de produção e não
se afirma que isso foi validado com a conta do usuário. Referências visuais:
pedido do mantenedor, captura existente, DESIGN.md e componentes SUI; alteração
local sem novo estilo ou dependência. Sem nova programação de renderização.

## Entrega

Fonte commit `c1acaba`, push main. Build Release publicado somente no eveo-apps:
`/opt/sufficit-telephony-panel/releases/queue-merged-20261113-c1acaba`.
Anterior preservada `queue-summary-20261113-9f6b09e`; configuração copiada no
servidor, troca atômica e reinício somente de `sufficit-telephony-panel`.

DLL SHA256 `f49a9cdbe32b2c245b3e3c661992b41e7b9a4dc81c785f768f065f407508b7cc`,
igual ao pacote local. Serviço ativo, health interno/socket e público HTTP200,
`/api/fleet` anônimo HTTP401. CSS público
`8865725c8c020d2c0e81a251524dfeef4e89a64b10dc1277f1bf5b100a64f562`.
Coletor `sufficit-ami-events` manteve MainPID409848. Nenhuma alteração ou reinício
Google/controladores. Todos os checkpoints do escopo autorizado concluídos.

Contrato permanente: [operator-board.md](../operator-board.md#cartão-único-por-fila).
