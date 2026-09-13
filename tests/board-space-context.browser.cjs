const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1994, height: 800 }, reducedMotion: 'reduce' });
    page.setDefaultTimeout(10000);
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    const contextId = '11111111111171118111111111111111';
    await page.goto(`http://127.0.0.1:5169/board?preview=true&company=${contextId}&q=6101&text=peers`);
    await page.waitForURL(u => u.searchParams.get('contextid') === contextId && !u.searchParams.has('company'));
    await page.locator('.board-tile').first().waitFor();
    assert.equal(new URL(page.url()).searchParams.get('q'), '6101');
    await page.reload(); await page.locator('.board-tile').first().waitFor();
    await page.locator('.board-filter-trigger').click();
    await page.locator('.company-filter summary').click();
    await page.getByRole('button', { name: 'Clear company · view exchange', exact: true }).click();
    await page.waitForURL(u => !u.searchParams.has('contextid') && !u.searchParams.has('company'));
    await page.getByRole('textbox', { name: 'Company (optional)', exact: true }).fill('demo');
    await page.getByRole('button', { name: 'Find company', exact: true }).click();
    await page.locator('.client-results button').first().click();
    await page.waitForURL(u => u.searchParams.get('contextid') === contextId && !u.searchParams.has('company'));
    const shared = page.url();
    const other = await browser.newPage(); await other.goto(shared);
    await other.locator('.board-tile').first().waitFor();
    assert.equal(new URL(other.url()).searchParams.get('contextid'), contextId);
    await other.close();
    await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
    await page.getByRole('button', { name: 'Close filters', exact: true }).click();

    // Layout-only fixtures reuse the real rendered tile markup/styles. They do not
    // represent live PBX inventory and are never sent to an API or persisted.
    const fixtures = async (trunks, queues) => page.evaluate(({ trunks, queues }) => {
      for (const [selector, count, name] of [['.board-trunks', trunks, 'Trunk'], ['.board-queues', queues, 'Queue']]) {
        const list = document.querySelector(selector);
        const template = list.querySelector('.board-tile') || document.querySelector('.board-grid .board-tile');
        const tiles = Array.from({ length: count }, (_, index) => {
          const tile = template.cloneNode(true); tile.querySelector('strong').textContent = `${name} ${index + 1}`; return tile;
        });
        list.replaceChildren(...tiles);
        list.parentElement.querySelectorAll('.board-empty').forEach(e => e.remove());
        list.parentElement.querySelector('.board-section-heading span').textContent = count;
        if (!count) { const empty = document.createElement('p'); empty.className = 'board-empty'; empty.textContent = 'No trunks match the selected filters. Configure classification in the normal panel.'; list.after(empty); }
      }
    }, { trunks, queues });
    const measure = () => page.evaluate(() => Object.fromEntries(['board-trunks', 'board-queues'].map(name => {
      const e = document.querySelector('.' + name), r = e.getBoundingClientRect();
      return [name, { height: e.clientHeight, content: e.scrollHeight, bottom: r.bottom, top: r.top }];
    })));
    fs.mkdirSync('.impeccable/review', { recursive: true });
    await fixtures(0, 6);
    let sizes = await measure();
    assert(sizes['board-queues'].content <= sizes['board-queues'].height + 1, 'Six queues fit without unnecessary scrollbar');
    assert(sizes['board-queues'].bottom <= 800);
    await page.screenshot({ path: '.impeccable/review/board-space-six-queues.png' });
    await fixtures(1, 12);
    sizes = await measure();
    assert(sizes['board-queues'].height > 550, 'Queues use room left by the short trunk list');
    assert(sizes['board-queues'].bottom <= 800);
    await fixtures(25, 25);
    sizes = await measure();
    assert(sizes['board-trunks'].content > sizes['board-trunks'].height && sizes['board-queues'].content > sizes['board-queues'].height, 'Both lists scroll only when shared capacity is exceeded');
    assert(sizes['board-queues'].bottom <= 800, 'Crowded sidebar remains in viewport');
    await page.screenshot({ path: '.impeccable/review/board-space-crowded.png' });
    await page.setViewportSize({ width: 1994, height: 1500 });
    sizes = await measure(); assert(sizes['board-queues'].height > 650, 'Taller monitor expands lists');
    await fixtures(0, 6);
    await page.setViewportSize({ width: 390, height: 844 });
    sizes = await measure(); assert(sizes['board-queues'].content <= sizes['board-queues'].height + 1, 'Mobile queues use natural height');
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    await page.screenshot({ path: '.impeccable/review/board-space-mobile.png', fullPage: true });
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.reload(); await page.locator('.board-tile').first().waitFor();
    await page.locator('.board-filter-trigger').click();
    await page.getByRole('button', { name: 'Full screen', exact: true }).click();
    await page.waitForFunction(() => document.fullscreenElement?.id === 'board-focus');
    if (await page.locator('#board-options').isVisible()) await page.getByRole('button', { name: 'Close filters', exact: true }).click();
    await fixtures(0, 10);
    sizes = await measure(); assert(sizes['board-queues'].content <= sizes['board-queues'].height + 1, 'Fullscreen does not restore half-height cap');
    assert.deepEqual(errors, []);
    console.log('PASS contextid canonical/legacy/select/clear/reload/share; sidebar natural/shared height, crowded, resize, mobile and fullscreen');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
