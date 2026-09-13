#!/usr/bin/env python3
"""Add a snapshot-only key to an idle test controller. Never modify the PBX."""
import importlib.util
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import subprocess
import time
import urllib.request
import urllib.error
import http.client
import sys

host = socket.gethostname().split('.')[0]
assert os.geteuid() == 0 and host in ('eveo-voip', 'apoint-voip')
spec = importlib.util.spec_from_file_location('presence', '/tmp/sufficit-acd-registration-stage-20260913/install-agent-presence.py')
i = importlib.util.module_from_spec(spec)
spec.loader.exec_module(i)
env = dict(line.split('=', 1) for line in (i.CONFIG / 'controller.env').read_text().splitlines()
           if line and not line.startswith('#') and '=' in line)
key_path = i.CONFIG / 'panel-monitoring-key'
dropin = Path('/etc/systemd/system/sufficit-acd-lab-controller.service.d/70-panel-monitoring.conf')
resume = sys.argv[1:] == ['--resume-rolled-back']
failed = dropin.with_suffix('.failed')
expected_dropin = '[Service]\nEnvironment=ACD_MONITORING_KEY_FILE=' + str(key_path) + '\n'
assert not dropin.exists(), 'Already installed; inspect instead of retrying'
assert (resume and key_path.is_file() and failed.read_text() == expected_dropin) or (not resume and not key_path.exists())
i.idle(env)
pbx = i.run('pidof', 'asterisk')
before = {p: i.digest(p) for p in (i.CONFIG / 'controller.env', Path(env['ACD_AGENT_ROUTES_FILE']), Path(env['ACD_AGENT_IDENTITIES_FILE']))}
os.umask(0o077)
if not resume:
    with key_path.open('x') as output:
        output.write(secrets.token_urlsafe(48) + '\n')
shutil.chown(key_path, user='root', group='sufficit-acd')
key_path.chmod(0o640)
dropin.parent.mkdir(exist_ok=True)
with dropin.open('x') as output:
    output.write(expected_dropin)
base = 'http://' + ('172.19.2.104' if host == 'eveo-voip' else '172.19.1.104') + ':18189'
path = '/api/acd/contexts/' + i.CONTEXT + '/nodes/' + env['ACD_NODE_UUID'].replace('-', '')
key = key_path.read_text().strip()
def request(suffix, method='GET'):
    class PrivateConnection(http.client.HTTPConnection):
        def connect(self):
            self.sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            self.sock.settimeout(4)
            self.sock.connect('/run/sufficit-acd-management/api.sock')
    connection = PrivateConnection('localhost', timeout=4)
    try:
        connection.request(method, path + suffix, headers={'Authorization': 'Bearer ' + key})
        response = connection.getresponse()
        body = response.read(4 * 1024 * 1024)
        return response.status, json.loads(body) if response.status == 200 else None
    finally:
        connection.close()
try:
    i.run('systemctl', 'daemon-reload')
    i.run('systemctl', 'restart', i.SERVICE)
    for attempt in range(20):
        try:
            status, body = request('/snapshot')
            assert status == 200 and body['controllerConnected']
            break
        except Exception:
            if attempt == 19: raise
            time.sleep(1)
    assert request('/queues')[0] == 403 and request('/snapshot', 'PUT')[0] == 403
    assert i.run('pidof', 'asterisk') == pbx
    assert all(i.digest(p) == h for p, h in before.items())
    print(json.dumps(dict(host=host, snapshot=200, management=403, write=403, asteriskPid=pbx,
        keyFile=str(key_path), agents=len(body.get('agents', [])))))
except Exception:
    # Only this new drop-in is rolled back; the private key remains for inspection.
    dropin.rename(dropin.with_suffix('.failed'))
    i.run('systemctl', 'daemon-reload')
    i.run('systemctl', 'restart', i.SERVICE)
    raise
