# Cards em branco no contexto de cliente

Relato (21/10): login de cliente (`teste@`, contexto `026246a851a7485991e0b06e1bebb045`) mostrava
cards sem título na mesa.

Causa: o catálogo de cards do endpoints (`EventsPanelRuntime.GetCards`) usa a **descrição do ramal**
(`suff_extensao.descricao`) como título. Dos 99 ramais do contexto, 5 têm descrição vazia:
0000286650, 0000286651, 0000286705, 0000286706, 0000286934.

Correção (commit `05629fd`, v0.14.1): título vazio cai para o identificador do endpoint
(`SIP/0000286651`), no ponto de entrada dos cards (`OperationsApi.Cards`). Publicado como
`card-title-20261021-05629fd` (anterior `support-chat-20261021-45c6b42` preservado), health 200.
995 checks PASS.

Sugestão de cadastro: preencher a descrição desses 5 ramais no portal.

Observação: todos os cards do cliente mostravam "sem informação" porque a observação é parcial.
O estado só aparece depois que chega um evento de registro, qualify ou chamada do ramal. O painel
foi reiniciado várias vezes hoje, e o contexto de cliente ainda não tinha inventário lembrado.
**Correção desta conclusão (mesmo dia):** era filtragem. `AuthorizedObservation.ForCustomer`
descartava toda linha sem AccountCode, e os eventos de registro de ramal não têm AccountCode.
O gestor via os ramais verdes no mesmo contexto e o cliente não. Corrigido na v0.14.4: o status do
ramal é atribuído ao contexto selecionado só por igualdade exata com um ramal catalogado da própria
empresa. Ver `docs/customer-access.md`.
