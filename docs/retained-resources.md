# Recursos já vistos

Na Mesa de operação, **Manter recursos já vistos** vem ligado por padrão, também
ao carregar configurações antigas. A preferência é compartilhada pelos gerentes.
Ramais SIP/PJSIP/IAX2, troncos e filas que aparecerem na visão autorizada são
lembrados sem prazo de expiração, mesmo depois de atualizar a página, reconectar
ou reiniciar o painel. A classificação e os filtros FOP continuam sendo aplicados.

O arquivo guarda apenas identidade, servidor, nome, identificadores e última
observação. Não guarda canais, números dos interlocutores, estados de registro,
atividade ou ações de supervisão. Sem evidência atual, o cartão fica neutro:
**Já visto · sem estado atual**. Isso não afirma que está offline nem livre.
Ao chegar uma observação atual, o estado volta a ser derivado dessa observação.
A data salva é aproximada: atualizações são amostradas em intervalos de um minuto
para limitar gravações, sem atrasar a primeira persistência de uma identidade.

Desligar a opção só esconde os recursos lembrados sem observação atual; a coleta
continua. Ligar novamente os exibe. **Limpar recursos lembrados** exige confirmação,
limpa a visão selecionada (central ou empresa) para todos os gerentes e mantém as
regras e os recursos observados atualmente. Eventos novos podem recriar o histórico;
snapshots anteriores à limpeza não o recriam. A limpeza não altera o PBX, chamadas,
ramais nem filas. Não há desfazer desse histórico; novos eventos podem reconstruí-lo.

## Segurança e operação

- Acesso `manager` revalidado no servidor, usando o cache de revogação existente.
- Histórico por contexto. A visão de empresa nunca lê o inventário central e só
  reapresenta cartões ainda autorizados pela API para aquela empresa. Revogação da
  sessão remove a visualização; o histórico não concede privilégios.
- `/var/lib/sufficit-telephony-panel/known-resources.json`, fora dos releases,
  arquivo 0600, diretório do serviço 0700. Override: `Panel:BoardInventoryFile`.
- Gravação serializada e atômica. Falha ou corrupção é exibida, sem substituir o
  histórico silenciosamente. A mesa continua com as observações atuais quando há
  falha recuperável de armazenamento.
- Instância única Eveo, sem escrita compartilhada entre servidores. Nenhuma conexão
  AMI extra. Nenhuma alteração no Asterisk ou nos coletores.
- Sem TTL nem remoção automática. Limite operacional de 100.000 identidades e
  128 MB na leitura: ao atingir o limite há aviso, preservando o inventário anterior;
  exige limpeza explícita ou ampliação planejada, nunca expulsão silenciosa.
- Captura somente o que foi observado na visão autorizada enquanto o painel a
  consulta. Não recupera eventos anteriores à implantação nem pretende ser um
  cadastro completo do PBX. Canais técnicos Local temporários não são inventário.
- As outras abas continuam baseadas na projeção de eventos; retenção é da mesa.
  Filas têm seção própria, com até 40 cartões por resultado; refine a busca.

Testes: `tests/BoardInventoryChecks.cs` verifica persistência, isolamento, limpeza,
retorno de eventos, arquivo privado e estados. `tests/board-inventory.browser.cjs`
exercita os controles com fixture identificada. A fixture não prova login real.
