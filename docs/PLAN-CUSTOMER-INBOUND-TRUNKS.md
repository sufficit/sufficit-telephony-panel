# Entradas dos clientes como troncos

Objetivo: mostrar DIDs e gateways de entrada pertencentes às empresas autorizadas
na seção existente de troncos, inclusive carteiras de representantes.

1. Concluído — identificar cadastro autoritativo de DIDs/gateways e evidência de chamadas.
2. Concluído — implementar catálogo isolado e apresentação adequada, com testes.
3. Em andamento — validar, registrar atividade, commit/push e publicação.

Referência visual: captura do usuário e DESIGN.md, mantendo a seção e os cards existentes.
Não inferir disponibilidade a partir de cadastro ou expor atividade de gateways compartilhados.
Aceite: entradas próprias visíveis mesmo sem chamadas; nenhuma entrada/atividade alheia.

Decisões: catálogo DID por ContextId e WhatsAppGatewayRoute por ContextId/Deleted=false.
A seção existente é a referência visual, sem novas cores/layout. Estados de cadastro
são neutros; nenhuma atividade de gateway compartilhado é correlacionada por nome.
Pesquisa complementar: https://lovable.dev/templates/apps/dashboards (catálogo de
painéis de métricas, sem adoção de gráficos) e https://dribbble.com/search/telecom-dashboard
(busca telecom dashboard; exemplos analíticos não substituem a mesa operacional).
WhatsApp SessionId legado pode ser credencial: identidade opaca SHA-256, Token nunca retornado.

Validação: 1.409 checks de painel; 198 testes contra candidato da API; closure sem faltas, 2.800 tipos carregados. Browser 1440/390 px validou estados e ausência de overflow/erros JS. Capturas artifacts/inbound-catalogue-*.png inspecionadas.
