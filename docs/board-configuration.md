# Configurar mesa: troncos, ramais e filtros

Acesse **Telefonia ao vivo → Configurar mesa**. As regras são compartilhadas entre
os gerentes deste painel. Elas organizam apenas a Mesa de operação; não configuram
troncos no Asterisk, não mudam permissões nem removem canais das abas de diagnóstico.

## Como usar

1. Clique em **Nova regra** e dê um nome ao cartão, por exemplo “WhatsApp”.
2. Escolha **Tronco**, **Ramal** ou **Ocultar da mesa**.
3. Informe os identificadores exatos, um por linha. Para limitar ao servidor,
   preencha seu identificador observado; vazio significa todos os servidores.
4. Clique em **Aplicar ao rascunho** e confira a prévia.
5. Clique em **Salvar configuração** para publicar para os gerentes.

Cancelar edição descarta apenas o formulário aberto. Remover exige confirmação e
altera o rascunho; a mesa não muda até salvar. Recarregar exige confirmação para
descartar o rascunho e buscar a versão salva. Trocar abas locais mantém o rascunho;
atualizar o navegador ou encerrar a sessão pode perdê-lo.

**Preparar exemplo WhatsApp** preenche um formulário com os dois identificadores
do exemplo FOP; não importa o catálogo e não publica nada automaticamente:

```text
PJSIP/gateway-whatsapp-official
PJSIP/gateway-whatsapp-quepasa-inbound
```

Esse padrão segue o agrupamento de aliases em `writeTrunks` no controlador legado
`sufficit-endpoints/src/Controllers/Telephony/FlashOperatorPanelController.cs`.
Nenhuma credencial, rota privilegiada ou código de autenticação FOP foi reutilizado.

Após autorização explícita, os 14 grupos estáticos do FOP foram incluídos no Eveo.
Veja [catálogo, adaptações e procedimento da importação](fop-rule-import.md).
Isto foi uma importação pontual; salvar/deploy não sincroniza novamente o FOP.

## Correspondência e prioridade

- A primeira regra ativa que combinar identificador e servidor vence.
- **Subir/Descer** muda essa prioridade no rascunho. Regra desativada não participa.
- `SIP/in-dtr-67` é exato; `SIP/in-dtr-*` aceita explicitamente aquele prefixo.
- Diferencia maiúsculas/minúsculas. SIP e PJSIP não são intercambiáveis.
- Não há regex, wildcard inicial ou inferência por nomes como `in-`/`out-`.
- Somente o sufixo hexadecimal de oito posições de um canal observado é removido
  para a comparação. Identificadores de peers não perdem sufixos numerados.
- Aliases combinados formam um cartão por regra e servidor. Servidores diferentes
  nunca são fundidos. Observações repetidas são deduplicadas pelo ID.
- Uma regra pode separar parte dos aliases de um cartão antigo do portal.
- **Mostrar recursos sem regra correspondente** vem ativado. Desativar oculta os
  recursos não classificados apenas na mesa; não apaga eventos nem tira acesso.

O filtro de empresa continua aplicado **antes** das regras. Nenhuma regra amplia
o conjunto autorizado ou fabrica estado/ramal que não esteja nele. A prévia usa
esse conjunto, sem os filtros temporários de texto/estado/servidor. Mostra até 12
cartões, priorizando os grupos configurados; a mesa conserva seus limites próprios.
Estados continuam sendo evidências observadas, não garantia de disponibilidade.

## Persistência e acesso

A opção compartilhada `RetainSeenResources` vem ativa; seu inventário fica em um
[arquivo separado](retained-resources.md). Alterar a opção mantém as regras FOP.

- Leitura e gravação revalidam a sessão `manager` no servidor, com o cache de
  revogação já utilizado no painel. Nenhum novo privilégio Identity foi criado.
- Arquivo: `/var/lib/sufficit-telephony-panel/board-rules.json`, fora dos releases.
  Pode ser configurado por `Panel:BoardRulesFile` para ambientes isolados.
- Diretório existente do systemd pertence a `sufftelpanel`, modo 0700. Arquivo novo
  recebe modo 0600; substituição atômica por arquivo temporário no mesmo diretório.
- Configuração inicial vazia, mostrando todos os recursos autorizados. O arquivo
  só é criado ao salvar; nenhuma importação ou regra default é aplicada no deploy.
- IDs e revisões UUIDv7 no formato N (32 caracteres), sem IDs inteiros de negócio.
- Até 100 regras, 64 padrões por regra, 512 padrões no total, nomes de 80 caracteres
  e arquivo de até 1 MB. Não há cache ou conexão AMI adicional por regra.
- Revisão otimista: se outro gerente salvar primeiro, a gravação antiga é recusada.
  Recarregue e reaplique a alteração; não há overwrite silencioso.
- Mesa aberta recebe regras salvas no próximo ciclo autorizado, aproximadamente 2s.
  O editor conserva seu rascunho até recarregar, para não perder trabalho em curso.
- Arquivo corrompido é sinalizado e não substituído automaticamente. A mesa mantém
  a última configuração da sessão (ou defaults se ainda não carregou).
- **Escopo atual: uma instância Eveo.** Não copiar este arquivo para múltiplos writers
  sem adicionar armazenamento compartilhado e concorrência distribuída.

## Verificação

`tests/BoardConfigurationChecks.cs`: aliases, precedência, ocultação, isolamento de
nós, autorização anônima negada, limites de padrões, gravação/releitura, modo 0600,
conflito concorrente e corrupção sem overwrite.

`tests/board-settings.browser.cjs`: formulário, validação, editar/cancelar,
salvar, agrupamento, ocultação, diagnóstico preservado e descartar/recarregar.
Capturas `.impeccable/review/settings-*.png` são da fixture Development, sem
credencial nem gravação real. Persistência real é testada em diretório temporário.
