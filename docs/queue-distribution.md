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

**Filas novas (controlador Queue do sufficit-ari):** não usam `app_queue`, então o coletor não as vê. O painel lê as chamadas e os eventos por oferta (`offered`, `ringing`, `connected`, `offer_finished`) das últimas 24 h pela API de diagnóstico do controlador, já gravados no dbvoiprt. Chamadas encerradas ficam em cache; a leitura por fonte se repete no máximo a cada 15 s. Essas filas aparecem com o nome amigável (título do controlador) e não zeram com reinício do coletor. `channel_ended` em menos de 2 s após discar conta como indisponível; depois disso, como "tocou e não atendeu".

O painel lê pelo RPC `Distribution` no socket Unix privado do coletor (o mesmo da telemetria). Pedido por TCP é recusado.

## Permissões

O filtro de empresa é montado no servidor do painel a partir das permissões atuais e enviado ao coletor; o painel ainda refiltra a resposta.

- Cliente: só filas e chamadas com AccountCode da própria empresa (ou do contexto escolhido). Fila sem empresa nunca aparece.
- Gestor: todas as filas, inclusive as sem empresa identificada, quando nenhum contexto está selecionado.

## Limites

- Conta só a partir do momento em que o PABX envia os eventos `Agent*` ao usuário AMI `suffobserve`.
- Mostra o número da fila no PABX (ex.: `0007366500`), ainda sem o nome amigável do portal.
- Turnos e histórico longo: ver `PLAN-DISTRIBUICAO-FILAS.md` (fase 2).

## Referências visuais (22/10/2026)

Gráfico por hora e barras por ramal seguem:

- [Bar size and spacing (data.europa.eu)](https://data.europa.eu/apps/data-visualisation-guide/bar-size-and-spacing): largura da barra não carrega dado, equilíbrio entre barra e espaço.
- [Bar chart best practices (Shoreline/VTEX)](https://shoreline.vtex.com/components/bar-chart/best-practices): espaço menor que a barra, sem prejudicar a leitura.
- [Bar charts best practices (Nastengraph)](https://nastengraph.substack.com/p/bar-charts-best-practices): eixo a partir de zero, cuidado com cantos arredondados.
- [MUI X bar charts](https://v7.mui.com/x/react-charts/bars/): razão espaço/barra.
- [Multi segment progress bar (Siteimprove)](https://fancy.siteimprove.com/lab/components/Data visualization/Multi segment progress bar/): segmentos com cor própria, valores fora da barra, sem excesso de elementos.
- [Progress (Bootstrap via Symfony UX)](https://ux.symfony.com/toolkit/kits/bootstrap/components/progress.md) e [Progress bar (Singapore Design System)](https://designsystem.tech.gov.sg/components/progress-bar): nome acessível e não depender só de cor.

Decisões: o gráfico segue o período escolhido (1, 4 ou 24 barras, sempre finas: no máximo 30 px, nunca uma barra única
ocupando a largura toda); barras com preenchimento translúcido e topo na cor, 62% da coluna e no máximo 30 px, canto só no topo; barras por ramal finas
(8 px) com fio separando segmentos e os números fora da barra com chave de cor.
