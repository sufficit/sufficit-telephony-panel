# Entrega do painel à R & L Conect

Registro consolidado da entrega e comunicação de 06/11/2026. Os contratos técnicos
vigentes estão em [Customer access](customer-access.md); os registros abaixo descrevem
as versões verificadas nesta entrega, não uma consulta contínua do estado de produção.

## Acesso e uso

- Painel: https://panel.sufficit.com.br
- E-mail de acesso confirmado no histórico de suporte: `comercial@rlconect.com.br`.
- Empresa: R & L Conect, contexto `10a1c455-d2e4-4b13-9ab9-399643b2f606`.
- A visão consolidada inclui a própria empresa e seus clientes representados ativos.
  É possível selecionar uma empresa individualmente.
- Ramais, chamadas e filas respeitam o conjunto de empresas autorizado. Recursos
  sem chamadas permanecem no catálogo; nomes iguais não unem empresas diferentes.
- Em Troncos, DIDs e entradas WhatsApp das empresas aparecem com número/descrição
  e estado de cadastro. DID vencido e rota desabilitada são identificados.

A carteira não concede escuta, sussurro ou acesso administrativo. A expansão usa
somente vínculos diretos ativos, sem percorrer a carteira de outro representante.
Os cards de entrada não atestam registro, disponibilidade ou chamadas em tempo real.
Não se atribui atividade de uma operadora compartilhada a um cliente pelo nome do
tronco. Identificadores de sessão WhatsApp são opacos; tokens não são publicados.

## Correções e evidências

| Entrega | Painel | API | Validação registrada |
| --- | --- | --- | --- |
| Carteira do representante | `8a97463` | `0f45a29` | 1.396 checks; 209 testes do candidato da API |
| Filas na visão consolidada | `c76307e` | Sem mudança | 1.402 checks; filas consultadas em eveo-voip e apoint-voip |
| DIDs e WhatsApp em Troncos | `22bec4e` | `e356766` | 1.409 checks; 206 testes da API; navegador 1440/390 px |

As contagens são checkpoints sucessivos, não devem ser somadas como testes distintos.
A falha das filas estava na leitura agregada sem expansão das fontes AnyContext.
A correção enumera explicitamente as empresas autorizadas, com orçamento compartilhado
de cinco segundos e até quatro consultas simultâneas. O google-voip retornou 404
na consulta do contexto R & L; isso não significa falha nas outras fontes.

A última publicação desta entrega ativou no eveo-apps a release
`inbound-catalogue-20261106-22bec4e`, com rollback disponível para
`representative-queues-20261106-c76307e`. A API foi publicada via deploy.py nos
três hosts eveo-apps, apoint-apps e castrum-apps, preservando configurações e
alterações paralelas de FOP. Serviços ativos, health HTTP 200 e hashes instalados
foram conferidos. A rota de catálogo anônima retornou 401 no primário.

- SHA-256 do painel: `a1d06e11b61fa6becca5054f6882904a4104b085c653a56df486b3ecba572c3e`.
- SHA-256 da API: `c80d13825c9b1fb5295f80de76aa944f55a8eb78fb9ef7f20ec4af138e2be9cc`.

O navegador foi validado com fixtures locais, sem overflow ou erros JavaScript.
Não foi executado nesta entrega um login de produção com as credenciais do cliente.
O envio de instruções ao grupo não comprova login bem-sucedido nem aceite funcional.

Evidências detalhadas:

- [Carteiras](activities/202611062020-representative-portfolios.md).
- [Filas](activities/202611062042-representative-queues.md).
- [Entradas em Troncos](activities/202611062049-inbound-trunk-cards.md).
- [Comunicação ao grupo](activities/202611062136-rl-panel-communication.md).
- [Registro da API](https://github.com/sufficit/sufficit-endpoints/blob/main/docs/activities/202611062049-panel-inbound-catalogue.md).
