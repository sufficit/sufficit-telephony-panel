---
name: Sufficit Telephony Panel
description: Observação interna da telefonia com identidade Sufficit e componentes SUI.
colors:
  primary-light: "#a94210"
  primary-dark: "#ff9c60"
  on-primary-light: "#ffffff"
  on-primary-dark: "#231e1b"
  primary-soft: "color-mix(in srgb, var(--sui-color-primary) 14%, transparent)"
  surface-light: "#ffffff"
  surface-dark: "#1f2226"
  surface-2-light: "#f5f6f7"
  surface-2-dark: "#272b30"
  surface-3-light: "#e7eaed"
  surface-3-dark: "#353b42"
  text-light: "#20252b"
  text-dark: "#f2f4f5"
  muted-light: "#515e6a"
  muted-dark: "#bdc7d0"
  border-light: "#d3dae1"
  border-dark: "#434b55"
  border-strong-light: "#8998a5"
  border-strong-dark: "#6a7885"
  success-light: "#146139"
  success-dark: "#80d5a4"
  warning-light: "#805400"
  warning-dark: "#f6cf76"
  error-light: "#b02020"
  error-dark: "#ffa3a3"
  info-light: "#215a80"
  info-dark: "#a3d5f5"
typography:
  display:
    fontFamily: "-apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif"
    fontSize: "clamp(38px, 5vw, 62px)"
    fontWeight: 650
    lineHeight: 1.15
    letterSpacing: "-.025em"
  headline:
    fontFamily: "-apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif"
    fontSize: "clamp(28px, 3vw, 42px)"
    fontWeight: 650
    lineHeight: 1.15
    letterSpacing: "-.025em"
  title:
    fontFamily: "-apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif"
    fontSize: "22px"
    fontWeight: 650
  body:
    fontFamily: "-apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif"
    lineHeight: 1.6
  label:
    fontFamily: "-apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif"
    fontSize: ".75rem"
    fontWeight: 600
  button:
    fontFamily: "-apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif"
    fontSize: ".875rem"
    fontWeight: 500
    lineHeight: 1.3
  board-title:
    fontSize: "12px"
  board-status:
    fontSize: "11px"
  board-node:
    fontSize: "10px"
rounded:
  board-tile: "5px"
  board-section: "6px"
  field: "8px"
  button: "10px"
  empty: "12px"
  detail: "14px"
spacing:
  board-gap: "6px"
  board-rail-gap: "14px"
  board-gutter: "18px"
  control-y: "4px"
  xs: "8px"
  sm: "12px"
  md: "16px"
  compact-detail: "20px"
  lg: "24px"
  detail: "26px"
  section: "32px"
  columns: "40px"
components:
  button-primary:
    backgroundColor: "{colors.primary-light}"
    textColor: "{colors.on-primary-light}"
    typography: "{typography.button}"
    rounded: "{rounded.button}"
    padding: "4px 14px"
  button-primary-dark:
    backgroundColor: "{colors.primary-dark}"
    textColor: "{colors.on-primary-dark}"
    typography: "{typography.button}"
    rounded: "{rounded.button}"
    padding: "4px 14px"
  button-outlined:
    textColor: "{colors.primary-light}"
    typography: "{typography.button}"
    rounded: "{rounded.button}"
    padding: "4px 14px"
  button-text:
    textColor: "{colors.primary-light}"
    typography: "{typography.button}"
    rounded: "{rounded.button}"
    padding: "4px 14px"
  select:
    backgroundColor: "{colors.surface-light}"
    textColor: "{colors.text-light}"
    rounded: "{rounded.field}"
    padding: "0 12px"
  node-row-selected:
    backgroundColor: "{colors.primary-soft}"
    textColor: "{colors.text-light}"
    padding: "22px 16px"
  detail:
    backgroundColor: "{colors.surface-2-light}"
    textColor: "{colors.text-light}"
    rounded: "{rounded.detail}"
    padding: "26px"
  board-tile:
    backgroundColor: "{colors.surface-2-light}"
    textColor: "{colors.text-light}"
    rounded: "{rounded.board-tile}"
    padding: "6px 8px"
---

# Design System: Sufficit Telephony Panel

