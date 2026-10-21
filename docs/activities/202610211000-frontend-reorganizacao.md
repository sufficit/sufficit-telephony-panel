# Reorganização do front-end (etapas 1, 2, 4 e 6)

Pedido do usuário: avaliar o front-end, instalar skills de front-end e reorganizar
a interface "confusa e bagunçada"; implementar e publicar. Plano:
[PLAN-FRONTEND-REORGANIZACAO](../PLAN-FRONTEND-REORGANIZACAO.md).

## Mudanças
- `Home.razor`: navegação das áreas dentro do cabeçalho (links com `aria-current`),
  rota `/infrastructure`, recarga da área ao navegar no cliente (antes a troca SPA
  para `/queues` podia exigir recarregar a página), chip de demonstração no cabeçalho.
- `LiveOperations.razor`: status do stream ao lado do H1; "Configurar mesa" saiu das
  abas e virou botão; contexto recolhido por padrão (abre com resultados de busca);
  abas antes dos filtros; limites de observação e "Recursos lembrados e limpeza"
  recolhidos abaixo do mosaico; barra "Mesa de operação · Toda a central" removida
  (título só para leitor de tela) e "Tela cheia" na linha de filtros.
- `Pager.razor` novo nas listas de Filas; oculto quando tudo cabe numa página.
- `panel.css`: shell único (gutter 24px, H1 24px, cabeçalho compacto), navegação
  rolável no mobile, abas internas em linha única rolável no mobile.
- `panel.js`: sem preferência salva, tema segue `prefers-color-scheme`.
- `App.razor`: briefing de design virou comentário Razor (não vai mais ao HTML).
- Skills instaladas no usuário: `frontend-design` (plugin oficial), `refero-design`
  (link para o espelho), `web-design-guidelines` (Vercel).

## Validação
- `dotnet run --project tests/Panel.Tests.csproj`: 971 checks PASS.
- 16/16 `tests/*.browser.cjs` PASS com fixture Development (5168/5169). Ajustados
  por mudança intencional: `board-inventory` (abre "Recursos lembrados"),
  `call-grouping` (chip de demonstração), `monitoring-migration` (Infra em `/infrastructure`).
- Capturas desktop/mobile, claro/escuro: sem overflow horizontal nem erros de página.
