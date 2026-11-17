# Mesa de operação

## Página dedicada e links compartilháveis

O domínio público agora abre sem prefixo: `https://panel.sufficit.com.br/`.
The dedicated board is `/board`; `/mesa` remains a compatibility redirect.
Only the mosaic is shown. Filters and display options open from the icon beside
the extension count. Details open in a popup; normal-page/settings links open
another tab. Fullscreen uses the board container, not the whole normal page.
On desktop, trunks and queues share available viewport height according to content:
short/empty trunks leave room for queues, with internal scrolling only when needed.
On mobile, sidebar lists follow natural height in the responsive document flow.

### Resizing sections

On screens wider than 900px, drag the divider between extensions and the sidebar
to change their widths; drag the divider between trunks and queues to change
their heights. Narrow visible grips highlight on hover/focus. Minimum sizes keep
each section accessible. The normal board and dedicated `/board` share this layout
preference; filters, selected contexts and authorization are unchanged.

The browser stores only `{ version: 1, columns, rows }` under
`telephony-panel-board-layout-v1` in localStorage. Values are proportions, not
fixed pixel dimensions. `rows: null` means automatic content-based sizing: short
trunk lists leave room for queues. Moving the horizontal divider selects a manual
height ratio. The preference survives reloads; it is local to this browser/origin,
not synchronized to other devices or users. No tenant data is stored there.

Focus a divider and use the corresponding arrow keys (2% steps; Shift: 10%).
Home/End select the size limits; Enter or double-click resets that divider.
Escape cancels an active drag. On narrower screens the existing stacked layout is
used and the desktop preference is retained. Malformed/blocked storage never
breaks the board: resizing still works for the current session.

Example: `/board?contextid=d21cfb049d37473b837c67591a26feed&q=6007&text=peers`.

- `contextid`: company UUID; omitted means the manager-authorized entire exchange.
  Generated links use the compact 32-character form. Legacy `company` is accepted
  on input and replaced with `contextid` without adding a history entry. If both
  are present, `contextid` wins; invalid/duplicate canonical values never fall back
  to a different legacy company. Clear removes both keys. URLs do not grant access.
- `q`: texto (máximo 100 caracteres), aplicado aos tipos escolhidos.
- `text`: combinação `peers,trunks,queues`; omitido afeta tudo. `none` não aplica
  texto a nenhum tipo. Um tipo desmarcado continua visível, sujeito aos outros filtros.
- `node`: servidor; omitido significa todos.
- `state`: online, offline, registered, unregistered, busy ou unknown; omitido=todos.
- `channels=true`: somente cartões com canais observados; omitido=desligado.
- `tab` e `page`: aba e página (base zero), omitidos na mesa/página inicial.

Controles atualizam a URL; recarregar, compartilhar e voltar/avançar restauram
preferências. Digitação substitui a entrada atual para não gerar histórico por
caractere. Trocar empresa ou reconectar preserva os demais filtros. Limpar filtros
remove também empresa, sem alterar regras ou inventário. URLs nunca concedem acesso:
autorização de gerente/empresa continua conferida no servidor. Texto pode conter
dados pessoais; compartilhar links apenas com pessoas autorizadas.

Retenção e regras são configurações compartilhadas, não filtros de URL. Permanecem
na área expansível “Recursos lembrados e limpeza” e em “Configurar mesa”.

A aba inicial usa o mosaico compacto solicitado: recursos à esquerda, troncos
conhecidos e filas à direita. As abas Ramais, Chamadas,
Canais e Filas permanecem disponíveis. O mosaico usa os mesmos dados autorizados
e a mesma conexão gRPC compartilhada, sem AMI por cartão ou navegador.

## Leitura

