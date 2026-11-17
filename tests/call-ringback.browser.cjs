const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.addInitScript(() => {
            localStorage.setItem('telephony-panel-language-v2', 'pt-BR');
            localStorage.setItem('telephony-panel-theme', 'light');
        });
        for (const state of ['dialing', 'ringing', 'answered', 'removed']) {
            await page.goto(`http://127.0.0.1:5169/?preview=true&ringbackpreview=${state}`);
            const origin = page.locator('.board-grid .board-tile').filter({ hasText: 'Hugo · atendimento' });
            const destination = page.locator('.board-grid .board-tile').filter({ hasText: '6007 · destino' });
            await origin.waitFor();
            const label = await origin.locator('.tile-status').innerText();
            if (state === 'ringing') {
                assert.match(label, /^Destino tocando ·/);
                assert.match(await origin.getAttribute('class'), /tone-ringing/);
                assert.match(await destination.locator('.tile-status').innerText(), /^Tocando ·/);
                for (const tile of [origin, destination]) {
                    assert.match(await tile.evaluate(el => getComputedStyle(el, '::after').animationName), /^call-ring-pulse/);
                }
                fs.mkdirSync('.impeccable/review', { recursive: true });
                await origin.screenshot({ path: '.impeccable/review/ringback-origin.png' });
                await destination.screenshot({ path: '.impeccable/review/ringback-destination.png' });
                await page.getByRole('button', { name: 'Tema escuro', exact: true }).click();
                await page.setViewportSize({ width: 390, height: 844 });
                await page.emulateMedia({ reducedMotion: 'reduce' });
                assert.match(await origin.locator('.tile-status').innerText(), /^Destino tocando ·/);
                assert.equal(await origin.evaluate(el => getComputedStyle(el, '::after').animationName), 'none');
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                await page.emulateMedia({ reducedMotion: 'no-preference' });
                await page.setViewportSize({ width: 1440, height: 1000 });
            } else if (state === 'answered') {
                assert.doesNotMatch(label, /Discando|Tocando|Destino tocando/);
                assert.equal(await origin.evaluate(el => getComputedStyle(el, '::after').animationName), 'none');
                assert.equal(await destination.evaluate(el => getComputedStyle(el, '::after').animationName), 'none');
            } else {
                assert.match(label, /^Discando ·/);
            }
            for (const tile of await page.locator('.board-trunks .board-tile, .board-queues .board-tile').all()) {
                assert.equal(await tile.evaluate(el => getComputedStyle(el, '::after').animationName), 'none');
            }
        }
        assert.deepEqual(errors, []);
        console.log('PASS ringback: dialing → destination ringing → answer/removal, source/target labels, extension-only motion, dark/mobile/reduced motion');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
