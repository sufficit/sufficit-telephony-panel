#!/usr/bin/env python3
"""Narrow ACD monitoring rollout. No Asterisk/AMI/dialplan operations.

Run the explicit phase on its designated host. Keep credentials out of stdout.
Schema additions are backward-compatible and are never automatically dropped.
"""
import hashlib
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request

RELEASE = 'acd-board-20260912-2010'
CONTEXT = 'd21cfb049d37473b837c67591a26feed'
NODE = '01a091d2251c7b128c3ff0529ed1a192'
BASE = 'http://172.19.254.250:18189'
SNAPSHOT = '/api/acd/contexts/' + CONTEXT + '/nodes/' + NODE + '/snapshot'
BACKUP = Path('/var/backups/sufficit-acd-board') / RELEASE
GOOGLE_CONFIG = Path('/etc/sufficit-acd-google-pilot')
GOOGLE_SERVICE = 'sufficit-acd-google-pilot-controller'
GOOGLE_DROPIN = Path('/etc/systemd/system/' + GOOGLE_SERVICE + '.service.d/60-board-monitor.conf')
PANEL_DROPIN = Path('/etc/systemd/system/sufficit-telephony-panel.service.d/60-acd-board.conf')


def run(*args):
    return subprocess.check_output(args, stderr=subprocess.PIPE).decode('utf-8').strip()


def private(path, text, group=None):
    path = Path(path)
    with path.open('x') as output:
        output.write(text)
    path.chmod(0o640 if group else 0o600)
    if group:
        shutil.chown(str(path), user='root', group=group)


def begin():
    BACKUP.mkdir(parents=True, mode=0o700, exist_ok=False)


def request(key, path=SNAPSHOT, method='GET'):
    req = urllib.request.Request(BASE + path, method=method,
                                 headers={'Authorization': 'Bearer ' + key})
    try:
        with urllib.request.urlopen(req, timeout=4) as response:
            body = response.read(1048577)
            if len(body) > 1048576:
                raise RuntimeError('Oversized snapshot')
            return response.status, json.loads(body)
    except urllib.error.HTTPError as error:
        return error.code, None


def snapshot(key):
    code, body = request(key)
    if code != 200 or body['contextId'] != CONTEXT or body['nodeId'] != NODE:
        raise RuntimeError('Snapshot identity/status mismatch')
    if not body['controllerConnected'] or body['observerOnly']:
        raise RuntimeError('Controller not observing')
    return body


def monitoring_checks(key):
    body = snapshot(key)
    assert isinstance(body.get('visits'), list), 'Visit export missing'
    assert request(key, SNAPSHOT.rsplit('/', 1)[0] + '/queues')[0] == 403
    assert request(key, SNAPSHOT, 'PUT')[0] == 403
    assert request(key, SNAPSHOT.replace(CONTEXT, '1' * 32))[0] == 404
    assert request('invalid-credential')[0] == 401
    print(json.dumps({'snapshot': 200, 'queues': len(body['queues']),
                      'visits': len(body['visits']), 'write': 403,
                      'management': 403, 'wrongContext': 404, 'anonymous': 401}))


