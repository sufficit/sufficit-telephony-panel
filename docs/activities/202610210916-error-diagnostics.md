# Diagnósticos de erros e bloqueios de supervisão

Implementação local em 21/10/2026, após investigação do erro no tablet Lenovo.
Sem deploy, alteração de permissões ou originação de chamadas nesta entrega.

## Comportamento entregue

- Alertas de telefonia ao vivo e ações de supervisão mostram detalhes expansíveis
  com referência, UTC e categoria; a mesma referência identifica a operação e a
  stack no log. Exceções não enviam mensagens brutas ao navegador e o registrador
  novo não grava mensagens de exceção, corpos HTTP ou tokens.
- Fontes de filas mostram nó/contexto autorizado, conexão, última leitura e status
  HTTP quando disponível. A consulta falha mantém somente o catálogo anterior,
  marcado indisponível; HTTP 401/403 elimina o snapshot anterior.
- Ouvir/Sussurrar desabilitados possuem explicação textual junto dos controles:
  canal não SIP/PJSIP, empresa ausente/divergente, evento vencido/futuro, fluxo
  desconectado, nó ausente, falta de permissão, solicitação em andamento ou ramal
  preferido ausente. As regras existentes de autorização e validade são preservadas.
- Falha na consulta do ramal preferido recebe diagnóstico próprio e não impede a
  observação central. O áudio continua no ramal telefônico, não no navegador.
- Coluna de supervisão mantém controles sem quebra de palavras em tablet.

Referência visual: screenshot do usuário + DESIGN.md, com SUI e detalhes nativos
já presentes em Contexto. Motivos adjacentes atendem toque sem depender de hover.
Sem Refero MCP disponível; pesquisa proporcional usou referências locais e
anti-ai-slop. Contrato permanente: [error-diagnostics.md](../error-diagnostics.md).

## Evidência de validação

- `dotnet build src/Sufficit.Telephony.Panel.csproj -m:1 --no-restore --nologo`:
  5 projetos, zero erros/avisos de compilação.
- `dotnet run --project tests/Panel.Tests.csproj --no-restore`: **970 verificações
  passaram**. Inclui limites de tempo, injeção, contexto, correlação de IDs,
  sanitização e preservação de HTTP 403/503. O runner informa NU1903 preexistente
  de Microsoft.AspNetCore.SignalR.Common 10.0.11; atualização de dependências não
  fez parte desta tarefa.
- `tests/error-diagnostics.browser.cjs`: passou. Detalhes por teclado/toque,
  motivos de bloqueio, único canal elegível, confirmação sem originação,
  1280/768 pixels, tema escuro, sem overflow da página ou erros JS.
- `tests/call-grouping.browser.cjs`: passou. Contagem/expansão de canais,
  confirmação, filtros, tema escuro e viewport móvel preservados.
- `tests/monitoring-migration.browser.cjs`: passou com
  `PANEL_PREVIEW_URL=http://127.0.0.1:5169`. Seu seletor legado `.queues` não existia
  mais no HEAD anterior à tarefa e foi corrigido para `.queue-queues` para
  exercitar filtros e a nova apresentação compartilhada das fontes.
- Tablet Lenovo HA1NP62H: fixture Development aberta via ADB reverse; detalhes
  expansíveis e motivos confirmados no DOM e em screenshots, sem overflow.
  Capturas locais em `artifacts/diagnostics/`, ignoradas no Git.
- `git diff --check`: passou.

## Limitações e entrega

Arquivos alterados e novos permanecem no checkout local, sem commit/push/deploy.
A fixture não comprova áudio real. A falta de contexto nos canais e os snapshots
com controlador Google desconectado continuam problemas de origem; esta alteração
explica os bloqueios e oferece referências para investigação, sem inventar vínculo
com empresa ou considerar leituras antigas atuais.
