# Integração das pendências na main

Autorizada pelo usuário em 09/10/2026. A branch de recuperação preservava redimensionamento dos cards e acesso contextual de clientes. Integrada sobre a main atual, sem alterar configuração de grants em produção. Os parâmetros genéricos dos selects e campos de texto foram atualizados de T para TValue, conforme a API atual da SUI.

Validação: build do painel e 822 verificações de segurança, concorrência, revogação, projeção e ações aprovadas. Nenhuma ação de telefonia foi executada. A validação inicial falhou na compilação com os nomes antigos de parâmetros; a nova execução passou após o ajuste.
