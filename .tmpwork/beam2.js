const { chromium } = require('playwright-core');
const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
(async () => {
  const browser = await chromium.launch({ executablePath: EDGE, headless: true });
  const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
  await page.goto('http://localhost:9990/#/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2500);
  const box = await page.locator('.login-box').boundingBox();
  const clip = { x: box.x - 30, y: box.y - 30, width: box.width + 60, height: box.height + 60 };
  // 5s 周期、每条边只占 1.4s，采样要密一点才能抓到流星头
  for (let i = 0; i < 10; i++) {
    await page.screenshot({ path: `mtr_${i}.png`, clip });
    await page.waitForTimeout(280);
  }
  await browser.close();
})();
