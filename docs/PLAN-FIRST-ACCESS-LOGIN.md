# Primeiro acesso após expiração da sessão

1. [concluído] Diagnosticar captura e logs reais: em19/11às09:26:34 o erro veio de Automatic sign-in stopped, apenas com cookie de tentativa; login manual às09:26:48 concluiu. Não há falha OIDC registrada nesse intervalo. Cookie de tentativa pendente estava sendo interpretado como conclusão sem sessão.
2. [concluído] Vincular marcador anti-loop à conclusão real de um login automático, ignorar marcadores antigos/pendentes e impedir disparos por prefetch/prerender; preservar permissões, logout e falhas reais.
3. [em andamento] Validar regressão antes/depois, Release e HTTP local; commit/push e publicar somente painel, conferir saúde/hash; converter em atividade.

Restrições: não alterar Identity, tempo de sessão ou permissões; não remover proteção contra loop real; não registrar tokens/state/cookies completos; preservar sessões válidas. Hipótese de prefetch ou acessos concorrentes explica o marcador antecipado, mas não está comprovada nos logs. A causa comprovada é a decisão prematura da proteção local. Sem alteração/reinício Google/controladores/API.

Regressão antes da correção falhou: A pending first access is not a completed failed login. Após correção:1.659 checks Release passaram. Teste HTTP local passou com sessão ausente/expirada, marcador antigo/pendente, duas navegações, marcador concluído ainda bloqueando loop, prefetch/prerender e logout/erros explícitos. Marcador novo __Host- com Secure/HttpOnly/Path=/ e30s, apenas OnTicketReceived do fluxo automático identificado no state protegido do OIDC. Sem mudar duração de sessão/refresh/permissões.

Também validada serialização/proteção/restauração das propriedades do state OIDC: o indicador automático sobrevive ao round trip protegido. HTTP local não segue redirects nem contata Identity.
