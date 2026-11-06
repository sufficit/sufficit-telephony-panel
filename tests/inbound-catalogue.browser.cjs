const { chromium } = require('playwright');
const assert = require('node:assert/strict');
(async () => {
  const browser = await chromium.launch({headless: true});
  try {
    const page = await browser.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    for (const width of [1440, 390]) {
      await page.setViewportSize({width, height: 1000});
      await page.goto('http://127.0.0.1:5169/board?preview=true');
      const did = page.locator('.board-trunks .board-tile').filter({hasText:'551140001234'});
      await did.waitFor();
      assert.match(await did.innerText(), /Entrada cadastrada · sem estado em tempo real/);
      assert.match(await page.locator('.board-trunks').innerText(), /DID vencido/);
      assert.match(await page.locator('.board-trunks').innerText(), /Entrada desabilitada/);
      assert.equal(await page.locator('.board-trunks .board-tile').filter({hasText:'551140001234'}).count(), 1);
      assert(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1), 'No horizontal page overflow');
      await page.screenshot({path:`artifacts/inbound-catalogue-${width}.png`, fullPage:true});
    }
    assert.deepEqual(errors, []);
    console.log('PASS inbound catalogue browser: desktop/mobile, configured/expired/disabled, no page overflow or JS errors.');
  } finally { await browser.close(); }
})().catch(error => {console.error(error); process.exit(1);});
