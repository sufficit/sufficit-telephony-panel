# Chamadas recebidas por DID no painel

Objetivo: o card DID 557137170655 deve refletir a chamada recebida do cliente 223809ff00624cf890b9d6ea538dcffd, respeitando isolamento e o fim da chamada.

1. [concluído] Confirmar fonte e contrato para correlacionar DID, canal e proprietário, sem reload/restart no Google.
2. [concluído] Implementar snapshot AMI somente de leitura na API e projeção no card, com testes de titularidade, direção, contagem, falhas e encerramento.
3. [em andamento] Validar candidatos, commit/push na main e publicar API/painel.
4. [pendente] Conferir binários/saúde/fontes em produção, limpar artefatos próprios e arquivar como atividade.

Evidência inicial: o card é um inventário `entry:did:<contexto>:<guid>:configured`, deliberadamente sem matcher de canal; seu status também sobrepõe a atividade. O stream isolado do observador não recebe NewAccountCode/VarSet; a conta Google permite somente eventos explicitamente filtrados e write=none. O dialplan atribui a empresa e `NUMENTRADA` depois de criar o canal. A chamada da captura já não estava ativa ao consultar os canais.

Estratégia: usar a conta AMI existente da API para Status e Getvar (apenas leitura), sem tocar no observador/PABX Google. Atribuição exige accountcode exato, DID cadastrada no contexto, canal raiz e Uniqueid confirmado após ler NUMENTRADA. Nenhum vínculo por nome do fornecedor ou número de destino discado. A API deve informar falha de fonte e não a transformar em zero confirmado.

Validação: testes Release do painel e API (regressões e gates CustomerSearch/InternalPermission/BillingCheckout/Voice/MarkdownCoverage), guards do deploy; consultas read-only de produção e hashes dos candidatos. Nenhuma chamada real ou hangup de teste sem instrução explícita.

Implementação definida: snapshot read-only separado na API por contexto; Status compartilha cache privado por nó, Getvar somente em canais raiz do proprietário, com NUMENTRADA/UNIQUEID/accountcode confirmados. DTO usa o GUID da DID já presente no card, dispensando derivar identidade da descrição ou mudar o coletor. Alertas existentes do painel são a referência visual para falha de fonte; sem redesenho.

Regressão: o novo teste de associação falhou com o matcher original e passou com a correção. Painel: 1687 checks aprovados. API: 309 testes aprovados em cada candidato com as dependências reais por host; 32 guards de deploy. Consultas de produção responderam nos três PABX; o caminho público Eveo não respondeu do eveo-apps, e a leitura usa 172.19.2.104 pela VPN. As identidades do resolver (freepbx/eveo/apoint) são mapeadas explicitamente aos rótulos do stream (google-voip/eveo-voip/apoint-voip). Nenhum ajuste de credencial/permissão/PABX foi necessário.
