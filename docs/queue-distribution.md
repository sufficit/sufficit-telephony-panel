# Distribuição das filas

Seção da página **Filas** (`/queues`) que mostra quem recebe as chamadas de cada fila.

## O que aparece

- **Números-chave:** chamadas recebidas, atendidas (com taxa), abandonadas, espera média e ramais que atenderam.
- **Chamadas por hora:** barras empilhadas (atendidas, abandonadas, outras) por hora do relógio.
- **Por ramal:** uma barra por ramal com quantas vezes **atendeu**, **tocou e não atendeu**, ficou **indisponível** (o canal falhou na hora, por exemplo ramal desregistrado) e **outro atendeu ou quem ligou desistiu**. Abaixo: vezes que tocou, taxa de atendimento (sem contar indisponível), toque médio, conversa média e último atendimento.
- **Últimas chamadas e quem tocou:** horário, número, resultado, espera e a sequência de ramais com a cor do resultado de cada um.
- **Período:** hora atual, últimas 4 h ou últimas 24 h. A janela soma a hora corrente (parcial) e as anteriores.

## De onde vem

Eventos do `app_queue` recebidos em tempo real pelo coletor de observação (`sufficit-ami-observation`): `QueueCallerJoin/Leave/Abandon`, `AgentCalled`, `AgentRingNoAnswer`, `AgentConnect` e `AgentComplete`. O coletor soma em memória (`QueueDistribution`, sufficit-ami-events/streaming), sem banco de dados, por 24 h. Reiniciar o coletor recomeça a contagem.

O painel lê pelo RPC `Distribution` no socket Unix privado do coletor (o mesmo da telemetria). Pedido por TCP é recusado.

## Permissões

O filtro de empresa é montado no servidor do painel a partir das permissões atuais e enviado ao coletor; o painel ainda refiltra a resposta.

- Cliente: só filas e chamadas com AccountCode da própria empresa (ou do contexto escolhido). Fila sem empresa nunca aparece.
- Gestor: todas as filas, inclusive as sem empresa identificada, quando nenhum contexto está selecionado.

## Limites

- Conta só a partir do momento em que o PABX envia os eventos `Agent*` ao usuário AMI `suffobserve`.
- Mostra o número da fila no PABX (ex.: `0007366500`), ainda sem o nome amigável do portal.
- Turnos e histórico longo: ver `PLAN-DISTRIBUICAO-FILAS.md` (fase 2).