## Overview

**Creative North Star: "Observação operacional Sufficit"**

Identidade Sufficit com ações em laranja e superfícies neutras, brancas no tema claro e grafite no escuro. A hierarquia combina linhas de servidores comparáveis, números tabulares e um detalhe de leitura com fundo tonal; estados e horários têm prioridade sobre decoração.

Esta documentação preserva o mundo visual já declarado em src/App.razor e implementado com Sufficit.Blazor.UI (SUI). O nome descritivo acima sintetiza esse contrato; não representa uma nova identidade aprovada em workshop. A interface é PT-BR e interna, restrita ao papel manager.

**Key Characteristics:**

- SUI como biblioteca de componentes e fonte de tokens.
- Uma única cor de identidade; cores semânticas acompanhadas de texto.
- Tema claro e escuro com a mesma hierarquia.
- Movimento limitado à interação e à consulta; som opcional.

Fontes: PRODUCT.md, src/App.razor, src/PanelTheme.cs, src/Pages/Home.razor,
src/wwwroot/panel.css e src/wwwroot/panel.js, complementados pelos contratos
locais de SUI. As capturas .impeccable/review/desktop.png, dark.png e mobile.png
são fixtures locais de demonstração explicitamente sinalizadas, não evidência
operacional. A revisão de fechamento aprovou três correções do histórico;
essa aprovação não valida login, autorização em produção ou deploy.

## Colors

Laranja quente orienta ações e seleção; superfícies neutras sustentam comparação
e leitura prolongada. Os pares light/dark no frontmatter correspondem ao
PanelTheme, não a paletas alternativas para misturar na mesma tela.

### Primary

- **Laranja Sufficit profundo / luminoso:** primary-light e primary-dark
  alimentam ações preenchidas, links, foco, seleção e traçado do histórico.
- **Contraste de ação:** on-primary-light e on-primary-dark acompanham o
  respectivo fundo, inclusive ícones.
- **Laranja translúcido:** primary-soft conserva a expressão CSS original da
  SUI e acompanha a cor primária ativa; é usado na linha selecionada.

### Neutral

- **Papel / grafite:** surface é o fundo da aplicação; surface-2 separa o
  detalhe, estados vazios e hover; surface-3 dá apoio aos estados da SUI.
- **Texto principal / texto auxiliar:** text e muted distinguem números,
  títulos, horários, unidades e notas, sem depender de opacidade global.
- **Divisória / contorno de campo:** border organiza seções; border-strong
  delimita controles editáveis.

### Semantic states

Success significa leitura disponível; warning sinaliza coleta sem leitura
atual ou alerta em confirmação; error apresenta falha de consulta; info
permanece disponível no tema SUI. Cada estado deve ter texto associado.
Nenhuma dessas cores é um segundo acento de marca.

Na Mesa de operação, success indica registro ou alcance observado, nunca
disponibilidade garantida; warning indica canais observados ou uso; info indica
toque; error indica ausência de registro ou resposta; neutro indica informação
ausente. A legenda explicita os cinco significados. Os fundos dos cartões misturam
respectivamente 16%, 22%, 20% e 12% da cor semântica à superfície ativa; pontos
e rótulos mantêm a evidência legível nos dois temas. As faixas de seção usam
grafite fixo com texto branco, sem disputar com o laranja de seleção.

**The Evidence Rule.** Cor e movimento comunicam apenas estado observado; dados ausentes não equivalem a zero nem a sucesso.

## Typography

A família de sistema da SUI é compartilhada por títulos, corpo e controles;
não há fonte web adicional. O painel usa uma escala própria de títulos em CSS
sobre os tokens de tipografia da biblioteca.

- **Display:** chamada de entrada, com largura de leitura limitada a 15ch.
- **Headline:** título operacional principal, responsivo e sem caixa alta.
- **Title:** títulos de seção; nomes dos nós usam 20px, reduzidos para 18px
  na composição móvel.
- **Body:** parágrafos com largura máxima de 72ch e entrelinha do frontmatter.
  O tamanho base é herdado; não há tamanho global de body declarado no painel.
- **Label:** rótulos SUI; estados e descrições locais variam entre 11px e 14px
  conforme a função, preservando o texto de unidade junto ao número.
