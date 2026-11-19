# Encerrar canais e reunir troncos por fornecedor

1. [concluído] Identificar contrato de encerramento, autorização e transporte AMI existentes; definir agrupamento seguro dos troncos e preservar supervisão por servidor.
2. [concluído] Implementar encerramento por canal em qualquer tipo de cartão com confirmação/auditoria/revalidação e agrupar troncos por nome/fornecedor após autorização e filtro de servidor.
3. [concluído] Validar segurança/regressão, Release, interface desktop/mobile e gates dos repositórios afetados; sem derrubar chamadas reais como teste.
4. [em andamento] Commit/push main dos arquivos próprios; publicar componentes necessários dentro das autorizações, conferir saúde/versões e converter plano em atividade.

Restrições: preservar espera somente nas filas, GUID/gestão e fluxos de ramal; não alterar/reiniciar Google/controladores. Agrupamento visual não concede acesso nem muda o nó real do canal. Seguir DESIGN.md/SUI e padrões de tabela/confirmar já existentes; referência dominante é o painel atual. Não encerrar automaticamente canal ao abrir detalhes, atualizar ou confirmar outra ação. Sem chamadas de teste em produção. Pedidos anteriores autorizam publicação do painel e deploy normal dos3 apps quando necessário; conferir regras locais antes de publicar backend.

Contrato: gerente somente para encerramento, sem conceder operação destrutiva a telephony.monitor. POST central telephony/panel/channels/hangup com Node/Channel/UniqueId; nó precisa existir no resolver, UNIQUEID é relido no AMI antes de Hangup, causa16 e auditoria pelo usuário autenticado. Troncos agrupados por título normalizado, sem unir entradas DID/WhatsApp de catálogo. Dados já autorizados e filtro de servidor precedem agrupamento; canais mantêm nó/empresa/ID originais.

Implementação concluída: menu em cartões com canais, ação na tabela comum e confirmação explícita; proteção de gerente em painel/API, stream e ID revalidados, cancelamento limpa intenção e sucesso só acusa aceitação, aguardando evento real. Grupo de tronco mantém todas as pernas por nó; escuta contínua escolhe nó+prefixo corretos. Entradas de catálogo não são fundidas. 1.649 checks do painel passaram. Navegador novo passou nos três tipos de cartão, cancelamento/confirmação demo, três nós no fornecedor, filtro por nó, clientes sem ação, PT/mobile/dark e ausência de overflow. Supervisão e filas existentes passaram; detector sem achados; capturas desktop/mobile inspecionadas.

API candidata compilada contra bibliotecas instaladas:287 regressões/security/capacidades +30 guards passaram;3.132 tipos carregaram e leitura comercial real preservou26 itens/6 ProductId. Somente DLL da API muda. Validação do painel concluída, publicação sequencial API→painel em andamento.
