// Queue distribution (preview fixture): per-extension bars, hourly chart, period switch and call sequences.
const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  try {
    for (const [w, h] of [[1400, 1000], [390, 844]]) {
      const page = await browser.newPage({ viewport: { width: w, height: h } });
      const errors = []; page.on('pageerror', e => errors.push(e.message));
      await page.goto('http://127.0.0.1:5169/queues?preview=true');
      const section = page.locator('.distribution');
      await section.locator('.member').first().waitFor({ timeout: 30000 });
      if (await section.locator('.member').count() !== 6) throw new Error('six members expected');
      if (await section.locator('.hour').count() !== 24) throw new Error('24 hourly bars by default');
      if (!(await section.locator('.member').last().textContent()).includes('nunca')) throw new Error('offline member shows no last answer');
      await section.getByRole('button', { name: /Fila e período/ }).click();
      await page.locator('#distribution-options').getByRole('button', { name: /^Hora atual$/ }).click();
      await page.keyboard.press('Escape');
      await page.waitForFunction(() => document.querySelectorAll('.distribution .hour').length === 1);
      await section.locator('summary').click();
      if (await section.locator('.attempt').first().textContent() !== '6004') throw new Error('chips keep four digits');
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
      if (overflow) throw new Error(`horizontal overflow at ${w}px`);
      if (errors.length) throw new Error(errors.join('; '));
      await page.close();
    }
    console.log('PASS queue distribution');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exit(1); });
