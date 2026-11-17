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
        const load = async state => {
            await page.goto(`http://127.0.0.1:5169/?preview=true&callmotionpreview=${state}`);
            const card = page.locator('.board-grid .board-tile').filter({ hasText: 'Hugo · atendimento' });
            await card.waitFor();
            return card;
        };
        const animations = card => card.evaluate(el => ({
            border: getComputedStyle(el, '::after').animationName,
            dot: getComputedStyle(el.querySelector('.tile-dot')).animationName,
            background: getComputedStyle(el).backgroundColor,
            text: getComputedStyle(el).color,
            count: el.getAnimations({ subtree: true }).length
        }));
        fs.mkdirSync('.impeccable/review', { recursive: true });
        for (const [state, tone, label, effect] of [
            ['dialing', 'dialing', 'Discando', 'call-dial-progress'],
            ['ringing', 'ringing', 'Tocando', 'call-ring-pulse'],
            ['answered', 'busy', 'chamadas observadas', 'none'],
            ['idle', 'busy', 'Em uso', 'none']
        ]) {
            const card = await load(state);
            await page.waitForTimeout(250);
            assert.match(await card.getAttribute('class'), new RegExp(`tone-${tone}\\b`));
            assert.match(await card.locator('.tile-status').innerText(), new RegExp(label));
            for (const other of await page.locator('.board-trunks .board-tile, .board-queues .board-tile').all()) {
                const motion = await animations(other);
                assert.equal(motion.border, 'none', 'Trunks/queues never animate their border');
                assert.equal(motion.dot, 'none', 'Trunks/queues never animate their dot');
            }
            const observed = await animations(card);
            assert.match(observed.border, new RegExp(`^${effect}(?:-b-.*)?$`), `${state} effect`);
            if (effect === 'none') assert.equal(observed.dot, 'none', `${state} stops the dot effect`);
            else {
                assert(observed.count > 0, `${state} animation runs`);
                const titleBefore = await card.locator('.tile-title').boundingBox();
                const phases = [];
                for (let sample = 0; sample < 6; sample++) {
                    phases.push(await card.evaluate(el => getComputedStyle(el, '::after').transform + getComputedStyle(el, '::after').opacity));
                    await page.waitForTimeout(180);
                }
                assert(new Set(phases).size > 1, 'Motion changes over its cycle');
                assert.deepEqual(await card.locator('.tile-title').boundingBox(), titleBefore, 'Text does not move');
                await card.scrollIntoViewIfNeeded();
                await card.screenshot({ path: `.impeccable/review/card-${state}-detail.png` });
                await page.screenshot({ path: `.impeccable/review/card-${state}-light.png`, fullPage: true });
                await page.getByRole('button', { name: 'Tema escuro', exact: true }).click();
                await page.waitForTimeout(250);
                const dark = await animations(card);
                assert.notEqual(dark.background, observed.background, 'Semantic surface follows dark theme');
                assert.notEqual(dark.text, observed.text, 'Text follows dark theme');
                await page.screenshot({ path: `.impeccable/review/card-${state}-dark.png`, fullPage: true });
                await page.setViewportSize({ width: 390, height: 844 });
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No mobile overflow');
                await page.screenshot({ path: `.impeccable/review/card-${state}-mobile.png`, fullPage: true });
                await page.emulateMedia({ reducedMotion: 'reduce' });
                const reduced = await animations(card);
                assert.equal(reduced.border, 'none');
                assert.equal(reduced.dot, 'none');
                assert.equal(reduced.count, 0, 'Reduced motion removes all loops');
                assert.match(await card.locator('.tile-status').innerText(), new RegExp(label));
                await card.focus();
                await page.keyboard.press('Enter');
                await page.getByRole('dialog').waitFor();
                await page.keyboard.press('Escape');
                await page.getByRole('dialog').waitFor({ state: 'detached' });
                await page.emulateMedia({ reducedMotion: 'no-preference' });
                await page.setViewportSize({ width: 1440, height: 1000 });
            }
            // The remote Local/Ring leg belongs to the same flow, but cannot animate the answered card.
        }
        await page.goto('http://127.0.0.1:5169/board?preview=true&sessionpreview=expired&node=eveo-voip');
        const recovery = page.getByRole('link', { name: 'Entrar novamente', exact: true });
        await recovery.waitFor();
        const href = new URL(await recovery.getAttribute('href'), page.url());
        assert.equal(href.pathname, '/login', 'Session recovery performs a real login');
        assert.equal(href.searchParams.get('returnUrl'), '/board?preview=true&sessionpreview=expired&node=eveo-voip', 'Sign-in retains board and filters');
        assert.equal(await page.locator('.board-blocked').getByRole('button', { name: 'Reconectar e atualizar', exact: true }).count(), 0, 'Expired session does not offer an ineffective circuit retry');
        assert.equal(await page.locator('.board-tile').count(), 0, 'Expired session hides all authorized data');
        assert.deepEqual(errors, []);
        console.log('PASS card motion: dialing/ringing/answered/idle, live CSS animation, stable text, light/dark/mobile, reduced motion, keyboard details');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