- **Métricas:** números tabulares; 26px na lista e 27px no detalhe, ambos
  reduzidos para 23px em telas estreitas.
- **Mesa de operação:** escala compacta própria, sem alterar as outras abas:
  título principal de 24px (22px no móvel), contexto e detalhe de 18px,
  cabeçalhos de seção de 13px/peso 650 e contagens tabulares de 12px.
  Os papéis board-title, board-status e board-node distinguem nome, evidência
  e servidor no cartão; canais usam board-status. Nome, estado e servidor
  truncam com reticências; o detalhe revela a informação completa.

## Layout

O invólucro tem largura máxima de 1600px, centralizado, com margens internas
laterais de 40px. Cabeçalho e rodapé usam divisórias, não superfícies flutuantes.
A área principal começa com 42px de respiro vertical.

Lista de nós e detalhe formam uma grade
minmax(400px, 1fr) / minmax(360px, .9fr), com intervalo de 40px. Até 1000px,
a grade passa a uma coluna, o intervalo cai para 20px e as margens laterais
para 24px. Até 600px, as margens são 16px, o topo operacional e o rodapé
empilham, as ações dividem a linha e o detalhe usa 20px de preenchimento.

O histórico ocupa a largura disponível abaixo da comparação; tem 180px de
altura de gráfico, com faixa lateral reservada ao eixo. Os controles mantêm
rótulos explícitos e seguem a mudança responsiva do cabeçalho do histórico.
A tabela de amostras é uma alternativa textual recolhível.

### Mesa de operação

Somente enquanto esta aba está ativa, o invólucro ocupa toda a largura, sem o
limite de 1600px; usa board-gutter nas laterais e 12px até 600px. Cabeçalho
tem 12px de preenchimento vertical e a área principal, 16px. Esta composição
compacta é uma extensão local; as demais abas preservam o invólucro anterior.

A grade principal combina `minmax(0, 1fr)` com um trilho lateral
`minmax(290px, 24%)`, separados por board-rail-gap. Ramais ficam à esquerda;
troncos conhecidos e filas ocupam seções independentes
à direita. A grade usa colunas automáticas de no mínimo 210px, board-gap,
8px de preenchimento, altura mínima de 300px e rolagem vertical local até 65vh.
O trilho usa o mesmo intervalo/preenchimento interno e rolagem até 42vh por lista.

Até 900px, o trilho passa abaixo da grade em duas colunas e a grade limita-se
a 60vh. Até 600px, o trilho empilha, os ramais permanecem em duas colunas
iguais, o intervalo da grade cai para 5px e seu preenchimento para 6px.
A legenda quebra linhas. O detalhe selecionado fica abaixo do conjunto.

### Configurar mesa

A lista de regras e o editor inline usam grade `minmax(0, 1.2fr)` /
`minmax(320px, 1fr)`, com intervalo de section. Até 800px, empilham em uma
coluna com intervalo de compact-detail; o editor reduz seu preenchimento de
compact-detail para md. A barra de ações quebra linhas e seus botões dividem
o espaço disponível nessa composição estreita.

As regras usam linhas com divisória inferior, preenchimento vertical md e
ações textuais abaixo. A regra em edição recebe surface-2, o mesmo fundo do
editor; ambos preservam os tokens de tema existentes. Editor e confirmações
inline usam a curva board-section, sem sombra. Nomes, identificadores e
servidores longos quebram linha em vez de alargar a tela.

A prévia fica abaixo de lista e editor, separada por divisória, margem section
e preenchimento superior lg. Seus itens são texto, sem reproduzir cartões de
estado: nome seguido de tipo e servidor em texto auxiliar de 13px. A grade
automática parte de 210px, com intervalos sm/lg; até 800px vira uma coluna.
Essa composição pertence apenas a Configurar mesa, sem substituir o mosaico
ou os layouts das outras abas.

## Elevation & Depth

A hierarquia principal é tonal e delimitada por linhas. O detalhe não recebe
sombra. A SUI mantém relevo sutil no botão preenchido e sombra de nível 2
no menu de seleção; os valores completos estão no sidecar. Não estender
essa elevação aos blocos de dados.

