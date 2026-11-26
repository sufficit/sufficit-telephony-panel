# Dashboard de telefonia no Hermes

Exibir a mesa de telefonia em uma aba de conversa, com o Context ID do contato e com a autorização existente do painel.

- [x] Confirmar o contrato da aba, a rota contextual, a política de frames e os acessos de operação.
- [x] Permitir o frame na rota contextual, limitado à origem do Hermes, e validar localmente filtro, cabeçalhos e preservação das permissões.
- [x] Entregar o ajuste do painel e verificar os cabeçalhos em produção.
- [x] Cadastrar a aba na conta que possui Vendas e Ramais e verificar uma conversa com Context ID e a ausência de contexto.

Evidência de aceitação: aba disponível na lista da conversa e iframe de `/board?contextid=<uuid>` exibindo o cliente para uma sessão autorizada. A aba deve ficar sem iframe quando o contato não tiver Context ID.

A conta 1 de produção possui Vendas (4) e Ramais (8), ambos no contexto da conversa. O painel será cadastrado como **Painel de Telefonia**, usando `/board?embed=hermes&contextid={{contact.custom_attributes.contextid}}`. Os quatro aliases HTTPS do mesmo Hermes de produção foram confirmados no virtual host. O painel preserva a autorização de leitura existente e abre a autenticação em outra guia quando necessária.

Validação local: 1.747 checks de Panel.Tests, regressão HTTP de frame/autenticação e teste de navegador do acesso por empresa com fixtures de Development passaram. A publicação em Release será validada antes do deploy. O compilador reportou o alerta preexistente NU1903 para SignalR.Common 10.0.11.

Publicação: commit `333b2d0` na main, release `hermes-dashboard-20261126-333b2d0` ativa no eveo-apps. SHA-256 da DLL `4f5d2ef9f828fb025a5cc2d74d5522617cc6b54a29c9722745d23196752fa95d`. Health interno e externo HTTP 200. O helper preservou o release anterior `device-tiles-20261124-5ec3667` como referência de rollback.

## Cadastro e verificação

- O Dashboard App **Painel de Telefonia** foi criado pela API da conta 1 do Hermes em produção e relido pelo ID 20, UUID `01a2196c-0669-72cc-b060-b0a8f63929fb`. A configuração usa posição `conversation`, preserva cookies do painel e exige `contact.custom_attributes.contextid`.
- Os 25 testes focados do Hermes passaram. Os hashes do resolver e do componente de frame são iguais aos instalados no Hermes em produção. O componente deixa o iframe sem montar quando falta o parâmetro exigido.
- No navegador, a URL cadastrada abriu a empresa autorizada e negou a empresa externa nas fixtures de Development. Na ausência de Context ID, o resolver sinalizou o parâmetro obrigatório.
- O painel de produção abriu dentro de um iframe em página de teste na origem `chatwoot.sufficit.com.br` e ofereceu login em outra guia com o Context ID no retorno. O navegador bloqueou o mesmo iframe em origem externa. O health retornou 200 e a API restrita retornou 401 sem autenticação.
- A etapa de navegador em produção usou uma sessão anônima e um identificador sintético. A visualização de dados de uma conversa real com sessão autenticada ficou sem verificação nesta execução.

Para reverter o cadastro, remover somente o app de ID 20 na conta 1. Para reverter a publicação do painel, usar o release anterior registrado pelo helper em `/var/backups/sufficit-telephony-panel/hermes-dashboard-20261126-333b2d0/release.json`.