def schema():
    def sql(query):
        command = ['mariadb', '--no-defaults', '--socket=/run/sufficit-acd-db/mysql.sock',
                   '-uroot', '-N', '-B', 'sufficit_acd_shared']
        result = subprocess.run(command, input=query.encode(), stdout=subprocess.PIPE,
                                stderr=subprocess.PIPE, check=True)
        return result.stdout.decode('utf-8').strip()
    columns = sql("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='sufficit_acd_shared' AND ((TABLE_NAME='telp_acd_members' AND COLUMN_NAME IN ('dynamic','logged_in','pause_reason','auto_paused','failure_count','answered_count','last_answered_at')) OR (TABLE_NAME='telp_acd_visits' AND COLUMN_NAME='answered_at'))")
    assert columns == '0', 'Migration already applied or partially applied; inspect before proceeding'
    assert sql('SELECT COUNT(*) FROM telp_acd_visits WHERE ended_at IS NULL') == '0', 'Active visits'
    dump = os.environ.get('ACD_DUMP_BINARY') or shutil.which('mariadb-dump') or shutil.which('mysqldump')
    assert dump and Path(dump).is_file(), 'Verified database dump tool required'
    begin()
    with (BACKUP / 'acd-shared.sql').open('xb') as output:
        os.chmod(str(BACKUP / 'acd-shared.sql'), 0o600)
        subprocess.run([dump, '--no-defaults', '--socket=/run/sufficit-acd-db/mysql.sock',
                        '-uroot', '--single-transaction', 'sufficit_acd_shared'],
                       stdout=output, stderr=subprocess.PIPE, check=True)
    before = sql('SELECT HEX(id),HEX(context_id),HEX(title),enabled,max_wait_seconds,max_waiting,HEX(destination),HEX(options_json) FROM telp_acd_queues ORDER BY id')
    private(BACKUP / 'catalog.sha256', hashlib.sha256(before.encode()).hexdigest())
    sql("SET SESSION lock_wait_timeout=3; ALTER TABLE telp_acd_members ADD COLUMN dynamic BOOLEAN NOT NULL DEFAULT FALSE, ADD COLUMN logged_in BOOLEAN NOT NULL DEFAULT FALSE, ADD COLUMN pause_reason VARCHAR(100) NOT NULL DEFAULT '', ADD COLUMN auto_paused BOOLEAN NOT NULL DEFAULT FALSE, ADD COLUMN failure_count INT NOT NULL DEFAULT 0, ADD COLUMN answered_count BIGINT NOT NULL DEFAULT 0, ADD COLUMN last_answered_at DATETIME(6) NULL;")
    sql("SET SESSION lock_wait_timeout=3; ALTER TABLE telp_acd_visits DROP CONSTRAINT ck_acd_visit_status, ADD CONSTRAINT ck_acd_visit_status CHECK (status IN ('greeting','waiting','announcing','offering','connected','completed','timed_out','abandoned','left','rejected','interrupted','empty','media_failed')), ADD COLUMN answered_at DATETIME(6) NULL;")
    after = sql('SELECT HEX(id),HEX(context_id),HEX(title),enabled,max_wait_seconds,max_waiting,HEX(destination),HEX(options_json) FROM telp_acd_queues ORDER BY id')
    assert before == after, 'Queue configuration changed during migration'
    print(json.dumps({'schema007': 'applied', 'queueConfigurations': 'unchanged', 'backup': str(BACKUP)}))


def google_rollback():
    assert (BACKUP / 'google-before.json').exists(), 'Missing rollback baseline'
    GOOGLE_DROPIN.unlink()
    run('systemctl', 'daemon-reload')
    run('systemctl', 'restart', GOOGLE_SERVICE)
    print('Controller rolled back; additive schema and private key preserved')


def google():
    release = Path('/opt/sufficit/acd-google-pilot/releases') / RELEASE
    assert (release / 'Sufficit.Telephony.ACD.Node.dll').is_file()
    assert not GOOGLE_DROPIN.exists()
    environment = dict(line.split('=', 1) for line in (GOOGLE_CONFIG / 'controller.env').read_text().splitlines()
                       if '=' in line and not line.startswith('#'))
    assert environment['ACD_ENABLE_AGENT_DISPATCH'] == '0'
    assert environment['ACD_MANAGEMENT_READ_ONLY'] == '1'
    assert 'ACD_QUEUE_MEDIA_FILE' not in environment
    key = (GOOGLE_CONFIG / 'management-key').read_text().strip()
    body = snapshot(key)
    assert sum(q['waiting'] + q.get('connected', 0) for q in body['queues']) == 0, 'Active pilot visits'
    ari = json.loads(run('curl', '-fsS', '--max-time', '4', '--config',
                         str(GOOGLE_CONFIG / 'ari-curl.conf'),
                         environment['ACD_ARI_URL'].rstrip('/') + '/applications/' + environment['ACD_ARI_APPLICATION']))
    assert not ari['channel_ids'] and not ari['bridge_ids'], 'Active ARI pilot objects'
    baseline = {'asterisk': run('pgrep', '-x', 'asterisk'),
                'controller': run('systemctl', 'show', GOOGLE_SERVICE, '-p', 'MainPID').split('=', 1)[1],
                'queueIds': sorted(q['queueId'] for q in body['queues'])}
    begin()
    private(BACKUP / 'google-before.json', json.dumps(baseline))
    monitoring = GOOGLE_CONFIG / 'monitoring-key'
    private(monitoring, secrets.token_urlsafe(48) + '\n', 'suffacdgp')
    GOOGLE_DROPIN.parent.mkdir(exist_ok=True)
    private(GOOGLE_DROPIN, '[Service]\nEnvironment=ACD_MONITORING_KEY_FILE=' + str(monitoring) +
            '\nExecStart=\nExecStart=/usr/bin/dotnet ' + str(release / 'Sufficit.Telephony.ACD.Node.dll') + '\n')
    switched = False
    try:
        # Last admission check immediately before replacing only the isolated controller.
        latest = snapshot(key)
        assert sum(q['waiting'] + q.get('connected', 0) for q in latest['queues']) == 0
        run('systemctl', 'daemon-reload')
        switched = True
        run('systemctl', 'restart', GOOGLE_SERVICE)
        for attempt in range(20):
            try:
                current = snapshot(monitoring.read_text().strip())
                break
            except Exception:
                if attempt == 19:
                    raise
                time.sleep(1)
        assert sorted(q['queueId'] for q in current['queues']) == baseline['queueIds']
        assert run('pgrep', '-x', 'asterisk') == baseline['asterisk']
        monitoring_checks(monitoring.read_text().strip())
        print(json.dumps({'controller': run('systemctl', 'show', GOOGLE_SERVICE, '-p', 'MainPID').split('=', 1)[1],
                          'asteriskUnchanged': baseline['asterisk'], 'backup': str(BACKUP)}))
    except Exception:
        if switched:
            google_rollback()
        else:
            GOOGLE_DROPIN.unlink()
        raise


