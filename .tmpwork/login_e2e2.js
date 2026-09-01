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

  // 焦点在空账号框内按回车 → 前端校验拦下，不发请求
  const user = page.locator('.form-input input').first();
  const pwd = page.locator('.form-input input').nth(1);
  await user.click();
  await user.press('Enter');
  console.log('A 账号框内空值回车 toast:', await toast(page), '| 请求数:', reqs.length);
  await page.waitForTimeout(3200);

  // 密码框内回车(账号已填) → 走登录
  await user.fill('admin');
  await pwd.fill('123456');
  await pwd.press('Enter');
  await page.waitForTimeout(2500);
  console.log('B 正确密码回车 → url:', page.url(), '| 请求数:', reqs.length);
  console.log('  localStorage.user 有 token:', await page.evaluate(() => {
    try { return !!JSON.parse(localStorage.getItem('user') || '{}').token; } catch { return false; }
  }));
  await page.screenshot({ path: 'after_login.png' });
  console.log('console errors:', JSON.stringify(errs.slice(0, 4)));
  await browser.close();
})();
