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
Publicação do painel e conferência final pendentes neste checkpoint.
