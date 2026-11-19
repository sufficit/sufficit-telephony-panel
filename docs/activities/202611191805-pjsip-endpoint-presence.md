# Ramais PJSIP por identidade canônica — atividade concluída

Objetivo: exibir o registro do ramal 6200 e demais ramais com identidade PJSIP GUID, mesmo quando somente SIP está marcado no cadastro legado. Sem alterar cadastro, qualify, Asterisk ou controlador Google.

1. [concluído] Confirmar contrato, identidade e fonte canônica de registro; definir correção na API e no painel.
2. [concluído] Implementar aliases por propriedade comprovada e presença por contatos ativos, com regressões de isolamento, expiração e ausência de qualify.
3. [concluído] Validar candidatos com dependências reais, commit/push direto na main e publicar API/painel.
4. [concluído] Verificar registro 6200 na produção, hashes/saúde, limpar artefatos próprios e converter este plano em atividade.

Evidência: contexto 223809ff00624cf890b9d6ea538dcffd, ramal 0004756200, GUID 2052c18310ca4868b53dc40a985429f2. Registro ativo no Google confirmado por `pjsip show aor` e contato canônico não expirado/não excluído. `tech_pjsip=0`, `tech_sip=1`. Todos os SIP têm projeção PJSIP automática (contrato já utilizado por EndPointDeviceCommandRuntime). O catálogo legado usa tecnologia/título e perde esse vínculo. Eventos AMI isolados não fornecem snapshot inicial completo de registros realtime.

Validação: regressões API/painel; gates API CustomerSearch/InternalPermission/BillingCheckout/Voice/MarkdownCoverage e guards de deploy; testes Release do painel. Verificação sem chamadas reais, sem escrita de configuração de telefonia e sem recarga/restart Google.

Validação até aqui: painel 1670 checks; API 297 testes em cada candidato eveo/castrum/apoint com suas dependências instaladas; 30 guards de deploy. Build Release dos projetos de teste concluído. Warnings preexistentes de pacotes/obsolescência permanecem. A regressão também revelou cache de matcher por instância do card: aliases novos usam cópia do card no painel, preservando a imutabilidade esperada.

Consulta read-only dos três hosts: 3160 tipos carregados; 27 itens de catálogo, 7 com ProductId preservado. O contato do 6200 foi associado ao contexto correto e às identidades SIP/PJSIP/WSS esperadas em cada candidato. API commit `be28287` publicado na main.

Publicações concorrentes: Eveo mudou apenas a DLL principal já contemplada por main; Castrum mudou dependências durante upload e a transação recusou o swap mantendo serviço saudável. Castrum/Apoint foram recompilados/retestados com as dependências finais. Apoint abriu HTTP aos 13s, porém o heartbeat ainda estava Degraded na janela de 15 tentativas; rollback automático confirmou versão anterior saudável. Orquestrador ampliado para 60 tentativas sem aceitar Degraded; duas regressões falharam antes e passaram depois (32 guards totais). Commit API/orquestrador `b87fa75`.

Entrega: API saudável nos três hosts (176 DLLs comparadas integralmente com cada candidato; rota de presença sem token responde 401). Painel release `pjsip-presence-20261119-6195a4b`, SHA256 `956bc6aad1fcb428d301752d47561ac4b8c7cde4f8db92a7fb30cced15253dd3`, saúde pública/socket running. Coletor AMI mantém PID 409848. API Eveo SHA `b00f6717e3d685a6b269bb52d5f42b09444213d0f441d2011b411ea8d16cf1a2`; Castrum `a42706bc369b237eccded7586aa6598a22bcb767c16545ef7635c5afcf624a59`; Apoint `39f0668be395fa60fc4df2da1e40b9053b0d4b01d78939bd775ee18d047b19f3`. Cobertura Markdown final: 8 testes aprovados.

## Encerramento

Entrega concluída em 19/11/2026. Commits na main: painel `6195a4b`; API `be28287`; orquestrador de deploy `b87fa75`. Saúde pública/socket do painel e saúde dos três hosts da API confirmadas. O registro do 6200 foi reconfirmado após publicação, associado ao cliente e GUID corretos. Os pacotes, cópias de runtime, probes e diretórios locais de publicação desta tarefa foram removidos. Rollbacks e artefatos de publicação recusada/revertida permanecem preservados pela política do deploy.

Sem login na conta do usuário ou chamada real: validação por dados de produção somente de leitura, projeção testada e identidade dos binários instalados. A presença tem cache de dez segundos, é descartada após quinze segundos sem observação atual e respeita a expiração do contato. Qualify/alcance e estado de uso do aparelho continuam independentes do registro; nenhuma configuração de telefonia foi alterada.

Contrato permanente: [propriedade e fontes de monitoramento](../monitoring-ownership.md).
