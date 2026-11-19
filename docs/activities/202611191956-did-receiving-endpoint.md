# Correção da chamada recebida na DID e no ramal 6200

Objetivo: corrigir o caso real da DID 557137170655 que permaneceu com zero durante uma chamada conectada pelo Sufficit Phone após o rollout anterior.

1. [concluído] Capturar Status e variáveis da chamada real nos três nós, identificar a condição exata que a exclui.
2. [concluído] Implementar somente a correção comprovada, incluindo regressão com a forma real dos dados.
3. [concluído] Validar, commit/push na main, publicar painel/API, verificar hashes/saúde e replay da chamada capturada; remover a coleta temporária e arquivar a atividade.

A captura enviada mostra a chamada conectada por sete segundos e o card em “Sem chamadas recebidas”. Nas consultas feitas depois, não havia canais do contexto/DID nos três nós. Uma amostra real do Status no Google trouxe 13 canais, todos com Uniqueid e Linkedid, cinco raízes e nenhum do cliente. Portanto, não se presume que Linkedid esteja ausente. Hipóteses incluem continuação de chamada interna em canal filho e diferenças nas variáveis de entrada/conta; nenhuma delas foi aplicada como correção sem captura.

Coleta temporária, exclusivamente de leitura, por até dez minutos no eveo-apps, usando a configuração AMI já existente e a VPN para Eveo. Registra somente canais ligados ao contexto/DID alvo; não registra outros clientes, CallerID, senha ou token. Captura NUMENTRADA, CDR(did), UID/Linkedid, conta e decisão do filtro atual. Não gera ligação, hangup, reload ou restart no PABX.

Dependência inicial resolvida: o usuário manteve uma nova chamada durante a coleta. As 11 amostras abaixo substituem a hipótese anterior; os testes sintéticos anteriores, sozinhos, não comprovavam o cenário relatado.

Captura real: 11 amostras entre 22:13:37 e 22:13:48 UTC. Canal PJSIP/0004756200-00000a92 Up, Uniqueid 1795126417.571344, Linkedid 1795126416.571333, NUMENTRADA 557137170655. Accountcode herdou o originador (outro contexto), portanto o filtro Root o excluiu. O endpoint numérico foi confirmado no PABX com accountcode 223809ff00624cf890b9d6ea538dcffd e AOR 2052c18310ca4868b53dc40a985429f2. O catálogo tinha somente aliases GUID/WSS e não o alias numérico PJSIP.

Correção delimitada: alias PJSIP numérico exato pelo cadastro proprietário; aceitar somente o canal recebedor comprovado por cadastro + conta configurada do endpoint no PABX + UID/Linkedid atuais + NUMENTRADA/DID da empresa. Canais brutos de conta estrangeira continuam excluídos. Conservar todos os canais próprios comprovados para ramais e marcar um só âncora por DID/Linkedid/nó para evitar dupla contagem da entrada.

Validação da correção: três regressões falharam antes (alias numérico, recebedor com conta herdada e âncora duplicada); após a correção, 1691 checks do painel e 319 testes da API em cada candidato real, mais 32 guards de deploy, passaram. A captura de 11 amostras foi reproduzida com cadastro real e PJSIP_ENDPOINT(accountcode) real: uma chamada deduplicada. A projeção com permissão somente da empresa e catálogo antigo sem alias numérico produziu 1 na DID, 1 no 6200 e zero após término. As três combinações de dependências carregaram 3184 tipos e preservaram 27 itens de catálogo/7 ProductId. Coleta encerrada naturalmente após dez minutos, sem restart/reload no PABX.

Ordem de entrega: painel compatível com ambas as versões primeiro; depois API nos três hosts. A prova de propriedade também admite chamadas do ramal sem DID, sempre sem criar associação de entrada nesse caso. A política de comandos/supervisão não foi ampliada.

Publicação em andamento: commits main/remoto painel `4abd99c` e API `868dea8`. Painel instalado em `did-receiver-20261119-4abd99c`, SHA-256 `8c4d4a7c852d57d65ee7b3df65164cc4e145ffbd51ed523ee8ee2c14d6e2d46e`; health local e público aprovados. Baselines das 176 DLLs conferidas nos três hosts antes da entrega da API. API em transferência para Eveo; Castrum/Apoint pendentes.

Eveo concluído: publicação oficial aprovada, 176 DLLs instaladas idênticas ao candidato e health Healthy. Castrum e Apoint em transferência; ainda pendentes de promoção e conferência.

## Entrega e encerramento

Painel `4abd99c89dfcecea42894c513a14b6aff8113420` e API `868dea87ade50e945c970e9b8d12544b29c52940`, publicados na main remota. Painel instalado primeiro, release `did-receiver-20261119-4abd99c`; SHA-256 `8c4d4a7c852d57d65ee7b3df65164cc4e145ffbd51ed523ee8ee2c14d6e2d46e`. Saúde pública e socket aprovadas.

API publicada pelo `deploy.py` oficial em Eveo, Castrum e Apoint. As 176 DLLs de cada instalação foram comparadas integralmente ao respectivo candidato e conferem. SHA-256 da API nos três: `63c2ef4d2f7c44f7435465918004fcbaa2f478e0ccbc56c56bdde37f2e04cfbb`. Health Healthy nos três; rota `telephony/panel/inbound` sem autenticação respondeu 401 nos três. O informationalVersion da compilação conserva o commit anterior à criação do commit de entrega; a identidade instalada foi comprovada pelo SHA-256 e pelos manifestos, não por esse campo.

AMI Events PID 409848 e observador PID 2829095 permaneceram iguais. Nenhum restart/reload/configuração no PABX Google. Coleta temporária terminou naturalmente com sucesso e PID zero. Removidos 16 caminhos temporários remotos e 49 locais próprios, além dos três candidatos locais; mantidas releases, configurações, dados e rollbacks de produção. Anexos e arquivos de outras tarefas preservados.

## Evidência funcional e limites

O replay das 11 amostras reais, com propriedade consultada no cadastro e PJSIP_ENDPOINT(accountcode) no PABX, passou nas três combinações reais de dependências. A projeção do painel com acesso somente à empresa mostrou `extension6200Calls=1`, `didCalls=1`, `afterEnd=0`, sem depender do alias numérico no catálogo em cache. Não se trata de chamada artificial nem de nova ligação após o deploy.

Não foi feito login na conta do usuário nem observada uma nova ligação no navegador após a publicação. Portanto, o resultado na tela com uma nova chamada permanece uma confirmação manual; não é apresentado como teste realizado. Snapshot continua sem histórico completo: chamadas inteiramente entre consultas podem não ser observadas. Falha de fonte deve aparecer como alerta, não como zero confirmado.

Contratos permanentes: [Mesa operacional](../operator-board.md) e [Acesso por cliente](../customer-access.md). A regra de filas (somente espera) e as permissões de comandos/supervisão permanecem fora desta correção.
