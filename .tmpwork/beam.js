const { chromium } = require('playwright-core');
const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
(async () => {
  const browser = await chromium.launch({ executablePath: EDGE, headless: true });
  const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
  await page.goto('http://localhost:9990/#/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2500);
  const box = await page.locator('.login-box').boundingBox();
  console.log('box:', JSON.stringify(box));
  // 沿一个 6s 周期每 0.8s 截一张卡片区域，看光束是否真的绕圈
  const clip = { x: box.x - 24, y: box.y - 24, width: box.width + 48, height: box.height + 48 };
  for (let i = 0; i < 8; i++) {
    await page.screenshot({ path: `beam_${i}.png`, clip });
    await page.waitForTimeout(700);
  }
  await page.screenshot({ path: 'beam_full.png' });
  await browser.close();
})();
