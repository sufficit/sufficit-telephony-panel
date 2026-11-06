# Normalização de 0 + DDD + telefone

Pedido: tratar o destino 021964032745 da imagem FLUX. O normalizador compartilhado
agora remove exatamente um zero inicial quando o restante corresponde ao formato
nacional de DDD + fixo/móvel, apresentando +5521964032745. Também cobre
061992053002 → +5561992053002, antes preservado. Aplica-se a origem e destino;
os valores recebidos continuam intactos para diagnóstico.

Regressão reproduzida antes da correção; suíte Release passou 1330 checks após
alteração. Cobertura inclui fixos, móveis, DDD 55, idempotência, originais,
ramais da imagem, 00, CSP, locais, formatos inválidos e prefixo + explícito.
Diff --check passou. Aviso NU1903 preexistente em SignalR.Common 10.0.11.
Sem mudança visual; não foi necessário teste de navegador para a função pura.

Código enviado à main conforme regra do mantenedor. Publicação segue autorização
na sessão e será registrada separadamente com evidência da versão ativa.
