// Isolated Development fixture: no PBX connection or real calls.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    page.setDefaultTimeout(15000);
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    const base = process.env.PANEL_TEST_URL || 'http://127.0.0.1:5177';
    await page.goto(base + '/board?preview=true&numberpreview=true&contextid=11111111111171118111111111111111');
    const tile = page.locator('.board-grid .board-tile').filter({ hasText: 'Hugo' }).first();
    await tile.waitFor();
    assert.match(await tile.innerText(), /\+558000000697 → Atendimento geral/);
    assert(!((await tile.innerText()).includes('5508000000697')), 'Card presents normalized number');
    await tile.focus(); await page.keyboard.press('Enter');
    const dialog = page.getByRole('dialog'); await dialog.waitFor();
    const row = dialog.locator('tbody tr').first();
    assert.match(await row.innerText(), /\+558000000697 → \+558000000697/);
    assert.match(await row.innerText(), /Origem original: 5508000000697; destino original: \+5508000000697/);
    assert.match(await row.innerText(), /eveo-voip/);
    assert.match(await row.innerText(), /PJSIP\/6007-000000ab/);
    assert.match(await row.innerText(), /Evento/);
    assert.equal(await dialog.locator('.number-normalization').first().evaluate(e => getComputedStyle(e).display), 'block', 'Diagnostic starts on its own line');
    fs.mkdirSync('tests/artifacts', { recursive: true });
    await page.screenshot({ path: 'tests/artifacts/phone-number-normalization-desktop.png' });
    await page.setViewportSize({ width: 390, height: 844 });
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No page overflow');
    assert(await row.locator('.number-normalization').isVisible(), 'Original remains visible on mobile');
    await page.screenshot({ path: 'tests/artifacts/phone-number-normalization-mobile.png' });
    await page.keyboard.press('Escape'); await dialog.waitFor({ state: 'detached' });
    await page.goto(base + '/board?preview=true&contextid=11111111111171118111111111111111');
    await tile.waitFor(); await tile.click(); await dialog.waitFor();
    assert.equal(await dialog.locator('.number-normalization').count(), 0, 'Unchanged observations have no normalization notice');
    await page.goto(base + '/board?preview=true&numberpreview=e164&contextid=11111111111171118111111111111111');
    await tile.waitFor();
    assert.match(await tile.innerText(), /\+552140627711 → Atendimento geral/);
    await tile.click(); await dialog.waitFor();
    assert.match(await row.innerText(), /\+552140627711 → \+5511999999999/);
    assert.match(await row.innerText(), /Origem original: 552140627711; destino original: 5511999999999/);
    assert(!((await row.innerText()).includes('550800')), 'Adding plus does not falsely report a tollfree correction');
    await page.screenshot({ path: 'tests/artifacts/phone-number-e164-mobile.png' });
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.screenshot({ path: 'tests/artifacts/phone-number-e164-desktop.png' });
    assert.deepEqual(errors, []);
    console.log('PASS normalized card, original source/destination, server/channel/event context, mobile, keyboard, unchanged observations');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
