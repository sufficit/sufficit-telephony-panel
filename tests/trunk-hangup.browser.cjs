const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');
(async()=>{
 const browser=await chromium.launch({headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1440,height:1050}});
 await page.addInitScript(()=>localStorage.setItem('telephony-panel-language-v2','en'));
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const base='http://127.0.0.1:5169/board?preview=true&trunkpreview=true&acdpreview=waiting';
 await page.goto(base);
 const trunk=page.locator('.board-trunks .board-tile').filter({hasText:'Fornecedor demonstração'});
 await trunk.waitFor();assert.equal(await trunk.count(),1);assert.equal(await trunk.locator('.tile-node').count(),0);
 await trunk.click();let dialog=page.getByRole('dialog');await dialog.waitFor();
 assert.equal(await dialog.locator('tbody tr').count(),3);
 assert.match(await dialog.innerText(),/google-voip/);assert.match(await dialog.innerText(),/eveo-voip/);assert.match(await dialog.innerText(),/apoint-voip/);
 const end=dialog.getByRole('button',{name:'End channel',exact:true});assert.equal(await end.count(),3);
 await end.nth(1).click();const confirmation=dialog.getByRole('region',{name:'End this channel?'});await confirmation.waitFor();
 assert.match(await confirmation.innerText(),/eveo-voip/);
 await confirmation.getByRole('button',{name:'Cancel',exact:true}).click();await confirmation.waitFor({state:'detached'});
 await end.nth(1).click();await confirmation.waitFor();
 fs.mkdirSync('.impeccable/review',{recursive:true});await dialog.screenshot({path:'.impeccable/review/trunk-hangup-desktop.png'});
 await confirmation.getByRole('button',{name:'Confirm and end channel',exact:true}).click();
 await page.getByText('Demonstration: termination confirmed. No channel was ended.',{exact:true}).waitFor();
 assert.equal(await confirmation.count(),0);assert.equal(await dialog.locator('tbody tr').count(),3,'Demo never removes real/synthetic channels on a simulated acceptance');
 await page.keyboard.press('Escape');
 for(const selector of ['.board-grid .board-tile','.board-queues .board-tile']){
   await page.locator(selector).filter({hasText:selector.includes('queues')?'Homologação Sufficit':'Hugo'}).first().click();
   await dialog.waitFor();const button=dialog.getByRole('button',{name:'End channel',exact:true}).first();assert(await button.isEnabled());
   await button.click();await dialog.getByRole('heading',{name:'End this channel?',exact:true}).waitFor();
   await dialog.getByRole('button',{name:'Cancel',exact:true}).click();await page.keyboard.press('Escape');
 }
 await page.goto(base+'&node=eveo-voip');await trunk.waitFor();await trunk.click();await dialog.waitFor();
 assert.equal(await dialog.locator('tbody tr').count(),1);assert.match(await dialog.innerText(),/eveo-voip/);assert.doesNotMatch(await dialog.innerText(),/google-voip|apoint-voip/);
 await page.keyboard.press('Escape');
 await page.goto(base);await trunk.waitFor();await page.locator('.board-trunks .board-card').filter({hasText:'Fornecedor demonstração'}).locator('.card-menu-trigger').click();
 await page.getByRole('menuitem',{name:'Listen continuously by prefix',exact:true}).click();await dialog.waitFor();
 const choices=dialog.getByRole('combobox',{name:'Authorized prefix',exact:true});await choices.click();
 await page.getByRole('option').filter({hasText:'SIP/provider-'}).first().waitFor();assert.equal(await page.getByRole('option').filter({hasText:'SIP/provider-'}).count(),3);
 await page.getByRole('option').filter({hasText:'eveo-voip'}).click();
 await page.keyboard.press('Escape');
 await page.goto('http://127.0.0.1:5169/?preview=true&tab=channels');
 await page.getByRole('button',{name:'End channel',exact:true}).first().click();await dialog.waitFor();
 await dialog.getByRole('button',{name:'Cancel',exact:true}).click();await dialog.waitFor({state:'detached'});
 await page.goto(base+'&previewaccess=single&contextid=11111111111171118111111111111111');await trunk.waitFor();await trunk.click();await dialog.waitFor();
 assert.equal(await dialog.getByRole('button',{name:'End channel',exact:true}).count(),0,'View-only customer never gets termination action');
 await page.keyboard.press('Escape');
 await page.addInitScript(()=>{localStorage.setItem('telephony-panel-language-v2','pt-BR');localStorage.setItem('telephony-panel-theme','dark');});
 await page.setViewportSize({width:390,height:844});await page.goto(base);await trunk.waitFor();await trunk.click();await dialog.waitFor();
 await dialog.getByRole('button',{name:'Encerrar canal',exact:true}).first().click();await dialog.getByRole('heading',{name:'Encerrar este canal?',exact:true}).waitFor();
 assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
 await page.screenshot({path:'.impeccable/review/trunk-hangup-mobile.png'});
 assert.deepEqual(errors,[]);console.log('PASS provider grouping, 3 exact nodes, hangup confirmation/cancel/demo, every card type, node filter, per-node prefixes, channels tab, customer denial, PT/mobile/dark');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
