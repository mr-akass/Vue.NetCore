const { chromium } = require('playwright-core');
const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
const toast = async (page) => {
  try { return (await page.locator('.el-message').first().innerText({ timeout: 4000 })).replace(/\n/g, ' '); }
  catch { return '(no toast)'; }
};
(async () => {
  const browser = await chromium.launch({ executablePath: EDGE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1600, height: 900 } });
  const page = await ctx.newPage();
  const errs = [];
  page.on('console', (m) => { if (m.type() === 'error') errs.push(m.text()); });
  const reqs = [];
  page.on('request', (r) => { if (r.url().includes('/api/user/login')) reqs.push(r.method()); });

  await page.goto('http://localhost:9990/#/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);

  // 1) 空账号直接回车 → 前端校验提示，且不应发请求
  await page.keyboard.press('Enter');
  console.log('1 空账号回车 toast:', await toast(page), '| 请求数:', reqs.length);
  await page.waitForTimeout(3200);

  // 2) 只填账号回车 → 提示请输入密码
  await page.locator('.form-input input').first().fill('admin');
  await page.locator('.form-input input').first().press('Enter');
  console.log('2 缺密码回车 toast:', await toast(page), '| 请求数:', reqs.length);
  await page.waitForTimeout(3200);

  // 3) 错误密码回车 → 应真的发请求并返回后端错误
  await page.locator('.form-input input').nth(1).fill('wrong-pwd-xxx');
  await page.locator('.form-input input').nth(1).press('Enter');
  await page.waitForTimeout(1500);
  console.log('3 错误密码回车 toast:', await toast(page), '| 请求数:', reqs.length);
  await page.waitForTimeout(3200);

  // 4) 密码框眼睛图标切换明文
  console.log('4 密码框 type(切换前):', await page.locator('.form-input input').nth(1).getAttribute('type'));
  await page.locator('.pwd-eye').click();
  console.log('  密码框 type(切换后):', await page.locator('.form-input input').nth(1).getAttribute('type'));

  console.log('console errors:', JSON.stringify(errs.slice(0, 5)));
  await browser.close();
})();
