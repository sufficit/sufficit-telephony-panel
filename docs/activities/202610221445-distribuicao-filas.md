# Distribuição das filas (22/10/2026)

Pedido: ver pelo painel em quais ramais as chamadas de uma fila caem, com que frequência, de forma gráfica. CDR descartado por ser lento; decisão "por enquanto, sem banco".

- Coletor de observação soma em memória por fila × ramal × hora (24 h) a partir de `AgentCalled/RingNoAnswer/Connect/Complete` e `QueueCaller*` (ami-events 253d869, versão 0.2.0).
- Painel: seção "Distribuição das filas" em `/queues` (0.19.0, 52623f1 + 26278bd). Testes: 1111 unitários, 32 do streaming, 17 de navegador.
- PABX google-voip: 4 eventos incluídos no filtro do `suffobserve`, `manager reload` e reinício do coletor.
- Validação em produção: sonda no socket privado mostrou contagens chegando poucos minutos após a troca.
- Removido o arquivo temporário com a credencial de leitura do CDR em eveo-apps.
- Planejado (fase 2): turnos por fila configurados pelo cliente, com métricas por turno; exige persistência.
