# Destino e código no resultado da tentativa

Pedido: mostrar o que foi discado e o código no aviso de tentativa encerrada. Comentários em inglês. Asterisk Cause não equivale a status SIP; não inferir resposta SIP ausente.

1. CONCLUÍDO — Preservar destino original observado e causa numérica no resultado transitório, mantendo isolamento e expiração10s; reproduzir perda anterior em teste.
2. CONCLUÍDO — Exibir destino literal e causa Asterisk com descrição no card; validar fonte, navegador, falta de dados, mobile/escuro.
3. CONCLUÍDO — Commit/push, publicar observador/painel no eveo-apps, conferir hashes/saúde, converter planos em atividades.

Manter o visual existente e escopo somente extensões. Não usar número de outra chamada nem persistir histórico de discagens no inventário. Preferir exten original válido do canal; excluir extensões técnicas s/h. Se ausente, informar indisponibilidade. Atualização do contrato anterior que omitia destination nos resultados; agora é solicitada pelo usuário e continua apenas em memória.

Regressão anterior reproduzida: resultado perdia destination/cause. Depois: 1.621 checks PASS; preservação do exten original apesar de connected-line/rota posterior; s/h/desconhecido não viram número. Resultado mantém apenas destination e cause, sem caller/linkedid, por10s. Descrição causa16 = Encerramento normal, sem inferir SIP.

Browser com destino literal/causa/tooltip de distinção SIP, ausência de dados, estados anteriores, mobile/escuro/reduced-motion PASS.


## Resultado

O card da extensão inclui o destino observado no primeiro exten significativo
e a causa Asterisk, com descrição (16 · Encerramento normal, por exemplo).
Número permanece literal, com zeros/caracteres de discagem. O texto está em
bdi/Razor para preservar direção e escape HTML, sem interpretação como markup.
Ausência mostra Não informado, incluindo destination vazio vindo do protobuf.
Tooltip explicita que causa Asterisk não é código SIP; não há conversão inferida.
Dados só acompanham o resultado10s e a mesma conta/nó/endpoint; nenhuma
persistência de discagem no inventário. Documentação permanente em
[operator-board.md](../operator-board.md#falha-breve-de-discagem) e DESIGN.md.

Regressão perdeu destination/cause antes do fix. Depois:1.621 checks C# PASS,
42 streaming PASS, builds/publishes Release PASS. Browser PASS em Development
e contra pacote Release: destino literal/zeros, causa/descrição, ausência de
dados, escopo extensões, expiração/nova tentativa, mobile/escuro/reduced-motion.
Captura .impeccable/review/dial-failure-light.png inspecionada. Os testes de
interface usam fixture; não se presume teste autenticado de produção.

## Publicação e recuperação

Fonte painel e98998d/main; observador7bfb017/main. Releases exclusivas eveo-apps:
- Painel dial-details-20261117-e98998d, PID2829236/NRestarts0.
- Observador dial-details-20261117-7bfb017, PID2829095/NRestarts0.
- Legado AMIEvents permaneceu PID409848; sem alteração de Google/Asterisk/AMI,
  controladores, Identity ou nginx.
- DLL painel SHA256 c97c37e359c39cdf72214d68e9974415fb6639cdda77fc1a736b853b73609108.
- Manifestos239 arquivos painel/36 observador conferidos antes/depois de ativar.
- Configurações externas e token de observação preservados; copiada apenas
  configuração do painel, dentro do host, para a nova release.
- CSS público Sufficit.Telephony.Panel.o12hvpzrax.styles.css idêntico ao pacote,
  SHA256 646801f1c2ede664fc636d7d49acf92d6bdab0f63123ebdbf3484580bfc392aa.
- Health socket/público200, API fleet anônima401, preview público sem fixture;
  journal sem erros na janela2min consultada. Gates de rollback passaram.

Rollback painel: anterior immediate-attempt-20261117-84ad8ae retida, manifesto
/var/backups/sufficit-telephony-panel/dial-details-20261117-e98998d/release.json.
Conferir paths, voltar current e reiniciar só painel. Observador possui rollback
independente na atividade correspondente. Não houve necessidade de rollback.
Servidor local de validação encerrado. Commit/push main registram a entrega;
a evidência de publicação foi conferida separadamente. Resposta SIP real permanece
indisponível nesta fonte AMI; é uma limitação explícita, não um código omitido.

Entrega do fluxo: [atividade](../../../sufficit-ami-events/docs/activities/202611171543-telemetry-dial-result-details.md).
