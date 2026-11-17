# Diagnóstico de tentativas ausentes no PJSIP

Pedido: versão publicada não mostra as tentativas inválidas no ramal PJSIP do mantenedor. Confirmar versão e evidência do endpoint visto nas capturas desta sessão antes de alterar a interpretação.

1. CONCLUÍDO — Confirmar versões ativas e capturar somente estados/causas/presença de resultado para o PJSIP identificado nesta sessão, sem números discados ou credenciais.
2. CONCLUÍDO — Corrigir o defeito comprovado ou registrar exatamente qual evidência falta; validar regressão/isolamento e comportamento real disponível.
3. EM ANDAMENTO — Publicar alterações necessárias, validar saúde, registrar entrega e converter plano quando todos os checkpoints estiverem comprovados; commit/push main.

Versões confirmadas: painel dial-failure-20261117-b938273, observador dial-failure-20261117-5617f2b; ambos ativos. A validação anterior de causa1 era agregada da central; não comprovava as tentativas específicas do ramal PJSIP do usuário. Não transformar causa normal/ausente em número inválido.


Evidências da investigação: versões ativas e PIDs painel2606600/observador2606396
confirmados. Captura UDS de 40s com o identificador extraído da captura anterior
não encontrou linhas/sessões nem eventos desse identificador. Leitura de endpoints
PJSIP e últimas100mil linhas do log no Google também não encontrou o prefixo;
isso não prova inexistência, nem confirma o identificador atual do usuário.
Nenhuma falha específica desse ramal foi demonstrada ainda; não alterar causas
ou rotas com base nesse resultado inconclusivo.

Captura temporária read-only em eveo-apps, duração máxima10min:
/tmp/pjsip-attempt-watch-20261117/Probe.dll;
saída privada /tmp/pjsip-attempt-watch-20261117.log (0600), sem identificadores,
contas, números ou credenciais. Para correlacionar depois, cada endpoint aparece
como SHA256(endpoint normalizado) em hexadecimal maiúsculo, primeiros12 caracteres.
Campos: timestamp, tipo de evento, estado permitido, causa numérica e existência
de AccountCode. Não cria conexões AMI nem chama canais; lê apenas UDS existente.
Ao retomar, conferir PID/log e expiração; não confundir eventos agregados com
reprodução do usuário. Próximo passo depende de uma tentativa nova e do
identificador completo/atual do card PJSIP. Encerrar watcher se ainda ativo após
a evidência necessária; inspecionar PID explícito antes de parar o processo.


Retomada 17/11/2026 12:19 BRT: usuário informou que fez a tentativa, mas suspeita
que a janela terminou. Processo anterior PID2616315 já encerrado; log terminou
11:47:34 BRT. Foram preservados210 eventos PJSIP de um endpoint anonimizado,
com69 Hangup (causa21:31,16:25,17:7,20:6); não há como atribuí-los ao ramal do
usuário sem o identificador atual e o horário. Nenhum deles confirmou causa1/28,
portanto não reinterpretar rejeição/normal/ocupado/ausente como número inválido.

Captura reaberta às12:19:48 BRT, duração máxima60min, até aproximadamente13:20 BRT:
PID2653023 (runuser), /tmp/pjsip-attempt-watch-extended-20261117/Probe.dll;
log privado /tmp/pjsip-attempt-watch-extended-20261117.log, modo0600 confirmado.
Versão do probe e campos continuam somente leitura e anonimizados. A captura
anterior foi preservada; nenhum serviço foi reiniciado ou pacote de produto
alterado. Ao retomar, conferir processo/log, correlacionar SHA256 do identificador
informado pelo usuário e finalizar o diagnóstico. Ainda falta identificador
atual do card e horário aproximado da tentativa; pode ser necessário reproduzir
na nova janela. Plano permanece aberto, sem encerrar os checkpoints pendentes.


Identificação fornecida pelo mantenedor: endpointHash E5E1EEC4C20A. Captura real
17/11 12:21:56.034 BRT: NewChannel/Ring seguido de Hangup/Ring causa16, no mesmo
milissegundo observado. Confirma defeito de cobertura: regra anterior mostrava
somente causas explícitas1/2/3/5/28, omitindo esta tentativa. Não há evidência de
número inválido nessa causa. Acrescentar resultado neutro “Tentativa encerrada”
para Ring/4 encerrado normalmente em até2s e sem Up observado. Nunca apresentar
chamada atendida ou cancelamento após toque prolongado como erro de discagem.

Regressão baseada na reprodução falhou antes da correção por resultado ausente. Depois: 1.606 checks PASS, incluindo exclusão de Up, Down, unknown, toque prolongado e transferência com Up anterior. Caso real será apresentado como Tentativa encerrada (warning), nunca Falha/número inválido. Mesmo prazo10s e isolamento anteriores.
