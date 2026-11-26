# Dashboard de telefonia no Hermes

Exibir a mesa de telefonia em uma aba de conversa, com o Context ID do contato e com a autorização existente do painel.

- [x] Confirmar o contrato da aba, a rota contextual, a política de frames e os acessos de operação.
- [x] Permitir o frame na rota contextual, limitado à origem do Hermes, e validar localmente filtro, cabeçalhos e preservação das permissões.
- [~] Entregar o ajuste do painel e verificar os cabeçalhos em produção.
- [ ] Cadastrar a aba na conta que possui Vendas e Ramais e verificar uma conversa com Context ID e a ausência de contexto.

Evidência de aceitação: aba disponível na lista da conversa e iframe de `/board?contextid=<uuid>` exibindo o cliente para uma sessão autorizada. A aba deve ficar sem iframe quando o contato não tiver Context ID.

A conta 1 de produção possui Vendas (4) e Ramais (8), ambos no contexto da conversa. O painel será cadastrado como **Painel de Telefonia**, usando `/board?embed=hermes&contextid={{contact.custom_attributes.contextid}}`. Os quatro aliases HTTPS do mesmo Hermes de produção foram confirmados no virtual host. O painel preserva a autorização de leitura existente e abre a autenticação em outra guia quando necessária.

Validação local: 1.747 checks de Panel.Tests, regressão HTTP de frame/autenticação e teste de navegador do acesso por empresa com fixtures de Development passaram. A publicação em Release será validada antes do deploy. O compilador reportou o alerta preexistente NU1903 para SignalR.Common 10.0.11.
