# Distribuição das filas e turnos

Objetivo: o painel deve mostrar, por fila, em quais ramais as chamadas caem, com que frequência, quem atende e quem deixa tocar, de forma gráfica. Fonte em tempo real; o CDR é lento e fica de fora.

Decisão de 22/10/2026: **por enquanto, sem banco de dados.** A contagem fica em memória no coletor de observação (`sufficit-ami-observation`), em janela de 24 h com baldes por hora. Um reinício do coletor recomeça a contagem. Deploy do painel não afeta.

## Fase 1: distribuição ao vivo (sem banco)

1. [concluído] Coletor: `QueueDistribution` (sufficit-ami-events/streaming) soma, por fila × ramal × hora, quantas vezes tocou, atendeu, não atendeu (tocou até o tempo esgotar), ficou indisponível (falhou na hora, tempo de toque 0) e os tempos de toque e de conversa. Também guarda as últimas 3.000 chamadas com a sequência de ramais. O RPC `Distribution` responde só pelo socket privado e filtra por empresa.
2. [concluído] Painel: a página Filas ganhou a seção "Distribuição das filas", com números-chave, chamadas por hora, barras por ramal, últimas chamadas com os ramais tocados e janelas de hora atual, 4 h e 24 h. O filtro de empresa é montado no servidor a partir das permissões; cliente só vê a própria empresa e nunca filas sem empresa.
3. [concluído] PABX: incluir `AgentCalled`, `AgentRingNoAnswer`, `AgentConnect` e `AgentComplete` no filtro AMI do usuário `suffobserve` (`manager_local_observation.conf`) e dar `manager reload`. `eventwhencalled=yes` já está ligado no google-voip.
4. [concluído] Deploy do coletor (versão 0.2.0) e do painel (0.19.0); conferir com a fila 0007366500 ("atendimento") do cliente 0e647909-7a4e-43be-8350-6bf25a05ce1b.

Limites: só conta o que acontecer depois do passo 3; nome amigável da fila (ex.: "atendimento") ainda não aparece, só o número da fila no PABX; "outro atendeu" no ringall inclui quem tocava quando a chamada foi atendida ou abandonada.

Publicado em 22/10/2026: coletor `queue-distribution-20261022-253d869` (anterior `queue-flow-20260911-1503`, troca pelo link `current`), painel `queue-distribution-20261022-26278bd` (anterior `shared-prefix-fix-20261022-5433a24`). Filtro no google-voip com backup `manager_local_observation.conf.bak-20261022-distribution`; o filtro só vale após novo login AMI, por isso o coletor foi reiniciado. Sonda no socket privado minutos depois: 4 filas, 13 ramais, eventos de toque, atendimento e indisponível chegando. apoint-voip e eveo-voip ainda sem os eventos `Agent*`.

Filas novas (controlador Queue) entram desde 22/10 pela API de diagnóstico do controlador, com histórico persistido de 24 h e nome amigável; release `controller-distribution-20261022-790695b`.

## Fase 2: turnos e horários por fila, configurados pelo cliente

Decisão de 23/10/2026: o painel é só de monitoramento (TV na sala de atendimento). O cadastro dos turnos fica no sufficit-blazor; o painel apenas exibe as métricas por turno.

Pedido do usuário em 22/10/2026: configuração na fila para horários e turnos de trabalho, com métricas e estatísticas por turno, definida pelo próprio cliente.

5. [a fazer] Modelo: turnos por fila (nome, dias da semana, início e fim, fuso), com sobreposição proibida e feriados opcionais. Configurado pelo cliente final no **sufficit-blazor** (nunca no painel), com a permissão da própria empresa, e auditado. O painel só lê os turnos.
6. [a fazer] Métricas por turno: chamadas recebidas, atendidas, abandonadas, nível de serviço, espera média, distribuição por ramal e cobertura (ramais disponíveis × chamadas) dentro de cada turno. Comparação entre turnos e entre dias.
7. [a fazer] Fora do turno: destacar chamadas que chegaram fora de qualquer turno e ramais que atenderam fora do próprio turno.
8. [a fazer] Persistência (decisões de 23/10/2026). Regra: nenhum app de voz pode depender completamente de banco.
   - Curto prazo (até 7 dias, suficiente): visitas e eventos por toque que o controlador (sufficit-ari) já grava no dbvoiprt; retenção de 7 dias mantida. Métricas por turno dentro dessa janela saem de consulta agregada no controlador; turno é janela de horário aplicada na leitura.
   - Gravação sem depender do banco: o `CallDiagnostics` nunca bloqueia a chamada; quando o banco estoura o prazo, o lote vai para arquivo local e é regravado depois (idempotente pelo ID do evento). Hoje ele descarta o lote após 3 tentativas, então esse fallback é pré-requisito.
   - Longo prazo: consolidado diário, uma linha por dia × fila (turno como coluna/dimensão do mesmo agregado, sem tabela extra), no **logdata** (outro cluster), nunca no dbvoiprt. Gerado a partir dos 7 dias de detalhe antes da limpeza, pelo mesmo caminho banco + arquivo local.
9. [a fazer] Relatórios: exportar por turno e período (CSV) e resumo diário opcional.

Aceite da fase 2: o cliente cria os turnos da própria fila no sufficit-blazor sem suporte e vê no painel, para cada turno, quantas chamadas entraram, quem atendeu e quanto ficou sem resposta.
