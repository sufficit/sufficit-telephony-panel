# QueueMonitor

Renomeados AcdMonitor.razor e CSS para QueueMonitor, além de QueueMonitorOverview e QueueMonitorFixture. Atualizados Home, diagnóstico, testes e classes CSS/IDs privados do monitor, sem aliases. Preservadas mudanças locais existentes no link de gestão e docs/system-overview.md. Outros tipos Acd do contrato/integracao continuam fora desta alteração pontual.

Build com restore: 5 projetos, zero erros/avisos. Verificações existentes: PASS901. Restore dos testes emitiu NU1903 de Microsoft.AspNetCore.SignalR.Common10.0.11 (dependência existente). Diff check passou, nenhuma referência AcdMonitor no código/testes. Sem publicação ou alteração de comportamento visual; publicação Blazor do trabalho anterior continua aguardando decisão.
