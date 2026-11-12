# Contagem de filas na mesa global

Objetivo: explicar e corrigir, se confirmado, o contador de chamadas em espera que permanece zerado na mesa global, sem confundir chamadas em troncos com espera em fila.

## Checkpoints
1. [concluído] Rastrear a origem dos cartões/contadores e reproduzir a falha com evidência.
2. [concluído] Corrigir a causa e executar regressão e validações pertinentes.
3. [concluído] Registrar evidências em atividade, revisar alterações e preparar entrega na main.

## Aceite e restrições
- Verificar a correlação entre estado de fila, eventos e escopo global; números de troncos não provam espera.
- Preservar alterações paralelas e autorizações de clientes.
- Validação prevista: testes de regressão no harness existente e build .NET; consultar evidência operacional disponível sem expor credenciais.
- Commit/push não comprovam publicação nem comportamento de produção.

## Diagnóstico e decisão
- Fluxo privado gRPC, amostra de 45s em 12/11: eventos NewChannel/Hangup nos três nós, nenhum evento de fila; snapshot sem filas/esperas.
- Google `queue show`: apenas default sem chamadas; filas comerciais migradas para ARI.
- Fontes AnyContext nos três nós, mas Read(null) seleciona só ContextIds fixos (laboratório/uma empresa), sem descoberta global.
- BoardTotals.Waiting usa quantidade de visitas incluindo atendidas e ignora pending autoritativo quando há visitas (podem ser truncadas).
- Implementação coordenada: snapshot agregado privado de contextos no controlador (somente leitura, mesma credencial de monitoramento); leitor global consulta uma vez por nó com limites/cache existentes. Carteiras e seleção individual não consultam o agregado global. Um diretório com centenas de consultas por empresa foi descartado após medir 464 contextos em produção.
- Não publicar controlador nesta tarefa: atualização exige drenagem de chamadas e pacote completo conforme AGENTS.md do ARI; entregar fontes e testes, registrar implantação pendente separadamente.

## Mudança e evidências

A consulta privada ao controlador Google encontrou 464 contextos, 1.160 filas,
2 visitas aguardando e 10 atendidas, com todas as respostas conectadas. Essa amostra
foi obtida durante o diagnóstico, não no instante exato da imagem. O snapshot gRPC
AMI não continha filas/esperas; a amostra de 45s recebeu canais nos três nós e
nenhum QueueCaller*. Não houve leitura de números dos interlocutores nos registros
nem exposição de credenciais. Nenhuma alteração operacional em produção.

Fontes AnyContext passam a consultar `/api/queue/nodes/{node:N}/snapshot` (uma
requisição por fonte, cache compartilhado 2s). Fontes fixas preservam caminho
anterior; carteiras e contextos individuais continuam restritos. Agregado inválido
ou indisponível mantém catálogo como indisponível, nunca atividade atual; 401/403
limpam os dados em cache. Falha inicial usa uma amostra sem contexto, visível só a
quem tem direito global, para não ocultar o alerta de fonte. O inventário de
recursos lembrados não foi apagado nem convertido em estado atual.

Espera usa pending atual do controlador mesmo com visitas truncadas. Sem pending,
conta somente visitas não atendidas, excluindo answered/connected.
Contrato permanente: [mesa de operação](../operator-board.md).

## Validações executadas

- Regressão do contador falhou antes da correção: "Answered queue calls are not waiting".
- Regressão global com leitor/acesso originais falhou antes: "Global overview
  discovers active companies beyond fixed labs without AMI queue events".
- `dotnet run --project tests/Panel.Tests.csproj --no-restore`: 1.428 checks
  passaram, incluindo atualização dos totais, cache concorrente, ausência de
  consultas por empresa no agregado, escopo/nó inválido, duplicados, limite,
  fonte antiga 404, descarte 403, recuperação e revogação para carteira restrita.
- Build compilado pela suíte e publish Release do painel concluído.
- Pacote local: `/tmp/sufficit-global-queue-release/panel`.
- DLL SHA-256: `c56f2a7a13668656a6a22387676a7517ed5fa5a5ab17689e3827fc54fa8504ec`.
- Repositório ARI: oito checks HTTP, três testes Python da fachada e builds/publish
  net7/net10 concluídos; commit `8a85048`, atividade
  `202611121213-global-queue-monitor.md` naquele repo.
- Aviso preexistente na suíte do painel: advisory SignalR.Common 10.0.11.
- Não houve teste visual autenticado ou aceitação do contador em produção; nenhuma
  dessas verificações é presumida pelos testes ou pelo push.

## Entrega e publicação

Fonte e atividade na main no commit que contém este registro; commit/push não são
publicação. Pacotes preparados, sem instalação. É necessário atualizar os três
controladores ARI com drenagem/parada e pacote completo, atualizar a fachada Google
e só então o painel. Preservar estado/configuração e fazer backup/rollback conforme
regras de cada serviço. Antes dessa operação, obter a decisão do mantenedor sobre
a drenagem de chamadas; não interromper o tráfego a partir deste diagnóstico.
