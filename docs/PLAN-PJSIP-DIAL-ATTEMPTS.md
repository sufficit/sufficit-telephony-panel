# Diagnóstico de tentativas ausentes no PJSIP

Pedido: versão publicada não mostra as tentativas inválidas no ramal PJSIP do mantenedor. Confirmar versão e evidência do endpoint visto nas capturas desta sessão antes de alterar a interpretação.

1. AGUARDANDO REPRODUÇÃO/IDENTIFICAÇÃO — Confirmar versões ativas e capturar somente estados/causas/presença de resultado para o PJSIP identificado nesta sessão, sem números discados ou credenciais.
2. PENDENTE — Corrigir o defeito comprovado ou registrar exatamente qual evidência falta; validar regressão/isolamento e comportamento real disponível.
3. PENDENTE — Publicar alterações necessárias, validar saúde, registrar entrega e converter plano quando todos os checkpoints estiverem comprovados; commit/push main.

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
