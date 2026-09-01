/**
 * 主题个性化：颜色/效果(玻璃、渐变)/布局排版/全局字号/背景图，按"用户+应用"分别保存
 * 设计说明：
 *  1) 所有旋钮最终都落成挂在 <html> 上的 CSS 变量：改一个变量整个 Element-Plus 组件库跟着变。
 *     旧的 .vol-theme-xx 是写死的 less 块,加一种颜色就要抄一份规则,不可持续,所以自定义主题走变量这条路
 *  2) 配置整体存服务端 Sys_ThemeSetting.ThemeJson,同时在 localStorage 留一份缓存：
 *     进页面先用缓存同步应用(避免"先默认色再跳成自定义色"的闪烁),再拿服务端结果覆盖
 *  3) 每个应用一份配置(缓存key=用户+应用,服务端主键=UserId+AppId)。切应用会整页刷新,刷新后读到的就是新应用那份
 *  4) 布局(layout)仍写回 localStorage 的 vol-layout/vol-theme,让 Index.vue 原有的读取逻辑不用改
 */
import { reactive } from 'vue'
import http from '@/api/http'
import { getSavedAppId } from '@/config/appConfig'

export const THEME_CACHE_PREFIX = 'vol-theme-config_'
/** Index.vue 里用的主题类名(自定义主题一律用这个,避开写死的 .vol-theme-blue 等旧规则) */
export const CUSTOM_THEME_NAME = 'custom'

/** 颜色预设：primary=主色, sider=侧边菜单, header=顶栏 */
export const COLOR_PRESETS = [
  { name: 'aurora', label: '极光紫', primary: '#6366f1', sider: '#1e2233', siderText: '#c9cede', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'classic', label: '经典蓝', primary: '#2d8cf0', sider: '#001529', siderText: '#c3cede', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'ocean', label: '深海青', primary: '#13c2c2', sider: '#062b32', siderText: '#bfe6e6', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'forest', label: '墨绿', primary: '#19be6b', sider: '#0b2b1d', siderText: '#c2e2d1', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'coral', label: '珊瑚红', primary: '#ed4014', sider: '#2b1512', siderText: '#e3cbc6', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'amber', label: '琥珀橙', primary: '#ff9900', sider: '#2b2013', siderText: '#e6d7c2', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'violet', label: '紫罗兰', primary: '#8b5cf6', sider: '#241a3a', siderText: '#d5cbe8', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'rose', label: '玫紫', primary: '#eb2f96', sider: '#2c1122', siderText: '#e6c8d9', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'graphite', label: '石墨黑', primary: '#4b5563', sider: '#17181c', siderText: '#c8cacf', header: '#ffffff', headerText: '#303133', dark: true },
  { name: 'snow', label: '素白', primary: '#409eff', sider: '#ffffff', siderText: '#4a5160', header: '#ffffff', headerText: '#303133', dark: false }
]

/** 整套方案：一键套用颜色+效果+排版(用户说的"布局排版多做几套") */
export const THEME_SCHEMES = [
  { name: 'default', label: '默认经典', config: { preset: 'classic', effect: 'flat', layout: 'top', density: 'default', radius: 'round', navStyle: 'card', pageStyle: 'plain' } },
  { name: 'aurora-glass', label: '极光玻璃', config: { preset: 'aurora', effect: 'glass', layout: 'classics', density: 'default', radius: 'pill', navStyle: 'pill', pageStyle: 'card' } },
  { name: 'compact', label: '紧凑高密', config: { preset: 'graphite', effect: 'flat', layout: 'top', density: 'compact', radius: 'square', navStyle: 'line', pageStyle: 'plain' } },
  { name: 'card', label: '圆润卡片', config: { preset: 'ocean', effect: 'gradient', layout: 'classics', density: 'loose', radius: 'pill', navStyle: 'pill', pageStyle: 'card' } },
  { name: 'immersive', label: '深色沉浸', config: { preset: 'violet', effect: 'gradient', layout: 'left', density: 'default', radius: 'round', navStyle: 'card', pageStyle: 'card' } }
]

