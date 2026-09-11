# Chamadas e canais observados

A aba **Chamadas** agrupa os canais autorizados que possuem o mesmo `Linkedid`
no mesmo servidor. SIP/PJSIP e as duas pernas Local podem integrar um único grupo.
A aba **Canais** mantém a visão técnica, uma linha por canal, com estado, origem,
destino, identificação de correlação e ações de supervisão por canal.

Um canal sem `Linkedid` fica como **Canal sem correlação** e não aumenta o contador
de chamadas correlacionadas. Não se usa número, nome, ramal, `Uniqueid` isolado
ou coincidência de identificadores entre servidores para inventar correlação.
Um grupo pode conter somente um canal observado: a coleta é parcial.

Na aba Chamadas, o texto de filtro identifica grupos; ao encontrar um canal, os
demais canais autorizados do grupo continuam visíveis nos detalhes. A autorização
por empresa ocorre antes do agrupamento, sem buscar membros ocultos na central.
A correlação não concede permissão para ouvir/sussurrar: as mesmas validações de
sessão, vínculo, evento recente e confirmação continuam aplicadas ao canal escolhido.

`Down`/“Inativo no evento” não prova encerramento da chamada. O `Hangup` remove
somente seu canal; o grupo deixa a projeção quando não restam canais observados,
ou por expiração/reset da observação. A duração mostrada começa na primeira
observação disponível, não necessariamente no início real da chamada.

Não há deduplicação entre PBXs, reconstrução completa de transferências/conferências,
histórico persistido nem garantia de cardinalidade de atendimentos comerciais.
O painel agrupa a evidência de `Linkedid` recebida; não equivale à métrica de
chamadas ativas coletada diretamente de cada PBX na tela Infraestrutura.

## Validação

`dotnet run --project tests/Panel.Tests.csproj`: inclui correlação das pernas Local,
isolamento por nó, ausência de correlação, filtros, autorização e Hangup parcial.

`tests/call-grouping.browser.cjs` usa somente a fixture Development em
`http://127.0.0.1:5169/?preview=true`, com Playwright já instalado via NODE_PATH.
Confere uma chamada/4 canais/1 sem correlação, expansão por teclado, alvo da
confirmação, filtros e vazio, temas e viewport móvel sem overflow da página.
As capturas `.impeccable/review/call-groups-*.png` são sintéticas, não evidência de
uma chamada real ou do acesso autenticado de um operador em produção.
