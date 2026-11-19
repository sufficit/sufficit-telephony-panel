# Rechecagem de chamada ativa por DID

Objetivo: corrigir o caso real da DID 557137170655 que permaneceu com zero durante uma chamada conectada pelo Sufficit Phone após o rollout anterior.

1. [concluído] Capturar Status e variáveis da chamada real nos três nós, identificar a condição exata que a exclui.
2. [concluído] Implementar somente a correção comprovada, incluindo regressão com a forma real dos dados.
3. [em andamento] Validar, commit/push na main, publicar o necessário e verificar o caso ativo; remover a coleta temporária e arquivar a atividade.

A captura enviada mostra a chamada conectada por sete segundos e o card em “Sem chamadas recebidas”. Nas consultas feitas depois, não havia canais do contexto/DID nos três nós. Uma amostra real do Status no Google trouxe 13 canais, todos com Uniqueid e Linkedid, cinco raízes e nenhum do cliente. Portanto, não se presume que Linkedid esteja ausente. Hipóteses incluem continuação de chamada interna em canal filho e diferenças nas variáveis de entrada/conta; nenhuma delas foi aplicada como correção sem captura.

Coleta temporária, exclusivamente de leitura, por até dez minutos no eveo-apps, usando a configuração AMI já existente e a VPN para Eveo. Registra somente canais ligados ao contexto/DID alvo; não registra outros clientes, CallerID, senha ou token. Captura NUMENTRADA, CDR(did), UID/Linkedid, conta e decisão do filtro atual. Não gera ligação, hangup, reload ou restart no PABX.

Dependência para concluir o primeiro checkpoint: nova ligação do usuário enquanto a coleta estiver ativa, mantida por alguns segundos. Sem uma chamada ativa, os testes sintéticos anteriores não comprovam o cenário relatado.

Captura real: 11 amostras entre 22:13:37 e 22:13:48 UTC. Canal PJSIP/0004756200-00000a92 Up, Uniqueid 1795126417.571344, Linkedid 1795126416.571333, NUMENTRADA 557137170655. Accountcode herdou o originador (outro contexto), portanto o filtro Root o excluiu. O endpoint numérico foi confirmado no PABX com accountcode 223809ff00624cf890b9d6ea538dcffd e AOR 2052c18310ca4868b53dc40a985429f2. O catálogo tinha somente aliases GUID/WSS e não o alias numérico PJSIP.

Correção delimitada: alias PJSIP numérico exato pelo cadastro proprietário; aceitar somente o canal recebedor comprovado por cadastro + conta configurada do endpoint no PABX + UID/Linkedid atuais + NUMENTRADA/DID da empresa. Canais brutos de conta estrangeira continuam excluídos. Conservar todos os canais próprios comprovados para ramais e marcar um só âncora por DID/Linkedid/nó para evitar dupla contagem da entrada.

Validação da correção: três regressões falharam antes (alias numérico, recebedor com conta herdada e âncora duplicada); após a correção, 1691 checks do painel e 319 testes da API em cada candidato real, mais 32 guards de deploy, passaram. A captura de 11 amostras foi reproduzida com cadastro real e PJSIP_ENDPOINT(accountcode) real: uma chamada deduplicada. A projeção com permissão somente da empresa e catálogo antigo sem alias numérico produziu 1 na DID, 1 no 6200 e zero após término. As três combinações de dependências carregaram 3184 tipos e preservaram 27 itens de catálogo/7 ProductId. Coleta encerrada naturalmente após dez minutos, sem restart/reload no PABX.

Ordem de entrega: painel compatível com ambas as versões primeiro; depois API nos três hosts. A prova de propriedade também admite chamadas do ramal sem DID, sempre sem criar associação de entrada nesse caso. A política de comandos/supervisão não foi ampliada.
