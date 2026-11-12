# GUID de fila antiga por consulta ao cadastro

1. [concluído] Criar resolução autorizada na API e comprovar identidade real do número informado.
2. [concluído] Consultar ao abrir detalhes sem identidade; mostrar GUID e gestão com estados explícitos, sem consulta a cada refresh. Testar autorização, troca de cartão, desktop/mobile.
3. [concluído] Publicar API/painel nos destinos autorizados, verificar pacotes/dados reais e converter em atividade com commit/push.

Sem alterações em Google/controladores. Reusar estrutura SUI e referências da tarefa anterior, sem novo redesign. Consulta central com token delegado, autorização antes/depois; vínculo exato e único. Se não existir cadastro nativo, exibir GUID legado e gestão da empresa sem prometer uma edição inexistente. Fixtures não comprovam resultado real; registrar leitura operacional.

Consulta real pela DLL candidata confirmou 0000006500 →
ab845088-6170-409a-b7b4-ae9acff33f3f, empresa
d21cfb04-9d37-473b-837c-67591a26feed, Native=true.
A tabela antiga está vazia; resolução usa vínculos preservados da migração e
confere existência/proprietário no catálogo nativo a cada lookup.
1.462 checks do painel e navegador desktop/mobile claro/escuro passaram, incluindo
resolução, ausência, ambiguidade e erro/retry. Consulta ao abrir, sem repetição por
refresh; troca/fechamento cancela solicitação, e token fica no servidor.

## Resultado publicado e evidências

Código do painel: c5f8c46. Release ativa em eveo-apps:
`legacy-queue-guid-20261112-c5f8c46`. DLL local/instalada SHA-256
`9454a7980ff00900b1c72408ba47581df640fead2c0c1ff26fe54811e26deebf`.
CSS público/instalado igual ao pacote:
`8865725c8c020d2c0e81a251524dfeef4e89a64b10dc1277f1bf5b100a64f562`.
Health interno/público OK e fleet anônimo401. Backup de referência anterior pelo
helper release-panel.py, sem sobrescrever release em execução. Coletor AMI
permaneceu no PID410178; nenhum serviço/controlador Google foi alterado/reiniciado.

API 1.26.1112.1816, fonte127caf8, publicada nos três apps pelo deploy.py padrão.
A DLL mudou para d99a213fc31206c5af28b1eedffa104533088fad4d5e839efc106ba94f49380a;
as demais dependências foram preservadas e as173 DLLs conferidas nos três nós.
Health Healthy e lookup anônimo401 em todos; mapa privado legível pelo serviço.
233 regressões CustomerSearch/InternalPermission/BillingCheckout/Voice/identidade,
sete testes específicos do lookup e30 guardas de deploy passaram. O catálogo
comercial real preservou25 itens e seis vínculos ProductId. Evidência da API em
sufficit-endpoints/docs/activities/202611121532-panel-legacy-queue-identity.md.

Índice privado com1.163 vínculos extraídos do registro preservado da migração;
todos tiveram o GUID físico conferido (zero divergências), e cada lookup valida
GUID+empresa contra telp_queue_queues atual. Não houve reativação de tabela antiga,
alteração de números/rotas nem concessão de SQL à credencial do monitoramento.

A conferência real mostrou suff_queue=0 e confirmou Native=true para0000006500:
GUID da fila **ab845088-6170-409a-b7b4-ae9acff33f3f**, empresa
**d21cfb04-9d37-473b-837c-67591a26feed**. Destino da gestão:
https://blazor.sufficit.com.br/pages/telephony/queue/edit?contextid=d21cfb049d37473b837c67591a26feed&queueid=ab8450886170409ab7b4ae9acff33f3f.
Blazor continua validando os direitos de gestão; o link não concede edição.

## Interface, regressão e limites

Lookup ao abrir somente se não houver identidade explícita. Guarda por recurso e
contexto evita repetir durante refresh; troca/fechamento cancela pedido. Resposta
é conferida por número, GUID, empresa e formato e a autorização é revalidada.
Ausência e ambiguidade são estados próprios; falha tem tentativa manual. Contexto
não comprovado nunca é inferido pelo título do cartão. As mesmas cores/fontes,
SUI e layout existentes foram preservados; não houve novo redesign. A referência
visual continua sendo a captura do usuário e DESIGN.md, conforme o ledger da
atividade anterior de detalhes/gestão.

`dotnet run --project tests/Panel.Tests.csproj --no-restore`:1.462 checks passaram.
`tests/legacy-queue-identity.browser.cjs`: fluxo com cartão numérico resolvido,
GUID/link exato, ausência, ambiguidade, legado sem nativo, erro/retry; desktop claro
e mobile escuro. Capturas inspecionadas em
`.impeccable/review/legacy-queue-guid-desktop.png` e
`.impeccable/review/legacy-queue-guid-mobile.png`, ignoradas no Git. Identificadores
das fixtures são sintéticos, não a evidência da consulta real. Detector mecânico
sem achados. Warning preexistente SignalR.Common10.0.11 fora deste escopo.
O primeiro teste de navegador revelou que o cenário padrão tinha chave não
numérica; foi criada fixture numérica explícita, restrita à demonstração local.

Não foi usada sessão autenticada do usuário para conferir a tela de produção.
As provas são fluxo visual sintético, consulta SQL real pela DLL candidata e
conferência do mesmo hash instalado, processo, arquivos públicos e proteção401.
Nenhuma chamada, fila, cliente ou permissão foi alterada como teste.

Entrega: fonte, atividade e push na main; a cobertura global dos controladores
continua sendo a pendência separada já documentada nesta sessão, sem publicação
no Google a partir desta tarefa. Recarregar o painel e reabrir o cartão usa o
componente novo e dispara a resolução autenticada.
