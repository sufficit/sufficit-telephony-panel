# Reorganização do front-end

Objetivo: tornar o painel legível de relance. Hoje o conteúdo principal (mosaico)
começa abaixo da dobra, há dois idiomas de abas, três layouts de página diferentes e
textos explicativos no lugar de hierarquia. Não muda backend, autorização, contratos
nem regras de supervisão. Avaliação feita em 21/10/2026 com fixture `?preview=true`
(desktop 1440 e mobile 390, tema claro/escuro).

## Diagnóstico

### Arquitetura de informação
- `Home.razor` atende `/`, `/board` e `/queues` e ainda contém a página inteira de
  infraestrutura inline; a área ativa é decidida por flags (`BoardPage`, `QueuePage`,
  `Operational`). "Infraestrutura e histórico" não tem rota própria (some no reload).
- `LiveOperations.razor` (≈820 linhas) concentra seleção de empresa, status do stream,
  lembrança de recursos, filtros, 7 abas, paginação, supervisão e ajuda.
- Duas camadas de abas: navegação principal com botões preenchidos e abas internas
  sublinhadas. "Fila de chamadas" aparece nas duas (aba interna + página `/queues`).
- "Configurar quadro" é aba com contador (sempre 0) misturada a vistas de dados; é
  configuração, não observação.
- "Recursos lembrados e limpeza" abre expandido acima do mosaico; "Simular eventos
  ausentes" (ferramenta de teste) fica no fluxo principal.

### Hierarquia visual
- No desktop o mosaico começa em y≈920px; no mobile a primeira tela é só cabeçalho,
  contexto e abas.
- Contexto da página muda de escala: H1 24px em Ao vivo × 40px em Filas/Infra;
  gutter 18px × 40px conforme a aba ativa (a página "pula" ao trocar de aba).
- Faixa "Demonstração local" com o mesmo peso de alerta real em todas as páginas.
- Parágrafos de disclaimer (observação parcial, correlação, Linkedid, capacidade)
  repetidos no corpo; deveriam ser tooltip/ajuda ou um único rodapé de seção.
- Filtro de contexto inconsistente: Ao vivo usa campo "Empresa (opcional)" + botão;
  Filas usa select "Contexto". Botão "Buscar empresa" parece desabilitado.
- Paginação de Filas usa botão preenchido apagado (parece desabilitado mesmo ativo);
  em Ao vivo é botão texto. Texto de ajuda dos filtros em Filas maior que os rótulos.
- Tema escuro ignora `prefers-color-scheme` (só alterna manualmente).

### Código
- Pages/ tem 13 componentes num diretório só, sem separação página × componente.
- `panel.css` global (≈250 linhas, 54 media/seletores) mistura layout de quadro,
  diálogos e overrides do SUI com prefixo `.panel-shell:not(.board-only)`.
- `App.razor` carrega comentário de briefing de design no `<body>` (vai para o HTML).

## Estrutura alvo

```
Cabeçalho fixo: marca · navegação (Ao vivo | Filas | Infraestrutura) · contexto · idioma/tema/sair
Barra de status (uma linha): stream conectado · último evento · [demo] chip
Ao vivo:   filtros em uma linha → mosaico (ramais | troncos+filas) já na 1ª dobra
           vistas secundárias: Ramais · Chamadas · Canais · Eventos (segmented control)
           Configurar quadro → rota /board/settings (somente manager)
           Recursos lembrados / simulação → dentro de Configurar quadro
Filas:     mesmo shell, mesmo gutter/H1; tabela de filas + presença/registro
Infra:     rota /infrastructure própria
/board:    inalterado (TV), só herda tokens
```

## Etapas

1. [pendente] Shell único: `MainLayout` com cabeçalho, navegação e barra de status;
   uma escala de título e um gutter para todas as rotas; rotas próprias para
   `/`, `/queues`, `/infrastructure`, `/board`, `/board/settings`. Contexto (empresa)
   vira um seletor só, compartilhado e persistido na URL.
2. [pendente] Ao vivo: mosaico na primeira dobra; filtros compactos em uma linha;
   vistas secundárias como segmented control; remover "Configurar quadro" e
   "Recursos lembrados" do fluxo principal; disclaimers em ajuda contextual.
3. [pendente] Quebrar `LiveOperations.razor` em componentes (`ContextPicker`,
   `StreamStatus`, `BoardFilters`, `CallsView`, `ChannelsView`, `EventsView`) e mover
   para `Pages/` (rotas) × `Components/` (peças).
4. [pendente] Filas e Infra no mesmo padrão; paginação e botões uniformes; chip de
   demonstração discreto; tema escuro seguindo o sistema na primeira visita.
5. [pendente] CSS: escopo por componente (`.razor.css`), `panel.css` só tokens e
   quadro; remover overrides de SUI que existam por causa do layout antigo.
6. [pendente] Validação: testes `tests/*.browser.cjs` atualizados, capturas
   desktop/mobile claro/escuro em `.impeccable/review/`, revisão com `impeccable`
   e `web-design-guidelines`.

Restrições: textos novos em `Localization/Portuguese.json`; nada de feature nova;
`/board` (TV) não pode regredir; supervisão continua exigindo confirmação explícita.
