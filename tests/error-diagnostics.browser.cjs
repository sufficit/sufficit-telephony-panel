// Development fixtures only: never originates a real supervision call.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1280, height: 1000 } });; await page.addInitScript(() => { if (!localStorage.getItem('telephony-panel-language-v2')) localStorage.setItem('telephony-panel-language-v2', 'en'); }); // English fixtures; product default is pt-BR
        const errors = []; page.on('pageerror', e => errors.push(e.message));
        await page.addInitScript(() => localStorage.setItem('telephony-panel-language-v2', 'pt-BR'));
        await page.goto('http://127.0.0.1:5169/?preview=true&diagnosticpreview=true&tab=extensions');
        await page.getByText('Detalhes do erro', { exact: true }).waitFor();
        await page.getByText('Detalhes do erro', { exact: true }).focus();
        await page.keyboard.press('Enter');
        await page.getByText('demo-error-reference', { exact: true }).waitFor();
        await page.getByText('Detalhes das fontes', { exact: true }).click();
        await page.getByText('O controlador de filas está desconectado.', { exact: true }).waitFor();
        await page.getByText('A fonte está atualizada.', { exact: true }).waitFor();
        assert(!await page.locator('.queue-source-details').innerText().then(t => t.includes('0001-01-01')), 'Missing date is labelled, never rendered as a real observation');
        await page.getByRole('button', { name: /^Canais 4$/ }).click();
        await page.locator('.supervision-block-reason').first().waitFor();
        assert.equal(await page.locator('.supervision-block-reason').count(), 3);
        const reasons = await page.locator('.supervision-block-reason').allTextContents();
        assert(reasons.some(s => s.includes('empresa identificada')));
        assert(reasons.some(s => s.includes('Local não são suportados')));
        assert(reasons.some(s => s.includes('mais de 30 segundos')));
        const listen = page.getByRole('button', { name: 'Ouvir', exact: true });
        assert.equal(await listen.count(), 4);
        assert.equal(await page.locator('.call-actions button:enabled').count(), 2, 'Only the eligible channel exposes listen/whisper');
        await listen.first().click();
        await page.getByRole('heading', { name: 'Confirmar escuta' }).waitFor();
        await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
        fs.mkdirSync('artifacts/diagnostics', { recursive: true });
        await page.screenshot({ path: 'artifacts/diagnostics/desktop.png', fullPage: true });
        await page.getByRole('button', { name: 'Tema escuro', exact: true }).click();
        await page.getByRole('button', { name: 'Tema claro', exact: true }).waitFor();
        await page.setViewportSize({ width: 768, height: 1024 });
        await page.screenshot({ path: 'artifacts/diagnostics/tablet-dark.png', fullPage: true });
        assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Tablet has no page overflow');
        assert.deepEqual(errors, []);
        console.log('PASS diagnostics browser: expandable error/reference, per-source state, all blocked reasons, eligible confirmation, tablet/dark, no real calls');
    } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
