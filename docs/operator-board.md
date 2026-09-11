# Mesa de operação

## Página dedicada e links compartilháveis

O domínio público agora abre sem prefixo: `https://panel.sufficit.com.br/`.
A mesa dedicada fica em `/mesa`, acessível por “Abrir mesa em página inteira”.
Não exibe a navegação de infraestrutura nem rodapé; filtros ficam no cabeçalho,
e “Tela cheia” alterna o modo do navegador quando suportado. No desktop a grade
aproveita a altura restante; no celular os controles e seções seguem fluxo vertical.

Exemplo: `/mesa?q=6007&text=peers&node=google-voip&channels=true`.

- `company`: UUID da empresa; omitido significa toda a central autorizada ao gerente.
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
  presentes no snapshot, não registro, alcance ou contagem de pessoas na fila.
  Não altera inventário, retenção, regras ou abas de diagnóstico. Desligar restaura
  os demais cartões. A preferência é preservada na URL, inclusive ao reconectar.
- Clique/Enter abre o detalhe abaixo do mosaico e move o foco para ele; nenhuma
  operação telefônica é disparada. Ouvir/Sussurrar mantém confirmação por canal.
- Paginação de 120 ramais por página; até 40 troncos e 40 filas
  na coluna lateral, com aviso quando excedidos. Refine filtros para os demais.
- Grid com rolagem local, colunas adaptativas e duas colunas no celular; lateral
  abaixo da grade em telas estreitas. O detalhe mostra até 100 canais, com aviso.
- Estado derivado e agrupamento reutilizam o snapshot autorizado. Não existem
  comandos novos de transferência, discagem, encerramento ou disponibilidade do
  operador. A toolbar da imagem de referência não foi copiada como ações fictícias.

## Validação visual

`tests/operator-board.browser.cjs` usa a fixture Development na porta local 5169,
com Playwright existente através de NODE_PATH. A fixture possui 72 cartões extras
apenas no mosaico para testar densidade; não modifica contagens da fixture de
chamadas/canais e jamais é ativada por query string em Production.

Capturas em `.impeccable/review/board-*.png`: claro 1440px e 1977px (largura da
referência), escuro 1440px, móvel 390px. São dados fictícios explicitamente
identificados; não comprovam fluxo autenticado nem inventário real.
