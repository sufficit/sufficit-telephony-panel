# Filtro de canais na mesa — 10/09/2026 19:07 -03

## Entrega

Versão 0.8.2 remove a seção lateral redundante “Com canais observados”.
O botão SUI “Só com canais observados” filtra os cartões existentes, desligado
por padrão. Tem aria-pressed, variante preenchida quando ativo, teclado e aviso
de observação parcial. Não infere disponibilidade pela ausência de canais.

O filtro é local à visualização, combina texto/nó/estado, roda antes da paginação
e não altera inventário, retenção, regras ou abas de diagnóstico. Trocar empresa
ou reconectar reinicia os filtros. Desativar restaura os recursos correspondentes
aos demais filtros. Limpeza continua explícita e confirmada.

Software-development orientou plano/checkpoints/validação; sufficit-frontend e
Impeccable/distill preservaram SUI e removeram duplicação sem redesenhar o painel.
Documentação permanente: docs/operator-board.md e DESIGN.md.

## Evidência

- `dotnet run --project tests/Panel.Tests.csproj -m:1 -v:q`: 153 checks aprovados.
- Playwright com NODE_PATH=/tmp/k8sui-edit/node_modules: operator-board,
  board-inventory, board-settings e call-grouping.browser.cjs aprovados.
- Cobertura nova: toggle inicialmente desligado, teclado, combinação com texto,
  75 ramais/1 tronco/1 fila restaurados, Canais com 4 linhas preservadas,
  recursos sem eventos ocultados/restaurados sem mudar retenção.
- Desktop 1977/1440px, móvel 390px, claro/escuro, sem overflow ou erros de página.
  Capturas `.impeccable/review/board-*.png`; desktop e móvel filtrado inspecionados.
- Publish Release e git diff --check sem erros. Servidor local de testes encerrado.

## Publicação

Somente painel Eveo: `/opt/sufficit-telephony-panel/releases/channel-filter-20260910-2206`.
Assembly SHA256: `f4597aa7095414aa57d8e1cd9a310b554d5fe50b6555c491ee993cac162c4b6e`.
Configurações privadas copiadas do release anterior no próprio host. Arquivos
board-rules.json e known-resources.json não foram sobrescritos nem apagados.
Hash das regras permaneceu `3714a5062515639e831067c9d6ee159e2657393d07acd6effb82999ab1c75a43`.
Inventário 0600, sufftelpanel, 265578 bytes na verificação.

Painel PID 2723533, ativo, NRestarts 0; health HTTP 200 e API anônima 401.
Socket gRPC presente; observer PID 2618869 e ami-events PID 2619544 inalterados.
Nenhum Asterisk, Identity, coletor ou Nginx reiniciado. Nenhum commit/push solicitado.
Alterações e planos preexistentes preservados; investigação PJSIP não retomada.

Limite: testes de interface usam fixtures Development identificadas. Sem sessão
real autenticada nesta verificação; saúde e 401 não comprovam esse fluxo.
