# Destino e código no resultado da tentativa

Pedido: mostrar o que foi discado e o código no aviso de tentativa encerrada. Comentários em inglês. Asterisk Cause não equivale a status SIP; não inferir resposta SIP ausente.

1. CONCLUÍDO — Preservar destino original observado e causa numérica no resultado transitório, mantendo isolamento e expiração10s; reproduzir perda anterior em teste.
2. CONCLUÍDO — Exibir destino literal e causa Asterisk com descrição no card; validar fonte, navegador, falta de dados, mobile/escuro.
3. EM ANDAMENTO — Commit/push, publicar observador/painel no eveo-apps, conferir hashes/saúde, converter planos em atividades.

Manter o visual existente e escopo somente extensões. Não usar número de outra chamada nem persistir histórico de discagens no inventário. Preferir exten original válido do canal; excluir extensões técnicas s/h. Se ausente, informar indisponibilidade. Atualização do contrato anterior que omitia destination nos resultados; agora é solicitada pelo usuário e continua apenas em memória.

Regressão anterior reproduzida: resultado perdia destination/cause. Depois: 1.621 checks PASS; preservação do exten original apesar de connected-line/rota posterior; s/h/desconhecido não viram número. Resultado mantém apenas destination e cause, sem caller/linkedid, por10s. Descrição causa16 = Encerramento normal, sem inferir SIP.

Browser com destino literal/causa/tooltip de distinção SIP, ausência de dados, estados anteriores, mobile/escuro/reduced-motion PASS.
