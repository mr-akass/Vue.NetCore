const { chromium } = require('playwright-core');
const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
(async () => {
  const browser = await chromium.launch({ executablePath: EDGE, headless: true });
  for (const [w, h, name] of [[1600, 900, 'wide'], [1366, 700, 'laptop'], [420, 860, 'mobile']]) {
    const page = await browser.newPage({ viewport: { width: w, height: h } });
    await page.goto('http://localhost:9990/#/login', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2200);
    await page.screenshot({ path: `resp_${name}.png` });
    // 卡片是否完整在视口内
    const box = await page.locator('.login-box').boundingBox();
    console.log(name, `${w}x${h}`, 'box:', JSON.stringify(box), '溢出:', box.y < 0 || box.y + box.height > h);
    await page.close();
  }
  await browser.close();
})();
