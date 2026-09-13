#!/usr/bin/env python3
"""Validate the private facade as Panel's Unix user before switching its source."""
import json
import os
from pathlib import Path
import pwd
import shutil
import socket
import subprocess
import time
import urllib.request
import urllib.error

DROPINS = Path('/etc/systemd/system/sufficit-telephony-panel.service.d')
TARGET = DROPINS / '80-google-registration.conf'
KEY = Path('/etc/sufficit/telephony-panel/google-registration-monitor.key')
CONTEXT = 'd21cfb049d37473b837c67591a26feed'
NODE = '01a091d2251c7b128c3ff0529ed1a192'


def run(*args, **kwargs):
    return subprocess.run(args, check=True, capture_output=True, text=True,
                          timeout=45, **kwargs).stdout.strip()


def probe():
    with KEY.open() as source: key = source.read().strip()
    base = 'http://172.19.254.250:18190/api/acd/contexts/'+CONTEXT+'/nodes/'+NODE
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self, *args): return None
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
    def request(suffix, method='GET', authenticated=True):
        headers = {'Authorization': 'Bearer '+key} if authenticated else {}
        req = urllib.request.Request(base+suffix, headers=headers, method=method)
        try:
            with opener.open(req, timeout=5) as response:
                return response.status, json.loads(response.read(4*1024*1024+1))
        except urllib.error.HTTPError as error:
            return error.code, None
    status, body = request('/snapshot')
    assert status == 200 and body['controllerConnected']
    assert {a['extension'] for a in body['agents']} == {'0000006001','0000006007'}
    assert all(a['registrationHistory'] for a in body['agents'])
    assert request('/snapshot', authenticated=False)[0] == 401
    assert request('/queues')[0] == 404
    assert request('/snapshot', 'POST')[0] == 501
    print(json.dumps({'snapshot':status, 'agents':len(body['agents']),
        'historyCounts':{a['extension']:len(a['registrationHistory']) for a in body['agents']}}))


def main():
    import sys
    if sys.argv[1:] == ['--probe']:
        probe(); return
    assert os.geteuid() == 0 and socket.gethostname().split('.')[0] == 'eveo-apps'
    source = Path('/tmp/80-google-registration.conf')
    assert not TARGET.exists() and source.is_file() and KEY.is_file()
    user = pwd.getpwnam('sufftelpanel')
    os.chown(KEY, 0, user.pw_gid); KEY.chmod(0o640)
    print(run('runuser','-u','sufftelpanel','--','python3',str(Path(__file__).resolve()),'--probe'))
    env = dict(os.environ)
    for path in list(sorted(DROPINS.glob('*.conf'))) + [source]:
        for line in path.read_text().splitlines():
            if line.startswith('Environment=Panel__AcdSources__'):
                name, value = line[len('Environment='):].split('=',1)
                env[name] = value
    print(run('runuser','-u','sufftelpanel','-p','--','dotnet',
        '/tmp/monitoring-probe-20260913/AcdSourceProbe.dll',env=env))
    previous = run('systemctl','show','sufficit-telephony-panel','-p','MainPID')
    shutil.copy2(source, TARGET)
    try:
        run('systemctl','daemon-reload')
        run('systemctl','restart','sufficit-telephony-panel')
        for attempt in range(20):
            try:
                health = run('curl','-fsS','--max-time','3','--unix-socket',
                    '/run/sufficit-telephony-panel/panel.sock','http://localhost/health')
                break
            except subprocess.CalledProcessError:
                if attempt == 19: raise
                time.sleep(1)
        print(json.dumps({'previous':previous, 'current':run('systemctl','show',
            'sufficit-telephony-panel','-p','MainPID'), 'health':health,'dropin':str(TARGET)}))
    except Exception:
        TARGET.rename(TARGET.with_suffix('.failed-'+time.strftime('%Y%m%d%H%M%S')))
        run('systemctl','daemon-reload')
        run('systemctl','restart','sufficit-telephony-panel')
        raise


if __name__ == '__main__': main()
