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
      // No queue chosen: ranking of the busiest queues; clicking one opens its distribution.
      await section.locator('.ranking-row').first().waitFor({ timeout: 30000 });
      if (await section.locator('.ranking-row').count() !== 2) throw new Error('two queues in the ranking');
      if (!(await section.locator('.ranking-row').first().innerText()).includes('0007366500')) throw new Error('busiest queue first');
      await section.locator('.ranking-row').first().click();
      await page.waitForURL(url => url.searchParams.has('dqueue'));
      await section.locator('.member').first().waitFor({ timeout: 30000 });
      // Calls waiting now, with the extensions ringing highlighted.
      await section.locator('.now-call').first().waitFor();
      if (await section.locator('.now-call .attempt.ringing').count() !== 2) throw new Error('two members ringing now');
      // The chosen (native) queue filters the whole page: no other queues' tiles, live strip or presence.
      await page.locator('.focus-note').waitFor();
      if (await page.locator('.queue-tile').count() !== 0 || await page.locator('.live-strip').count() !== 0 || await page.locator('.queue-diagnostics').count() !== 0)
        throw new Error('other queues still visible while a queue is chosen');
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
    // Back to all queues restores the page.
    const page = await browser.newPage({ viewport: { width: 1400, height: 900 } });
    await page.goto('http://127.0.0.1:5169/queues?preview=true&dqueue=google-voip%7C0007366500');
    await page.locator('.focus-note').waitFor({ timeout: 30000 });
    await page.locator('.filter-clear').click();
    await page.locator('.queue-tile').first().waitFor();
    await page.locator('.ranking-row').first().waitFor();
    if (new URL(page.url()).searchParams.has('dqueue')) throw new Error('dqueue must be cleared');
    // A controller queue tile filters the page; the tile list hides (the chip names the queue).
    await page.locator('.queue-tile').first().click();
    await page.waitForURL(url => url.searchParams.has('queueid'));
    await page.waitForFunction(() => document.querySelectorAll('.queue-tile').length === 0);
    if (!(await page.locator('.filter-chip').first().innerText()).startsWith('Fila')) throw new Error('queue chip expected');
    console.log('PASS queue distribution');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exit(1); });
