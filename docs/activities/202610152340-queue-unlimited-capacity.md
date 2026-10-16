# Capacidade ilimitada com padrão20

Solicitação do mantenedor: permitir sem limite no sistema novo, manter20 como
padrão e importar FreePBX ilimitado como20. Contrato:0=sem limite no motor/API/banco;
positivo=limite explícito, negativo inválido; SQL, API e formulário iniciam em20.
Orçamento global de recursos do controlador preservado. Nenhuma fila existente
recebe capacidade nova automaticamente. Esquema009 altera constraint/default sem
atualizar registros; teste de checksum preservado em MariaDB10.4.34.

ARI0.16.1 e importador atualizados. Testes: integração completa net10/net7 aprovada,
incluindo102 chamadas admitidas em fila ilimitada com orçamento global102 e rejeição
da103ª; padrão20 limita a21ª.20 testes Python/MariaDB10.4 aprovados. Blazor118 testes
aprovados; Panel907 verificações aprovadas. Interface conserva composição e
componentes existentes; rótulo0=sem limite, apresentação localizada e sidecars AI
atualizados. Referências: decisão do usuário, DESIGN.md existente e API SUI; sem
mudança de layout/tokens. Sem nova validação visual em navegador nesta etapa.

Publicação em preparação: migração via proxy, artefatos0.16.1 e deploys oficiais
dos consumidores. Capacidades, rotas e dados de clientes existentes preservados.
Importação geral permanece pendente das demais divergências (tempos, mídia/saídas).
