# Correção do escopo do movimento e recuperação de sessão

Pedido do mantenedor: animação exclusivamente nas extensões e diagnóstico da
captura com UnauthorizedAccessException. A tarefa original permanece aberta no
[plano](202611171057-extension-motion-completed.md) até liberação do filtro de estado no Google.

## Correções publicadas

- CSS de movimento exige classe call-motion, emitida somente para PEER.
  Troncos e filas mantêm estado/cor sem movimento, inclusive Ring/Ringing.
- Recuperação Unauthorized na mesa oferece Entrar novamente via /login com
  URL/filtros de retorno e navegação HTTP; não repete apenas o circuito expirado.
  Negação de empresa continua oferecendo as empresas autorizadas.
- Estado de canal reconhece também nomes de enum DialingOffhook/PreRing.
- Fixture de regressão inclui tronco e ramal no mesmo estado; prova que só ramal
  anima. Fixture de expiração prova link de login e ausência de dados protegidos.

Fonte painel fcc5335, push main. Publish Release e 1.530 checks PASS.
card-call-motion.browser.cjs passou contra o artefato Release (estados, escopo,
temas, mobile, reduced-motion, teclado e link de recuperação). Teste existente
error-diagnostics.browser.cjs PASS. Regressão do tronco reproduzida antes do fix.

## Diagnóstico privado, sem payloads de chamadas

A referência 01a1e9be6055736698048e5fdf882c7e corresponde a token expirado em
2026-11-17 13:42:08.8854384 UTC, sem renovação, seguido da falha 13:42:10 UTC.
A sessão foi invalidada por expiração do token, não por perda no deploy.
Permissões e limite de duração da sessão não foram ampliados.

Probe UDS somente leitura, executado como sufftelpanel, emitiu apenas contagens
agregadas. Snapshot: 24 canais de extensões Down/uso não informado; dez canais
Ring em troncos. Na janela de 15 segundos, 22 NewChannel/Down e 14
NewChannel/Ring, nenhum NewState. Conferência somente leitura do Google confirmou
que a conta suffobserve não tem filtro Newstate. Nenhum número, canal, conta ou
credencial foi incluído na saída desse probe. Segunda versão exigiu transferir
o conjunto atualizado de DLLs por mudança de versão automática da dependência;
a primeira execução dessa versão falhou por assembly ausente, e a execução
com o conjunto completo passou.

Template/helper/testes preparados em sufficit-ami-events, commit 49c61e1/main;
7 testes Python PASS. Aplique somente na janela aprovada: ver
/mnt/sufficit/sufficit-ami-events/docs/activities/202611171057-extension-channel-states.md.
Nada foi alterado no Google. Esta entrega não comprova toque real nas extensões:
a evidência de estado está bloqueada no filtro AMI, não deve ser inventada.

## Publicação e rollback

Release exclusiva eveo-apps:
/opt/sufficit-telephony-panel/releases/extension-motion-20261117-fcc5335.
SHA256 DLL b998b71a0ac242bcfa422634ca39f6594870a9386aee22fe625c96f519a16fb9.
Manifesto completo conferido antes de ativar; configuração copiada só no host,
SHA256 a0f5e8fe9674454ab4bb53bb3016fb22f5ae100005f87d36d1e0bd65fefa5e44 preservado.
Helper release-panel.py passou com rollback automático por health disponível.

- Painel ativo, PID2574156, NRestarts0; health socket/público 200/running.
- API fleet anônima 401; preview de produção não revela fixture.
- CSS público Sufficit.Telephony.Panel.keili74jh6.styles.css idêntico ao pacote,
  SHA256 6c1e0ba11904576236bbcde40656a0cec11bf5d32c01563dab128e81d171dbbf;
  exige .board-tile.call-motion para animação.
- Journal sem erros na janela de um minuto verificada.
- Observador permanece PID410178; AMIEvents legado permanece PID409848.
- Sem reinício de coletor, nginx, Identity, Asterisk ou controladores.
- Anterior card-motion-20261117-d05f861 retida. Manifesto privado:
  /var/backups/sufficit-telephony-panel/extension-motion-20261117-fcc5335/release.json.
  Reverter symlink e reiniciar só painel se necessário.
- Servidor local de validação encerrado.

A regra de janela do Google vem de
[decisão de publicação](202611121254-publish-panel-queue-details.md), seção
Limitações e decisão do mantenedor: painel pode ser publicado sem aguardar
horário, mas Google exige horário mais calmo. Continuidade aguarda decisão
sobre recarga AMI e eventual reconexão do observador, sem reiniciar Asterisk.


Atualização posterior: mantenedor autorizou a janela; filtro Newstate aplicado e validado no Google, com cinco extensões ringing e duas com canais Up na amostra real. Pendência encerrada: [conclusão](202611171057-extension-motion-completed.md).
