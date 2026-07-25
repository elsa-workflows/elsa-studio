import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const port = Number(process.env.EXTERNAL_AUTH_FIXTURE_PORT || 4178);
const directory = dirname(fileURLToPath(import.meta.url));
const browserHelperPath = resolve(
  directory,
  '../../../src/modules/Elsa.Studio.ExternalAuthentication.BlazorWasm/wwwroot/external-authentication.js');

const layout = body => `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width">
<title>External Authentication fixture</title>
<style>
body { color: #1f2937; background: #fff; font: 16px/1.5 system-ui, sans-serif; margin: 2rem; }
main { max-width: 64rem; }
label { display: block; margin-top: .75rem; }
input, select, button { font: inherit; margin: .25rem .5rem .5rem 0; padding: .5rem; }
button:focus-visible, input:focus-visible, select:focus-visible { outline: 3px solid #005fcc; outline-offset: 2px; }
table { border-collapse: collapse; width: 100%; }
th, td { border: 1px solid #6b7280; padding: .5rem; text-align: left; }
.external-methods { display: flex; flex-direction: column; align-items: flex-start; margin-top: 1rem; }
</style>
</head>
<body>${body}<script src="/external-authentication.js"></script></body>
</html>`;

const applicationScript = `
<script>
const key = 'elsa.external-authentication.tokens';
const renderLogin = () => {
  document.body.innerHTML = \`
    <main aria-labelledby="external-login-heading">
      <h1 id="external-login-heading">Sign in</h1>
      <p role="status">Choose an Elsa account or an enabled identity provider.</p>
      <section aria-labelledby="local-login-heading">
        <h2 id="local-login-heading">Elsa account</h2>
        <form id="local-login">
          <label for="username">User name</label>
          <input id="username" name="username" autocomplete="username">
          <label for="password">Password</label>
          <input id="password" name="password" type="password" autocomplete="current-password">
          <button type="submit">Sign in</button>
        </form>
      </section>
      <section class="external-methods" aria-label="External identity providers">
        <div><span role="status">Preferred</span><button type="button" data-external aria-label="Sign in with GitHub"><span aria-hidden="true">github</span><span>Sign in with GitHub</span></button></div>
        <button type="button" data-external aria-label="Sign in with Microsoft"><span aria-hidden="true">microsoft</span><span>Sign in with Microsoft</span></button>
        <button type="button" data-external aria-label="Sign in with Contoso"><span aria-hidden="true">identity provider</span><span>Sign in with Contoso</span></button>
      </section>
    </main>\`;
  document.querySelector('#local-login').onsubmit = event => {
    event.preventDefault();
    location.assign('/workflows');
  };
  document.querySelectorAll('[data-external]').forEach(button => button.onclick = async () => {
    const pkce = await window.elsaExternalAuthentication.createPkce();
    sessionStorage.setItem('elsa.external-authentication.pkce', JSON.stringify(pkce));
    const callback = location.origin + '/authentication/external/callback';
    location.assign('/authorize?response_type=code&client_id=studio-wasm&redirect_uri=' +
      encodeURIComponent(callback) + '&code_challenge=' + encodeURIComponent(pkce.codeChallenge) +
      '&code_challenge_method=S256&state=' + encodeURIComponent(pkce.state));
  });
};
const renderConnections = () => {
  document.body.innerHTML = \`
    <main aria-labelledby="connections-heading">
      <h1 id="connections-heading">SSO connections</h1>
      <p role="status">Configuration-owned connections are read-only. Database connections can be managed here.</p>
      <label for="connection-search">Search connections</label>
      <input id="connection-search" type="search">
      <label for="connection-source">Source</label>
      <select id="connection-source"><option>All</option><option>Database</option><option>Configuration</option></select>
      <label><input type="checkbox"> Include archived</label>
      <button type="button">Create connection</button>
      <table aria-label="Identity provider connections">
        <thead><tr><th>Connection</th><th>Source</th><th>Status</th><th>Latest test</th><th aria-label="Actions"></th></tr></thead>
        <tbody><tr><td>Contoso<br><small>contoso · openid-connect</small></td><td>Database</td><td>Enabled, valid</td><td>Not tested</td><td><button type="button">Manage Contoso</button></td></tr></tbody>
      </table>
    </main>\`;
};
const renderIdentityLinks = () => {
  document.body.innerHTML = \`
    <main aria-labelledby="identity-links-heading">
      <h1 id="identity-links-heading">External Identity Links</h1>
      <p role="status">Elsa stores a keyed subject hash; raw external subjects and provider tokens are never returned.</p>
      <section aria-labelledby="prelink-heading">
        <h2 id="prelink-heading">Create a prelink</h2>
        <label for="find-user">Find Elsa user</label><input id="find-user" type="search">
        <label for="elsa-user">Elsa user</label><select id="elsa-user"><option>Ada Lovelace</option></select>
        <label for="connection">Identity provider connection</label><select id="connection"><option>Contoso</option></select>
        <label for="issuer">Issuer namespace</label><input id="issuer" type="url">
        <label for="subject">External subject</label><input id="subject" type="password">
        <button type="button">Create prelink</button>
      </section>
      <label for="user-filter">Filter by user ID</label><input id="user-filter">
      <label for="connection-filter">Filter by connection ID</label><input id="connection-filter">
      <button type="button">Apply filters</button>
      <table aria-label="External identity links">
        <thead><tr><th>User</th><th>Connection</th><th>External identity</th><th>Activity</th><th aria-label="Actions"></th></tr></thead>
        <tbody><tr><td>Ada Lovelace</td><td>Contoso</td><td>https://login.contoso.example<br><small>Subject hash · …8f42</small></td><td>Never signed in</td><td><button type="button">Unlink Ada Lovelace from Contoso</button></td></tr></tbody>
      </table>
    </main>\`;
};
const renderWorkflows = (warning, logoutMode) => {
  document.body.innerHTML = '<main><h1>Workflows</h1>' +
    (warning ? '<p role="status">' + warning + '</p>' : '') +
    (logoutMode ? '<button id="logout">Sign out ' + logoutMode + '</button>' : '') + '</main>';
  const logout = document.querySelector('#logout');
  if (logout) logout.onclick = () => {
    sessionStorage.removeItem(key);
    localStorage.removeItem(key);
    location.assign(logoutMode === 'upstream' ? '/logout/continue/one-time' : '/login');
  };
};
const tokenFor = storage => JSON.stringify({ accessToken: 'access', refreshToken: 'refresh-rotated' });
const signIn = params => {
  const storage = params.get('storage') || 'Memory';
  const logoutMode = params.get('logout');
  if (storage === 'Session') sessionStorage.setItem(key, tokenFor(storage));
  if (storage === 'Durable') localStorage.setItem(key, tokenFor(storage));
  sessionStorage.setItem('fixture.storage', storage);
  if (logoutMode) sessionStorage.setItem('fixture.logout', logoutMode);
  history.replaceState({}, '', '/workflows');
  renderWorkflows(
    storage === 'Session' ? 'Security warning: credentials use browser session storage.' :
    storage === 'Durable' ? 'Security warning: credentials use persistent browser storage.' : '',
    logoutMode);
};
const restore = () => {
  const storage = sessionStorage.getItem('fixture.storage');
  const authenticated = storage === 'Session' ? sessionStorage.getItem(key) :
    storage === 'Durable' ? localStorage.getItem(key) : null;
  if (!authenticated) return location.replace('/login');
  renderWorkflows(
    storage === 'Session' ? 'Security warning: credentials use browser session storage.' :
    'Security warning: credentials use persistent browser storage.',
    sessionStorage.getItem('fixture.logout'));
};
const path = location.pathname;
if (path === '/login') renderLogin();
else if (path === '/settings/sso-connections' || path === '/security/external-authentication') renderConnections();
else if (path === '/security/external-authentication/identity-links') renderIdentityLinks();
else if (path === '/__external-authentication-fixture/sign-in') signIn(new URLSearchParams(location.search));
else if (path === '/__external-authentication-fixture/reuse-rotated-refresh-token') {
  sessionStorage.removeItem(key); localStorage.removeItem(key); location.replace('/login');
}
else if (path === '/__external-authentication-fixture/callback-replay') {
  sessionStorage.removeItem('elsa.external-authentication.pkce'); location.replace('/login?choose=true');
}
else if (path === '/workflows' || path === '/') restore();
</script>`;

createServer(async (request, response) => {
  const url = new URL(request.url, `http://127.0.0.1:${port}`);
  if (url.pathname === '/external-authentication.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(await readFile(browserHelperPath));
    return;
  }
  if (url.pathname === '/logout/continue/one-time') {
    response.writeHead(302, { location: '/login' });
    response.end();
    return;
  }
  response.writeHead(200, {
    'content-type': 'text/html; charset=utf-8',
    'cache-control': 'no-store',
    'content-security-policy': "default-src 'self'; script-src 'self' 'unsafe-inline'"
  });
  response.end(layout(applicationScript));
}).listen(port, '127.0.0.1', () => {
  process.stdout.write(`External Authentication browser fixture listening on http://127.0.0.1:${port}\n`);
});
