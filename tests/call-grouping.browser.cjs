// Run against an isolated Development fixture, never an authenticated PBX session.
// NODE_PATH=<existing playwright install>/node_modules node tests/call-grouping.browser.cjs
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 1100 } });
        const errors = [];
        page.on('pageerror', e => errors.push(e.message));
        await page.addInitScript(() => localStorage.setItem("telephony-panel-language", "pt-BR"));
        await page.goto('http://127.0.0.1:5169/?preview=true');
        await page.getByText('Demonstração local: clientes e eventos fictícios.', { exact: false }).waitFor();
        const calls = page.getByRole('button', { name: /^Chamadas 1$/ });
        await calls.click();
        await page.locator('.call-group').first().waitFor();
        assert.equal(await page.locator('.call-group').count(), 2);
        const group = page.locator('.call-group').filter({ has: page.locator('summary', { hasText: 'Chamada correlacionada' }) });
        await group.locator('summary').focus();
        await page.keyboard.press('Enter');
        await page.waitForFunction(() => document.querySelector('.call-group[open]'));
        assert.equal(await group.locator('tbody tr').count(), 3);
        await group.getByRole('button', { name: 'Ouvir', exact: true }).first().click();
        await page.getByRole('heading', { name: 'Confirmar escuta' }).waitFor();
        assert.match(await page.locator('.monitor-confirm').innerText(), /PJSIP\/6007-000000ab/);
        await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
        const filter = page.getByRole('textbox', { name: 'Filtrar recursos' });
        await filter.fill('Local/6007');
        await page.waitForFunction(() => document.querySelectorAll('.call-group').length === 1);
        assert.equal(await group.locator('tbody tr').count(), 3);
        await filter.fill('');
        await page.waitForFunction(() => document.querySelectorAll('.call-group').length === 2);
        fs.mkdirSync('.impeccable/review', { recursive: true });
        await page.screenshot({ path: '.impeccable/review/call-groups-desktop.png', fullPage: true });
        await page.getByRole('button', { name: 'Tema escuro', exact: true }).click();
        await page.getByRole('button', { name: 'Tema claro', exact: true }).waitFor();
        await page.screenshot({ path: '.impeccable/review/call-groups-dark.png', fullPage: true });
        await page.setViewportSize({ width: 390, height: 844 });
        await page.screenshot({ path: '.impeccable/review/call-groups-mobile.png', fullPage: true });
        assert(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), 'No page-level mobile overflow');
        await page.getByRole('button', { name: /^Canais 4$/ }).click();
        await page.waitForFunction(() => !document.querySelector('.call-group') && document.querySelectorAll('.live-table tbody tr').length === 4);
        await filter.fill('missing-channel');
        await page.getByText('Nenhum canal observado com estes filtros.', { exact: true }).waitFor();
        assert.equal(errors.length, 0, errors.join('\n'));
        console.log('PASS browser: call/channel counts, 3-leg expansion, keyboard, channel-target confirmation, filtering, empty state, desktop/dark/mobile, no overflow or page errors');
    } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
