# Publicação dos diagnósticos no Eveo Apps

Autorização do usuário: publicar a implementação de diagnósticos desta sessão.
Ativação em 21/10/2026 09:20:32 -03, somente sufficit-telephony-panel no Eveo Apps.

- Release: `/opt/sufficit-telephony-panel/releases/error-diagnostics-20261021-0918`.
- Anterior preservado: `/opt/sufficit-telephony-panel/releases/queue-capacity-20261015-a60d8a3`.
- SHA256 DLL: `494138f1050efd49bd9f0960319f49b5db86def410af99b48e50f3bef1801f2b`.
- SHA256 pacote: `8fc3418073eb3bbed9db14b4f9b17acf8cba1ca8d2af5f09b2e1e6359179acc6`.
- Manifesto do helper: `/var/backups/sufficit-telephony-panel/error-diagnostics-20261021-0918/release.json`.
- PID após ativação: 4173090; serviço ativo. Implementação no checkout main com
  alterações locais; não foi criado commit nem feito push nesta publicação.

## Preparação e ativação

`dotnet publish src/Sufficit.Telephony.Panel.csproj -c Release -o
artifacts/error-diagnostics-20261021-0918 -m:1 --nologo` concluído. Verificados
222 arquivos, static web assets e somente appsettings.json no artefato. Arquivo
transferido por SSH, hashes do pacote/DLL conferidos no host antes da ativação.
Appsettings do release anterior copiado no host e confirmado idêntico após o deploy.

Ativado por `deploy/release-panel.py` com release anterior esperado e rollback
automático em falha do health. Não houve rollback: gates passaram. Configuração
externa e estado persistente mantidos. Nginx, Identity, PBXs e coletores não foram
alterados ou reiniciados por esta publicação.

## Verificação em produção

- `/health`: HTTP 200 público e gate pelo socket Unix passou.
- `/api/fleet` anônimo: HTTP 401.
- DLL instalada corresponde ao hash local; CSS da página usa fingerprint novo.
- Tablet Lenovo: login SSO após reinício concluído; fluxo de eventos conectado,
  leituras/chamadas presentes e sem o erro genérico anterior.
- Aba de canais: motivos de bloqueio reais de canal Local e empresa ausente
  visíveis, trinta linhas e coluna de supervisão com largura de aproximadamente
  312 pixels; sem overflow horizontal da página.
- `/queues`: detalhes das quatro fontes presentes; Google (dois contextos),
  Eveo e Apoint informaram controlador conectado e leituras atuais às 09:22.
  Isso é estado observado nesse horário, não prova de que o deploy reparou os
  controladores. A navegação SPA para /queues exigiu recarregar para trocar o
  conteúdo da rota; após recarga a tela correta foi validada.
- Nenhum botão de confirmação de supervisão foi acionado; áudio real não validado.

Rollback operacional: recolocar `current` no release anterior preservado e
reiniciar somente `sufficit-telephony-panel`, repetindo health público/socket.
Os hashes/paths exatos estão no manifesto do helper. Não sobrescrever releases
nem arquivos de inventário/regras para voltar à versão anterior.

Implementação e testes: [evidência local](202610210916-error-diagnostics.md).
Contrato permanente: [diagnósticos](../error-diagnostics.md).
