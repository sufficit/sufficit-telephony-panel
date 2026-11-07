# Comunicação do novo painel ao grupo R & L

Em 06/11/2026, o usuário autorizou expressamente publicar o link, e-mail e instruções
na conversa indicada. Destino confirmado por consulta autenticada:
**SUFF CLI - R&L CONECT**, canal Quepasa, conversa `54391`, inbox `6`, conta
`01a06928-abf7-783d-8fff-66f8383cc536` (ID numérico `1`).

[Conversa no Hermes](https://chatwoot.sufficit.com.br/app/conversations?account=01a06928abf7783d8fff66f8383cc536&conversation=54391&assignee_type=all&status=open&sort_by=last_activity_at_desc).

Inicialmente, sem credencial, a leitura retornou 401. Após o usuário apontar seu
Vault, a ferramenta Identity ficou disponível e a entrada pessoal
`personal/hermes/production/api-token` foi resolvida para executar a requisição.
Nenhum valor de token ou senha faz parte desta documentação. A autenticação foi
confirmada por GET /api/v1/profile; o grupo e o histórico foram lidos antes do envio.
Contrato da API consultado no Swagger da instância, versão `1.26.1105.1111`.

POST público em `/api/v1/accounts/{account_id}/conversations/54391/messages`,
com `message_type=outgoing` e `private=false`, criou a mensagem **9662286**.
Um GET posterior encontrou o mesmo ID, conteúdo idêntico, `private=false` e
`status=sent`. Isso confirma o registro do envio; não comprova leitura dos membros.
Não houve mudança de responsável, status ou etiquetas da conversa.

## Texto enviado

> Pessoal da R & L, o novo painel de telefonia está disponível!
>
> Link: https://panel.sufficit.com.br
> E-mail de acesso: comercial@rlconect.com.br
>
> Acessem o link e façam login com esse e-mail. O painel reúne os dados da R & L e dos clientes representados, permitindo acompanhar ramais, chamadas e filas em uma mesma visão.
>
> Na seção Troncos, também aparecem os números de entrada (DIDs) e as entradas WhatsApp cadastradas para essas empresas. Esses cards indicam cadastro, desabilitação ou vencimento; por enquanto, não mostram disponibilidade nem chamadas dessas entradas em tempo real.
>
> Acessem e nos avisem por aqui caso encontrem alguma dificuldade ou sintam falta de algum cliente, ramal ou fila.

## Documentação consolidada

O pedido posterior “documente tudo” resultou no [registro da entrega](../customer-rollout-rl.md)
e nas entradas do índice de documentação. A validação desta alteração documental
consiste na leitura dos arquivos, conferência de referências locais e git diff --check;
não houve alteração de código, novo deploy ou reenvio da mensagem.