Cada cartão pertence a um servidor. Verde indica evidência de registro ou alcance,
nunca disponibilidade garantida. Âmbar indica canais observados/uso; azul, toque;
vermelho, ausência de registro ou resposta; neutro, informação ausente. Os rótulos
explicitam a evidência, sem depender apenas de cor. O horário e as evidências
separadas de registro, alcance e uso estão no detalhe selecionável por teclado.

Um evento antigo continua sendo uma observação, não inventário reconciliado.
Mesmo canal Down não prova chamada encerrada. As restrições da projeção e da
[correlação de chamadas](call-channel-correlation.md) continuam aplicadas.

Na central geral, os eventos não fornecem classificação segura de tronco/ramal.
Use [Configurar mesa](board-configuration.md) para classificar explicitamente,
agrupar aliases e ocultar recursos. Ao filtrar empresa, os cartões autorizados do
portal continuam sendo a base; as regras de apresentação são aplicadas depois.
Não há inferência automática por prefixos nem fabricação de cadastro ausente.

Na visão central, canais técnicos como `Local/...;1` e `Local/...;2` não geram
cartões de ramal. Permanecem na aba Canais e na correlação das chamadas. Cartões
derivados de canais usam apenas identidades SIP/PJSIP/IAX2; o sufixo hexadecimal
da instância do canal é removido, sem truncar sufixos não hexadecimais do endpoint.
Isso não classifica automaticamente ramal versus tronco: as regras FOP continuam
responsáveis por essa distinção. Não há limpeza do inventário na atualização.

## Interação e limites

- [Manter recursos já vistos](retained-resources.md) vem ligado por padrão: guarda
  identidades sem congelar estados ou canais; limpeza explícita e confirmada.

- Empresa continua filtro opcional. Texto, nó e estado filtram o mosaico.
- **Só com canais observados** é um botão liga/desliga, inicialmente desligado,
  que substitui a antiga seção lateral duplicada. Filtra ramais, troncos e filas
  após as regras e junto dos demais filtros, antes da paginação. Usa canais
  presentes no snapshot e entradas observadas na fila, não apenas registro ou alcance.
  Não altera inventário, retenção, regras ou abas de diagnóstico. Desligar restaura
  os demais cartões. A preferência é preservada na URL, inclusive ao reconectar.
- Na página dedicada, Clique/Enter abre detalhes em um popup sobre o mosaico,
  sem acrescentar conteúdo abaixo nem sair da página. Nenhuma operação telefônica
  é disparada. Ouvir/Sussurrar mantém confirmação por canal.
- Paginação de 120 ramais por página; até 40 troncos e 40 filas
  na coluna lateral, com aviso quando excedidos. Refine filtros para os demais.
- Grid com rolagem local, colunas adaptativas e duas colunas no celular; lateral
  abaixo da grade em telas estreitas. O detalhe mostra até 100 canais, com aviso.
- Estado derivado e agrupamento reutilizam o snapshot autorizado. Não existem
  comandos novos de transferência, discagem, encerramento ou disponibilidade do
  operador. A toolbar da imagem de referência não foi copiada como ações fictícias.

## Correlação de chamadas e atividade das filas

- Os cartões mostram um resumo por `node + Linkedid`; os canais técnicos da mesma
  chamada ficam nos detalhes, sem gerar linhas de chamada duplicadas. Sem Linkedid,
  o canal continua explicitamente sem correlação. Não se juntam servidores por número.
- O contrato de fila do cadastro (`^Local/<extensão>`) é compatibilizado com o
  identificador nativo dos eventos QueueCaller. Isso não altera o plano de discagem.
- QueueCallerJoin indica espera; AgentConnect mantém a associação durante o
  atendimento; AgentComplete/abandono/desligamento removem a atividade. QueueCallerLeave
  remove a espera, mas não apaga um AgentConnect recebido antes dele.
- Campos vazios ou `<unknown>` não substituem origem, destino ou Linkedid conhecidos.
  NewConnectedLine atualiza o destino. O nome autorizado da fila aparece no resumo
  quando existe associação ativa com ela.
