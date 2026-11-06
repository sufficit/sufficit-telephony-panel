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

Publicado em eveo-apps: release representatives-20261106-8a97463; serviço ativo,
health público HTTP 200 e hash instalado conferido:
ac22ebe7ee098b225d2975aff8349f5b1fadf110510ea413f148ce2a4a7a6f77.
API publicada antes do painel nos três apps, com a rota anônima retornando 401.
Commit de implementação 8a97463 enviado à main. Plano encerrado após publicação.
A conferência visual autenticada com o representante permanece a cargo do usuário;
os testes locais verificaram a sessão e a projeção, sem usar suas credenciais.