**The Tonal Depth Rule.** Superfícies operacionais se distinguem por tom e divisórias; elevação fica nos controles que já a utilizam.

## Shapes

Campos têm cantos moderados; botões são ligeiramente mais arredondados;
o detalhe usa a maior curva recorrente. Os valores normativos estão em
rounded. Linhas de servidores continuam retangulares, com bordas superior
e inferior finas. Pontos circulares de estado são pequenos e acompanham
rótulos; não são indicadores isolados.

A Mesa de operação usa curvas menores próprias: board-tile nos cartões e
board-section nas seções e no inspetor. São superfícies planas com contorno de
1px; o mosaico não herda o raio amplo do detalhe de infraestrutura.

## Components

### Buttons

SUIButton Filled destaca Atualizar e a entrada via Identity. Outlined controla
a preferência sonora; Text alterna tema. O botão médio tem altura mínima
de 36px, elevada pela SUI para 44px em ponteiro coarse ou largura até
599.98px. Hover e active ajustam o tom, sem trocar a hierarquia.
Disabled/loading bloqueiam a interação. O foco SUI usa contorno de 2px;
controles próprios do painel usam o contorno global de 3px.

### Inputs / Fields

O painel usa SUISelect para indicador, período e servidor observado. SUITextField
recebe a busca de cliente e o filtro imediato de recursos, com rótulo persistente,
limite de caracteres e botão de limpeza no filtro. Placeholder usa text-secondary
com opacidade explícita 1 nos dois temas; não substitui o rótulo.
Rótulos ficam acima do gatilho; a seleção tem fundo de superfície, contorno
forte e foco com halo primário. Estados desabilitado e inválido pertencem
ao contrato da SUI. Não substituir o combobox real por um desenho inerte.

### Navigation

A marca retorna à raiz; o cabeçalho mantém alternância de tema e saída.
Dois botões SUI alternam Telefonia ao vivo e Infraestrutura e histórico sem trocar
de identidade visual. Na operação, botões com aria-pressed alternam Mesa de
operação (inicial), ramais, chamadas, canais, troncos e filas; contagens são de recursos configurados ou observados,
nunca apresentadas como inventário reconciliado. No móvel eles formam duas colunas.
As linhas de servidores são botões com aria-pressed, não cartões com links
sobrepostos. Hover adiciona fundo tonal e deslocamento horizontal de 3px;
a linha selecionada recebe o tom primário translúcido.

### Mesa de operação

Mosaico compacto de botões selecionáveis por recurso e servidor, sem ações
telefônicas no clique. Cada cartão mostra nome, estado escrito, servidor e até
dois canais; excedentes recebem indicação para consultar detalhes. A altura
mínima é 62px, elevada a 68px até 600px. Hover troca o contorno para a primária;
seleção usa contorno interno primário de 2px e aria-pressed. Foco por teclado
permanece visível; transições de fundo e borda duram 200ms e respeitam
reduced-motion.

A rota /board usa a mesma identidade SUI com cabeçalho compacto e sem rodapé ou
navegação de infraestrutura. Os filtros ficam agrupados acima dos cartões; o
fieldset “Aplicar texto em” controla ramais, troncos e filas independentemente,
todos selecionados por padrão. Preferências de visualização acompanham a URL.
Empresa, limites de observação e retenção usam disclosure nativo acessível.
O cabeçalho de filtros é sticky no desktop, normal no móvel; a grade mede a altura
disponível e não assume tamanho fixo do cabeçalho. O botão Tela cheia é opt-in e
usa a API do navegador somente no contêiner board-focus: filtros, mosaico e
inspetor selecionado. Cabeçalho global, abas, retenção e ajuda ficam fora dele.
O botão de saída permanece dentro da tela cheia; sem suporte a página dedicada
continua funcionando com aviso. /mesa redireciona para /board preservando filtros.

English é o idioma padrão; o seletor SUI English/Português salva a preferência
em telephony-panel-language no localStorage. Rótulos, mensagens e estados usam
traduções; nomes de clientes, recursos e identificadores permanecem como dados.
O serviço de idioma é scoped por circuito, sem mudar cultura global do servidor.

