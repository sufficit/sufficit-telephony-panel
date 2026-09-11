# Mesa de operação em mosaico — 0.6.0

## Objetivo e estado inicial

Adaptar a referência visual enviada pelo usuário ao painel SUI existente: grade
compacta de ramais, cabeçalhos grafite e coluna lateral de troncos/canais. O painel
anterior usava listas e abas, com correlação de chamadas já implementada.

## Entrega

- Nova aba inicial Mesa de operação, com modelo OperatorTile por recurso e nó.
- Cores com rótulos de evidência, filtros opcionais, seleção por teclado e detalhe.
- Grade paginada (120), troncos (40), recursos ativos (12) e detalhe (100 canais),
  todos com limites explícitos. Cache do agrupamento por snapshot autorizado.
- Temas claro/escuro, duas colunas no celular e trilho abaixo em telas estreitas.
- Chamadas, canais, filas, autorização manager e gRPC compartilhado preservados.
- DESIGN.md/sidecar atualizados sem substituir a identidade ou regras das outras
  abas. README aponta para [contrato permanente](../operator-board.md).

As skills software-development, sufficit-frontend e Impeccable orientaram plano,
reuso SUI, verificação no navegador e revisão independente. Parecer final: ship;
a única correção pedida na revisão foi documentar os overrides compactos.
Nenhuma credencial, URL privada ou dado da imagem foi reutilizado.

## Validação

- `dotnet build src/Sufficit.Telephony.Panel.csproj -m:1`: zero erros/avisos.
- `dotnet run --project tests/Panel.Tests.csproj`: 74 verificações aprovadas,
  incluindo oito novas de estados, classificação e isolamento entre nós.
- `NODE_PATH=/tmp/k8sui-edit/node_modules node tests/operator-board.browser.cjs`:
  aprovado; grade, filtros/vazio, foco por teclado, temas e ausência de overflow.
- Regressão `tests/call-grouping.browser.cjs`: aprovada.
- Release publicado em `/tmp/sufficit-panel-board-20260910-2045`.
- Capturas `.impeccable/review/board-*.png`: 1977px, desktop1440px, escuro e
  móvel390px. Fixture Development sintética; 72 cartões extras somente no mosaico.
- `git diff --check` nos arquivos de entrega: sem erro de whitespace.

## Publicação Eveo

`current` aponta para `/opt/sufficit-telephony-panel/releases/board-20260910-2045`.
Configurações privadas copiadas somente no host; nenhum segredo transferido à
documentação. Reiniciado somente sufficit-telephony-panel, PID 2651319, ativo,
NRestarts=0, memória observada 71.790.592 bytes após início.

- DLL SHA-256: `5a1b3a0f2df82e91186009d525130afceaa4bdef8a328887daf73338f6e624da`.
- CSS local, publicado e resposta HTTPS com SHA-256 idêntico:
  `588cf8ce73cc76b50b831f20f9e160db4a7bea4992b265cec335025b48dbf453`.
- `/telephony-panel/health`: 200; API fleet sem autenticação: 401.
- Socket gRPC conectado entre painel e observador, confirmado pelos descritores.
- Observador PID 2618869 e AMI legado PID 2619544 inalterados.
- Nenhum Asterisk, Identity, coletor ou Nginx foi alterado/reiniciado nesta entrega.
- Rollback disponível pelo release anterior `callgroups-20260910-2030`, trocando
  symlink e reiniciando somente painel. Sem backup adicional no host.

## Limitações explícitas

Cor representa observação, não disponibilidade garantida. Na visão central falta
classificação confiável de ramal/tronco; troncos conhecidos vêm dos cartões da
empresa filtrada. Não inferir classificação pelo nome SIP.

Health e conexão gRPC não comprovam login humano, permissões por empresa ou
inventário completo. Navegador foi validado com fixture, não sessão real de gerente.
A referência não justificou criar botões fictícios de transferência/discagem.
Worktree preexistente preservado; nenhum commit/push nesta solicitação.
