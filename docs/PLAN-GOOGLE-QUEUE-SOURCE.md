# Recuperar observação global Google no painel

1. [concluído] Consultar fontes reais somente leitura. E/A retornam 1.159 filas e zero atividade; fachada Google404, controlador direto200 com464 contextos,1.161 filas,3 em espera e3 em atendimento em2026-11-13T20:16:33Z.
2. [em andamento] Adaptar leitor do painel para endpoint/credencial de agregado separados, preservando fachada de registro/diagnóstico; validar regressão e contrato como usuário do serviço; publicar/configurar somente painel com rollback.
3. [pendente] Conferir mesma leitura pelo usuário do serviço, saúde e agregação/configuração efetiva; atividade, commit/push main.

Restrições: nenhuma alteração/reinício no Google/controladores. Endereço privado172.19.254.250:18189 e credencial de monitoramento existente; não divulgar chave nem persistir respostas privadas no repo. Não atribuir publicação externa a esta sessão: apenas a disponibilidade atual do endpoint foi constatada. Preservar mudanças paralelas. Se contrato inválido não ativar e registrar bloqueio concreto.

Decisão: a fachada18190 deve continuar para snapshots por empresa/presença/diagnóstico. Adicionar GlobalBaseUrl/GlobalKeyFile opcionais e privados no leitor; somente a descoberta global usa18189 com credencial existente. Configuração simples de BaseUrl removeria a função de registro, portanto não será usada.

Regressão falhou antes da correção (“Global discovery uses its dedicated controller URL and credential instead of the registration facade”); 1.502 checks passaram após a correção, incluindo separação de URL/chave, fachada por empresa e diagnóstico e validação de endpoints privados.
