import { createStore } from 'vuex'
import {
  APP_LIST_KEY,
  DEFAULT_APP_ID,
  fetchAppList,
  getAppById,
  getDefaultApp,
  isValidAppId,
  getSavedAppId,
  saveAppId
} from '@/config/appConfig'
const keys = { USER: 'user' }
function getUserInfo(state) {
  if (state.userInfo) return state.userInfo
  let userInfo = localStorage.getItem(keys.USER)
  if (userInfo) {
    try {
      state.userInfo = JSON.parse(userInfo)
    } catch {}
  }
  return state.userInfo || {}
}
//从 localStorage 加载应用列表缓存
function loadAppListCache() {
  try {
    const cached = localStorage.getItem(APP_LIST_KEY)
    return cached ? JSON.parse(cached) : []
  } catch {
    return []
  }
}
export default createStore({
  state: {
    data: {},
    permission: [],
    isLoading: false, //2020.06.03增加路由切换时加载提示
    userInfo: null,
    appLang: {},
    serviceList: [],
    //多应用(子系统)状态
    appList: loadAppListCache(),
    currentAppId: getSavedAppId() || DEFAULT_APP_ID,
    //用户有权限访问的appIds(登录时返回，仅内存)
    userAppIds: []
  },
  mutations: {
    setLocal(state, source) {
      state.appLang = source
    },
    setPermission(state, data) {
      //调用方式 this.$store.commit('setPermission', data)
      if (!data || typeof data != 'object') return
      if (data instanceof Array) {
        state.permission.push(...data)
      } else {
        state.permission = data
      }
    },
    setUserInfo(state, data) {
      state.userInfo = data
      localStorage.setItem(keys.USER, JSON.stringify(data))
    },
    clearUserInfo(state) {
      state.permission = []
      state.userInfo = null
      state.userAppIds = []
      localStorage.removeItem(keys.USER)
      //注意：不清除当前应用appId，重新登录后可直接进入之前选择的应用(key按用户隔离)
    },
    //设置用户有权限访问的appIds(登录时)
    setUserAppIds(state, appIds) {
      state.userAppIds = appIds || []
    },
    //设置应用列表
    setAppList(state, appList) {
      state.appList = appList
      localStorage.setItem(APP_LIST_KEY, JSON.stringify(appList))
    },
    //设置当前应用(通过appId)
    setCurrentApp(state, appId) {
      const id = parseInt(appId)
      if (isValidAppId(id, state.appList)) {
        state.currentAppId = id
        saveAppId(id)
      }
    },
    test(state) {
      return 113344
    },
    updateLoadingState(state, flag) {
      state.isLoading = flag
    },
    setServiceList(state, data) {
      state.serviceList = data
    }
  },
  getters: {
    getServiceList: (state) => (path) => {
      return state.serviceList || []
    },
    //获取应用列表
    getAppList: (state) => () => state.appList,
    //获取当前应用ID
    getCurrentAppId: (state) => () => state.currentAppId,
    //获取当前应用配置(完整对象)
    getAppConfig: (state) => () => getAppById(state.currentAppId, state.appList),
    //验证appId是否有效
    isValidAppId: (state) => (appId) => isValidAppId(appId, state.appList),
    //获取用户有权限访问的appIds
    getUserAppIds: (state) => () => state.userAppIds,
    local: (state) => () => {
      return state.appLang || {}
    },
    getPermission: (state) => (path) => {
      //调用方式 store.getters.getPermission('sys_User')
      if (!path) return state.permission
      path = path.toLowerCase()
      return state.permission.find((x) => x.path && x.path.toLowerCase() == path)
    },
    getUserInfo: (state) => () => {
      getUserInfo(state)
      return state.userInfo
    },
    getUserName: (state) => () => {
      getUserInfo(state)
      if (state.userInfo) {
        return state.userInfo.userName
      }
      return '未获取到登陆信息'
    },
    getToken: (state) => () => {
      getUserInfo(state)
      if (state.userInfo) {
        return 'Bearer ' + state.userInfo.token
      }
      return ''
    },
    isLogin: (state) => () => {
      if (getUserInfo(state)) {
        return true
      }
      return false
    },
    isLoading: (state) => () => {
      return state.isLoading
    },
    data: (state) => () => {
      return state.data
    },
    getData: (state) => () => {
      return state.data
    }
  },
  actions: {
    setPermission(context, data) {
      context.commit('setPermission', data) //调用方式 store.dispatch('push')
    },
    //初始化应用列表(从API获取，服务端按当前用户角色过滤)
    async initAppList({ commit, state }) {
      try {
        const appList = await fetchAppList()
        commit('setAppList', appList)
        //验证当前appId是否有效，无效则使用默认
        if (!isValidAppId(state.currentAppId, appList)) {
          const defaultApp = getDefaultApp(appList)
          if (defaultApp) {
            commit('setCurrentApp', defaultApp.appId)
          }
        }
        return appList
      } catch (error) {
        console.error('Failed to fetch app list:', error)
        return []
      }
    },
    toDo(context) {
      return context.Store.m
    },
    onLoading(context, flag) {
      context.commit('updateLoadingState', flag)
    }
  }
})