/** 排版密度：顶栏/标签/菜单/表格行的尺寸整体缩放(rail=双栏布局最左侧那条一级菜单栏的宽度) */
const DENSITY = {
  compact: { header: 48, tab: 30, menu: 40, cellY: 4, gap: 8, sider: 180, rail: 68 },
  default: { header: 61, tab: 36, menu: 48, cellY: 8, gap: 12, sider: 200, rail: 80 },
  loose: { header: 70, tab: 42, menu: 56, cellY: 12, gap: 16, sider: 230, rail: 92 }
}

/** 圆角档位 */
const RADIUS = {
  square: { base: '0px', small: '0px', round: '2px', card: '0px' },
  round: { base: '6px', small: '4px', round: '10px', card: '8px' },
  pill: { base: '10px', small: '8px', round: '999px', card: '14px' }
}

export const DEFAULT_THEME = {
  v: 1,
  scheme: 'default',
  preset: 'classic',
  primary: '',
  effect: 'flat',
  layout: 'top',
  density: 'default',
  radius: 'round',
  navStyle: 'card',
  pageStyle: 'plain',
  menuCollapsed: false,
  fontSize: 14,
  bgImage: '',
  bgMask: 0.35,
  surfaceAlpha: 0.86
}

/** 当前生效的主题(响应式,Index.vue 监听 layout/menuCollapsed 做实时切换) */
export const themeState = reactive({
  inited: false,
  custom: false, //是否已启用自定义主题(没启用时不碰任何样式,保持框架原样)
  isSuperAdmin: false, //由 GetMyTheme 带回,决定是否显示"设为应用默认"
  ...DEFAULT_THEME
})

/* ------------------------------ 颜色计算 ------------------------------ */

function toRgb(color) {
  let hex = (color || '').trim().replace('#', '')
  if (hex.length === 3) {
    hex = hex[0] + hex[0] + hex[1] + hex[1] + hex[2] + hex[2]
  }
  if (!/^[0-9a-fA-F]{6}$/.test(hex)) {
    return { r: 45, g: 140, b: 240 }
  }
  return {
    r: parseInt(hex.substring(0, 2), 16),
    g: parseInt(hex.substring(2, 4), 16),
    b: parseInt(hex.substring(4, 6), 16)
  }
}

/** 两色按比例混合(weight=第二个颜色的占比),用来生成 Element-Plus 的 light-1~9/dark-2 */
function mix(color, target, weight) {
  const c = toRgb(color)
  const t = toRgb(target)
  const v = (a, b) => Math.round(a + (b - a) * weight)
  const hex = (n) => n.toString(16).padStart(2, '0')
  return `#${hex(v(c.r, t.r))}${hex(v(c.g, t.g))}${hex(v(c.b, t.b))}`
}

function rgba(color, alpha) {
  const c = toRgb(color)
  return `rgba(${c.r},${c.g},${c.b},${alpha})`
}

/** 颜色深浅:决定这个背景上该用白字还是黑字 */
function isDarkColor(color) {
  const c = toRgb(color)
  return (c.r * 299 + c.g * 587 + c.b * 114) / 1000 < 140
}

export function getPreset(name) {
  return COLOR_PRESETS.find((x) => x.name === name) || COLOR_PRESETS[1]
}

/* ------------------------------ 配置读写 ------------------------------ */

function getCurrentUserId() {
  try {
    const user = JSON.parse(localStorage.getItem('user') || '{}')
    return user.userId || 0
  } catch (e) {
    return 0
  }
}

/** 缓存key带用户和应用:换账号/换应用互不污染,与快捷导航 current_app_id_{userId} 是同一套思路 */
function getCacheKey(appId) {
  return `${THEME_CACHE_PREFIX}${getCurrentUserId()}_${appId == null ? 0 : appId}`
}

/** 当前应用ID(未选应用时为0,与后端 AppId=0 表示"不区分应用"对应) */
export function currentAppId() {
  return getSavedAppId() || 0
}

