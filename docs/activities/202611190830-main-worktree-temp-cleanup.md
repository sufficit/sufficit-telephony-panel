# Consolidar main, limpar worktrees e publicar

Escopo contextual: sufficit-telephony-panel e sufficit-endpoints, incluindo branches/worktrees remanescentes desses repositórios. Temporários somente desta sessão/entrega, identificados por nome/proveniência. Não apagar trabalho paralelo ou diretórios desconhecidos; preservar commits antes de retirar worktrees.

1. [concluído] Atualizar referências e revisar branches/worktrees: diferenças, equivalência/ancestralidade, alterações não commitadas e processos ativos.
2. [concluído] Integrar mudanças pendentes compatíveis na main, validar e push; preservar ou registrar bloqueios concretos sem misturar tarefas de outras sessões.
3. [concluído] Conferir/publicar main consolidada no painel e API nos apps autorizados, preservando dependências próprias dos hosts e verificando hashes/saúde.
4. [concluído] Remover worktrees seguramente preservadas e temporários próprios; verificar árvore limpa, converter plano em atividade e commit/push final.

Restrições: não alterar/reiniciar Google/controladores; não derrubar chamadas reais. Deploy API pelo fluxo padrão, helpers completos e bibliotecas/segurança preservadas. Publicação idêntica já comprovada pode ser conferida em vez de reiniciar sem alteração funcional. Mantêm-se os recursos entregues de encerramento por canal e agrupamento por fornecedor.

Auditoria: branches locais dos dois repositórios são ancestrais de main (0 commits exclusivos). Worktrees API3426e2f/f0490d3/2e3384d também são ancestrais, sem processos com cwd nelas. Só há NuGet.Config temporário com feed local ep-localfeed que já não existe, sem credenciais. Não há diferenças de src/tests entre main e fontes já publicadas ad36bef/4c5e6e3.

Nenhuma branch remota tem commits fora de main; merge --ff-only origin/main confirmou Already up to date nos dois repos. Git diff de src/tests contra as fontes publicadas é vazio. Produção: painel hashd478a387… e APIs4623f147…/9ff314b6…/5c1036c1… iguais aos artefatos já testados/publicados; quatro serviços ativos/saudáveis.

Publicação consolidada já estava ativa e foi conferida por hash e health (sem diferença funcional para publicar). Removidas três worktrees ancestrais limpas, após eliminar o NuGet.Config obsoleto sem credenciais. Removidas cinco branches locais já integradas; referências remotas mantidas. Não houve merge de código novo nem reinício de versão idêntica.

## Encerramento comprovado

Não havia commits pendentes, locais ou remotos, nos dois repositórios. Todas as
branches eram ancestrais da main, e ambas estavam alinhadas a origin/main. Nenhum
merge artificial ou código de outra tarefa foi introduzido. Código do painel
(ad36bef) e da API(4c5e6e3) é idêntico ao da main nos diretórios src/tests/test.
As diferenças posteriores eram documentação.

Worktrees removidas, após conferir ancestralidade, estado do disco e ausência
de processos com cwd nelas:
- /mnt/sufficit/.legacy-audit-baseline.C8SumJ (3426e2f).
- /mnt/sufficit/.legacy-audit-validated.DeYiS0 (f0490d3).
- /mnt/sufficit/sufficit-endpoints-deploy-pjsip (2e3384d).

Na última, NuGet.Config era somente um feed local ep-localfeed já inexistente,
sem credenciais; arquivo de build obsoleto removido antes da remoção normal da
worktree, sem --force. Não havia outros arquivos não preservados.
Cinco branches locais já integradas removidas com git branch -d:
integration/panel-core-main, refactor/queue-names, native-global-settings-main-sync,
session/codex-01a1cc0b-a58d-7583-9f14-2bfb1a3988e5 e work/provisioning-tech-pjsip.
Referências remotas e commits continuam preservados. Cada repositório ficou
somente com sua worktree principal e branch local main.

Limpeza remota por lista explícita de temporários desta sessão:10 entradas no
Eveo,2 Castrum e2 Apoint, total481.498.165 bytes (~459MiB). Eram probes de runtime
e pacotes de fila/encerramento identificados pelas entregas anteriores. Antes
da remoção, nenhuma delas estava em uso por cwd/argumentos de processo. Todos
os caminhos-alvo foram conferidos ausentes após a limpeza. Temporários locais
correspondentes já estavam ausentes; candidatos publish-* da entrega já tinham
sido removidos. Não houve varredura destrutiva de /tmp nem do tmp compartilhado
do workspace. Helpers compartilhados, dados permanentes e backups de rollback
continuam preservados.

## Produção e validação

Em19/11, publicação consolidada já estava ativa e foi reconferida, sem nova
troca/reinício de binários idênticos. Painel ativo, health público running e
release trunk-hangup-20261118-ad36bef, SHA256:
`d478a387a33cb11847aa8f044fcffc504870ceca71cf4164107d22d37148c1b3`.

API ativa/Healthy por socket nos três hosts; hashes instalados iguais aos
candidatos validados na entrega anterior:
- Eveo:4623f147ab6bf9c695b008e4b381e4fec7979ccb206bf3b9ab4fa2ce437feedf.
- Castrum:9ff314b673229c66cfb28728cd486a0276c4bc395124777935e983e88d06e972.
- Apoint:5c1036c161b6e6fc4823bf430d7af7468ae976f0020b073214b83d593372b80b.

Não houve alteração funcional nem necessidade de repetir testes/builds já
comprovados para esses hashes. Verificações desta tarefa foram Git, disco,
processos e produção. Sem alteração/reinício Google/controladores e sem encerrar
chamadas. Nenhuma tarefa/Plano pendente de outra sessão foi encerrado.
Todos os checkpoints do escopo autorizado foram concluídos.
