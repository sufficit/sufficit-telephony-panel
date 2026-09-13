// Development-only ARI/AMI fixtures; no real calls or private API credentials.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1988, height: 1000 }, reducedMotion: 'reduce' });
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    const context = '11111111111171118111111111111111';
    const url = phase => `http://127.0.0.1:5169/board?preview=true&acdpreview=${phase}&contextid=${context}`;
    const queue = () => page.locator('.board-queues .board-tile').filter({ hasText: 'Homologação Sufficit' });
    const open = async phase => { await page.goto(url(phase)); await queue().waitFor(); };
    for (const [phase, caption] of [['greeting', 'Queue greeting'], ['waiting', 'Waiting in queue'], ['offering', 'Calling an agent'], ['connected', 'Answered in queue']]) {
      await open(phase);
      assert.match(await queue().innerText(), new RegExp(caption));
      assert.match(await queue().innerText(), /6007 → Homologação Sufficit/);
      const peer = page.locator('.board-grid .board-tile').filter({ hasText: 'Hugo · piloto 6599' });
      assert.match(await peer.innerText(), /6007 → Homologação Sufficit/);
      const currentUrl = page.url(); await queue().click();
      const dialog = page.getByRole('dialog'); await dialog.waitFor();
      assert.match(await dialog.innerText(), /not native AMI queue events/);
      assert.equal(await dialog.locator('tbody tr').count(), 1);
      assert.match(await dialog.innerText(), /SIP\/0000006007-000000ef/);
      assert.equal(await page.locator('.board-inspector').count(), 0);
      await page.keyboard.press('Escape'); await dialog.waitFor({ state: 'detached' });
      assert.equal(page.url(), currentUrl);
    }
    await open('legacy');
    assert.match(await queue().innerText(), /1 pending · 0 in service/);
    assert.equal(await queue().locator('.tile-channel').count(), 0);
    await page.goto(url('legacy') + '&channels=true&state=busy'); await queue().waitFor();
    await queue().click(); assert.match(await page.getByRole('dialog').innerText(), /reports counts only/);
    await page.keyboard.press('Escape');
    await open('empty'); assert.match(await queue().innerText(), /0 pending · 0 in service/);
    assert.equal(await queue().locator('.tile-channel').count(), 0);
    await open('stale'); assert.match(await queue().innerText(), /ACD state unavailable/);
    assert.equal(await queue().locator('.tile-channel').count(), 0);
    await page.goto(url('stale') + '&channels=true'); await page.locator('.board-tile').first().waitFor();
    assert.equal(await queue().count(), 0);
    await page.goto(url('waiting').replace(context, '44444444444474448444444444444444'));
    await page.locator('.board-tile').first().waitFor(); assert.equal(await queue().count(), 0);
    await page.goto(url('waiting') + '&node=eveo-voip'); await page.locator('.board-tile').first().waitFor(); assert.equal(await queue().count(), 0);
    await page.goto(url('waiting').replace(`&contextid=${context}`, '')); await queue().waitFor();
    assert.match(await queue().innerText(), /google-voip/);
    fs.mkdirSync('.impeccable/review', { recursive: true });
    await open('greeting');
    await page.screenshot({ path: '.impeccable/review/acd-board-desktop.png' });
    await page.evaluate(() => localStorage.setItem('telephony-panel-language', 'pt-BR'));
    await page.setViewportSize({ width: 390, height: 844 });
    await page.reload(); await queue().waitFor();
    assert.match(await queue().innerText(), /Saudação da fila/);
    assert.match(await queue().innerText(), /1 antes do atendimento · 0 em atendimento/);
    await queue().scrollIntoViewIfNeeded();
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    await page.screenshot({ path: '.impeccable/review/acd-board-mobile.png' });
    await queue().click(); await page.getByRole('dialog').waitFor();
    assert.match(await page.getByRole('dialog').innerText(), /não dos eventos AMI/);
    await page.screenshot({ path: '.impeccable/review/acd-board-details-mobile.png' });
    assert.deepEqual(errors, []);
    console.log('PASS ACD board: greeting/wait/offering/connected, exact channel, legacy counts, stale/empty, context/node/activity filters, manager overview, overlay, EN/PT and mobile');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
