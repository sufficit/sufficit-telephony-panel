# Carteiras de representantes no painel

O painel consulta Endpoints para resolver a carteira ativa além dos contextos diretos
concedidos pela Identity. Cache de 45 segundos, limitado por token e grants diretos,
sem ampliar permissões de escuta/sussurro. Links para o único contexto direto de um
representante abrem a visão consolidada. A escolha de uma empresa continua disponível.

Cada catálogo é projetado dentro de seu contexto antes da união. Ramais sem eventos
continuam visíveis; IDs incluem a empresa mesmo para ramais sem observação. Status
sem AccountCode só é atribuído quando a extensão tem um único dono nos catálogos.

Validação: 1.396 checks passaram, incluindo expansão direta, deduplicação, isolamento,
cache, revogação, falha de refresh, sessão real e contexto inicial. Build passou.
A API correspondente está no commit 0f45a29 de sufficit-endpoints, com 209 testes do
candidato e fechamento/carregamento de DLLs verificados.

Observação autenticada do representante em produção ainda pendente.