- Quando um evento de fila omite AccountCode, somente o canal exato no mesmo nó
  pode fornecer essa evidência. O filtro de contexto continua obrigatório para
  expor chamadas de um cliente. Canais internos relacionados também precisam do
  AccountCode do cliente; o nome da fila não concede acesso.
- Retenção de cartões não retém chamadas antigas. Ausência de eventos continua
  sendo estado desconhecido, não prova de fila vazia. A coleta não reconstrói chamadas
  encerradas antes da conexão ou perdidas em uma lacuna do observador.

## Testes visuais e de fluxo

`tests/operator-board.browser.cjs` usa a fixture Development na porta local 5169,
com Playwright existente através de NODE_PATH. A fixture possui 72 cartões extras
apenas no mosaico para testar densidade; não modifica contagens da fixture de
chamadas/canais e jamais é ativada por query string em Production.

Capturas em `.impeccable/review/board-*.png`: claro 1440px e 1977px (largura da
referência), escuro 1440px, móvel 390px. São dados fictícios explicitamente
identificados; não comprovam fluxo autenticado nem inventário real.

## Contagem global das filas ARI

A visão com direito global lê `GET /api/queue/nodes/{nodeId:N}/snapshot` de cada
fonte `AnyContext`: uma requisição por nó, compartilhada entre sessões com cache
de dois segundos, até quatro consultas simultâneas e orçamento total de cinco
segundos. O agregado suporta até 1.000 contextos por nó e 4 MiB. Fontes sem
`AnyContext` continuam limitadas aos contextos fixos. Empresa selecionada e
carteira de representante usam somente seus contextos autorizados; nunca consultam
o agregado global. A autorização é revalidada depois da leitura.

Filas comerciais migradas para ARI não produzem os eventos `QueueCaller*` do
Asterisk. Recursos lembrados também não comprovam estado atual. Por isso, os
contextos fixos de laboratório e os cartões históricos não substituem o snapshot
global dos controladores. O total de espera usa `pending` atual e autoritativo,
mesmo se a lista de visitas vier truncada; visitas já atendidas não contam como
espera. Na ausência desse total, usa apenas visitas observadas ainda não atendidas.
Chamadas nos troncos incluem atendimentos e outras etapas e não implicam espera.

Uma falha do agregado mantém somente nomes conhecidos com atividade indisponível;
401/403 descartam o snapshot em cache. O alerta de fontes informa a falha, inclusive
404 em controlador antigo. Preservar o inventário lembrado, sem recriar estado
atual a partir dele. O painel pode ser instalado antes dos controladores: se a rota agregada inicial
retornar 403/404, preserva os snapshots dos contextos fixos e mostra aviso de cobertura
global incompleta. A cobertura global exige um endpoint agregado disponível,
por atualização dos controladores/fachada ou configuração de `GlobalBaseUrl`
quando o controlador já o fornece. Commit e testes locais não comprovam publicação
nem aceitação em produção.

## Identificação e gestão nos detalhes da fila

Ao clicar em uma fila, os detalhes mostram o GUID completo da fila e da empresa,
com botão para editar exatamente esse cadastro no Blazor em nova aba. O endereço
usa `Panel:BlazorBaseAddress` (padrão `https://blazor.sufficit.com.br`) e a rota
`/pages/telephony/queue/edit?contextid={N}&queueid={N}`. A autorização para gestão
continua sendo aplicada pelo Blazor; visualizar a fila no painel não concede edição.

A identidade vem dos metadados do controlador ou da chave canônica
`queue:{empresa}:{fila}`, também preservada para filas nativas lembradas.
Cartão legado numérico sem GUID/contexto comprovados mostra a limitação e um botão
para a lista de gestão, sem inventar vínculo por nome ou número. Identidades
conflitantes também não geram destino de edição. Os detalhes de filas não mostram
registro SIP, alcance de ramal nem IP/aparelho; canais e supervisão permanecem.

