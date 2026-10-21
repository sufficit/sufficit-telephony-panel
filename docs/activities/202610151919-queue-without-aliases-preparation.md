# Queue sem aliases — preparação, produção pendente

Pedido explícito do mantenedor: atualizar todos os contratos, inclusive persistência,
sem aliases de compatibilidade. Esta decisão substitui a preservação dos nomes ACD
na entrega0.15.0. Não equivale a retirar normalização funcional de agentes SIP/GUID/WSS.
Plano completo: /mnt/sufficit/sufficit-ari/docs/activities/202610152240-queue-without-aliases-plan.md.

Fontes alteradas em worktrees isolados de Blazor, Panel e EndPoints; núcleo ARI0.16.0
e dialplan local atualizados. Sem rewrite /api/acd, header antigo, ambiente traduzido
ou contextos antigos no núcleo novo. Classes/rotas/contratos/nomes de arquivo dos
consumidores mudaram para Queue; sidecars AI e configuração acompanham a mudança.
Estrutura visual/tokens preservados, tomando as telas atuais e DESIGN.md como referência.
Sem verificação visual em navegador nesta etapa; renderizações bUnit passaram.

Persistência nova usa telp_queue_* no dbvoiprt e Redis queue:*/ari:shadow:*.
Ferramentas de migração preservam dados/checksums, índice/check/FK, grant por coluna,
plugin/hash/host de conta e tipo/ordem/TTL de Redis. Destinos DID são validados contra
cliente/fila e alterados condicionalmente, mantendo atendimento e cobrança.
Aliasing de caminhos antigos sufficit-telephony-acd* retirado; worktrees preservados.

Validações locais:412 integrações net10 e412 net7.0.20;115 frontend/BFF;901 painel;
oito EndPoints;39 dialplan Asterisk18;11 ingresso;12 presença;três proxy;cinco migração
Asterisk;11 shadow Redis. Migração MariaDB:20 verificações com seis tabelas populadas,
checksums/GUIDs/histórico/FKs/checks/grants e rejeição de DID entre clientes. Redis:
seis verificações, incluindo colisão sem alterações e TTL/ordem preservados.
Teste de ausência de alias de ambiente reproduziu falha antes da correção e passou
depois. Harness de dialplan passou a observar a terminação da perna Local e proibir
Stasis durante toda a tentativa, evitando uma falha de observação durante Hangup.
Build Server e publicações Node net10/net7 aprovados. Avisos preexistentes não foram
tratados como parte desta migração.

## Bloqueio e continuidade

Nenhuma troca em produção foi executada. A credencial de backup encontrada foi
recusada pelo proxy MariaDB (1045). Não houve acesso SQL direto ao nó para contornar
a regra. Falta um arquivo privado com credencial administrativa aceita pelo proxy
localhost:26493/dbvoiprt. Produção0.15.0 e consumidores atuais permanecem operantes.
As fontes precisam ser publicadas e ativadas em conjunto após fornecer a credencial,
com backup, drain e escritores parados. Migração completa de infraestrutura, contas,
configurações, persistência e consumidores, teste de atendimento e auditoria de
resíduos continuam pendentes no plano. Não considerar esta atividade como entrega
final em produção nem publicar um consumidor incompatível isoladamente.
