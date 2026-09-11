#!/usr/bin/env python3
"""Apply the dedicated client manifest using an operator-provided short-lived token file."""
import argparse
import json
import pathlib
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('--token-file', required=True)
parser.add_argument('--apply', action='store_true')
args = parser.parse_args()
token = pathlib.Path(args.token_file).read_text().strip()
body = pathlib.Path(__file__).with_name('identity-manifest.json').read_bytes()
for action in (['inventory', 'preview', 'apply'] if args.apply else ['inventory', 'preview']):
    request = urllib.request.Request('https://identity.sufficit.com.br/api/provisioning/manifest/' + action,
        data=body, headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token})
    with urllib.request.urlopen(request, timeout=15) as response:
        result = json.load(response)
    print(action, json.dumps(result))
