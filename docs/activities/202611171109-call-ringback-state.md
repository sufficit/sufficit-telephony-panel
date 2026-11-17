# Estado de toque confirmado na origem

Pedido: ao chamar PJSIP → SIP/6007, a origem continuava “Discando” quando o destino já estava “Tocando”. Corrigir a apresentação da fase da chamada na origem, preservando distinção entre receber toque e destino tocando. Continuidade da implementação/publicação autorizada nesta sessão.

1. CONCLUÍDO — Confirmar contrato dos canais/fluxos e reproduzir origem Ring com destino Ringing correlacionado.
2. CONCLUÍDO — Apresentar destino tocando na origem com evidência autorizada de linkedid/nó, sem contaminar chamadas independentes; validar regressões e navegador.
3. CONCLUÍDO — Commit/push, publicar somente painel em eveo-apps, verificar saúde/artefatos e converter plano em atividade.

Direção: manter os efeitos existentes exclusivamente em extensões (DESIGN.md e pesquisa desta sessão); corrigir o estado que os aciona. Usar texto próprio para toque no destino, manter foco/texto estáveis e reduced-motion. Não inferir vínculo por número discado; não animar troncos/filas. Nenhuma alteração de AMI, Asterisk ou controlador necessária.

Validação: regressão C# com origem/destino, isolamento por nó/conta/linkedid, estados antes/depois de atendimento/remoção, browser com origem e destino simultâneos e escopo de animação. Fonte real já fornece Newstate após operação anterior.

Contrato: preservar estado AMI Ring da origem, mas apresentar “Destino tocando” quando um canal de endpoint autorizado do mesmo linkedid/nó confirmar Ringing. Chamada sem correlação mantém Discando; origem Up permanece em uso; toque local continua Tocando. Testes de isolamento reproduzem o caso antes da correção.

Regressão reproduzida em C# antes da mudança (origem mantinha dialing). Após correção: 1.542 checks PASS; build PASS; call-ringback.browser.cjs PASS com ambos os cards, término do pulso, mobile/escuro/reduced-motion e troncos/filas estáticos. DESIGN.md e operator-board.md atualizados com a distinção de estado local/fase correlacionada.


## Decisões e evidências

A captura do usuário mostrou origem PJSIP âmbar/Discando e destino SIP/6007
azul/Tocando na mesma chamada correlacionada. O problema era de apresentação:
a origem continuava com Ring e o destino já tinha Ringing. O novo estado do card
combina somente evidência autorizada da mesma chamada/nó para apresentar a fase
de toque no destino, sem alterar o canal AMI original. O rótulo “Destino tocando”
evita afirmar que a origem está recebendo chamada. Mantidos os mesmos tokens e
pulso dos cards existentes; não houve mudança de CSS ou de identidade visual.

Somente origem PEER ainda em discagem usa essa confirmação. Origens já Up,
chamadas de outro linkedid/nó/conta, destinos Down/Local ou ausentes não herdam
ringing. Remover/atender o destino retira a indicação de toque correlacionado.
Não há inferência por número ou por todos os canais da central. Estado de tronco
continua local e troncos/filas não animam. Contrato permanente em
[operator-board.md](../operator-board.md#toque-no-destino-e-estado-da-origem).

A suíte falhou antes da correção em “Caller Ring reflects confirmed ringing on
the authorized destination”. Após correção, 1.542 checks passaram; build e publish
Release passaram. Testes de navegador call-ringback.browser.cjs e
card-call-motion.browser.cjs passaram. O teste de ringback também passou contra
as DLLs do publish, com origem/destino simultâneos, discagem, toque, atendimento,
remoção, rótulos distintos, escuro/mobile/reduced-motion e troncos/filas estáticos.
Capturas locais ignoradas pelo Git em .impeccable/review/ringback-origin.png e
ringback-destination.png foram inspecionadas. Registros de teste são fixtures;
nenhuma chamada ou sessão autenticada do cliente foi usada na validação.

## Publicação

Fonte a9c5564, commit/push main. Release no eveo-apps:
/opt/sufficit-telephony-panel/releases/ringback-20261117-a9c5564.
DLL SHA256 fed7484a2ff67ea72218b04b80758f88111fb29b145b837aba5476ceb5716523.
Manifesto completo de arquivos conferido antes/depois de ativar. Configuração
copiada somente no host e preservada:
a0f5e8fe9674454ab4bb53bb3016fb22f5ae100005f87d36d1e0bd65fefa5e44.

Helper release-panel.py ativou com rollback automático por health; não necessário.
Anterior extension-motion-20261117-fcc5335 retida. Manifesto privado de rollback:
/var/backups/sufficit-telephony-panel/ringback-20261117-a9c5564/release.json.
Para reverter, conferir paths e voltar current à anterior, reiniciando só painel.

Painel ativo PID2589920/NRestarts0; observador permaneceu PID2577787 e legado
AMIEvents PID409848. Sem alteração/reload de AMI/Asterisk/controladores/Google.
Health socket/público 200, API fleet anônima 401, sem erros no journal na janela
de um minuto consultada. CSS público idêntico ao pacote (sem mudança de CSS):
Sufficit.Telephony.Panel.keili74jh6.styles.css,
SHA256 6c1e0ba11904576236bbcde40656a0cec11bf5d32c01563dab128e81d171dbbf.
Preview público não expôs dados sintéticos. Servidor local de validação encerrado.
A publicação prova a versão/saúde; a reprodução visual da correção usa fixture
correlacionada do caso reportado. Não foi alegada nova chamada real autenticada.