A paginação limita ramais a 120 por página; o trilho mostra até 40 troncos e
40 filas, com avisos explícitos ao exceder. O botão SUI “Só com canais observados”
substitui a seção lateral duplicada, com aria-pressed e variante preenchida quando
ativo. Começa desligado e filtra somente a mesa, sem apagar recursos lembrados.
Cabeçalhos apresentam
contagens do conjunto filtrado, não apenas dos cartões renderizados. Sem
classificação autorizada, o título é “Ramais e endpoints” e Troncos orienta
a filtrar uma empresa; não se infere tipo por nome ou prefixo.

Clique ou Enter abre o inspetor abaixo do mosaico e transfere foco para ele.
O detalhe separa registro, alcance, uso e horários, exibe até 100 canais com
aviso de limite e oferece fechar. Ouvir/Sussurrar reutiliza a confirmação por
canal existente; não há toolbar fictícia de transferência, discagem ou encerramento.

Fontes desta extensão: src/Pages/OperatorBoard.razor, src/Pages/LiveOperations.razor,
src/wwwroot/panel.css e docs/operator-board.md. As capturas
`.impeccable/review/board-user-1977.png`, `board-desktop.png`, `board-dark.png`
e `board-mobile.png` documentam a composição em 1977px, 1440px e 390px, com
fixtures sintéticas identificadas. Não comprovam inventário completo, eventos
reais, autenticação ou supervisão em produção. A referência externa contribuiu
apenas com a composição; seus dados e credenciais não pertencem ao sistema.

### Configurar mesa

A aba organiza regras compartilhadas de apresentação, não alterações no
Asterisk ou nas permissões. A lista ordenada informa tipo, estado ativo,
servidor e identificadores; Subir/Descer explicitam prioridade sem depender
de arraste. A primeira regra ativa correspondente vence; regras agrupam
aliases por servidor, sem misturar nós. SUITextField, SUISelect e SUICheckbox
mantêm rótulos persistentes e instruções junto aos campos.

Nova regra e Editar transferem o foco ao editor inline. Aplicar ao rascunho
e Salvar configuração são etapas distintas, com estado pendente escrito e
horário da última gravação. Salvar fica indisponível durante edição aberta,
envio ou ausência de mudanças; a prévia avisa quando ainda não inclui a edição.
Remover e descartar/recarregar pedem confirmação inline. Erros usam SUIAlert;
confirmações de resultado usam texto success com role=status; avisos usam
warning acompanhado de explicação. Falhas preservam o rascunho.

A prévia compara contagens antes/depois e mostra até 12 itens, priorizando
grupos configurados; o limite é explícito. Usa somente o conjunto autorizado
da visão atual, sem filtros temporários de texto, estado ou servidor. Ausência
de correspondência não é tratada como regra inválida nem inventário completo.
O vazio oferece criar regra ou preparar exemplo, sem importação ou publicação
automática. Ocultar recursos afeta só a mesa, preservando as abas de diagnóstico.

Fontes desta extensão: src/Pages/BoardSettings.razor,
src/Operations/BoardConfiguration.cs, src/Operations/BoardRuleEngine.cs,
src/wwwroot/panel.css e docs/board-configuration.md. As capturas
`.impeccable/review/settings-desktop.png`, `settings-dark.png` e
`settings-mobile.png` documentam o editor com fixtures Development em temas
claro/escuro e composição móvel. O parecer visual final foi ship, sem correção
material; não comprova login, persistência real, eventos reais ou deploy.

### Cards / Containers

O detalhe do nó usa surface-2, sem sombra, e organiza as leituras em duas
colunas. Trabalho interno permanece em details/summary para reduzir a
densidade inicial. O vazio do histórico usa a mesma família tonal, com
raio próprio; erro é mensagem SUIAlert, não um gráfico zerado.

### History

A linha primária conserva segmentos separados nas lacunas. Cada amostra
tem marcador próprio, inclusive quando isolada; o eixo temporal corresponde
ao período solicitado e não estica apenas os pontos disponíveis. Indicador
e unidade precisam corresponder em gráfico e tabela. Carregamento, falha
e ausência de amostras são estados distintos. O SVG tem descrição acessível
e a tabela permite inspecionar as últimas leituras.

