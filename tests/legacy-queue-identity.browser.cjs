const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
 const browser=await chromium.launch({headless:true});
 try {
  fs.mkdirSync('.impeccable/review',{recursive:true});
  for(const [label,width,theme] of [['desktop',1440,'light'],['mobile',390,'dark']]) {
   const page=await browser.newPage({viewport:{width,height:1000},reducedMotion:'reduce'});
   const errors=[];page.on('pageerror',e=>errors.push(e.message));
   await page.addInitScript(theme=>{localStorage.setItem('telephony-panel-theme',theme);localStorage.setItem('telephony-panel-language-v2','pt-BR')},theme);
   const open=async scenario=>{
    await page.goto('http://127.0.0.1:5169/board?preview=true&queueidentitypreview='+scenario);
    await page.locator('.board-queues .board-tile').first().click();
    await page.getByRole('dialog').waitFor();
    return page.getByRole('dialog').locator('.queue-resource-details');
   };
   let detail=await open('resolved');
   await detail.getByText('99999999-9999-7999-8999-999999996500',{exact:true}).waitFor();
   assert.match(await detail.getByRole('link').getAttribute('href'),/\/queue\/edit\?contextid=11111111111171118111111111111111&queueid=99999999999979998999999999996500$/);
   assert.equal(await detail.getByRole('link').getAttribute('target'),'_blank');
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
   await page.screenshot({path:`.impeccable/review/legacy-queue-guid-${label}.png`});
   detail=await open('legacy');
   assert.match(await detail.innerText(),/99999999-9999-7999-8999-999999996500/);
   assert.match(await detail.innerText(),/cadastro legado/);
   assert.match(await detail.getByRole('link').getAttribute('href'),/\/queue\?contextid=/);
   detail=await open('ambiguous');
   assert.match(await detail.innerText(),/mais de uma fila/);assert.equal(await detail.locator('.queue-resource-identity code').count(),0);
   detail=await open('missing');
   assert.match(await detail.innerText(),/Nenhuma fila com esse número exato/);
   detail=await open('error');
   assert.match(await detail.innerText(),/Não foi possível consultar/);
   await detail.getByRole('button',{name:'Consultar novamente',exact:true}).click();
   assert.equal(await detail.locator('.queue-resource-identity code').count(),0);
   assert.deepEqual(errors,[]);
   await page.close();
  }
  console.log('PASS legacy queue identity: resolved GUID and exact edit URL; legacy-only, absent, ambiguous, error/retry; desktop/mobile light/dark');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
