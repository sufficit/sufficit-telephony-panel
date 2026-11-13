# Recuperar observação global Google no painel

1. [concluído] Consultar fontes reais somente leitura. E/A retornam 1.159 filas e zero atividade; fachada Google404, controlador direto200 com464 contextos,1.161 filas,3 em espera e3 em atendimento em2026-11-13T20:16:33Z.
2. [concluído] Adaptar leitor do painel para endpoint/credencial de agregado separados, preservando fachada de registro/diagnóstico; validar regressão e contrato como usuário do serviço; publicar/configurar somente painel com rollback.
3. [concluído] Conferir mesma leitura pelo usuário do serviço, saúde e agregação/configuração efetiva; atividade, commit/push main.

Restrições: nenhuma alteração/reinício no Google/controladores. Endereço privado172.19.254.250:18189 e credencial de monitoramento existente; não divulgar chave nem persistir respostas privadas no repo. Não atribuir publicação externa a esta sessão: apenas a disponibilidade atual do endpoint foi constatada. Preservar mudanças paralelas. Se contrato inválido não ativar e registrar bloqueio concreto.

Decisão: a fachada18190 deve continuar para snapshots por empresa/presença/diagnóstico. Adicionar GlobalBaseUrl/GlobalKeyFile opcionais e privados no leitor; somente a descoberta global usa18189 com credencial existente. Configuração simples de BaseUrl removeria a função de registro, portanto não será usada.

Regressão falhou antes da correção (“Global discovery uses its dedicated controller URL and credential instead of the registration facade”); 1.502 checks passaram após a correção, incluindo separação de URL/chave, fachada por empresa e diagnóstico e validação de endpoints privados.

Pré-ativação, com sufftelpanel e leitor candidato real: Google464 contextos/1.161 filas/5 espera/6 atendimento; E/A463 contextos/1.159 filas cada/zero. Projeção agrupada1.161 filas,5 espera,11 filas ativas nas primeiras40; snapshot por empresa na fachada preservou2 agentes.

## Verificação pós-ativação e entrega

Fonte `ea041a3`, commit/push main. Release instalada:
`/opt/sufficit-telephony-panel/releases/google-source-20261113-ea041a3`.
Anterior preservada: `queue-order-20261113-80b3d8b`. SHA256 da DLL instalado
igual ao pacote local:
`1af001606520ddf9f973198aa03347c27244d64bccff82109d2d378491748911`.

Drop-in novo `90-google-global-queue.conf`: somente GlobalBaseUrl e GlobalKeyFile
no índice Google. Campos anteriores de todas as fontes conferidos iguais no
ambiente efetivo do processo após o reinício. Snapshot por empresa/diagnóstico
continua na porta18190; agregado global vai à18189. Credencial existente já
protegida root:sufftelpanel0640, sem criar/conceder permissões novas.
Baseline privado em `/var/backups/sufficit-telephony-panel/google-source-config-20261113-ea041a3`;
release anterior/configuração anterior preservadas. Script de ativação preparado
para retirar o novo drop-in e voltar release se saúde ou prova real falhassem.

A validação usou as DLLs da release instalada e ambiente efetivo de fontes do
processo, rodando como sufftelpanel em20:23:00UTC:

- Google:464 contextos,1.161 filas, zero problemas de cobertura,4 chamadas em espera
  e5 em atendimento.
- Eveo/Apoint:463 contextos,1.159 filas por nó, zero problemas e zero atividade.
- Projeção real agrupada:1.161 filas distintas,4 em espera,8 filas ativas; as8
  estavam nas primeiras40 após ordenação por utilização.
- Snapshot por empresa na fachada permaneceu disponível com2 agentes; diagnóstico
  manteve porta18190.

Serviço ativo; health por socket e público HTTP200/running; `/api/fleet` anônimo
HTTP401; CSS público hash
`8865725c8c020d2c0e81a251524dfeef4e89a64b10dc1277f1bf5b100a64f562`.
Coletor sufficit-ami-events manteve MainPID409848. Reiniciado somente
sufficit-telephony-panel. Nenhuma escrita/reinício no Google ou controladores.

A verificação posterior refuta a hipótese anterior de que esta correção dependia
necessariamente de um novo deploy Google: o endpoint direto já estava disponível
na consulta atual. Não se atribui a esta sessão a alteração que o disponibilizou.
A fachada18190 permanecia404 no global e provocava o resultado incompleto.

Limites: não foi usada sessão autenticada do usuário em navegador de produção.
A prova é o leitor, validação de contrato, agrupamento e ordenação reais com o
mesmo pacote/configuração/permissões de sistema do serviço; não apenas HTTP200.
Chamadas mudam a cada instante;3/3,5/6 e4/5 são observações em horários diferentes.
1.502 checks passaram (regressão antes/depois e segurança/autorização incluídas);
aviso NU1903 SignalR.Common10.0.11 preexistente. Nenhuma mudança visual exigiu nova
rodada de screenshots; os componentes agrupados/ordenados já foram validados
na tarefa anterior.

Todos os checkpoints autorizados concluídos. Contrato permanente em
[operator-board.md](../operator-board.md#fonte-global-separada-da-fachada-de-registro).