/** 补全缺省值并把取值限制在合法范围内(服务端不校验每个旋钮,前端自己兜住) */
export function normalizeTheme(theme) {
  const src = theme || {}
  const t = {}
  //只取认识的字段:themeState 里还有 inited/custom 这类运行时状态,不能跟着存进 ThemeJson
  Object.keys(DEFAULT_THEME).forEach((k) => {
    t[k] = src[k] === undefined || src[k] === null ? DEFAULT_THEME[k] : src[k]
  })
  const inList = (v, list, def) => (list.indexOf(v) >= 0 ? v : def)
  t.effect = inList(t.effect, ['flat', 'gradient', 'glass'], 'flat')
  t.layout = inList(t.layout, ['classics', 'top', 'left'], 'top')
  t.density = inList(t.density, ['compact', 'default', 'loose'], 'default')
  t.radius = inList(t.radius, ['square', 'round', 'pill'], 'round')
  t.navStyle = inList(t.navStyle, ['card', 'line', 'pill'], 'card')
  t.pageStyle = inList(t.pageStyle, ['plain', 'card'], 'plain')
  t.preset = getPreset(t.preset).name
  t.fontSize = Math.min(20, Math.max(12, parseInt(t.fontSize) || 14))
  t.bgMask = Math.min(0.85, Math.max(0, parseFloat(t.bgMask) >= 0 ? parseFloat(t.bgMask) : 0.35))
  //下限放到 0.1:背景图模式下用户会想让外壳几乎完全透出图片(全透明就看不见界面了,所以不给 0)
  t.surfaceAlpha = Math.min(1, Math.max(0.1, parseFloat(t.surfaceAlpha) || 0.86))
  t.menuCollapsed = !!t.menuCollapsed
  t.primary = /^#[0-9a-fA-F]{3,6}$/.test(t.primary || '') ? t.primary : ''
  t.bgImage = typeof t.bgImage === 'string' ? t.bgImage : ''
  return t
}

