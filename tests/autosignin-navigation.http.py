"""Local document-navigation regression. Does not follow redirects or contact Identity."""
import http.client

def request(path='/', **headers):
    client = http.client.HTTPConnection('127.0.0.1', 5169, timeout=10)
    client.request('GET', path, headers={'Accept': 'text/html', **headers})
    response = client.getresponse()
    result = response.status, dict(response.getheaders()), response.read()
    client.close()
    return result

legacy = '__Secure-Sufficit.Telephony.Panel.SignIn'
marker = '__Host-Sufficit.Telephony.Panel.SignIn'
for cookie in ['', legacy+'=1', marker+'=1', '__Host-Sufficit.Telephony.Panel=expired; '+legacy+'=1']:
    status, headers, _ = request('/board?contextid=abc', Cookie=cookie)
    assert status == 302 and headers['Location'].startswith('/login?'), (status, headers.get('Location'))
    assert 'automatic=1' in headers['Location'] and 'contextid%3Dabc' in headers['Location']
    assert marker not in headers.get('Set-Cookie', ''), 'Pending navigation must not write a completion marker'
for _ in range(2):
    status, headers, _ = request(Cookie=legacy+'=1')
    assert status == 302 and 'login=failed' not in headers['Location'], 'Repeated pending navigation must not fail'
status, headers, _ = request(Cookie=marker+'=completed')
assert status == 302 and headers['Location'] == '/?login=failed', 'Actual completed-login loop remains bounded'
for key, value in [('Sec-Purpose', 'prefetch;prerender'), ('Purpose', 'prefetch'), ('X-Purpose', 'prerender')]:
    status, headers, _ = request(**{key:value})
    assert status == 200 and 'Location' not in headers, 'Speculation must not start Identity authentication'
for path in ['/?signedout=1', '/?login=failed', '/?login=denied']:
    status, headers, _ = request(path)
    assert status == 200 and 'Location' not in headers, 'Explicit outcomes/logout remain visible without loops'

context = '11111111-1111-7111-8111-111111111111'
embedded = f'/board?embed=hermes&contextid={context}'
status, headers, _ = request(embedded, **{'Sec-Fetch-Dest': 'iframe'})
assert status == 200 and 'Location' not in headers
assert 'X-Frame-Options' not in headers
assert set(headers['Content-Security-Policy'].split()) == {
    'frame-ancestors', 'https://chatwoot.sufficit.com.br', 'https://chat.sufficit.com.br',
    'https://hermes.sufficit.com.br', 'https://eveo-chatwoot.sufficit.com.br',
}
for path in (
    f'/board?contextid={context}', '/board?embed=hermes',
    '/board?embed=hermes&contextid=invalid', f'{embedded}&contextid={context}',
    '/board?embed=hermes&contextid=00000000-0000-0000-0000-000000000000',
    embedded.replace('/board', '/'), embedded.replace('/board', '/api/fleet'),
):
    status, headers, _ = request(path, **{'Sec-Fetch-Dest': 'iframe'})
    assert headers.get('X-Frame-Options') == 'DENY', path
    assert 'sufficit.com.br' not in headers.get('Content-Security-Policy', ''), path
    if path.startswith('/api/fleet'):
        assert status == 401, 'Embedding never grants API access'

print('PASS: sign-in navigation, scoped Hermes iframe headers and anonymous API denial')
