#!/usr/bin/env python3
"""Atomically switch a staged panel release, preserving configuration and state."""
import hashlib
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import sys
import time

def run(*args):
    return subprocess.check_output(args, stderr=subprocess.PIPE).decode().strip()

assert socket.gethostname().split('.')[0] == 'eveo-apps'
release_id, expected_id = sys.argv[1:3]
assert all(re.fullmatch(r'[a-zA-Z0-9-]+', value) for value in (release_id, expected_id))
root = Path('/opt/sufficit-telephony-panel')
current = root / 'current'
previous = root / 'releases' / expected_id
release = root / 'releases' / release_id
assert current.resolve() == previous and release != previous
assert (release / 'Sufficit.Telephony.Panel.dll').is_file()
assert (release / 'wwwroot').is_dir()
os.umask(0o077)
backup = Path('/var/backups/sufficit-telephony-panel') / release_id
backup.mkdir(parents=True, mode=0o700, exist_ok=False)
manifest = {'previous': str(previous), 'release': str(release),
            'sha256': hashlib.sha256((release / 'Sufficit.Telephony.Panel.dll').read_bytes()).hexdigest()}
with (backup / 'release.json').open('x') as output:
    json.dump(manifest, output)

def switch(target):
    temporary = root / ('current-' + release_id)
    temporary.symlink_to(target)
    os.replace(temporary, current)
    run('systemctl', 'restart', 'sufficit-telephony-panel')

try:
    switch(release)
    for attempt in range(20):
        try:
            run('curl', '-fsS', '--max-time', '3', '--unix-socket',
                '/run/sufficit-telephony-panel/panel.sock', 'http://localhost/health')
            break
        except Exception:
            if attempt == 19:
                raise
            time.sleep(1)
    print(json.dumps(manifest))
except Exception:
    switch(previous)
    print('Panel rolled back to previous release; configuration/state unchanged')
    raise
