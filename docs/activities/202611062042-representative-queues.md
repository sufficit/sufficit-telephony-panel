# Filas na visão consolidada de representantes

A visão consolidada chamava QueueBoardReader.Read(null), que lê somente contextos
fixos e não expande fontes AnyContext. QueueBoardAccess agora passa explicitamente
os contextos autorizados para ReadContexts. Fontes fixas de empresas alheias não
são consultadas. A leitura compartilha orçamento de 5 segundos e limite de quatro
requisições simultâneas; a autorização é revalidada depois da leitura.

Regressão reproduzida antes da correção em RepresentativeQueueChecks usando sessão,
autorização e leitor reais com HTTP simulado. Após a correção: 1.402 checks passaram,
incluindo duas filas homônimas em empresas distintas, seleção individual, negação
antes da consulta privada e remoção do vínculo. Publish Release concluído.

Leitura privada em produção confirmou uma fila do contexto R&L em eveo-voip e outra
em apoint-voip; google-voip retorna 404 (sem fila). Nenhuma credencial foi exposta.
Publicado em eveo-apps, release representative-queues-20261106-c76307e.
Serviço ativo e health público 200. Hash local/instalado idêntico:
2adc48fe2f69f54fb06cc4dbc5e15c1b925bbc94c76a514885448162d3a521a1.
Commit c76307e enviado à main. A API não foi alterada nem republicada.
A conferência visual autenticada fica para o usuário; a regressão foi validada
na suíte e a existência das filas foi confirmada por consulta privada somente leitura.
