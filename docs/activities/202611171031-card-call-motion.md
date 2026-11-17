# Movimento dos estados de chamada nos cards — 17/11/2026

Objetivo: distinguir visualmente a tentativa de discagem do toque confirmado nos cards de ramais, preservando o layout aprovado e os tokens SUI.

1. CONCLUÍDO — Identificar estados observados, associação canal/card e infraestrutura de validação.
2. CONCLUÍDO — Implementar indicadores distintos de discagem e ringing, com rótulos e reduced-motion.
3. CONCLUÍDO — Validar lógica, build e renderização no navegador; registrar evidências.
4. CONCLUÍDO — Plano convertido em atividade; arquivos e diff revisados para entrega na main. Referência Git: commit que contém esta atividade (feat: distinguish dialing and ringing card motion).

Aceitação: discagem e ringing têm movimentos diferentes; atendimento/encerramento removem o efeito; ausência de evidência não inventa toque; texto continua legível nos dois temas, no móvel e com movimento reduzido.

Referências: captura enviada pelo usuário; DESIGN.md (hierarquia/tokens existentes); Refero references/motion.md (movimento funcional, sem deslocar layout, preferência reduced-motion). Consultados https://lovable.dev/templates/apps/dashboards e https://dribbble.com/search/telecom-dashboard (busca telecom dashboard): catálogos de dashboards; não demonstram transições reais de telefonia e não fundamentam estado observado. Não adotar gráficos/KPIs/layouts novos para esta correção localizada.

Reference lock: manter grade, densidade, tipografia, menus e cores semânticas atuais. Discagem usa warning com movimento de progresso; ringing usa info com pulso de toque; somente quando sustentados pelos dados. CSS local sem dependências/timers de renderização. Com reduced-motion, manter indicação estática e rótulos distintos. Repetição enquanto estado está ativo é requisito operacional do usuário, exceção consciente à orientação de evitar loops decorativos.

Validação prevista: dotnet run --project tests/Panel.Tests.csproj; dotnet build src/Sufficit.Telephony.Panel.csproj; teste de navegador com estados de demonstração, temas, transições e reduced-motion.

Descoberta: LiveFormat já diferencia Ring/4 (Calling) de Ringing/5 (Ringing), mas OperatorTile os agrupava. Down/0 não comprova discagem; manter sem animação. Usar somente Rows do próprio card, nunca canais correlacionados de outros destinos. Fonte técnica: https://docs.asterisk.org/Latest_API/API_Documentation/AMI_Events/Newstate/.

Regressão reproduzida antes da correção: teste falhou em Ring classificado como ringing. Implementação separa estados textuais/numéricos e preserva contagens e identidade DOM por card.

Validação: 1.527 checks PASS; build 5 projetos/0 erros/0 avisos; card-call-motion.browser.cjs PASS em Chromium (movimento efetivo, texto imóvel, discagem/toque/atendido/sem canal, temas, mobile, reduced-motion e teclado). Capturas inspecionadas em .impeccable/review/card-*.png. operator-board.browser.cjs falha na expectativa antiga de 1 tronco (fixture atual contém 4); esta tarefa não altera os troncos. Teste novo ajustado para sufixos de CSS isolation, fase estável no ciclo e inicialização determinística do tema. Estado sem canal ainda pode ser busy pela evidência independente de dispositivo, mas não anima.


## Decisões e limites

| Decisão | Fonte | Aplicação |
| --- | --- | --- |
| Preservar grade e superfície | Captura do usuário e DESIGN.md | Sem redesenhar os cards ou alterar os menus |
| Discagem âmbar, toque azul | Papéis warning/info do DESIGN.md | Movimento acompanha estado já observado, sem inferência pelo número discado |
| Progresso de discagem e pulso duplo de toque | Pedido do usuário e Refero motion.md | Animação funcional por CSS, sem timers de renderização ou dependências |
| Indicação estática com movimento reduzido | Refero motion.md e política CSS existente | Mantém texto e formas distintas |

Pesquisa externa proporcional: os catálogos Lovable e Dribbble foram consultados,
mas a implementação está ancorada na captura, contrato existente e referência de
motion. Não foi inferida acessibilidade, responsividade ou interação das imagens
dos catálogos. Refero MCP não está disponível nesta sessão.

Evidência visual: `.impeccable/review/card-dialing-detail.png`,
`card-ringing-detail.png` e capturas light/dark/mobile no mesmo diretório
(artefatos locais ignorados pelo Git). Capturas de detalhe e móvel inspecionadas.
O ensaio usa fixture Development com estados sintéticos; não realiza chamadas
reais nem comprova propagação da telefonia em produção. Canais Down/unknown
continuam sem animação por ausência de evidência de discagem. Se o PBX reportar
somente Down antes de Ringing, não haverá progresso âmbar nesse intervalo.

Validação adicional: teste legado `tests/operator-board.browser.cjs` interrompido
por sua asserção de 1 tronco contra 4 já existentes em HEAD; confirmado com
`git show HEAD:src/Pages/LiveOperations.razor.cs`. Não alterado nesta tarefa.
O restore da suíte de C# também reportou NU1903 preexistente para
Microsoft.AspNetCore.SignalR.Common 10.0.11; nenhuma atualização de pacote foi
incluída. O build separado terminou com zero avisos e zero erros.

Entrega: commit e push direto na main conforme preferência do mantenedor.
Sem publicação/deploy solicitado ou executado. As verificações do navegador
usam `NODE_PATH=/home/hugodeco/.npm/_npx/31e32ef8478fbf80/node_modules` com
Playwright já disponível no ambiente.


Publicação posterior autorizada e concluída: [evidências do deploy](202611171038-deploy-card-call-motion.md).
