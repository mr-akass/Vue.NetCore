import { createRouter, createWebHistory, createWebHashHistory } from 'vue-router'
import viewgird from './viewGird'
import store from '../store/index'
import redirect from './redirect'
import { getSavedAppId } from '@/config/appConfig'
const routes = [
  {
    path: '/',
    name: 'Index',
    component: () => import('@/views/Index.vue'),
    redirect: '/home',
    children: [
      ...viewgird,
      ...redirect,
      {
        path: '/home',
        name: 'home',
        component: () => import('@/views/Home.vue')
      }, {
        path: '/UserInfo',
        name: 'UserInfo',
        component: () => import('@/views/sys/UserInfo.vue')
      },
      {
        path: '/sysMenu',
        name: 'sysMenu',
        component: () => import('@/views/sys/system/Sys_Menu.vue')
      }, {
        path: '/coder',
        name: 'coder',
        component: () => import('@/views/builder/coder.vue')
      },
      {
        path: '/formDraggable',  //表单设计
        name: 'formDraggable',
        component: () => import('@/views/formDraggable/formDraggable.vue')
      },
      {
        path: '/formSubmit',  //表单提交页面
        name: 'formSubmit',
        component: () => import('@/views/formDraggable/FormSubmit.vue'),
        meta:{
          keepAlive:false
        }
      },
      {
        path: '/formCollectionResultTree',  //显示收集的数据表单
        name: 'formCollectionResultTree',
        component: () => import('@/views/formDraggable/FormCollectionResultTree.vue'),
        meta:{
          keepAlive:false
        }
      },
      {
        path: '/signalR',  //消息推送
        name: 'signalR',
        component: () => import('@/views/signalR/Index.vue'),
        meta:{
          keepAlive:false
        }
      }
    ]
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/Login.vue'),
    meta:{
        anonymous:true
      }
  },
  {
    path: '/guide', //子系统(应用)选择页
    name: 'guide',
    component: () => import('@/views/Guide.vue'),
    meta: {
      requiresApp: false //标记此页面不需要已选应用
    }
  }
]

const router = createRouter({
  history: createWebHashHistory(), //createWebHistory(process.env.BASE_URL),
  routes
})


router.beforeEach((to, from, next) => {
  if (to.matched.length == 0) return next({ path: '/404' });
  //2020.06.03增加路由切换时加载提示
  store.dispatch("onLoading", true);

  //多应用支持：非超管已登录但未选择应用时，强制跳转到应用选择页(/guide)
  const isAnonymous = to.hasOwnProperty('meta') && to.meta.anonymous;
  const requiresApp = !(to.meta && to.meta.requiresApp === false) && !isAnonymous;
  if (requiresApp && to.path !== '/login' && store.getters.isLogin()) {
    let isSuperAdmin = false;
    try {
      const userInfo = store.getters.getUserInfo();
      isSuperAdmin = !!(userInfo && userInfo.isSuperAdmin);
    } catch (e) { /* ignore */ }
    if (!isSuperAdmin && !getSavedAppId()) {
      return next({ path: '/guide' });
    }
  }

  if (isAnonymous || store.getters.isLogin() || to.path == '/login') {
    return next();
  }

  next({ path: '/login', query: { redirect: Math.random() } });
})
router.afterEach((to, from) => {
  store.dispatch("onLoading", false);
})
router.onError((error) => {
  // const targetPath = router.currentRoute.value.matched;
  try {
      console.log(error.message);
    if (process.env.NODE_ENV == 'development') {
      //alert(error.message)
    }
    localStorage.setItem("route_error", error.message)
  } catch (e) {

  }
 // window.location.href = '/'
});
export default router
