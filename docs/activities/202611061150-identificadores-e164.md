# Prefixo + nos identificadores internacionais apresentados

Pedido: padronizar os identificadores telefônicos da operadora com +, mantendo o
original para diagnóstico conforme a alteração anterior.

Implementado no normalizador de apresentação compartilhado pela mesa e detalhes
de canais. Reconhece país 55 explícito, DDD brasileiro e formato de fixo/móvel,
além do 800. Não infere país/DDD de números locais ou de sequências estrangeiras.
A correção estrita de 550800 agora termina em +55800. Valores já com + não recebem
outro sinal. Eventos originais, correlação, supervisão e roteamento permanecem intactos.

O aviso passou a “Apresentação normalizada”: adicionar + não prova erro da operadora
nem origem externa. Origem e destino originais continuam visíveis. Não há novo
histórico permanente nem envio de notificações.

Validação: teste de regressão falhou antes da implementação; depois,
`dotnet run --project tests/Panel.Tests.csproj` compilou e passou 1285 checks.
Cobertura inclui fixo, móvel, 0800, + existente, ramais, locais, DDD inválido,
comprimento incorreto, letras, dígitos não ASCII, newline e preservação de originais.
Restore apresentou aviso preexistente NU1903 de SignalR.Common 10.0.11.
Playwright passou com fixture Development isolada em 5177: + na mesa e na tabela,
originais preservados, diagnóstico genérico, desktop/móvel e regressão do 0800.
Capturas locais em tests/artifacts/phone-number-e164-{desktop,mobile}.png; desktop
inspecionado visualmente. Referências: componente e DESIGN.md existentes, sem
mudança de layout. Códigos nacionais conferidos em resultados oficiais da Anatel;
link permanente em docs/call-channel-correlation.md.

Entrega via commit/push main conforme preferência do mantenedor. Sem deploy ou
chamada real de teste. Instância local de validação encerrada ao finalizar.
