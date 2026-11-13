# Contagem de filas e cobertura da espera global

Objetivo: corrigir o cabeçalho que apresenta 2.750 cartões como filas e zero parcial como espera global.

1. [concluído] Confirmar contagens e cobertura das fontes com consultas somente leitura; registrar evidência.
2. [concluído] Separar filas distintas com observação atual de cartões por servidor/históricos, sinalizar espera global indisponível e priorizar cartões atuais; validar regressão e navegador.
3. [em andamento] Publicar somente painel, verificar pacote/health, converter em atividade e commit/push na main.

Restrições: manter histórico e isolamento dos clientes; não alterar/reiniciar Google/controladores. Reusar SUI/DESIGN.md e composição existentes. Google permanece sob a restrição de publicação desta sessão. Não inferir fila vazia de ausência de cobertura.

Evidência inicial em13/11: inventário central2.750 cartões QUEUE (Google416, Apoint1.167, Eveo1.167),417 com chave antiga e2.333 com chave nativa. Agregados atuais E/A:463 contextos e1.159 filas em cada nó, espera0, fontes conectadas e observação recente. Google fachada global404. Nenhum dado do inventário histórico comprova espera atual.

Consulta direta somente leitura ao Google: 466 contextos consultados, 464 HTTP200 e 2 HTTP404; 1.161 filas, 4 chamadas em espera e 8 em atendimento naquele instante. A fachada global continua404.

Validação: build sem erros; 1.469 checks; queue-summary.browser.cjs e queue-board.browser.cjs passaram; capturas desktop/mobile inspecionadas; detector sem achados. Sem sessão autenticada de produção.
