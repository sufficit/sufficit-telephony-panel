# Rechecagem de chamada ativa por DID

Objetivo: corrigir o caso real da DID 557137170655 que permaneceu com zero durante uma chamada conectada pelo Sufficit Phone após o rollout anterior.

1. [em andamento] Capturar Status e variáveis da chamada real nos três nós, identificar a condição exata que a exclui.
2. [pendente] Implementar somente a correção comprovada, incluindo regressão com a forma real dos dados.
3. [pendente] Validar, commit/push na main, publicar o necessário e verificar o caso ativo; remover a coleta temporária e arquivar a atividade.

A captura enviada mostra a chamada conectada por sete segundos e o card em “Sem chamadas recebidas”. Nas consultas feitas depois, não havia canais do contexto/DID nos três nós. Uma amostra real do Status no Google trouxe 13 canais, todos com Uniqueid e Linkedid, cinco raízes e nenhum do cliente. Portanto, não se presume que Linkedid esteja ausente. Hipóteses incluem continuação de chamada interna em canal filho e diferenças nas variáveis de entrada/conta; nenhuma delas foi aplicada como correção sem captura.

Coleta temporária, exclusivamente de leitura, por até dez minutos no eveo-apps, usando a configuração AMI já existente e a VPN para Eveo. Registra somente canais ligados ao contexto/DID alvo; não registra outros clientes, CallerID, senha ou token. Captura NUMENTRADA, CDR(did), UID/Linkedid, conta e decisão do filtro atual. Não gera ligação, hangup, reload ou restart no PABX.

Dependência para concluir o primeiro checkpoint: nova ligação do usuário enquanto a coleta estiver ativa, mantida por alguns segundos. Sem uma chamada ativa, os testes sintéticos anteriores não comprovam o cenário relatado.
