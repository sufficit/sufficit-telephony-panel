# Ramais PJSIP por identidade canônica

Objetivo: exibir o registro do ramal 6200 e demais ramais com identidade PJSIP GUID, mesmo quando somente SIP está marcado no cadastro legado. Sem alterar cadastro, qualify, Asterisk ou controlador Google.

1. [concluído] Confirmar contrato, identidade e fonte canônica de registro; definir correção na API e no painel.
2. [concluído] Implementar aliases por propriedade comprovada e presença por contatos ativos, com regressões de isolamento, expiração e ausência de qualify.
3. [em andamento] Validar candidatos com dependências reais, commit/push direto na main e publicar API/painel.
4. [pendente] Verificar registro 6200 na produção, hashes/saúde, limpar artefatos próprios e converter este plano em atividade.

Evidência: contexto 223809ff00624cf890b9d6ea538dcffd, ramal 0004756200, GUID 2052c18310ca4868b53dc40a985429f2. Registro ativo no Google confirmado por `pjsip show aor` e contato canônico não expirado/não excluído. `tech_pjsip=0`, `tech_sip=1`. Todos os SIP têm projeção PJSIP automática (contrato já utilizado por EndPointDeviceCommandRuntime). O catálogo legado usa tecnologia/título e perde esse vínculo. Eventos AMI isolados não fornecem snapshot inicial completo de registros realtime.

Validação: regressões API/painel; gates API CustomerSearch/InternalPermission/BillingCheckout/Voice/MarkdownCoverage e guards de deploy; testes Release do painel. Verificação sem chamadas reais, sem escrita de configuração de telefonia e sem recarga/restart Google.

Validação até aqui: painel 1670 checks; API 297 testes em cada candidato eveo/castrum/apoint com suas dependências instaladas; 30 guards de deploy. Build Release dos projetos de teste concluído. Warnings preexistentes de pacotes/obsolescência permanecem. A regressão também revelou cache de matcher por instância do card: aliases novos usam cópia do card no painel, preservando a imutabilidade esperada.

Consulta read-only dos três hosts: 3160 tipos carregados; 27 itens de catálogo, 7 com ProductId preservado. O contato do 6200 foi associado ao contexto correto e às identidades SIP/PJSIP/WSS esperadas em cada candidato. API commit `be28287` publicado na main.
