// Synthetic development evidence only; never opens or mutates a production queue.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const errors = [];
    const contextId = '11111111111171118111111111111111';
    const queueId = '11111111111171118111111111116599';
    const destination = `https://blazor.sufficit.com.br/pages/telephony/queue/edit?contextid=${contextId}&queueid=${queueId}`;
    fs.mkdirSync('.impeccable/review', { recursive: true });
    for (const [label, width, theme, language] of [['desktop-light',1440,'light','pt-BR'],['desktop-dark',1440,'dark','en'],['mobile-light',390,'light','pt-BR'],['mobile-dark',390,'dark','en']]) {
      const page = await browser.newPage({ viewport: { width, height: 1000 }, reducedMotion: 'reduce' });
      page.on('pageerror', e => errors.push(e.message));
      await page.addInitScript(({theme, language}) => {
        localStorage.setItem('telephony-panel-theme',theme);
        localStorage.setItem('telephony-panel-language-v2',language);
      }, {theme,language});
      await page.goto(`http://127.0.0.1:5169/board?preview=true&acdpreview=waiting&contextid=${contextId}`);
      const queue = page.locator('.board-queues .board-tile').filter({hasText:'Homologação Sufficit'});
      await queue.waitFor(); await queue.focus(); await queue.press('Enter');
      const dialog = page.getByRole('dialog'); await dialog.waitFor();
      const identity = dialog.locator('.queue-resource-details');
      assert.match(await identity.innerText(), /11111111-1111-7111-8111-111111116599/);
      assert.match(await identity.innerText(), /11111111-1111-7111-8111-111111111111/);
      const link = identity.getByRole('link');
      assert.equal(await link.getAttribute('href'), destination);
      assert.equal(await link.getAttribute('target'), '_blank');
      assert.match(await link.getAttribute('rel'), /noopener/);
      assert.doesNotMatch(await dialog.innerText(), /Contact IP|IP do contato|Registration not reported|Registro não informado/);
      await link.focus(); assert(await link.evaluate(el => el === document.activeElement));
      assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
      assert(await identity.evaluate(el => el.scrollWidth <= el.clientWidth));
      await page.screenshot({path:`.impeccable/review/queue-details-${label}.png`});
      await page.keyboard.press('Escape'); await dialog.waitFor({state:'detached'});
      // Legacy observed queue: honest unavailable identity and generic management, no peer diagnostics.
      await page.goto('http://127.0.0.1:5169/board?preview=true');
      await page.locator('.board-queues .board-tile').first().click(); await dialog.waitFor();
      const fallback = dialog.locator('.queue-resource-details');
      assert.equal(await fallback.getByRole('link').getAttribute('href'), 'https://blazor.sufficit.com.br/pages/telephony/queue');
      assert.equal(await fallback.locator('code').count(),0);
      assert.doesNotMatch(await dialog.innerText(), /Contact IP|IP do contato|Registration not reported|Registro não informado/);
      if (label==='mobile-light') await page.screenshot({path:'.impeccable/review/queue-details-legacy-mobile.png'});
      await page.close();
    }
    assert.deepEqual(errors,[]);
    console.log('PASS queue details: GUID/company, exact edit route, safe new tab, keyboard, legacy fallback, no endpoint fields, EN/PT, desktop/mobile, light/dark');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode=1; });
