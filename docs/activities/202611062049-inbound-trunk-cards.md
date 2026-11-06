# DIDs e entradas WhatsApp na seção de troncos

Mantida a mesa aprovada em DESIGN.md/captura do usuário. Os cards existentes
recebem as entradas próprias e da carteira autorizada; cadastrado, desabilitado
e vencido são estados neutros de inventário, sem sinalizar disponibilidade.
Identidades entry: não correspondem a canais AMI de operadoras compartilhadas.

Validação: 1.409 checks; teste de navegador em 1440 e 390 px com três estados,
sem overflow ou erros JavaScript. Capturas em artifacts/inbound-catalogue-*.png
inspecionadas. Primeira tentativa de browser encontrou versão ausente; usado
Playwright já instalado e compatível com Chromium 1237. Nenhum navegador instalado.

Referências: layout existente e DESIGN.md (direção mantida); pesquisa complementar
https://lovable.dev/templates/apps/dashboards e
https://dribbble.com/search/telecom-dashboard, sem transportar layouts analíticos
para a mesa operacional. Não há alteração de tokens nem dependências visuais.
A apresentação foi validada com fixtures locais; fluxo autenticado real pendente.
Publicado em eveo-apps na release inbound-catalogue-20261106-22bec4e,
com troca atômica e rollback disponível para representative-queues-20261106-c76307e.
Serviço ativo e health público https://panel.sufficit.com.br/health HTTP 200.
SHA-256 instalado conferido: a1d06e11b61fa6becca5054f6882904a4104b085c653a56df486b3ecba572c3e.
Implementação commit 22bec4e enviada à main. API e356766 publicada nos três apps.
Plano concluído: investigação, implementação, testes, commit/push e publicação.
A verificação autenticada com dados reais do cliente não foi executada nesta entrega;
a validação visual usa fixtures e não comprova correlação de chamadas por DID/WhatsApp.
