// Development-only fixtures: no real calls or authenticated production data.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
    page.setDefaultTimeout(10000);
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    await page.goto('http://127.0.0.1:5169/board?preview=true');
    await page.locator('.board-tile').first().waitFor();
    const geometry = () => page.evaluate(() => ({
      x: scrollX, y: scrollY, height: document.documentElement.scrollHeight,
      grid: document.querySelector('.board-grid').scrollTop,
      top: document.querySelector('.board-columns').getBoundingClientRect().top
    }));
    const dialog = page.getByRole('dialog');
    const close = async () => { await dialog.getByRole('button', { name: 'Close details', exact: true }).click(); await dialog.waitFor({ state: 'detached' }); };
    fs.mkdirSync('.impeccable/review', { recursive: true });
    for (const selector of ['.board-trunks .board-tile', '.board-queues .board-tile', '.board-grid .board-tile']) {
      const tile = page.locator(selector).first();
      await tile.focus();
      const before = await geometry(); const url = page.url();
      await tile.press('Enter'); await dialog.waitFor();
      assert(await dialog.evaluate(e => e.matches(':modal')));
      assert.equal(await page.locator('.board-inspector').count(), 0, 'No inline details on dedicated page');
      assert.deepEqual(await geometry(), before, 'Opening details must not move or enlarge the board');
      assert.equal(page.url(), url);
      assert(await dialog.evaluate(e => e.contains(document.activeElement)), 'Focus enters dialog');
      await page.keyboard.press('Shift+Tab'); await page.keyboard.press('Tab');
      assert(await dialog.evaluate(e => e.contains(document.activeElement)), 'Tab stays inside modal');
      if (selector.includes('trunks')) await page.screenshot({ path: '.impeccable/review/board-trunk-dialog.png' });
      await page.keyboard.press('Escape'); await dialog.waitFor({ state: 'detached' });
      assert.deepEqual(await geometry(), before);
      assert(await tile.evaluate(e => e === document.activeElement), 'Focus returns to selected resource');
    }
    const hugo = page.locator('.board-grid .board-tile').filter({ hasText: 'Hugo' }).first();
    await hugo.scrollIntoViewIfNeeded(); await hugo.focus();
    const before = await geometry();
    await hugo.click(); await dialog.waitFor();
    await dialog.getByRole('button', { name: 'Listen', exact: true }).first().click();
    await dialog.getByRole('heading', { name: 'Confirm listening', exact: true }).waitFor();
    assert.equal(await page.locator('.monitor-confirm:not(dialog *)').count(), 0, 'Confirmation cannot be appended below the board');
    assert.deepEqual(await geometry(), before, 'Confirmation scroll is confined to popup');
    await page.screenshot({ path: '.impeccable/review/board-channel-dialog.png' });
    await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
    await close(); assert.deepEqual(await geometry(), before);
    await hugo.click(); await dialog.waitFor();
    await page.mouse.click(2, 2); await dialog.waitFor({ state: 'detached' });
    assert.deepEqual(await geometry(), before, 'Backdrop close preserves scroll');
    await page.locator('.board-filter-trigger').click();
    await page.getByRole('button', { name: 'Full screen', exact: true }).click();
    await page.waitForFunction(() => document.fullscreenElement?.id === 'board-focus');
    if (await page.locator('#board-options').isVisible()) await page.getByRole('button', { name: 'Close filters', exact: true }).click();
    await page.locator('.board-trunks .board-tile').first().click(); await dialog.waitFor();
    assert(await dialog.evaluate(e => document.fullscreenElement.contains(e) && e.matches(':modal')));
    await close();
    assert(await page.evaluate(() => document.fullscreenElement?.id === 'board-focus'));
    await page.evaluate(() => document.exitFullscreen());
    await page.waitForFunction(() => !document.fullscreenElement);
    const mobile = await browser.newPage({ viewport: { width: 390, height: 844 }, reducedMotion: 'reduce' });
    await mobile.addInitScript(() => { localStorage.setItem('telephony-panel-language', 'pt-BR'); localStorage.setItem('telephony-panel-theme', 'dark'); });
    await mobile.goto('http://127.0.0.1:5169/board?preview=true');
    const mobileTile = mobile.locator('.board-grid .board-tile').filter({ hasText: 'Hugo' }).first();
    await mobileTile.click(); await mobile.getByRole('dialog').waitFor();
    await mobile.getByRole('dialog').getByRole('button', { name: 'Ouvir', exact: true }).first().click();
    await mobile.getByRole('heading', { name: 'Confirmar escuta', exact: true }).waitFor();
    assert(await mobile.locator('.call-actions button').first().evaluate(e => getComputedStyle(e).whiteSpace === 'nowrap'), 'Supervision labels do not break into individual letters');
    assert(await mobile.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    const box = await mobile.getByRole('dialog').boundingBox();
    assert(box.y >= 0 && box.y + box.height <= 844, 'Popup stays within the mobile viewport');
    await mobile.screenshot({ path: '.impeccable/review/board-dialog-mobile.png' });
    await mobile.getByRole('button', { name: 'Fechar detalhes', exact: true }).click();
    await mobile.getByRole('dialog').waitFor({ state: 'detached' });
    assert.equal(await mobile.locator('.monitor-confirm').count(), 0);
    assert.deepEqual(errors, []);
    console.log('PASS dedicated dialog: resource types, unchanged geometry/URL, keyboard/focus, channel confirmation, backdrop, fullscreen and mobile/PT-BR/dark');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