/** 背景图/头像这类相对地址要拼上接口地址(图片是后端 wwwroot 里的静态文件,不在前端站点下) */
export function getImageUrl(url) {
  if (!url) return ''
  if (/^(https?:)?\/\//i.test(url) || url.startsWith('data:')) return url
  return (http.ipAddress || '/') + url.replace(/^\//, '')
}

/* ------------------------------ 应用到界面 ------------------------------ */

/** 已写入 <html> 的变量名,重置时按这个名单删除 */
const _appliedVars = []

/**
 * 把主题配置翻译成 CSS 变量挂到 <html> 上,并在 <body> 上打效果/排版标记类
 * 标记类必须打在 body 上:弹窗/下拉是渲染到 body 下的,挂在 #vol-container 上的类选不到它们
 */
export function applyTheme(theme) {
  const t = normalizeTheme(theme)
  const preset = getPreset(t.preset)
  const primary = t.primary || preset.primary
  const root = document.documentElement
  const set = (k, v) => {
    if (_appliedVars.indexOf(k) < 0) _appliedVars.push(k)
    root.style.setProperty(k, v)
  }
  const del = (k) => root.style.removeProperty(k)

  //Element-Plus 主色系列:light-1~9 是与白色按 10%~90% 混合,dark-2 是与黑色混合20%
  set('--el-color-primary', primary)
  for (let i = 1; i <= 9; i++) {
    set(`--el-color-primary-light-${i}`, mix(primary, '#ffffff', i / 10))
  }
  set('--el-color-primary-dark-2', mix(primary, '#000000', 0.2))
  set('--vol-primary', primary)
  set('--vol-primary-soft', rgba(primary, 0.12))
  set('--vol-primary-hover', rgba(primary, 0.08))

  //全局字号:同时缩放 Element-Plus 的组件高度,否则字变大了输入框还是原来的高度
  const scale = t.fontSize / 14
  set('--vol-font-size', `${t.fontSize}px`)
  set('--el-font-size-base', `${t.fontSize}px`)
  set('--el-font-size-extra-small', `${t.fontSize - 2}px`)
  set('--el-font-size-small', `${t.fontSize - 1}px`)
  set('--el-font-size-medium', `${t.fontSize + 2}px`)
  set('--el-font-size-large', `${t.fontSize + 4}px`)
  set('--el-component-size', `${Math.round(32 * scale)}px`)
  set('--el-component-size-small', `${Math.round(24 * scale)}px`)
  set('--el-component-size-large', `${Math.round(40 * scale)}px`)

  //圆角
  const radius = RADIUS[t.radius]
  set('--el-border-radius-base', radius.base)
  set('--el-border-radius-small', radius.small)
  set('--el-border-radius-round', radius.round)
  set('--vol-radius-card', radius.card)

  //排版密度
  const density = DENSITY[t.density]
  set('--vol-header-height', `${Math.round(density.header * scale)}px`)
  set('--vol-tab-height', `${Math.round(density.tab * scale)}px`)
  set('--vol-menu-height', `${Math.round(density.menu * scale)}px`)
  set('--vol-cell-padding-y', `${density.cellY}px`)
  set('--vol-gap', `${density.gap}px`)
  set('--vol-sider-width', `${density.sider}px`)
  set('--vol-rail-width', `${Math.round(density.rail * scale)}px`)

  //外壳配色:侧边栏用预设色,顶栏跟着效果走(渐变时用主色渐变)
  const siderBg = preset.sider
  const siderDark = isDarkColor(siderBg)
  set('--vol-sider-bg', t.effect === 'gradient' ? `linear-gradient(160deg, ${siderBg} 0%, ${mix(siderBg, primary, 0.45)} 100%)` : siderBg)
  set('--vol-sider-text', preset.siderText)
  set('--vol-sider-active-bg', primary)
  set('--vol-sider-active-text', isDarkColor(primary) ? '#ffffff' : '#1f2430')
  set('--vol-sider-hover-bg', siderDark ? 'rgba(255,255,255,0.08)' : rgba(primary, 0.1))
  set('--vol-sider-border', siderDark ? 'rgba(255,255,255,0.06)' : '#e9ecf2')
  set('--vol-header-bg', t.effect === 'gradient' ? `linear-gradient(90deg, ${mix(primary, '#ffffff', 0.86)} 0%, #ffffff 60%)` : preset.header)
  set('--vol-header-text', preset.headerText)
  set('--vol-border', '#e9ecf2')

  //效果:玻璃=半透明+背景模糊,其余=实色
  //**有背景图时一律按半透明处理**:外壳(顶栏/侧边/标签/表格)是实色的话,图片只能从内容区四周露出来一点,
  //不是用户要的"整个页面被背景图铺满",所以通透度不再只服务于玻璃效果,它就是背景图的透明度旋钮
  const glass = t.effect === 'glass'
  const translucent = glass || !!t.bgImage
  const alpha = translucent ? t.surfaceAlpha : 1
  set('--vol-surface', `rgba(255,255,255,${alpha})`)
  //模糊只给玻璃效果:背景图+平面效果时用户要的是看清图片,糊掉就没意义了(而且大面积 backdrop-filter 很吃性能)
  set('--vol-surface-blur', glass ? 'blur(14px)' : 'none')
  set('--vol-main-bg', translucent ? 'transparent' : '#f5f7fa')
  if (translucent) {
    //渐变效果下保留渐变的形状,只是把每个色标换成带透明度的,否则一开背景图就丢了渐变外观
    const siderAlpha = Math.min(0.9, alpha)
    set('--vol-sider-bg', t.effect === 'gradient' ? `linear-gradient(160deg, ${rgba(siderBg, siderAlpha)} 0%, ${rgba(mix(siderBg, primary, 0.45), siderAlpha)} 100%)` : rgba(siderBg, siderAlpha))
    set('--vol-header-bg', t.effect === 'gradient' ? `linear-gradient(90deg, ${rgba(mix(primary, '#ffffff', 0.86), alpha)} 0%, ${rgba('#ffffff', alpha)} 60%)` : `rgba(255,255,255,${alpha})`)
  }

  //把 Element-Plus 的白底也换成半透明,否则表格/卡片/弹窗一片死白盖住背景
  //表格的 --el-table-* 不在这里设:Element-Plus 是把它们定义在 .el-table 元素自己身上的,
  //挂到 <html> 上会被就近的定义盖掉(反而误以为生效),所以放在 theme-custom.less 里按 .el-table 选择器给
  if (translucent) {
    const surface = `rgba(255,255,255,${alpha})`
    set('--el-bg-color-overlay', surface)
    set('--el-fill-color-blank', surface)
  } else {
    ;['--el-bg-color-overlay', '--el-fill-color-blank'].forEach(del)
  }

  //背景图:遮罩与图片写在同一个 background 里(单独加遮罩层要处理层级,反而容易挡住点击)
  if (t.bgImage) {
    const dim = `linear-gradient(${rgba('#000000', t.bgMask)}, ${rgba('#000000', t.bgMask)})`
    set('--vol-bg-image', `${dim}, url("${getImageUrl(t.bgImage)}")`)
  } else {
    del('--vol-bg-image')
  }

  //标记类:效果/密度/圆角/导航样式/页面样式,供 theme-custom.less 里选择
  const body = document.body
  const classes = ['vol-theme-active', `vol-effect-${t.effect}`, `vol-density-${t.density}`, `vol-radius-${t.radius}`, `vol-nav-${t.navStyle}`, `vol-page-${t.pageStyle}`]
  if (t.bgImage) classes.push('vol-has-bg')
  //半透明是玻璃效果和背景图共用的开关,单独一个类:样式里不用把两种条件各写一遍
  if (translucent) classes.push('vol-translucent')
  body.className = body.className
    .split(/\s+/)
    .filter((x) => x && !/^vol-(theme-active|effect|density|radius|nav|page|has-bg|translucent)/.test(x))
    .concat(classes)
    .join(' ')

  //布局写回 localStorage,让 Index.vue 原有的读取逻辑(vol-layout/vol-theme)直接生效
  localStorage.setItem('vol-layout', t.layout)
  localStorage.setItem('vol-theme', t.layout === 'left' ? `${CUSTOM_THEME_NAME}-aside` : CUSTOM_THEME_NAME)

  Object.assign(themeState, t, { inited: true, custom: true })
  return t
}

/** 关闭自定义主题,把加过的变量和标记类全部撤掉(重置时用) */
export function clearTheme() {
  //只删自己加过的变量,不能直接清掉 <html> 的 style(可能有别处写入的内联样式)
  _appliedVars.forEach((k) => document.documentElement.style.removeProperty(k))
  _appliedVars.length = 0
  document.body.className = document.body.className
    .split(/\s+/)
    .filter((x) => x && !/^vol-(theme-active|effect|density|radius|nav|page|has-bg|translucent)/.test(x))
    .join(' ')
  localStorage.removeItem('vol-theme')
  localStorage.removeItem('vol-layout')
  Object.assign(themeState, DEFAULT_THEME, { inited: true, custom: false })
}

/* ------------------------------ 缓存与接口 ------------------------------ */

/** 读缓存(同步),用于首屏在渲染前就把主题铺上,避免闪一下默认色 */
export function getCachedTheme(appId) {
  try {
    const json = localStorage.getItem(getCacheKey(appId == null ? currentAppId() : appId))
    return json ? JSON.parse(json) : null
  } catch (e) {
    return null
  }
}

function setCachedTheme(appId, theme) {
  const key = getCacheKey(appId)
  if (theme) {
    localStorage.setItem(key, JSON.stringify(theme))
  } else {
    localStorage.removeItem(key)
  }
}

/**
 * 首屏同步应用缓存的主题(在 main.js 里 mount 之前调用)
 * 没有缓存就什么都不做:框架保持原来的 .vol-theme-blue 那套样式
 */
export function applyCachedTheme() {
  const cached = getCachedTheme()
  if (cached) {
    applyTheme(cached)
    return true
  }
  //换用户/换应用后可能残留上一份的 custom 标记:类名还在但 CSS 变量没了,外壳会变成没样式的白板
  const flag = localStorage.getItem('vol-theme')
  if (flag && flag.indexOf(CUSTOM_THEME_NAME) === 0) {
    localStorage.removeItem('vol-theme')
    localStorage.removeItem('vol-layout')
  }
  themeState.inited = true
  return false
}

/**
 * 从服务端拉当前应用的主题并应用:我的 > 应用默认 > 不启用自定义主题
 * 服务端两条数据一次返回,少一次请求
 * 返回值:主题对象 | null(服务端确实没有配置) | undefined(请求失败)——
 * 面板的[恢复上次]要靠这个区别决定是"清成默认值"还是"提示失败、什么都别动"
 */
export function loadTheme(appId) {
  const id = appId == null ? currentAppId() : appId
  return http
    .get(`api/Sys_ThemeSetting/GetMyTheme?appId=${id}`, {}, false)
    .then((x) => {
      const data = x && (x.data !== undefined ? x.data : x)
      themeState.isSuperAdmin = !!(data && data.isSuperAdmin)
      const json = (data && (data.theme || data.appDefault)) || ''
      if (!json) {
        //服务端没有配置:清掉本地缓存(可能是在别的机器上重置过),回到框架默认样式
        setCachedTheme(id, null)
        if (themeState.custom) clearTheme()
        themeState.inited = true
        return null
      }
      const theme = normalizeTheme(JSON.parse(json))
      setCachedTheme(id, theme)
      applyTheme(theme)
      return theme
    })
    .catch(() => {
      //拉不到就用缓存,主题不是关键路径,不要因此报错打扰用户
      //这里必须返回 undefined 而不是 null:调用方要能把"请求失败"和"服务端没有配置"分开处理
      themeState.inited = true
      return undefined
    })
}

/** 保存我的主题 */
export function saveTheme(theme, appId) {
  const id = appId == null ? currentAppId() : appId
  const t = normalizeTheme(theme)
  return http
    .post('api/Sys_ThemeSetting/SaveMyTheme', { themeJson: JSON.stringify(t), appId: id }, true)
    .then((x) => {
      if (x.status) {
        setCachedTheme(id, t)
        applyTheme(t)
      }
      return x
    })
}

/** 设为当前应用的默认主题(超管) */
export function saveAppDefault(theme, appId) {
  const id = appId == null ? currentAppId() : appId
  return http.post('api/Sys_ThemeSetting/SaveAppDefault', { themeJson: JSON.stringify(normalizeTheme(theme)), appId: id }, true)
}

/** 重置我的主题(服务端删记录 + 清本地缓存) */
export function resetTheme(appId) {
  const id = appId == null ? currentAppId() : appId
  return http.post('api/Sys_ThemeSetting/ResetMyTheme', { appId: id }, true).then((x) => {
    if (x.status) {
      setCachedTheme(id, null)
      clearTheme()
    }
    return x
  })
}

/** 上传背景图,返回可直接写进 theme.bgImage 的相对地址 */
export function uploadBackground(file, appId) {
  const id = appId == null ? currentAppId() : appId
  const forms = new FormData()
  forms.append('fileInput', file, file.name)
  return http.post(`api/Sys_ThemeSetting/UploadBackground?appId=${id}`, forms, true, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

/** 删除背景图 */
export function removeBackground(appId) {
  const id = appId == null ? currentAppId() : appId
  return http.post('api/Sys_ThemeSetting/RemoveBackground', { appId: id }, true)
}

export default {
  COLOR_PRESETS,
  THEME_SCHEMES,
  DEFAULT_THEME,
  themeState,
  getPreset,
  getImageUrl,
  normalizeTheme,
  applyTheme,
  clearTheme,
  applyCachedTheme,
  getCachedTheme,
  loadTheme,
  saveTheme,
  saveAppDefault,
  resetTheme,
  uploadBackground,
  removeBackground,
  currentAppId
}
