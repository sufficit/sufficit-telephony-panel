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

1. [concluído 21/10] Shell único: `MainLayout` com cabeçalho, navegação e barra de status;
   uma escala de título e um gutter para todas as rotas; rotas próprias para
   `/`, `/queues`, `/infrastructure`, `/board`, `/board/settings`. Contexto (empresa)
   vira um seletor só, compartilhado e persistido na URL.
2. [concluído 21/10] Ao vivo: mosaico na primeira dobra; filtros compactos em uma linha;
   vistas secundárias como segmented control; remover "Configurar quadro" e
   "Recursos lembrados" do fluxo principal; disclaimers em ajuda contextual.
3. [concluído parcial 21/10] Quebrar `LiveOperations.razor` em componentes (`ContextPicker`,
   `StreamStatus`, `BoardFilters`, `CallsView`, `ChannelsView`, `EventsView`) e mover
   para `Pages/` (rotas) × `Components/` (peças).
4. [concluído 21/10] Filas e Infra no mesmo padrão; paginação e botões uniformes; chip de
   demonstração discreto; tema escuro seguindo o sistema na primeira visita.
5. [concluído parcial 21/10] CSS: escopo por componente (`.razor.css`), `panel.css` só tokens e
   quadro; remover overrides de SUI que existam por causa do layout antigo.
6. [concluído 21/10] Validação: testes `tests/*.browser.cjs` atualizados, capturas
   desktop/mobile claro/escuro em `.impeccable/review/`, revisão com `impeccable`
   e `web-design-guidelines`.

Entregue em 21/10 (etapas 1, 2, 4, 6): navegação das três áreas no cabeçalho, rota
`/infrastructure` (links `?area=infrastructure` continuam válidos), título/gutter únicos,
status do stream ao lado do título, "Configurar mesa" como botão (alterna `tab=settings`),
contexto recolhido, limites de observação e "Recursos lembrados" recolhidos abaixo do
mosaico, tela cheia na linha de filtros, chip "Demonstração local" no cabeçalho no lugar
das faixas de alerta, paginação compartilhada (`Pager`, oculta com uma página só) e tema
escuro seguindo o sistema na primeira visita. Mosaico desktop começa em ≈420px (antes ≈920px).
Desvio do alvo: `/board/settings` não foi criado; a configuração segue em `tab=settings`
para manter links e testes existentes. Pendentes: etapas 3 e 5 (quebra de
`LiveOperations.razor` e escopo de CSS por componente).

Etapas 3 e 5 (21/10): lógica de `LiveOperations` movida para `LiveOperations.razor.cs`
(marcação caiu de ~840 para ~250 linhas); extraídos `ContextPicker`, `RetentionControls`,
`ObservationNote` e `Pager`; removidos o inspetor de recurso abaixo da mesa (detalhes sempre
em popup) e o CSS morto (`client-heading`, `board-focus-toolbar`, `board-demo-label`,
`board-inspector`, regras de cabeçalho da mesa dedicada). Pendente: extrair as vistas de
lista (Ramais/Chamadas/Canais/Eventos) e mover o CSS da mesa para `OperatorBoard.razor.css`;
ambas exigem tirar helpers de estado/supervisão de `LiveOperations` primeiro.

Continuação (21/10): formatação pura extraída para `Operations/LiveFormat.cs`; tabela de canais
com supervisão em `ChannelTable` (usada em Chamadas, Canais e no popup do recurso) e eventos em
`EventsView`; as 50 regras da mesa saíram de `panel.css` para `OperatorBoard.razor.css` (separação
automática por seletor, comparação de pixels antes/depois idêntica salvo timestamps do fixture).
Resta inline em `LiveOperations`: lista de Ramais/Troncos/Filas e grupos de Chamadas, que dependem
do índice `Observation.ByCard` e do filtro de status; extrair só junto com um modelo de vista.

Restrições: textos novos em `Localization/Portuguese.json`; nada de feature nova;
`/board` (TV) não pode regredir; supervisão continua exigindo confirmação explícita.

## Próximas etapas (pedido do usuário em 21/10)

