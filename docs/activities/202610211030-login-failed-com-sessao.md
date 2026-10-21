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