Atualização operacional de 12/11/2026: painel liberado pelo mantenedor para
publicação a qualquer momento; cuidado de horário reservado ao servidor Google.
A release `queue-details-20261112-cd7141e` publicou os detalhes de GUID/gestão e a
compatibilidade 403/404 somente no eveo-apps, sem alterar controladores. Evidências
em [publicação do painel](activities/202611121254-publish-panel-queue-details.md).

## Resolução dos cartões numéricos antigos

Ao abrir uma fila sem GUID, o painel consulta a API central com o token do usuário
em `GET telephony/panel/queue-identity?extension={numero}&contextid={opcional}`.
A consulta usa o identificador exato do cartão, preservando zeros; não usa título
nem regex genérica. A resposta é conferida por número e escopo e a autorização é
revalidada. O resultado vale para o detalhe aberto; não há busca por cartão a
cada refresh. Trocar/fechar o detalhe cancela a solicitação. Erros têm nova tentativa,
resultado ambíguo orienta selecionar empresa e ausência não inventa identidade.

A API cruza vínculos numéricos preservados da migração com GUID/empresa presentes
no cadastro nativo atual; não reativa suff_queue, não concede SQL ao monitoramento
e não depende de alterar os controladores Google/ARI. A gestão continua aplicando
sua própria autorização. Vínculo removido do catálogo nativo não gera edição.

## Contagem e cobertura das filas

O cabeçalho mostra chamadas em espera / filas distintas com dados atuais. Filas
nativas são identificadas por empresa e GUID, somando uma fila mesmo quando
observada em mais de um servidor. Cartões sem identidade comprovada permanecem
separados. Cartões históricos ou com estado indisponível não entram nessa
contagem. Na visão global do gerente, filas são ordenadas pela utilização atual
(chamadas ainda aguardando), maior primeiro; empate prioriza estado atual,
nome e identidade. A visão por empresa mantém atividade/estado atual/nome.
Avisos explicativos e contagem histórica não ocupam o corpo do card.

Se alguma fonte estiver indisponível, desatualizada ou sem cobertura, o total
em espera aparece como `—`; o motivo fica no tooltip do total e no diagnóstico
das opções/alertas externos. Zero só aparece com cobertura completa das fontes configuradas. O histórico permanece disponível nos filtros
e detalhes; a mudança não remove filas nem modifica os controladores.

## Cartão único por fila

Após autorização e filtro explícito de servidor, o quadro reúne a mesma empresa
e GUID em um cartão. Chamadas e canais de todos os nós aparecem juntos; o cartão
mostra a soma das chamadas ainda aguardando nos snapshots atuais, uma vez por nó.
Réplicas históricas não criam outro cartão. Estados antigos não entram na soma;
uma fila parcialmente observada indica dados parciais, além do aviso global.

A seleção usa identidade estável de empresa/GUID, preservando detalhes e link de
gestão. Os cartões de fila não exibem o servidor. Os canais mantêm nó e
identificador original para supervisão; prefixos protegidos continuam associados
aos nós originais e exigem as mesmas validações no servidor. Busca e filtros
de atividade atuam na fila reunida. Ramais/troncos e inventário persistido não
mudam. Agrupamento em memória reutiliza o resultado enquanto dados/nó não mudam.

Um nome ou número antigo, sem GUID/empresa comprovados, não autoriza unir filas
de clientes diferentes. Esses registros permanecem separados até identificação;
a resolução ao abrir detalhes continua sem disparar consultas por todos os cartões.

## Fonte global separada da fachada de registro

Uma fonte `AnyContext` pode definir `GlobalBaseUrl` e `GlobalKeyFile` juntos,
somente em configuração administrativa. O agregado `/api/queue/nodes/{node}/snapshot`
usa esse par; snapshots por empresa e diagnósticos mantêm `BaseUrl`/`KeyFile`.
Ambos os endereços obedecem à mesma restrição de IP privado e credenciais locais
protegidas. A ausência do par mantém o comportamento anterior. Escopo, cache,
limites, validação e autorização não mudam.

