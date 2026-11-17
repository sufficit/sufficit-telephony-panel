# Deploy do movimento dos cards

Autorização: pedido explícito de deploy nesta sessão; somente painel em eveo-apps, conforme deploy/README.md. Fonte d05f861 (main).

1. CONCLUÍDO — Conferir release ativa, acesso, estado Git e gerar publish Release sem configurações locais.
2. CONCLUÍDO — Instalar release isolada, conferir hashes, preservar configuração/estado e ativar pelo helper com rollback.
3. CONCLUÍDO — Validar health interno/público, negação anônima, assets e serviço; converter em atividade e commit/push.

Aceitação: DLL/CSS instalados iguais ao pacote validado, novo efeito no CSS público, serviço saudável; reiniciar somente painel. Sem alterações de Identity, nginx, coletores, Asterisk ou controladores. Conferência de chamada real autenticada não será presumida por health/assets.

Preflight: release queue-waiting-20261113-690c3b9, painel PID 2287312/NRestarts 0, coletor AMI PID 409848. Dependências Git limpas. Publish Release concluído em /tmp/panel-card-motion-d05f861. Configuração atual SHA256 a0f5e8fe9674454ab4bb53bb3016fb22f5ae100005f87d36d1e0bd65fefa5e44.

Ativação concluída pelo helper com rollback; release card-motion-20261117-d05f861. Pacote Release validado pelo card-call-motion.browser.cjs antes de ativar. Todos os hashes instalados conferidos. Painel PID 2561756/NRestarts 0; coletor permanece PID 409848. Health interno/público 200/running, API anônima 401, journal sem entradas de erro na janela de 5 minutos. Configuração preservada.

## Evidências finais e recuperação

- Fonte do produto: d05f861; destino exclusivo eveo-apps.
- Release ativa: `/opt/sufficit-telephony-panel/releases/card-motion-20261117-d05f861`.
- SHA256 DLL: `e247950bca777cd9988df2b1239beef2c814720850973f2486cb5e33839f9605`.
- CSS público: https://panel.sufficit.com.br/Sufficit.Telephony.Panel.17z9m57569.styles.css
  SHA256 `932bf5b09c7d4778b19b07091cb5e4fce233037908225b7a25fab7729ddc96a8`;
  idêntico ao arquivo do pacote, contém call-dial-progress, call-ring-pulse e reduced-motion.
- Todos os 239 arquivos do pacote (excluindo configuração copiada do host) conferidos
  via manifesto SHA256 antes/depois da ativação.
- `/?preview=true` em produção não expôs cards sintéticos. Não foi utilizada sessão
  autenticada do cliente nem efetuada chamada real; health/arquivos públicos não
  substituem aceitação visual autenticada.
- Build publish Release e teste de navegador do artefato publicado passaram.
- Serviço do painel ativo, PID 2561756, NRestarts 0. Coletor AMI permaneceu PID 409848.
- Sem erros de journal no intervalo de cinco minutos verificado. Health socket e
  público 200/running; API fleet anônima 401.
- Configuração appsettings.json copiada exclusivamente no host; hash preservado.
  Nenhuma configuração/credencial foi transferida à estação. Estado persistente,
  nginx, Identity, coletores, Asterisk e controladores preservados.

Rollback: release anterior `queue-waiting-20261113-690c3b9` retida. Manifesto
privado criado pelo helper em
`/var/backups/sufficit-telephony-panel/card-motion-20261117-d05f861/release.json`.
Se necessário, conferir paths, voltar symlink current à release anterior e reiniciar
somente sufficit-telephony-panel. Rollback automático por health não foi necessário.
Servidor local de validação do pacote encerrado ao finalizar. Registro entregue
por commit/push direto à main; a publicação foi verificada separadamente acima.
