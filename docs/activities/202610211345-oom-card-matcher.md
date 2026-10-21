# OutOfMemoryException na observação ao vivo

Achado no journal em 21/10 ao validar as sessões persistentes: três `OutOfMemoryException` em
`operation=live-observation` (12:26, 13:38, 13:40), com pilha em
`CardMatcher..ctor → Regex(NonBacktracking) → MintermGenerator`.

## Causa
`LiveProjection` guarda um `CardMatcher` por **instância** de card (`ConditionalWeakTable`). As instâncias
são recriadas a cada atualização: a visão central do gestor monta ~1.800 cards a cada 2 s e a visão por
contexto rebusca o catálogo a cada 60 s. O atalho literal do `CardMatcher` não cobria padrões ancorados
(`^SIP/0000286013`, formato do catálogo do portal), então cada card compilava uma regex
`NonBacktracking`. A alocação contínua estourou o limite de 256 MiB do GC (`DOTNET_GCHeapHardLimit`).
Ficou visível com várias sessões por contexto abertas ao mesmo tempo (gestor e cliente).

## Correção
`sufficit-ami-events` `763c287`: padrões literais e ancorados usam comparações ordinais com a mesma
semântica da regex (`^x` = começa com; `x` = contém; `x$` = termina com; `^x$` = igual). Padrões reais
são compilados uma vez por texto, num cache limitado a 4.096. Painel v0.15.2 com `CardMatcherChecks`:
equivalência com `Regex.IsMatch` em 10 padrões × 18 candidatos, padrão inválido e limite de alocação para
2.000 cards ancorados. 1025 checks PASS; testes do streaming (16) PASS.
