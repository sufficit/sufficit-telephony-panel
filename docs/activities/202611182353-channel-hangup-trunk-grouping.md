# Encerrar canais e reunir troncos por fornecedor

1. [concluído] Identificar contrato de encerramento, autorização e transporte AMI existentes; definir agrupamento seguro dos troncos e preservar supervisão por servidor.
2. [concluído] Implementar encerramento por canal em qualquer tipo de cartão com confirmação/auditoria/revalidação e agrupar troncos por nome/fornecedor após autorização e filtro de servidor.
3. [concluído] Validar segurança/regressão, Release, interface desktop/mobile e gates dos repositórios afetados; sem derrubar chamadas reais como teste.
4. [concluído] Commit/push main dos arquivos próprios; publicar componentes necessários dentro das autorizações, conferir saúde/versões e converter plano em atividade.

Restrições: preservar espera somente nas filas, GUID/gestão e fluxos de ramal; não alterar/reiniciar Google/controladores. Agrupamento visual não concede acesso nem muda o nó real do canal. Seguir DESIGN.md/SUI e padrões de tabela/confirmar já existentes; referência dominante é o painel atual. Não encerrar automaticamente canal ao abrir detalhes, atualizar ou confirmar outra ação. Sem chamadas de teste em produção. Pedidos anteriores autorizam publicação do painel e deploy normal dos3 apps quando necessário; conferir regras locais antes de publicar backend.

Contrato: gerente somente para encerramento, sem conceder operação destrutiva a telephony.monitor. POST central telephony/panel/channels/hangup com Node/Channel/UniqueId; nó precisa existir no resolver, UNIQUEID é relido no AMI antes de Hangup, causa16 e auditoria pelo usuário autenticado. Troncos agrupados por título normalizado, sem unir entradas DID/WhatsApp de catálogo. Dados já autorizados e filtro de servidor precedem agrupamento; canais mantêm nó/empresa/ID originais.

Implementação concluída: menu em cartões com canais, ação na tabela comum e confirmação explícita; proteção de gerente em painel/API, stream e ID revalidados, cancelamento limpa intenção e sucesso só acusa aceitação, aguardando evento real. Grupo de tronco mantém todas as pernas por nó; escuta contínua escolhe nó+prefixo corretos. Entradas de catálogo não são fundidas. 1.649 checks do painel passaram. Navegador novo passou nos três tipos de cartão, cancelamento/confirmação demo, três nós no fornecedor, filtro por nó, clientes sem ação, PT/mobile/dark e ausência de overflow. Supervisão e filas existentes passaram; detector sem achados; capturas desktop/mobile inspecionadas.

API candidata compilada contra bibliotecas instaladas:287 regressões/security/capacidades +30 guards passaram;3.132 tipos carregaram e leitura comercial real preservou26 itens/6 ProductId. Somente DLL da API muda. Validação do painel concluída, publicação sequencial API→painel em andamento.

## Entrega confirmada e contrato

Fonte do painel ad36bef e API4c5e6e3, main publicada nos dois repositórios. Troncos
com mesmo nome/fornecedor ficam num cartão após autorização e filtro de servidor.
Nomes normalizam caixa/espaços, sem inferência por padrão de canal; entradas
DID/WhatsApp de catálogo não são unidas. Identidade de seleção estável e
inventário original preservado. Canais com mesmo nome em nós distintos continuam
independentes em totais, detalhes e ações.

Gerente acessa “Encerrar um canal…” no menu e “Encerrar canal” na tabela comum
dos detalhes de ramais, troncos e filas com canais observados. Há confirmação
explícita por canal/servidor, cancelamento e limpeza ao fechar/navegar. Local
pode ser encerrado mesmo quando não suporta escuta. Cliente sem gerente não
recebe botão e é rejeitado pelo servidor. Encerramento não exige ramal preferido.
Stream e identidade são revalidados no painel; API confere nó conhecido e
UNIQUEID ao vivo antes do comando. Retorno é aceitação, nunca remoção antecipada
do canal; falha não é repetida automaticamente.

A escuta contínua mantém alvos distintos nó/prefixo dentro do fornecedor reunido;
não usa um nó arbitrário do cartão. Chamadas atendidas continuam fora da fila e
permanecem nos ramais. GUID/gestão das filas e escopos de cliente preservados.

1.649 checks do painel passaram em Release. Novo teste trunk-hangup.browser.cjs
conferiu três servidores no mesmo fornecedor, canais distintos, confirmação,
cancelamento e sucesso somente demo, todos os tipos de cartão, filtro por nó,
prefixos por servidor, aba Canais, cliente sem ação, PT/mobile/dark sem overflow.
board-supervision e queue-summary passaram. Desktop/mobile inspecionados em
.impeccable/review/trunk-hangup-*.png, ignorados no Git; detector sem achados.
O teste legado de menu foi atualizado para o item adicional, mantendo as cinco
ações anteriores e a verificação dedicada da nova ação. Dois primeiros checks
do navegador precisaram aguardar a resposta Blazor antes de contar os elementos;
não foram removidas as asserções de confirmação ou de escolha por nó.

Ledger visual: DESIGN.md e componentes/fluxos atuais são o alvo; referências
desta sessão e captura do usuário orientam agrupamento; tabela e confirmação
existentes orientam ações. SUIButton/SUITone.Danger mantêm tokens e responsividade.
Nenhum redesign, dependência visual ou timer de renderização novo.

API publicada nos três apps com candidatos específicos para preservar as
bibliotecas de cada host,287 checks por candidato,30 guards e catálogo real
26 itens/6 links ProductId.176 DLLs conferidas por host sem divergências; saúde
Healthy e POST anônimo401 em todos. Detalhes na atividade do repositório API.

Painel instalado no eveo-apps:
`/opt/sufficit-telephony-panel/releases/trunk-hangup-20261118-ad36bef`.
Anterior preservada `dial-details-20261117-e98998d`; configuração copiada no
servidor e troca atômica pelo helper existente. SHA256 da DLL instalado igual
ao pacote: d478a387a33cb11847aa8f044fcffc504870ceca71cf4164107d22d37148c1b3.
Serviço ativo; health interno/socket e público running. Coletor sufficit-ami-events
manteve MainPID409848 antes/depois. Sem alteração/reinício Google/controladores.

Limites: interface validada com fixtures locais, não com sessão autenticada do
usuário em produção. Nenhuma chamada real encerrada nos testes; nenhum teste
originou ligação. Prova de publicação é hash instalado, saúde e contrato da API;
validação de transporte usa mocks. Warnings SignalR.Common10.0.11 preexistentes.
Todos os checkpoints autorizados concluídos. Contrato permanente em
[operator-board.md](../operator-board.md#troncos-por-fornecedor-e-encerramento-de-canal).
