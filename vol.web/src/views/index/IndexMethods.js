import MessageConfig from './MessageConfig.js'
import { computed, watch, nextTick } from 'vue'
import { getSavedAppId } from '@/config/appConfig'

/** 顶部主导航「更多」预留宽度（与测量逻辑一致） */
export const TOP_NAV_RESERVE_MORE = 80

/** 顶部布局导航：宽度不足时折叠进「更多」下拉 */
export function registerTopNav(dataConfig) {
  const {
    layout,
    navMenuList,
    navCurrentMenuId,
    permissionInited,
    topNavRowRef,
    topNavMeasureRef,
    topNavVisibleList,
    topNavOverflowList
  } = dataConfig

  const getNavTopIndex = (item) => navMenuList.findIndex((x) => x.id === item.id)

  const navOverflowHasActive = computed(() =>
    topNavOverflowList.value.some((item) => item.id === navCurrentMenuId.value)
  )

  const recalcTopNav = () => {
    if (layout.value !== 'top') {
      topNavVisibleList.value = []
      topNavOverflowList.value = []
      return
    }
    const list = navMenuList
    if (!list.length) {
      topNavVisibleList.value = []
      topNavOverflowList.value = []
      return
    }
    nextTick(() => {
      const measure = topNavMeasureRef.value
      const row = topNavRowRef.value
      if (!measure || !row) {
        topNavVisibleList.value = [...list]
        topNavOverflowList.value = []
        return
      }
      const itemEls = measure.querySelectorAll('.nav-measure-item')
      if (itemEls.length !== list.length) {
        topNavVisibleList.value = [...list]
        topNavOverflowList.value = []
        return
      }
      const widths = Array.from(itemEls).map((el) => el.getBoundingClientRect().width)
      const avail = row.clientWidth
      const n = list.length
      if (avail <= 0) {
        topNavVisibleList.value = [...list]
        topNavOverflowList.value = []
        return
      }
      let sum = 0
      let visibleEnd = 0
      for (let i = 0; i < n; i++) {
        const w = widths[i] || 72
        const notLast = i < n - 1
        if (notLast) {
          if (sum + w + TOP_NAV_RESERVE_MORE <= avail) {
            sum += w
            visibleEnd = i + 1
          } else {
            break
          }
        } else if (sum + w <= avail) {
          visibleEnd = n
        }
      }
      if (visibleEnd === 0 && n > 0) {
        visibleEnd = 1
      }
      if (visibleEnd >= n) {
        topNavVisibleList.value = [...list]
        topNavOverflowList.value = []
      } else {
        topNavVisibleList.value = list.slice(0, visibleEnd)
        topNavOverflowList.value = list.slice(visibleEnd)
      }
    })
  }

  let topNavResizeObserver = null
  const bindTopNavResizeObserver = () => {
    topNavResizeObserver?.disconnect()
    topNavResizeObserver = null
    nextTick(() => {
      const row = topNavRowRef.value
      if (!row || typeof ResizeObserver === 'undefined') return
      topNavResizeObserver = new ResizeObserver(() => {
        recalcTopNav()
      })
      topNavResizeObserver.observe(row)
    })
  }

  watch(
    () => layout.value,
    () => {
      bindTopNavResizeObserver()
      recalcTopNav()
    },
    { flush: 'post' }
  )

  watch(
    () => permissionInited.value,
    (v) => {
      if (v) {
        nextTick(() => {
          bindTopNavResizeObserver()
          recalcTopNav()
        })
      }
    }
  )

  watch(
    navMenuList,
    () => {
      recalcTopNav()
    },
    { deep: true, flush: 'post' }
  )

  const mountTopNav = () => {
    bindTopNavResizeObserver()
    recalcTopNav()
  }

  const unmountTopNav = () => {
    topNavResizeObserver?.disconnect()
    topNavResizeObserver = null
  }

  return {
    getNavTopIndex,
    navOverflowHasActive,
    recalcTopNav,
    bindTopNavResizeObserver,
    mountTopNav,
    unmountTopNav
  }
}

/** 为了取"当前选中菜单id"塞进菜单数据里的首页项,不是真实菜单,不参与一级导航分组 */
export const HOME_MENU_ID = '0'

/**
 * 按布局把菜单分成"一级导航栏 + 侧边菜单"两份：
 *   classics(经典导航) —— 整棵树都在侧边栏,没有一级导航栏
 *   top(顶部导航)/left(双栏导航) —— 一级菜单单独成一栏(顶栏链接 / 最左侧窄栏),侧边栏只放当前一级下的子树
 * 布局在主题面板里是可以实时切换的,所以这段必须能反复执行,两个"还原"动作缺一不可：
 *   1) 分组时把直接子菜单的 parentId 改成 0(让它在侧边栏里当根),再次分组前要用 pid 还回去,否则层级散了
 *   2) children 一并清空交给 VolMenu 按 parentId 重拼,否则上一次分组平铺出来的孙子菜单会变成二级菜单
 */
