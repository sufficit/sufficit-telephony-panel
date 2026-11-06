# Filas na carteira de representantes

Objetivo: carregar filas de todas as empresas autorizadas na visão consolidada,
incluindo fontes AnyContext, sem consultar empresas alheias à carteira.

1. Concluído — reproduzir a ausência com teste de sessão e QueueBoardAccess.
2. Concluído — corrigir seleção de fontes com orçamento e concorrência limitados.
3. Em andamento — validar suíte, registrar atividade, commit/push e publicar o painel.

Aceite: filas próprias e dos representados aparecem; seleção individual continua;
revogação e empresa alheia não geram exposição. A API não precisa mudar.
Validação: dotnet run --project tests/Panel.Tests.csproj; publish Release; hash e health.

Regressão reproduzida: teste de QueueBoardAccess.Read(null) com sessão de representante falhou por não carregar os dois contextos autorizados.

Correção: leitura explícita do conjunto autorizado, com o mesmo orçamento global de 5 s e concorrência de 4 requisições. 1.402 checks passaram após a correção.