No Google, a fachada de registro na porta18190 não expõe o agregado global.
O controlador18189 já o fornece. `deploy/90-google-global-queue.conf` direciona
somente o agregado ao controlador, preservando os registros/diagnósticos na
fachada e usando uma credencial de monitoramento existente. Essa correção exige
somente atualização/reinício do painel, sem alteração do serviço Google.

Em 13/11/2026, a leitura pelo usuário do serviço, com a DLL instalada e configuração
efetiva, confirmou cobertura válida de todos os três nós: Google464 contextos e
1.161 filas; E/A463 contextos e1.159 filas cada. Na verificação pós-ativação havia
4 chamadas em espera e5 em atendimento, com8 filas ativas nas primeiras40. Esses
valores são uma observação daquele instante, não constantes do produto.

## Atendimento encerra a participação na fila

Na apresentação do quadro, fila contém somente quem ainda não foi atendido por
um ramal. Saudação, anúncio, espera e chamada do atendente continuam na fila;
`connected`/`answered` deixam de aparecer nos fluxos, canais de detalhes e atividade
do cartão da fila. O cabeçalho e a ordenação global contam somente espera.
A chamada atendida continua nos ramais/troncos conforme seus canais autorizados.
Uma fila configurada pode permanecer visível vazia, sem a chamada atendida.

Os snapshots e o histórico do controlador preservam a contagem original de
atendimentos para os demais diagnósticos. O filtro ocorre na apresentação e
não encerra chamadas, altera roteamento ou muda a autorização. É o atendimento
do ramal, não o estado `Up` do canal do chamador, que remove a chamada da fila.


### Toque no destino e estado da origem

Uma origem PJSIP pode continuar com estado de canal Ring enquanto o destino SIP
já está Ringing. A origem apresenta **Destino tocando** e o destino **Tocando**
quando há correlação autorizada por linkedid no mesmo nó, sem contas conflitantes.
O estado AMI original permanece disponível no detalhe. Sem evidência correlacionada,
a origem mantém Discando. Canais Local não confirmam toque de endpoint; uma origem
Up não volta a animar por causa de outra perna em Ringing. Efeitos continuam
restritos às extensões; troncos/filas permanecem estáticos.


### Falha breve de discagem

O card da extensão exibe por dez segundos o motivo explícito reportado no Hangup:
número inexistente (causa 1), sem rota (2/3), prefixo inválido (5) ou formato
inválido (28). Ocupado, ausência de resposta, cancelamento e encerramento normal
não viram “número inválido”. Trata-se de resultado transitório em memória, sem
canal ativo, sem histórico persistido e sem animação de toque. Nova tentativa
limpa o aviso. Sem causa reportada pelo PBX, não há resultado inferido.

O PBX também pode encerrar imediatamente uma tentativa PJSIP com causa16
(normal), mesmo quando o usuário relata discagem inválida. Se Ring/4 termina
em até2s sem nenhum Up observado, o card apresenta **Tentativa encerrada**, em
âmbar e por dez segundos. Não é diagnóstico de número inválido. Uma chamada
atendida, estado desconhecido/Down ou cancelamento após toque prolongado não
usa esse aviso.

O aviso inclui o destino originalmente observado no exten do canal e a causa
numérica do Hangup (por exemplo,16 · Encerramento normal). Isso não é uma
resposta SIP. O AMI usado aqui não fornece status SIP, que não é inventado ou
convertido da causa. Ausências aparecem como Não informado. O exten original
é preservado apesar de mudança de connected-line/rota, sem formatar zeros ou
caracteres de discagem. Esses dados só acompanham o resultado transitório em
memória, com a mesma autorização e expiração; não vão para histórico de inventário.
