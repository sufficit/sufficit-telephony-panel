# Painel de representantes

Objetivo: incluir o próprio cliente e seus representados atuais no painel telefônico, mantendo isolamento e permissões de escuta independentes.

1. Concluído — identificar fonte autoritativa da carteira, autorização e caminhos de projeção/publicação.
2. Concluído — implementar resolução de contextos e agregação, com testes de carteira, isolamento e revogação.
3. Concluído — executar validações pertinentes e registrar resultados e limitações.
4. Em andamento — commit e push na main dos repositórios alterados; verificar fluxo de publicação conforme autorização da conversa.

Aceite: representante vê seus recursos e os de seus representados; cliente comum não ganha acesso; vínculos encerrados e empresas alheias não entram; escuta não é concedida pela representação.

Validação prevista: testes de autorização da API e testes do painel, build dos projetos afetados. Comandos exatos serão registrados após identificar os projetos de teste.

Fonte confirmada: SalesRuntime.GetActiveCommissionedCustomers, já usada pelo FlashOperatorPanelController. Expansão direta (sem percorrer subcarteiras), aplicada apenas à leitura.

Implementação validada por 1.396 checks do painel. Build da API passou; execução do projeto completo de testes encontrou referências transitivas ausentes (Campaigns, EFData, gateways). Próxima validação: publicar candidato e executar contra suas DLLs reais usando CandidateAssemblyDirectory, suportado pelo projeto.

Candidato da API: 209 testes passaram contra DLLs reais; PublicationClosure sem faltas e 2.790 tipos carregados. Painel: 1.396 checks passaram. API commit 0f45a29; entrega em andamento.