### Operational details and supervision

A visão inicial de gerente é Toda a central, sem seleção obrigatória de empresa.
Empresa é um filtro opcional, pesquisável e removível; seu identificador compacto
e o estado da conexão ficam juntos. A visão central inclui endpoints recebidos
sem cartão cadastrado e não inventa sua classificação entre ramal e tronco.
Uma nota explica o início e a incompletude da observação. Busca por texto,
seletor de nó e filtro de estado precedem linhas expansíveis de recursos.
Registro SIP, alcance e uso do dispositivo são evidências independentes. O último
registro observado é o horário de recepção de um evento de registro, nunca um RTT
nem um histórico anterior à conexão. Datas incluem o deslocamento UTC. A última
evidência de contato não representa todos os dispositivos de um endpoint.
Detalhes usam superfície tonal e tabela sem cartões aninhados. Regiões de tabela
com rolagem têm nome acessível e foco por teclado; uma instrução aparece em telas
estreitas para tornar a coluna de último evento descobrível.

Chamadas têm tabela própria, idade desde a observação, origem/destino, nó e ações.
Desde 0.5.1, a aba Chamadas apresenta grupos expansíveis por servidor/Linkedid,
com os canais na tabela de detalhes. A aba Canais mantém a tabela individual.
Canais sem correlação são explicitamente separados da contagem de chamadas;
estado e supervisão pertencem ao canal. A composição reutiliza details/summary,
divisórias, tokens e rolagem local, sem uma nova identidade visual.
Ouvir e Sussurrar abrem uma confirmação inline com foco, cliente, canal, nó e o
ramal do supervisor. Confirmar e cancelar são ações distintas; durante o envio
há bloqueio de repetição. Estados inválidos, antigos ou sem permissão não habilitam
a supervisão. Resultado aceito não é apresentado como prova de áudio conectado.

As capturas `.impeccable/review/operations-desktop.png`, `operations-calls.png`,
`operations-queues.png`, `operations-dark.png` e `operations-mobile.png` são
fixtures sintéticas locais identificadas. Cobrem desktop 1440px, móvel 390px,
temas, filtros, detalhes e confirmação. Não comprovam login autorizado, eventos
dos três PBXs nem uma chamada real. A liberação de entitlements é independente
da aprovação visual e fica registrada no plano operacional.

A revisão da visão central usa `central-desktop.png`, `central-peer.png`,
`central-dark.png`, `central-mobile.png` e `central-user-1941.png` na mesma pasta:
1440px, 390px e 1941px, dados sintéticos. A observação geral de manager independe
da ativação de entitlements; filtro por empresa e supervisão ainda dependem dela.
Nenhuma captura comprova inventário completo, histórico de registros ou teste
autenticado com dados reais. Desconexão de socket não é inferida de qualify.

### Effects and sound (existing infrastructure)

O detalhe entra em 350ms com deslocamento curto e recorte; o gráfico revela
seu traçado em 600ms; a consulta usa pulso de 1s somente enquanto Busy.
prefers-reduced-motion remove animações e transições. A decoração não
afirma que uma conexão está saudável.

Som começa desativado e requer ação explícita. Ativar sons produz uma
prévia curta; alertas subsequentes dependem de novos estados firing,
com página visível e intervalo mínimo de 15 segundos. O tema é persistido
localmente; a preferência de som não é persistida nesta implementação.

## Do's and Don'ts

### Do:

- Do usar SUIThemeProvider, SUIButton, SUISelect e SUIAlert para manter os contratos existentes.
- Do preservar horários, unidades, estados desconhecidos e lacunas nos dados.
- Do manter tema claro/escuro, foco visível e navegação por teclado.
- Do respeitar redução de movimento e ativação explícita do som.
- Do identificar dados sintéticos como demonstração local.

### Don't:

- Don't substituir SUI por MudBlazor neste painel.
- Don't transformar ausência de coleta em zero, sucesso ou animação de atividade.
- Don't conectar trechos de histórico através de períodos sem amostras.
- Don't usar cor ou som como único portador de estado.
- Don't interpretar capturas de fixtures como validação de login, deploy ou métricas reais.
