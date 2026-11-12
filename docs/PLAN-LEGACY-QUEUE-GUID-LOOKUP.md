# GUID de fila antiga por consulta ao cadastro

1. [concluído] Criar resolução autorizada na API e comprovar identidade real do número informado.
2. [concluído] Consultar ao abrir detalhes sem identidade; mostrar GUID e gestão com estados explícitos, sem consulta a cada refresh. Testar autorização, troca de cartão, desktop/mobile.
3. [em andamento] Publicar API/painel nos destinos autorizados, verificar pacotes/dados reais e converter em atividade com commit/push.

Sem alterações em Google/controladores. Reusar estrutura SUI e referências da tarefa anterior, sem novo redesign. Consulta central com token delegado, autorização antes/depois; vínculo exato e único. Se não existir cadastro nativo, exibir GUID legado e gestão da empresa sem prometer uma edição inexistente. Fixtures não comprovam resultado real; registrar leitura operacional.

Consulta real pela DLL candidata confirmou 0000006500 →
ab845088-6170-409a-b7b4-ae9acff33f3f, empresa
d21cfb04-9d37-473b-837c-67591a26feed, Native=true.
A tabela antiga está vazia; resolução usa vínculos preservados da migração e
confere existência/proprietário no catálogo nativo a cada lookup.
1.462 checks do painel e navegador desktop/mobile claro/escuro passaram, incluindo
resolução, ausência, ambiguidade e erro/retry. Consulta ao abrir, sem repetição por
refresh; troca/fechamento cancela solicitação, e token fica no servidor.
