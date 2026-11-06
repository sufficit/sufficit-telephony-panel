# Deploy da normalização 0 + DDD + telefone

Publicação como continuidade da correção autorizada nesta sessão, em 06/11/2026
às 12:18:33 BRT, somente no serviço sufficit-telephony-panel do eveo-apps.

- Código: 16dd467, enviado à main.
- Release ativa: /opt/sufficit-telephony-panel/releases/trunk-20261106-16dd467.
- Anterior preservada: /opt/sufficit-telephony-panel/releases/national-20261106-46686bc.
- Build novo via dotnet publish em Release, -m:1; transferência rsync conferida
  por checksum sem diferenças; ativação pelo helper release-panel.py sob flock.
- SHA-256 local e ativo: 830ceab7597b3688e9ef39b1ad5efa359851b41ff84465936319b90182f8a743.
- Manifesto de rollback: /var/backups/sufficit-telephony-panel/trunk-20261106-16dd467/release.json.

Suíte Release: 1330 checks; aviso NU1903 preexistente em SignalR.Common 10.0.11.
Serviço active/running, PID 1872662, NRestarts=0. Health local pelo socket e público
retornaram running; /board público retornou HTTP 200. Hash ativo corresponde ao
artefato. Configuração e estado preservados. Sem chamada real ou sessão autenticada
na validação; somente o painel reiniciado.

Rollback: helper oficial sob a mesma trava, destino national-20261106-46686bc,
atual esperada trunk-20261106-16dd467. Registro enviado à main.
