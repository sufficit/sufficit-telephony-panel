# Estado de toque confirmado na origem

Pedido: ao chamar PJSIP → SIP/6007, a origem continuava “Discando” quando o destino já estava “Tocando”. Corrigir a apresentação da fase da chamada na origem, preservando distinção entre receber toque e destino tocando. Continuidade da implementação/publicação autorizada nesta sessão.

1. CONCLUÍDO — Confirmar contrato dos canais/fluxos e reproduzir origem Ring com destino Ringing correlacionado.
2. CONCLUÍDO — Apresentar destino tocando na origem com evidência autorizada de linkedid/nó, sem contaminar chamadas independentes; validar regressões e navegador.
3. EM ANDAMENTO — Commit/push, publicar somente painel em eveo-apps, verificar saúde/artefatos e converter plano em atividade.

Direção: manter os efeitos existentes exclusivamente em extensões (DESIGN.md e pesquisa desta sessão); corrigir o estado que os aciona. Usar texto próprio para toque no destino, manter foco/texto estáveis e reduced-motion. Não inferir vínculo por número discado; não animar troncos/filas. Nenhuma alteração de AMI, Asterisk ou controlador necessária.

Validação: regressão C# com origem/destino, isolamento por nó/conta/linkedid, estados antes/depois de atendimento/remoção, browser com origem e destino simultâneos e escopo de animação. Fonte real já fornece Newstate após operação anterior.

Contrato: preservar estado AMI Ring da origem, mas apresentar “Destino tocando” quando um canal de endpoint autorizado do mesmo linkedid/nó confirmar Ringing. Chamada sem correlação mantém Discando; origem Up permanece em uso; toque local continua Tocando. Testes de isolamento reproduzem o caso antes da correção.

Regressão reproduzida em C# antes da mudança (origem mantinha dialing). Após correção: 1.542 checks PASS; build PASS; call-ringback.browser.cjs PASS com ambos os cards, término do pulso, mobile/escuro/reduced-motion e troncos/filas estáticos. DESIGN.md e operator-board.md atualizados com a distinção de estado local/fase correlacionada.
