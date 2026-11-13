const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
 const browser = await chromium.launch({headless:true});
 try {
  const page = await browser.newPage({viewport:{width:1440,height:1000}});
  await page.addInitScript(() => localStorage.setItem('telephony-panel-language-v2','pt-BR'));
  const errors=[]; page.on('pageerror',e=>errors.push(e.message));
  const base='http://127.0.0.1:5169/board?preview=true&acdpreview=waiting';
  const region=page.locator('#board-queue-region');
  await page.goto(base+'&queuesummarypreview=partial');
  await region.locator('.board-tile').first().waitFor();
  assert.equal((await region.locator('.board-totals').innerText()).trim(),'— / 2');
  assert.match(await region.innerText(), /4 cartões por servidor, 1 sem estado atual/);
  assert.match(await region.getByRole('status').innerText(), /Total em espera indisponível/);
  assert.equal(await region.locator('.board-tile').count(),4);
  assert.match(await region.locator('.board-tile').last().innerText(),/0000006500/);
  fs.mkdirSync('.impeccable/review',{recursive:true});
  await region.screenshot({path:'.impeccable/review/queue-summary-desktop.png'});
  await page.setViewportSize({width:390,height:844});
  await region.scrollIntoViewIfNeeded();
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await page.screenshot({path:'.impeccable/review/queue-summary-mobile.png'});
  await page.goto(base); await region.locator('.board-tile').first().waitFor();
  assert.match(await region.locator('.board-totals').innerText(),/^\d+ \/ 2$/);
  assert.equal(await region.getByRole('status').count(),0);
  assert.deepEqual(errors,[]);
  console.log('PASS queue summary: partial coverage, distinct native replicas, retained history, current-first order, complete coverage and mobile overflow');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
