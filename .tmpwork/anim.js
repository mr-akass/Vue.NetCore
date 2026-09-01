const { chromium } = require('playwright-core');
const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
(async () => {
  const browser = await chromium.launch({ executablePath: EDGE, headless: true });
  const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
  await page.goto('http://localhost:9990/#/login', { waitUntil: 'domcontentloaded' });
  // 等到动画进行到中段再截，看光点/极光的实际观感
  await page.waitForTimeout(9000);
  await page.screenshot({ path: 'anim_t9.png' });
  await page.waitForTimeout(8000);
  await page.screenshot({ path: 'anim_t17.png' });
  await browser.close();
})();
