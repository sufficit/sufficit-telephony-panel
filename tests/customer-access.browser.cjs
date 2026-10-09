const { chromium } = require('playwright');
const assert = require('node:assert/strict');

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    const open = async mode => {
      await page.goto(`http://127.0.0.1:5169/board?preview=true&previewaccess=${mode}`);
      await page.locator('.board-tile').first().waitFor();
      await page.locator('.board-filter-trigger').click();
      await page.locator('#board-options').waitFor();
    };
    await open('single');
    assert.equal(new URL(page.url()).searchParams.get('contextid'), '11111111111171118111111111111111');
    assert.equal(await page.locator('.company-filter').count(), 0, 'Single context needs no selector');
    assert.equal(await page.getByRole('link', { name: 'Configure board', exact: true }).count(), 0);
    await page.getByRole('button', { name: 'Close filters', exact: true }).click();
    await page.locator('.card-menu-trigger').first().click();
    await page.getByRole('menu').waitFor();
    assert.equal(await page.getByRole('menuitem', { name: 'Listen to a channel', exact: true }).count(), 0);
    assert.equal(await page.getByRole('menuitem', { name: 'Open details', exact: true }).count(), 1);
    await page.keyboard.press('Escape');
    await page.reload();
    await page.locator('.board-tile').first().waitFor();
    assert.equal(await page.locator('.company-filter').count(), 0, 'Single context survives reload');
    await open('multiple');
    await page.locator('.company-filter summary').click();
    const select = page.locator('.company-filter').getByRole('combobox');
    await select.click();
    await page.getByRole('option', { name: '22222222-2222-7222-8222-222222222222', exact: true }).click();
    await page.waitForURL(url => url.searchParams.get('contextid') === '22222222222272228222222222222222');
    await page.reload();
    await page.locator('.board-tile').first().waitFor();
    assert.equal(new URL(page.url()).searchParams.get('contextid'), '22222222222272228222222222222222');
    await page.setViewportSize({ width: 390, height: 844 });
    await page.locator('.board-filter-trigger').click();
    assert(await page.locator('.company-filter').isVisible());
    await page.screenshot({ path: '/tmp/panel-customer-mobile.png' });
    await open('global');
    await page.locator('.company-filter summary').click();
    assert.equal(await page.locator('.company-filter').getByRole('textbox').count(), 1, 'Global access keeps context search');
    assert.equal(new URL(page.url()).searchParams.has('contextid'), false);
    assert.deepEqual(errors, []);
    console.log('PASS: single/multiple/global context UI, reload/URL, mobile and view-only card actions (synthetic Development fixture).');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
