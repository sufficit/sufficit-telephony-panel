#!/usr/bin/env python3
"""Apply only the panel token-format rule; never copy a host's full configuration."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile

CLIENT = "sufficit-telephony-panel"


def merge(configuration, rollback=False):
    result = json.loads(json.dumps(configuration))
    options = result.setdefault("Sufficit", {}).setdefault("Identity", {}).setdefault("Tokens", {})
    clients = options.setdefault("AccessTokenFormatsByClient", {})
    current = clients.get(CLIENT)
    if current not in (None, "Jwt"):
        raise ValueError("Unexpected panel rule; refuse to overwrite it")
    if rollback:
        clients.pop(CLIENT, None)
    else:
        clients[CLIENT] = "Jwt"
    return result


def apply(path, expected_sha, rollback=False):
    path = Path(path)
    if path.is_symlink() or not path.is_file():
        raise ValueError("Configuration must be an existing regular file")
    original = path.read_bytes()
    if hashlib.sha256(original).hexdigest() != expected_sha:
        raise ValueError("Configuration changed since preflight")
    config = json.loads(original.decode("utf-8-sig"))
    updated = merge(config, rollback)
    if updated == config:
        return expected_sha
    stat = path.stat()
    content = (json.dumps(updated, indent=2, ensure_ascii=False) + "\n").encode()
    descriptor, temporary = tempfile.mkstemp(prefix=".panel-token-format-", dir=path.parent)
    try:
        with os.fdopen(descriptor, "wb") as output:
            os.fchmod(output.fileno(), stat.st_mode & 0o777)
            os.fchown(output.fileno(), stat.st_uid, stat.st_gid)
            output.write(content)
            output.flush()
            os.fsync(output.fileno())
        if path.read_bytes() != original:
            raise ValueError("Configuration changed during merge")
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)
    return hashlib.sha256(path.read_bytes()).hexdigest()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path")
    parser.add_argument("expected_sha")
    parser.add_argument("--rollback", action="store_true")
    args = parser.parse_args()
    print(apply(args.path, args.expected_sha, args.rollback))
