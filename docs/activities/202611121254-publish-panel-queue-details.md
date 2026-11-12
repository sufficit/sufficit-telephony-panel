# Publicação do painel: detalhes de filas

Autorização: mantenedor esclareceu que o painel pode ser publicado a qualquer momento; aguardar horário mais calmo apenas para o servidor Google. Publicar somente sufficit-telephony-panel em eveo-apps, preservando controladores e Google.

1. [concluído] Garantir compatibilidade com controlador antigo sem endpoint agregado e preparar pacote atualizado com GUID/link.
2. [concluído] Instalar em release isolado, conferir hashes e trocar/reiniciar somente o painel com rollback existente.
3. [concluído] Verificar health, ativos e contrato; registrar evidência, converter plano em atividade e commit/push.

Compatibilidade: a main contém leitura agregada nova; fontes atuais ainda não oferecem a rota. Manter snapshots fixos anteriores quando o agregado inicial retorna 403/404, junto ao aviso explícito de cobertura global incompleta. Não apresentar ausência de dados como prova de fila vazia. Seleção individual permanece inalterada.

Validação: regressão de controlador antigo, suíte Panel.Tests e publish Release; hash DLL/CSS remoto, health interno/público e 401 anônimo. Login/aceitação visual de produção não presumidos por esses checks.

Compatibilidade commit 11ecda2 na main, 1.443 checks passaram. Publish Release atualizado concluído; DLL ca0ba675e42f0c540368306aa3d18d131b7916f298f308444a166e6997f9516e.

Primeira ativação 11ecda2 realizada somente no painel, PID 769209. Coletor preservado PID 410178. Verificação real: Google 404, Eveo/Apoint 403 no agregado novo. Reaberto checkpoint de compatibilidade para incluir 403 com consulta individual ainda autorizada, sem usar cache global negado.

Compatibilidade 403/404: commit cd7141e, 1.445 checks passaram. Publish atualizado e release queue-details-20261112-cd7141e ativado somente em eveo-apps. DLL 6b924fef3f042703be91aff52712c627186e82a24794d38e63f76d6d4d0abbd6.

## Resultado e evidências da publicação

Publicado SOMENTE sufficit-telephony-panel no eveo-apps, release final
`queue-details-20261112-cd7141e`. Detalhes com GUID/link do commit 0c7f58a,
compatibilidade inicial 11ecda2 e ajuste 403/404 cd7141e.

- Publish Release concluído e DLL instalada conferida:
  `6b924fef3f042703be91aff52712c627186e82a24794d38e63f76d6d4d0abbd6`.
- CSS instalado e resposta pública idênticos:
  `8865725c8c020d2c0e81a251524dfeef4e89a64b10dc1277f1bf5b100a64f562`;
  contém queue-resource-identity.
- Health interno (socket) e `https://panel.sufficit.com.br/health`: HTTP 200,
  service running. `/api/fleet` anônimo: 401.
- Serviço ativo, PID final 771068 (original 410648). Coletor AMI permaneceu ativo
  no mesmo PID 410178 antes/depois. Journal do painel sem erros na janela conferida.
- Probe temporário executado contra a DLL instalada, com as fontes reais e chaves
  lidas somente no host: quatro snapshots fixos disponíveis, sete filas em cada
  amostra; Google duas amostras, Eveo/Apoint uma cada. Não representam 28 filas
  distintas globais: são os contextos fixos preservados por fonte.
- Agregado indisponível sinalizado separadamente: Google 404, Eveo/Apoint 403.
  Nenhuma credencial, payload de interlocutores ou conteúdo de configuração exposto.
- 1.445 checks locais passaram antes do pacote final. Aviso preexistente
  SignalR.Common 10.0.11 preservado fora do escopo.

A primeira versão ativada tratava apenas 404; a verificação privada revelou 403
nos outros dois nós. A versão final trata ambos, preservando somente consultas
individualmente autorizadas; não restaura cache global negado. As duas ativações
reiniciaram exclusivamente o painel. Houve backup de referência de release e
rollback automático por health, pelo helper existente `deploy/release-panel.py`.
Configuração atual copiada no próprio host antes de ativar cada release; regras,
inventário, sessões e credenciais persistentes preservados.

Release original para rollback: `inbound-catalogue-20261106-22bec4e`. Manifestos
privados em `/var/backups/sufficit-telephony-panel/queue-details-20261112-11ecda2`
e `queue-details-20261112-cd7141e`. Reverter symlink/reiniciar somente painel se
necessário, nunca reutilizar diretório em execução.

## Limitações e decisão do mantenedor

O painel está liberado para deploy sem aguardar horário. O cuidado de janela é
o servidor Google; nesta publicação nenhum controlador ARI, Asterisk ou serviço
Google foi alterado/reiniciado. A correção de cobertura global integral depende
da atualização posterior dos controladores/fachada. O aviso atual de fonte
representa essa limitação real, não comprova ausência de chamadas em espera.

Os detalhes visuais foram testados anteriormente com fixtures desktop/mobile,
claro/escuro e PT/EN. Esta publicação confirma processo, arquivos públicos e
leitura privada; não presume conferência visual autenticada do usuário.
Fonte e documentação na main; push do registro não substitui os checks de
produção efetivamente executados acima.
