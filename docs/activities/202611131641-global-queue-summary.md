# Contagem de filas e cobertura global

Todos os checkpoints do escopo autorizado foram concluídos: diagnóstico somente
leitura, correção/validação e publicação somente do painel. Fonte `9f6b09e`, main
publicada no GitHub. O trabalho nos controladores Google permanece adiado pelo
mantenedor e não faz parte desta entrega.

## Diagnóstico de 13/11/2026

O inventário central continha 2.750 cartões QUEUE: 416 Google, 1.167 Apoint e
1.167 Eveo; 417 chaves antigas e 2.333 nativas. Esse total incluía histórico e
a mesma fila em servidores diferentes, portanto não representava filas distintas.
Os agregados E/A retornaram HTTP200, 463 contextos e 1.159 filas em cada nó,
com espera zero e observação recente. A fachada global Google retornou HTTP404.

Consulta direta e somente leitura ao controlador Google, usando a credencial de
monitoramento existente, cobriu 466 contextos do índice privado: 464 HTTP200 e
2 HTTP404 sem filas. Resultado naquele instante: 1.161 filas, **4 chamadas em
espera e 8 em atendimento**. A leitura comprova que zero parcial não servia de
total global. Não representa uma medição simultânea à captura do usuário. Nenhum
serviço, chamada, fila, cliente ou permissão foi alterado nessa consulta.

## Comportamento entregue

O cabeçalho conta filas nativas distintas por empresa/GUID com observação atual.
Réplicas entre servidores contam uma vez; identidades desconhecidas continuam
separadas. Histórico e estado indisponível não inflacionam o denominador.
O texto informa cartões por servidor e quantos estão sem estado atual. A lista
prioriza atividade e observação atual, preservando o histórico e os detalhes
de GUID/gestão entregues anteriormente.

Havendo problema em alguma fonte configurada, espera global mostra `—` com aviso.
O painel não fabrica zero a partir de ausência de cobertura. Não foi implantado
nenhum endpoint novo no Google nesta tarefa. A visibilidade global completa das
chamadas desse nó ainda depende da publicação separada já adiada.

Ledger visual: captura do usuário e DESIGN.md orientam a composição existente;
componentes e estilos SUI preservados. Contagem distinta segue identidade
comprovada; ausência explícita segue o contrato das fontes. Sem redesign ou
remoção de cartões. Sem mudanças no agendamento de renderização Blazor.

## Validação e publicação

- Build: zero erros/avisos no comando de build registrado. Suite: **1.469 checks**
  passaram; aviso NU1903 preexistente sobre SignalR.Common10.0.11 registrado.
- `queue-summary.browser.cjs`: cobertura parcial, réplica nativa, histórico,
  ordem, cobertura completa e ausência de overflow no mobile.
- `queue-board.browser.cjs`: saudação/espera/oferta/atendimento, canais,
  legado, estado vazio/indisponível, filtros, detalhes, EN/PT e mobile.
- Capturas desktop/mobile inspecionadas em `.impeccable/review/queue-summary-*`;
  detector mecânico sem achados. Capturas locais ignoradas pelo Git.
- A regressão foi demonstrada pelo inventário e leituras reais; não foi
  executado teste automatizado contra a versão anterior. Navegador usa fixtures
  locais e não comprova sessão autenticada de produção.

Publicação Release no eveo-apps:
`/opt/sufficit-telephony-panel/releases/queue-summary-20261113-9f6b09e`.
Anterior preservada: `legacy-queue-guid-20261112-c5f8c46`. Configuração copiada
no próprio servidor, inventário preservado e troca atômica pelo helper existente.
Somente o serviço `sufficit-telephony-panel` foi reiniciado.

DLL SHA256: `cf2594609113ddd24f21110968500439a27ca8d2aa262c8d9ed994099cee48ec`.
CSS público SHA256: `8865725c8c020d2c0e81a251524dfeef4e89a64b10dc1277f1bf5b100a64f562`.
Hash instalado igual ao pacote, serviço ativo, health por socket e público
HTTP200/running, `/api/fleet` anônimo HTTP401. `sufficit-ami-events` manteve
MainPID409848 antes/depois. Não houve publicação/reinício no Google nem nos
controladores E/A.

Contrato permanente em [operator-board.md](../operator-board.md#contagem-e-cobertura-das-filas).