export function groupMenuByLayout(dataConfig, layoutValue) {
  const { navMenuList, menuData, navCurrentMenuId } = dataConfig
  const all = dataConfig.menuOptions.value || []
  navMenuList.splice(0)
  menuData.splice(0)
  if (!all.length) return
  all.forEach((m) => {
    m.parentId = m.pid
    m.children = []
  })
  if (layoutValue === 'classics') {
    menuData.push(...all)
    return
  }
  navMenuList.push(...all.filter((c) => !c.pid && c.id != HOME_MENU_ID))
  if (!navMenuList.length) {
    //只配了单层菜单时退回经典布局的显示方式,否则一级栏和侧边栏会同时是空的
    menuData.push(...all)
    return
  }
  navMenuList.forEach((m) => {
    const group = all.filter((c) => c.parentId == m.id)
    group.forEach((c) => (c.parentId = 0))
    //循环里继续往 group 里追加,顺带把三级及更深的菜单也收进来
    for (let i = 0; i < group.length; i++) {
      group.push(...all.filter((c) => c.parentId == group[i].id))
    }
    m.groupMenus = group
  })
  //记住的一级菜单还在就接着用它,否则回到第一个
  let index = navMenuList.findIndex((c) => c.id === navCurrentMenuId.value)
  if (index == -1) index = 0
  navCurrentMenuId.value = navMenuList[index].id
  menuData.push(...navMenuList[index].groupMenus)
}

export default async function (proxy, dataConfig, router, onSelect) {
  const store = proxy.$store
  let _userInfo = store.getters.getUserInfo()
  if (!_userInfo) {
    router.push({ path: "/login" })
    return;
  }
  const userInfo = dataConfig.userInfo
  userInfo.name = _userInfo.userName
  if (_userInfo.img) {
    userInfo.img = proxy.base.getImgSrc(
      _userInfo.img,
      proxy.$global.oss?.url || proxy.http.ipAddress
    )
  }

  //多应用支持：初始化应用列表并应用当前应用的标题配置
  await store.dispatch('initAppList')
  const appConfig = store.getters.getAppConfig()
  if (appConfig && appConfig.title) {
    document.title = appConfig.title
  }
  //按当前选择的应用加载菜单(超管可不带appId加载全部菜单)
  const currentAppId = getSavedAppId()
  const menuUrl = currentAppId ? `api/menu/getTreeMenu?appId=${currentAppId}` : 'api/menu/getTreeMenu'
  proxy.http.get(menuUrl, {}, false).then((result) => {
    const menuOptions = dataConfig.menuOptions
    const selectId = dataConfig.selectId
    proxy.base.setAsyncApi(result.asyncApi)

    let data = result.menu
    let dataItem = data.find((x) => {
      return x.linkType == 3 && (!x.enable || x.enable == 1)
    })
    data.push({ id: '0', name: '首页', url: '/home', icon: 'bi-house' }) // 为了获取选中id使用

    initQueryParams(data)

    store.dispatch('setPermission', data)

    menuOptions.value = data
    //一级导航与侧边菜单按当前布局分组(布局能在主题面板里实时切换,分组逻辑抽成了可反复执行的函数)
    groupMenuByLayout(dataConfig, dataConfig.layout.value)

    // console.log(JSON.stringify(menuOptions.value))

    dataConfig.permissionInited.value = true

    //开启消息推送（main.js中设置是否开启signalR)
    if (proxy.$global.signalR) {
      MessageConfig(proxy.http, (result) => {
        // messageList.unshift(result)
        //    console.log(result)
      })
    }

    //当前刷新是不是首页
    if (router.currentRoute.value?.path != dataConfig.navigation[0]?.path) {
      //查找系统菜单
      let item = menuOptions.value.find((x) => {
        return x.url && x.url == router.currentRoute.value.fullPath
      })
      if (!item) {
        item = menuOptions.value.find((x) => {
          return x.path == router.currentRoute.value.path
        })
      }
      if (item) return onSelect(item.id)
      //查找顶部快捷连接
      item = dataConfig.links.value.find((x) => {
        return x.path == router.currentRoute.value.path
      })
      //查找最后一次跳转的页面
      if (!item) {
        item = getItem(proxy, router)
      }
      if (item) {
        return proxy.$tabs.open(item, false)
      } else {
        const indexVal = dataConfig.navigation.findIndex(x => { return x.path == router.currentRoute.value.path })
        if (indexVal != -1) {
          selectId.value = indexVal + '';
          return;
        }
      }
    }
    selectId.value = '0'
  })
}

const getItem = (proxy, router) => {
  let item =
    router.options.routes[0].children.find((x) => {
      return x.path == router.currentRoute.value.path
    }) || {}
  //生成的编辑页面tabs名称
  if (item.meta && item.meta.name) {
    let name = item.meta.name
    if (item.meta.edit) {
      name =
        proxy.$ts(name) +
        (router.currentRoute.value.query.id
          ? '(' + proxy.$ts('编辑') + ')'
          : '(' + proxy.$ts('新建') + ')')
    }
    item = {
      name: name,
      path: router.currentRoute.value.path,
      query: router.currentRoute.value.query
    }
    return proxy.$tabs.open(item, false)
  } else {
    let nav = localStorage.getItem(window.location.origin + '_tabs')
    return nav ? JSON.parse(nav) : null
  }
  //  return null;
}

const initQueryParams = (data) => {
  for (let index = 0; index < data.length; index++) {
    const d = data[index]
    d.pid = d.parentId
    if (d.url && d.url.indexOf('?') != -1) {
      let _arr = d.url.split('?')
      d.path = _arr[0]
      _arr = _arr[1].split('&')
      let queryObj = {}
      for (let i = 0; i < _arr.length; i++) {
        // 遍历参数
        if (_arr[i].indexOf('=') != -1) {
          // 如果参数中有值
          let str = _arr[i].split('=')
          queryObj[str[0]] = str[1]
        }
      }
      d.query = queryObj
    } else {
      d.path = d.url
    }
    d.to = d.url
  }
}
