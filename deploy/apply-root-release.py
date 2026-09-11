#!/usr/bin/env python3
"""Activate the root-domain release; rollback configuration in memory on failure."""
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import sys

release = Path(sys.argv[1])
expected = sys.argv[2]
assert release.parent == Path('/opt/sufficit-telephony-panel/releases')
assert release.is_dir() and release.name.startswith('board-page-')
current = Path('/opt/sufficit-telephony-panel/current')
previous = current.resolve()
assert previous.name == 'channel-filter-20260910-2206'
assert hashlib.sha256((release / 'Sufficit.Telephony.Panel.dll').read_bytes()).hexdigest() == expected
site = Path('/etc/nginx/sites-available/sufficit-panel-domain')
snippet = Path('/etc/nginx/snippets/sufficit-telephony-panel.conf')
assert hashlib.sha256(site.read_bytes()).hexdigest() == '6a8293f9d068571746a4928fe409b66c5c4203fbdd40ea5654ecf04f4a8098e5'
assert hashlib.sha256(snippet.read_bytes()).hexdigest() == '2d9abd26d6c22fc76256b827a130bf6b8d74061cdc73161254a8931b6a0ac9c2'
dropin = Path('/etc/systemd/system/sufficit-telephony-panel.service.d/root-path.conf')
assert not dropin.exists()
original_site, original_snippet = site.read_bytes(), snippet.read_bytes()
for config in previous.glob('appsettings*.json'):
    shutil.copy2(config, release / config.name)

def run(*args):
    subprocess.run(args, check=True, timeout=45)

def activate(path):
    staging = current.with_name('current-root-migration')
    assert not staging.exists() and not staging.is_symlink()
    staging.symlink_to(path)
    os.replace(staging, current)

activated = False
try:
    site.write_bytes((release / 'deploy/nginx-panel-domain.conf').read_bytes())
    snippet.write_bytes((release / 'deploy/nginx.conf').read_bytes())
    run('nginx', '-t')
    dropin.parent.mkdir(exist_ok=True)
    shutil.copy2(release / 'deploy/root-path.conf', dropin)
    activate(release)
    activated = True
    run('systemctl', 'daemon-reload')
    run('systemctl', 'restart', 'sufficit-telephony-panel')
    run('systemctl', 'is-active', '--quiet', 'sufficit-telephony-panel')
    run('systemctl', 'reload', 'nginx')
except Exception:
    site.write_bytes(original_site)
    snippet.write_bytes(original_snippet)
    if dropin.exists():
        dropin.unlink()
    if activated:
        activate(previous)
    run('nginx', '-t')
    run('systemctl', 'daemon-reload')
    if activated:
        run('systemctl', 'restart', 'sufficit-telephony-panel')
    run('systemctl', 'reload', 'nginx')
    raise
print('Root-domain release activated; no PBX/collector restart')
