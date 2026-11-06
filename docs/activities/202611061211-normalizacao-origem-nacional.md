# Origem nacional com DDD

A origem 21960185389 da imagem não era normalizada porque faltava o país 55.
O normalizador compartilhado agora apresenta DDD + fixo/móvel brasileiro com
+55, em ambas as pontas, sem modificar os valores observados. Reconhecimento
internacional tem prioridade; números já com + continuam preservados.

O contrato assume Brasil para formatos nacionais reconhecidos, sem inferir DDD
para números locais, alterar ramais ou comprovar alocação/país real. Documentado
em docs/call-channel-correlation.md. Sem alteração visual ou de componentes.

Validação: regressão falhou antes da correção para 21960185389; depois,
`dotnet run --project tests/Panel.Tests.csproj` passou 1310 checks. Casos incluem
imagem, DDD 55, idempotência, valores locais/inválidos e originais preservados.
`git diff --check` passou. Aviso NU1903 preexistente em SignalR.Common 10.0.11.
Sem teste no navegador nesta tarefa, pois a alteração é no normalizador puro.

Entrega: commit e push na main conforme regra do mantenedor. Sem deploy nesta
tarefa; comportamento em produção ainda depende de publicação.