def panel_rollback():
    prior = (BACKUP / 'panel-before.txt').read_text().strip()
    assert prior.startswith('/opt/sufficit-telephony-panel/releases/') and Path(prior).is_dir()
    current = Path('/opt/sufficit-telephony-panel/current')
    temporary = current.with_name('current-rollback-' + RELEASE)
    temporary.symlink_to(prior)
    os.replace(str(temporary), str(current))
    PANEL_DROPIN.unlink()
    run('systemctl', 'daemon-reload')
    run('systemctl', 'restart', 'sufficit-telephony-panel')
    print('Panel release/configuration rolled back; state preserved')


def panel():
    release = Path('/opt/sufficit-telephony-panel/releases') / RELEASE
    assert (release / 'Sufficit.Telephony.Panel.dll').is_file()
    assert not PANEL_DROPIN.exists()
    key = Path('/etc/sufficit/telephony-panel/acd-google-monitor.key')
    assert key.is_file() and key.stat().st_mode & 0o037 == 0
    monitoring_checks(key.read_text().strip())
    begin()
    current = Path('/opt/sufficit-telephony-panel/current')
    private(BACKUP / 'panel-before.txt', str(current.resolve()) + '\n')
    settings = {'Node': 'google-voip', 'NodeId': NODE,
                'ContextIds__0': CONTEXT, 'BaseUrl': BASE + '/', 'KeyFile': str(key)}
    # .NET Guid configuration accepts canonical D UUIDs, unlike legacy JSON visit IDs.
    import uuid
    settings['NodeId'] = str(uuid.UUID(NODE))
    settings['ContextIds__0'] = str(uuid.UUID(CONTEXT))
    private(PANEL_DROPIN, '[Service]\n' + ''.join('Environment=Panel__AcdSources__0__' + k + '=' + v + '\n' for k, v in settings.items()))
    temporary = current.with_name('current-' + RELEASE)
    temporary.symlink_to(release)
    os.replace(str(temporary), str(current))
    try:
        run('systemctl', 'daemon-reload')
        run('systemctl', 'restart', 'sufficit-telephony-panel')
        for attempt in range(20):
            try:
                assert run('curl', '-fsS', '--max-time', '3', '--unix-socket',
                           '/run/sufficit-telephony-panel/panel.sock', 'http://localhost/health')
                break
            except Exception:
                if attempt == 19:
                    raise
                time.sleep(1)
        print(json.dumps({'panelRelease': str(current.resolve()), 'backup': str(BACKUP)}))
    except Exception:
        panel_rollback()
        raise


def probe():
    environment = {'PATH': '/usr/sbin:/usr/bin:/bin', 'DOTNET_CLI_TELEMETRY_OPTOUT': '1'}
    for line in PANEL_DROPIN.read_text().splitlines():
        if line.startswith('Environment=Panel__AcdSources__'):
            key, value = line[len('Environment='):].split('=', 1)
            environment[key] = value
    subprocess.run(['/usr/sbin/runuser', '-u', 'sufftelpanel', '--', '/usr/bin/dotnet',
                    '/opt/sufficit-telephony-panel/releases/' + RELEASE + '-verification/AcdSourceProbe.dll'],
                   env=environment, check=True)


if __name__ == '__main__':
    action = sys.argv[1]
    host = socket.gethostname().split('.')[0]
    expected = {'schema': 'eveo-voip', 'google': 'google-voip', 'google-rollback': 'google-voip',
                'panel': 'eveo-apps', 'panel-rollback': 'eveo-apps', 'verify': 'eveo-apps', 'probe': 'eveo-apps'}
    assert host == expected[action], 'Wrong deployment host'
    os.umask(0o077)
    if action == 'verify':
        monitoring_checks(Path('/etc/sufficit/telephony-panel/acd-google-monitor.key').read_text().strip())
    else:
        {'schema': schema, 'google': google, 'google-rollback': google_rollback,
         'panel': panel, 'panel-rollback': panel_rollback, 'probe': probe}[action]()