7. [concluído 21/10] **Suporte em contexto pelo web chat do Hermes** quando faltar permissão.
   Em vez de mandar o usuário ao WhatsApp, o painel abre direto o chat de suporte do Hermes
   (SDK `https://chatwoot.sufficit.com.br/packs/js/sdk.js`, o mesmo usado em
   `sufficit-web/src/Scripts/SufficitChatWoot.js`), já com o contexto preenchido para o
   suporte agir sem perguntar nada.
   - **Situações**:
     a) login negado antes de entrar no painel (`?login=denied`: autenticado no Identity, sem
        `manager` nem `telephony.panel`);
     b) dentro do painel, quando uma ação esbarra em permissão: supervisão sem
        `telephony.monitor`, sem ramal preferido, contexto não autorizado.
   - **Interface**: botão "Falar com o suporte" na mensagem de acesso negado e ao lado do motivo
     de bloqueio. O widget fica sem bolha (`hideMessageBubble`) e o botão o abre com
     `$chatwoot.toggle('open')`. Locale conforme o idioma do painel; `darkMode` seguindo o tema.
   - **Contexto enviado** (atributos de contato/conversa, `setUser` + `setCustomAttributes`/
     `setConversationCustomAttributes`, label `painel-telefonia`):
     - quem: `sub`, nome e e-mail do usuário;
     - onde: aplicação `telephony-panel`, release, URL sem segredos e horário UTC;
     - o que falta: motivo (`login-denied`, `monitor-missing`, `preferred-extension-missing`,
       `context-not-authorized`), contexto solicitado e permissões atuais
       (`manager`, contextos de `telephony.panel`/`telephony.monitor`);
     - pedido pronto: permissão necessária no formato do preset, por exemplo
       `telephony.panel:<contexto>`; referência de erro (`PanelDiagnostic.Reference`) quando houver.
   - **Segurança**:
     - o contexto é montado no servidor, nunca a partir de parâmetros do navegador;
     - identidade verificada por `identifier_hash` (HMAC do inbox calculado no servidor; o
       segredo HMAC fica no Vault do Identity, nunca no JS nem no repositório);
     - nunca enviar tokens, claims crus, cookies nem dados de chamadas;
     - no caso (a) não existe sessão do painel: guardar só `sub`/nome/e-mail/permissões atuais num
       cookie protegido por DataProtection, de curta duração (10 min) e só leitura.
   - **Inbox**: decidir entre o inbox web atual do site e um inbox dedicado "Painel de
     Telefonia"; incluir `panel.sufficit.com.br` em `allowed_domains` e ligar `hmac_mandatory`.
   - **Testes**: teste de navegador com stub do SDK, validando atributos enviados, ausência de
     segredos e as duas situações; teste unitário do HMAC e do cookie de contexto.
   - **Levantamento no Hermes (21/10, só leitura)**: existe uma única caixa web, "Atendimento"
     (inbox 15, conta 1, `www.sufficit.com.br`): sem restrição de domínio, `hmac_mandatory`
     desligado mas com segredo HMAC já gerado, formulário pré-chat ligado. A conta 1 não tem
     definições de atributo para o painel; sem elas o Chatwoot não mostra atributos customizados
     ao atendente. O SDK só aceita `name`, `email`, `avatar_url` e `identifier_hash` no `setUser`.
     Os atributos de conversa só valem depois da primeira mensagem.
   - **Decisões em aberto**:
     1) usar a caixa "Atendimento" ou criar uma caixa "Painel de Telefonia";
     2) criar na conta 1 as definições de atributo de contato `panel.reason`,
        `panel.context`, `panel.permissions`, `panel.request` e `panel.reference`;
     3) guardar o segredo HMAC da caixa no Vault do Identity e entregá-lo ao painel como
        credencial do systemd. Sem HMAC, `setUser` com identificador permitiria assumir
        o histórico de outro contato, então não será usado.

8. [concluído 21/10 11:05] **Entrega das permissões pelo Identity.** Produção
   (`ff8c411`, nós eveo/apoint/castrum-apps) já tem o recurso, mas não tem
   `Sufficit:Identity:ClaimScopeMap:ServerResolvedEntitlementKeys = ["telephony.panel",
   "telephony.monitor"]` (ver `deploy/identity-server-resolved-entitlements.json`). Sem isso,
   `userinfo?entitlements=current` devolve `grants: []`: uma permissão concedida é ignorada pelo
   painel e ainda vai dentro dos access tokens. Aplicar nos 3 nós, reiniciando um por vez,
   **antes** de conceder `telephony.panel`/`telephony.monitor` a qualquer usuário. Depois disso,
   validar com `teste@sufficit.com.br` o login de cliente e que um contexto não enxerga outro.
   Aplicado nos 3 nós em 21/10 (backup e registro em `sufficit-identity/docs/activities/202610211105-server-resolved-entitlement-keys.md`).
   Pendente: validar o login de cliente com `teste@sufficit.com.br`, que já tem
   `telephony.panel` e `telephony.monitor` no contexto `026246a851a7485991e0b06e1bebb045`.

Etapa 7 entregue em 21/10 (v0.14.0): caixa "Atendimento" (decisão do usuário); campos de contato
`panel.reason`, `panel.user`, `panel.context`, `panel.permissions`, `panel.request` e
`panel.reference` criados na conta 1 do Hermes (ids 883–888, pelo modelo do Rails); segredo HMAC
no Vault (`hermes/web-widget/atendimento-hmac`, contexto global) e em
`/etc/sufficit/telephony-panel/hermes-widget-hmac`; drop-in `deploy/60-support-chat.conf`.
Botões: "Falar com o suporte" no acesso negado (contexto em cookie protegido de 10 min, endpoint
`/support/denied`) e "Pedir acesso ao suporte" ao lado dos bloqueios de permissão de supervisão e de
ramal preferido (tabela de canais, chamadas e popup do recurso). O diálogo de supervisão da mesa
(`BoardSupervisionDialog`) ganhou o botão em 21/10 (v0.14.3) para o caso de ramal preferido ausente.

9. [concluído 21/10, v0.15.0, aprovado pelo usuário] **Sessões sobrevivendo a deploy.** `PanelSessions` guarda tickets
   e tokens só em memória (máx. 200), por decisão de segurança. Cada publicação reinicia o serviço e
   obriga todos a entrar de novo: o journal de 21/10 mostra o `teste@` entrando 5 vezes em 1 hora, uma
   por deploy. Opção: persistir os tickets cifrados com DataProtection (chaves já em
   `/var/lib/sufficit-telephony-panel/keys`) num arquivo 0600 com limite de tamanho e expiração,
   mantendo a revalidação de permissão a cada 60 s. Custo: o token passa a ficar em disco, cifrado.
   Entregue: `Security/SessionFileStore.cs`, um arquivo por sessão em
   `/var/lib/sufficit-telephony-panel/sessions` (diretório 0700, arquivos 0600), cifrado com o key ring
   persistido, limite de 200 e expiração do próprio ticket (corte absoluto de 9 h). Só é ativado quando
   `Panel:KeyDirectory` está configurado. Sair do painel apaga o arquivo; perda de permissão também.
