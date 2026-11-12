# Publicação do painel: detalhes de filas

Autorização: mantenedor esclareceu que o painel pode ser publicado a qualquer momento; aguardar horário mais calmo apenas para o servidor Google. Publicar somente sufficit-telephony-panel em eveo-apps, preservando controladores e Google.

1. [em andamento] Garantir compatibilidade com controlador antigo sem endpoint agregado e preparar pacote atualizado com GUID/link.
2. [pendente] Instalar em release isolado, conferir hashes e trocar/reiniciar somente o painel com rollback existente.
3. [pendente] Verificar health, ativos e contrato; registrar evidência, converter plano em atividade e commit/push.

Compatibilidade: a main contém leitura agregada nova; fontes atuais ainda não oferecem a rota. Manter snapshots fixos anteriores quando o agregado inicial retorna 404, junto ao aviso explícito de cobertura global incompleta. Não apresentar ausência de dados como prova de fila vazia. Seleção individual permanece inalterada.

Validação: regressão de controlador antigo, suíte Panel.Tests e publish Release; hash DLL/CSS remoto, health interno/público e 401 anônimo. Login/aceitação visual de produção não presumidos por esses checks.
