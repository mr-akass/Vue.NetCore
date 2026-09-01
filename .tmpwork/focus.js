const { chromium } = require('playwright-core');
const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
(async () => {
  const b = await chromium.launch({ executablePath: EDGE, headless: true });
  const p = await b.newPage({ viewport: { width: 1600, height: 900 } });
  await p.goto('http://localhost:9990/#/login', { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2200);
  console.log('初始 activeElement:', await p.evaluate(() => document.activeElement.tagName + '.' + document.activeElement.className));
  // 直接按回车(焦点在 body) → 事件不会冒泡到 .login-page，预期无反应
  await p.keyboard.press('Enter');
  await p.waitForTimeout(1200);
  console.log('body 焦点回车 toast 数:', await p.locator('.el-message').count());
  // 点击卡片空白处(不是输入框)再回车 → 焦点在 div 内，应触发校验
  await p.locator('.card-title').click();
  await p.keyboard.press('Enter');
  await p.waitForTimeout(1200);
  console.log('卡片内点击后回车 toast 数:', await p.locator('.el-message').count());
  await b.close();
})();
