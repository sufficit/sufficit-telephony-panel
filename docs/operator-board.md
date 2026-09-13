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
