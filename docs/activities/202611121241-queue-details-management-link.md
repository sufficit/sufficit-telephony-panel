# Identidade e gestão nos detalhes da fila

Objetivo: ao clicar em uma fila da mesa, exibir GUID completo e acesso direto à gestão no Blazor, com conteúdo próprio de fila.

1. [concluído] Confirmar identidade disponível, rota real da gestão e referências visuais existentes.
2. [concluído] Implementar detalhes de fila, estados sem identificação e link seguro; validar suíte e navegador desktop/mobile, claro/escuro.
3. [concluído] Registrar evidências, revisar diff, converter plano em atividade e preparar entrega na main.

Restrições: deploy adiado explicitamente pelo mantenedor; não publicar painel/controladores. Preservar componentes/tokens SUI e restante da mesa. Não deduzir GUID ou empresa de número legado nem criar link para identidade não comprovada.

Direção/ledger inicial: captura do usuário + DESIGN.md e modal existente são referência primária; nome e estado primeiro, GUID/empresa agrupados, ação “Gerenciar fila no Blazor” explícita em nova aba. Não mostrar registro SIP/IP de aparelho em fila. Ajuste local, sem nova linguagem visual. Refero sem MCP: referências locais anti-ai-slop e screenshot do usuário. Consulta proporcional a Lovable dashboards e Dribbble telecom-dashboard realizada; referências externas não substituem SUI nem comprovam interação.

Contrato confirmado no Blazor: `/pages/telephony/queue/edit?contextid={N}&queueid={N}`. Identidade nativa existe em metadados e na chave `queue:{contexto}:{fila}` preservada no inventário; cartão legado numérico não comprova GUID/contexto. Não consultar banco nem inferir mapeamento por título/número.

Validação: build sem erros/avisos; 1.441 checks passaram; navegador nos quatro casos desktop/mobile × claro/escuro, PT/EN, teclado, link/contexto exatos, noopener, ausência de campos SIP e fallback legado. Detector mecânico: nenhum achado. Capturas em .impeccable/review/queue-details-*.png, sintéticas. O primeiro harness .NET expôs escrita concorrente não sincronizada na lista do Fake HTTP de RepresentativeQueueChecks; protegida por lock, sem alteração de produto nesse teste. Playwright disponível no cache 1237 foi usado após identificar que outra instalação apontava para Chromium ausente.

## Entrega

Implementação: `QueueResourceDetails` com componentes SUI, rótulos PT/EN e GUIDs
selecionáveis; identidade explícita e rota conferida no QueueNavigation e na
QueueEditPage do Blazor. Registro/alcance/IP de aparelhos removidos somente da
ramificação de filas. Nome/estado seguem primeiro, identidade e ação agrupadas
em seguida; cores, fontes, modal e restante da mesa preservados.

Referências de pesquisa: https://lovable.dev/templates/apps/dashboards e
https://dribbble.com/search/telecom-dashboard (consulta proporcional; nenhum
redesign ou comportamento deduzido de shots). Referência primária efetiva:
DESIGN.md, captura fornecida e componentes SUI existentes. As referências locais
do Refero orientaram preservação do produto e ausência de novos cards decorativos.
Ledger: GUID/empresa juntos → pedido/rota real; ação explícita → botão SUI;
quebra de identificadores → mobile/craft; sem SIP em fila → conteúdo específico;
fallback honesto → ausência de metadados no cartão histórico. Metadados auxiliares
do Impeccable estão defasados do DESIGN.md; não foram alterados fora deste escopo.

Evidências no disco: `.impeccable/review/queue-details-desktop-light.png`,
`queue-details-desktop-dark.png`, `queue-details-mobile-light.png`,
`queue-details-mobile-dark.png` e `queue-details-legacy-mobile.png` (ignoradas no Git).
Capturas inspecionadas: desktop claro, mobile escuro e fallback mobile claro,
sem corte de GUID ou overflow de página; tabela de canais mantém rolagem local.
Link e teclado validados sem navegar nem editar cadastro de produção.

Validação adicional: `tests/queue-board.browser.cjs` passou, cobrindo fases,
stale/empty, filtros, overlay e PT/EN/mobile. O build sem avisos foi o projeto
principal; a suíte ainda informa advisory preexistente SignalR.Common 10.0.11.
Fixtures não comprovam login, permissão de edição nem aceitação em produção.

Commit/push da fonte na main, no commit associado a este registro. Sem deploy:
o mantenedor pediu aguardar horário mais calmo. O pacote de painel preparado na
tarefa anterior em `/tmp/sufficit-global-queue-release/panel` não contém esta UI;
gerar novo publish da main antes da futura publicação coordenada.
