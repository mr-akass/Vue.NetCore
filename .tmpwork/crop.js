const { chromium } = require('playwright-core');
const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
(async () => {
  const browser = await chromium.launch({ executablePath: EDGE, headless: true });
  const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
  await page.goto('http://localhost:9990/#/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2500);
  // 卡片区域放大截图(看细节：高光线、输入框、图标)
  await page.locator('.login-box').screenshot({ path: 'crop_normal.png' });
  // 聚焦态
  await page.locator('.form-input input').first().click();
  await page.locator('.form-input input').first().fill('admin');
  await page.waitForTimeout(400);
  await page.locator('.login-box').screenshot({ path: 'crop_focus.png' });
  // 按钮 hover 态
  await page.locator('.login-btn').hover();
  await page.waitForTimeout(700);
  await page.locator('.login-box').screenshot({ path: 'crop_hover.png' });
  await browser.close();
})();
