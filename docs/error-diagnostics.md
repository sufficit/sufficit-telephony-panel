# Diagnósticos de erros e supervisão

O painel apresenta informações acionáveis sem enviar mensagens brutas de exceção,
credenciais ou pilhas técnicas ao navegador.

## Falhas de consulta

Os alertas da telefonia ao vivo, pesquisa de empresas, inventário, leitura de regras
e ações de supervisão incluem **Detalhes do erro** quando uma exceção foi capturada.
A referência UUID, horário UTC e categoria identificam o mesmo evento nos logs do
serviço. A ausência de referência indica um estado relatado pela fonte, como
controlador desconectado, e não uma exceção local capturada.

`PanelDiagnostics` registra referência, operação, tipo da exceção e stack trace.
Não registra `Exception.Message`, corpos HTTP, tokens, parties ou eventos AMI.
Para investigar, use o identificador mostrado na tela:

```sh
journalctl -u sufficit-telephony-panel --since '1 hour ago' --no-pager | rg -F '<referência>'
```

Não habilitar logs de payloads/tokens para obter detalhes adicionais.

## Fontes de filas

**Detalhes das fontes** informa nó, contexto autorizado, situação da consulta,
HTTP quando disponível, conexão do controlador e última observação em UTC.
Uma data ausente (`DateTimeOffset` padrão) aparece como não informada.
A mesma apresentação é usada em Telefonia ao vivo e Filas e registros.

Critérios de atualidade permanecem: consulta disponível, controlador conectado,
atividade além de observação de registros, data até cinco segundos no futuro e
leitura de no máximo vinte segundos. Cache não torna uma consulta falha saudável.
HTTP 401/403 descarta também o snapshot anterior. Reconectar e atualizar refaz
as leituras respeitando o cache compartilhado existente.

## Botões Ouvir e Sussurrar

O motivo aparece em texto junto dos botões, sem depender de hover. O mesmo
`MonitorActionGuard` explica e aplica as condições de elegibilidade: canal de
chamada SIP/PJSIP válido, empresa confirmada e coincidente, nó identificado,
fluxo conectado e evento de no máximo trinta segundos. Permissões atuais e
ramal preferido continuam necessários. Estado Down/Up não substitui essas provas.

Falha ao consultar o ramal preferido é distinta da ausência de configuração e
possui referência própria; ela não derruba a observação central.
Ouvir chama o ramal preferido. O navegador não recebe áudio e nenhuma chamada
é refeita automaticamente após falha ambígua.

## Validação local

A fixture `/?preview=true&diagnosticpreview=true` existe somente em Development.
Ela demonstra um erro com referência fictícia, fontes atuais/desconectadas e
canais elegível, sem contexto, Local e expirado. Não acessa PBXs nem permite
bypass de autenticação em Production.

Os testes de navegador estão em `tests/error-diagnostics.browser.cjs` e usam
`http://127.0.0.1:5169`. Capturas locais ficam em `artifacts/diagnostics/` (ignoradas
pelo Git). A fixture não comprova originação nem áudio real.

Publicado no Eveo Apps em 21/10/2026: release `error-diagnostics-20261021-0918`.
[Evidência e rollback](activities/202610210922-error-diagnostics-deploy.md).
