// Synthetic authorized fixture only; never originate a real PBX call.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    for (const language of ['en', 'pt-BR']) {
      await page.goto('http://127.0.0.1:5169/board?preview=true&contextid=11111111111171118111111111111111');
      await page.evaluate(value => localStorage.setItem('telephony-panel-language', value), language);
      await page.reload(); await page.locator('.board-tile').first().waitFor();
      const hugo = page.locator('.board-grid .board-tile').filter({ hasText: 'Hugo' }).first();
      const queue = page.locator('.board-queues .board-tile').first();
      assert.equal(await hugo.locator('.tile-channel').count(), 1, 'Three related legs render as one flow');
      assert.match(await hugo.innerText(), /Cliente demonstração → Atendimento geral/);
      assert.match(await queue.innerText(), language === 'en' ? /1 waiting · 0 answered/ : /1 aguardando · 0 em atendimento/);
      assert(!await hugo.innerText().then(text => text.includes('<unknown>')));
      await queue.click();
      const dialog = page.getByRole('dialog'); await dialog.waitFor();
      assert.equal(await dialog.locator('tbody tr').count(), 3, 'Queue popup retains all three related channels');
      assert.equal(await page.locator('.board-inspector').count(), 0);
      await page.keyboard.press('Escape'); await dialog.waitFor({ state: 'detached' });
      fs.mkdirSync('.impeccable/review', { recursive: true });
      await page.screenshot({ path: `.impeccable/review/queue-call-flow-${language}.png` });
      await page.setViewportSize({ width: 390, height: 844 });
      assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No horizontal page overflow');
      await page.setViewportSize({ width: 1440, height: 1000 });
    }
    assert.deepEqual(errors, []);
    console.log('PASS queue activity, linked channel grouping, destination, popup diagnostics, EN/PT and mobile');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
