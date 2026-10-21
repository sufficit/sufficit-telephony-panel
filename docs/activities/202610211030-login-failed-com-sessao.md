# `?login=failed` com o painel carregado

Relato do usuário (21/10): login com usuário de teste em janela anônima terminou em
`/?login=failed`, mas a mesa carregou inteira (1795 ramais, visão central).

## Diagnóstico
- Journal às 10:24:41: `OpenIdConnectHandler[15] '.AspNetCore.Correlation.…' cookie not found`.
  O callback OIDC chegou sem o cookie de correlação (callback repetido/antigo, outra aba ou
  cookie descartado), então o login daquela tentativa **não** foi concluído e o handler
  redirecionou para `?login=failed`.
- Uma falha de callback não remove a sessão existente: a mesa só carrega com cookie de sessão
  válido que passa `OnValidatePrincipal` (permissão do painel). Portanto havia uma sessão
  anterior válida naquela janela anônima, provavelmente de outra conta. Sem log de sujeito e sem
  nome na tela, não era possível saber qual.
- Nginx do domínio do painel tem `access_log off`; não há trilha HTTP do callback.

## Correção
- `OnTokenValidated` registra `Sign-in completed for subject {sub}`.
- `OnRemoteFailure` autentica o cookie existente: com sessão válida redireciona para
  `?login=kept` e registra o sujeito mantido; sem sessão continua `?login=failed`.
- Cabeçalho mostra o nome do usuário conectado (claim `name`/`preferred_username`/`email`).
- Com sessão válida e `login=` na URL, toast de aviso ("O login não foi concluído. Você continua
  conectado como X. Para usar outra conta, saia primeiro.") e o parâmetro sai da URL.
- 978 checks + 16/16 navegador PASS; validação local do toast com fixture.

Pendente com o usuário: confirmar se o usuário de teste tem papel `manager`. Se não tiver e
mesmo assim o cabeçalho mostrar esse usuário com a mesa central, é falha de autorização e deve
ser tratada como incidente.

Publicado 21/10: release `login-kept-20261021-3d4b275` (anterior `views-css-20261021-32656b1`
preservado), DLL `6cfeddd2c76be3ca3113c5fc93268cc65357aa6e752e27852968197e68263527`, appsettings
idêntico, health 200, `/api/fleet` anônimo 401, `/login` redireciona para o authorize do Identity.

## Segunda tentativa: `teste@sufficit.com.br` em janela anônima limpa
Resultado: tela de entrada com "login não concluído" e nenhum log de falha. O nível mínimo de log
em produção é Warning, então o "Sign-in completed" (Information) não aparecia. Causa provável:
o Identity autenticou, mas `OnValidatePrincipal` rejeitou a sessão por falta de permissão do painel,
e o usuário voltou para o `returnUrl` que ainda carregava `?login=failed` da tentativa anterior.

Correção:
- `OnTokenValidated` consulta a permissão do painel antes de criar a sessão. Sem permissão,
  registra `Sign-in denied: subject … has no panel permission` (Warning) e redireciona para
  `?login=denied`, com a mensagem "Sua conta Sufficit entrou, mas não tem acesso ao Painel de Telefonia…".
- O login concluído é registrado como Warning (trilha de auditoria visível com o nível atual).
- O link "Entrar" não leva mais o parâmetro `login=` para o `returnUrl`.

Confirmação no Identity (leitura): `teste@sufficit.com.br` (id `ee277377-f229-4552-b97a-c8f6e73fd36c`)
não tem papel `manager` nem entitlement `telephony.panel`/`telephony.monitor`; só tem
`telephonyclient`, `phonecalls`, `audioupdate`, `balanceview`, `payment` e `aiuser` no contexto
`026246a851a7485991e0b06e1bebb045`. A negação está correta: não houve vazamento de dados para esse
usuário. Para testar o acesso de cliente é preciso conceder `telephony.panel:<contexto>` (e
`telephony.monitor:<contexto>` para supervisão) e fazer novo login.

Publicado 21/10: release `login-denied-20261021-0efe90e` (anterior `login-kept-20261021-3d4b275`
preservado), DLL `6eb7edc987c9f6d5b6b17fb169d72612e5b956632bfd639b5b141293e860ddc6`, appsettings
idêntico, health 200, `/api/fleet` anônimo 401.

## Aceite: outra empresa (21/10, ~13:52)
O usuário abriu `/?contextid=d21cfb049d37473b837c67591a26feed` logado como `teste@`. A negação funcionou:
`UnauthorizedAccessException` registrada como aviso de diagnóstico e nenhum dado entregue. Mas a mesa
ficou em branco, porque na mesa em tela cheia o erro só aparecia dentro do popover do funil. Correção na
v0.15.3: o aviso "Sem acesso a esta empresa" aparece no lugar da mesa, com "Abrir minhas empresas" e
"Pedir acesso ao suporte" (`panel.reason=context-not-authorized`, `panel.request=telephony.panel:<contexto>`,
`panel.permissions` com as permissões atuais). Outros erros de carga mostram "Não foi possível carregar a
mesa" com reconectar. Teste de navegador `customer-access` estendido. 1029 checks e 16/16 navegador PASS.
