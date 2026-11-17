const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
        const errors = []; page.on('pageerror', error => errors.push(error.message));
        await page.addInitScript(() => {
            localStorage.setItem('telephony-panel-language-v2', 'pt-BR');
            localStorage.setItem('telephony-panel-theme', 'light');
        });
        for (const state of ['invalid', 'no-route', 'normal', 'expired', 'new-attempt']) {
            await page.goto(`http://127.0.0.1:5169/?preview=true&dialfailurepreview=${state}`);
            const card = page.locator('.board-grid .board-tile').filter({ hasText: 'Hugo · atendimento' });
            await card.waitFor();
            if (state === 'invalid' || state === 'no-route') {
                await card.locator('.tile-failure').waitFor();
                assert.match(await card.locator('.tile-failure').innerText(), state === 'invalid' ? /Formato de número inválido/ : /Sem rota para o destino/);
                assert.equal(await card.locator('.tile-channel').count(), 0, 'Failure is not kept as an active call');
                assert.equal(await card.evaluate(el => getComputedStyle(el, '::after').animationName), 'none', 'Failure never rings');
                assert.equal(await page.locator('.board-trunks .tile-failure, .board-queues .tile-failure').count(), 0);
                if (state === 'invalid') {
                    fs.mkdirSync('.impeccable/review', { recursive: true });
                    await card.screenshot({ path: '.impeccable/review/dial-failure-light.png' });
                    await page.getByRole('button', { name: 'Tema escuro', exact: true }).click();
                    await page.setViewportSize({ width: 390, height: 844 });
                    await card.screenshot({ path: '.impeccable/review/dial-failure-mobile-dark.png' });
                    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                    await page.setViewportSize({ width: 1440, height: 1000 });
                }
            } else assert.equal(await card.locator('.tile-failure').count(), 0, `${state} does not display an invalid-dial warning`);
        }
        assert.deepEqual(errors, []);
        console.log('PASS invalid dialing: explicit reason, no route, normal clearing, expiry, new attempt, no live channel, extension-only, mobile/dark/reduced motion');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
