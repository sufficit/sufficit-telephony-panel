# Primeiro acesso após expiração da sessão

1. [concluído] Diagnosticar captura e logs reais: em19/11às09:26:34 o erro veio de Automatic sign-in stopped, apenas com cookie de tentativa; login manual às09:26:48 concluiu. Não há falha OIDC registrada nesse intervalo. Cookie de tentativa pendente estava sendo interpretado como conclusão sem sessão.
2. [concluído] Vincular marcador anti-loop à conclusão real de um login automático, ignorar marcadores antigos/pendentes e impedir disparos por prefetch/prerender; preservar permissões, logout e falhas reais.
3. [concluído] Validar regressão antes/depois, Release e HTTP local; commit/push e publicar somente painel, conferir saúde/hash; converter em atividade.

Restrições: não alterar Identity, tempo de sessão ou permissões; não remover proteção contra loop real; não registrar tokens/state/cookies completos; preservar sessões válidas. Hipótese de prefetch ou acessos concorrentes explica o marcador antecipado, mas não está comprovada nos logs. A causa comprovada é a decisão prematura da proteção local. Sem alteração/reinício Google/controladores/API.

Regressão antes da correção falhou: A pending first access is not a completed failed login. Após correção:1.659 checks Release passaram. Teste HTTP local passou com sessão ausente/expirada, marcador antigo/pendente, duas navegações, marcador concluído ainda bloqueando loop, prefetch/prerender e logout/erros explícitos. Marcador novo __Host- com Secure/HttpOnly/Path=/ e30s, apenas OnTicketReceived do fluxo automático identificado no state protegido do OIDC. Sem mudar duração de sessão/refresh/permissões.

Também validada serialização/proteção/restauração das propriedades do state OIDC: o indicador automático sobrevive ao round trip protegido. HTTP local não segue redirects nem contata Identity.

## Entrega e evidências

Fonte bab6c76, main publicada. Removida a criação de cookie de tentativa antes
do redirecionamento. O login automático recebe propriedade própria nos Items
do state protegido OIDC; apenas OnTicketReceived emite marcador de conclusão.
O marcador novo é host-only (__Host-), Secure/HttpOnly/Path=/ e30s. Legados e
marcadores pendentes não bloqueiam navegação. Login manual e sessão validada
limpam o marcador. Prefetch/prerender, inclusive Sec-Purpose combinado, não
iniciam autenticação. Logout, recusas e falhas reais continuam sem loop; cookie
não autentica nem concede qualquer acesso. PKCE, nonce/correlação, permissões e
lifetimes de sessão/token não foram alterados.

Regressão compilável falhou antes do ajuste, passando depois. Suite Release:
1.659 checks, incluindo round trip de state protegido, ausência de marcador
manual, atributos host-only, pendente/legado, conclusão, rotas/filtros e logout.
Teste HTTP local tests/autosignin-navigation.http.py cobre sessão ausente/expirada,
marcadores, repetição de navegação, especulação e proteção real de loop. Não segue
redirect nem contata Identity. Aviso NU1903 SignalR.Common10.0.11 preexistente.
Uma tentativa inicial de compilação precisou usar TryGetValue porque Items é
IDictionary, não IReadOnlyDictionary; corrigido antes da validação/entrega.

Publicação somente no eveo-apps:
`/opt/sufficit-telephony-panel/releases/first-access-login-20261119-bab6c76`.
Anterior preservada trunk-hangup-20261118-ad36bef; configuração e key ring
persistentes preservados; troca atômica e rollback pelo helper existente.
SHA256 da DLL instalada igual ao pacote:
`dcd06cf80eaa55c8937719ae4fbc37eca853a784d50cda38f559ead8a40434b0`.
Serviço ativo, health interno/socket e público running; coletor AMI manteve
MainPID409848. Nenhuma alteração/reinício Identity, API, Google/controladores.

HTTP HTTPS real de produção, sem seguir redirects, confirmou:
- Sem sessão, cookie legado e marcador pendente retornam login normal com destino
  local e automatic=1, sem login=failed.
- Sec-Purpose: prefetch;prerender retorna200 sem iniciar login.
- Marcador concluído sem sessão ainda retorna login=failed, comprovando proteção
  contra loop. Esse último pedido foi deliberadamente sintético e pode produzir
  um aviso de Automatic sign-in stopped no journal após a publicação; não é
  evidência de nova falha do usuário. User-Agent do probe: Sufficit-panel-auth-regression.

Limites: não foi usado login real da conta do usuário nem aguardado um novo dia
calendário. A evidência reproduz a condição de sessão/marcador e valida o fluxo
HTTP, state e middleware. Prefetch/concorrência são possíveis fontes do cookie
antecipado, não causas específicas demonstradas pelos logs disponíveis. O fato
comprovado era a classificação prematura de uma tentativa como falha local.
Nenhuma mudança de UI exigiu nova rodada visual. Todos os checkpoints concluídos.

Contrato permanente em [customer-access.md](../customer-access.md#retomada-após-expiração-e-proteção-de-login).
