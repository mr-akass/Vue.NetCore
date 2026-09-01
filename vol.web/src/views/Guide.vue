<template>
  <div class="guide-page">
    <!-- 背景：光斑 + 网格(与登录页同一套视觉) -->
    <div class="bg-glow glow-1"></div>
    <div class="bg-glow glow-2"></div>
    <div class="bg-glow glow-3"></div>
    <div class="bg-grid"></div>

    <!-- 顶栏：品牌 + 退出 -->
    <div class="top-bar">
      <div class="brand-mini">
        <div class="brand-mark">
          <i class="el-icon-platform"></i>
        </div>
        <span>Vol开发框架</span>
      </div>
      <span class="logout-link" @click="logout">{{ $ts("退出登录") }}</span>
    </div>

    <div class="content-wrapper">
      <div class="guide-header">
        <div class="guide-title">{{ $ts("选择应用") }}</div>
        <div class="guide-subtitle">SELECT AN APPLICATION</div>
      </div>

      <div v-if="loading" class="loading-container">
        <div class="loading-spinner"></div>
        <span>{{ $ts("正在加载") }}...</span>
      </div>

      <div v-else-if="apps.length || isSuperAdmin" class="app-grid">
        <!-- 超管专属：不按应用过滤的完整菜单视图 -->
        <div v-if="isSuperAdmin" class="app-card" style="--delay: 0s; --app-color: #8b5cf6"
          @click="selectFullMenu">
          <div class="app-icon">
            <i class="el-icon-s-grid"></i>
          </div>
          <div class="app-title">{{ $ts("完整菜单") }}</div>
          <div class="app-desc">{{ $ts("不按应用过滤，查看全部菜单(超级管理员)") }}</div>
          <div class="app-arrow">
            <span>{{ $ts("进入") }}</span>
            <i class="el-icon-right"></i>
          </div>
        </div>
        <div v-for="(app, index) in apps" :key="app.appId" class="app-card"
          :style="{ '--delay': (index + 1) * 0.08 + 's', '--app-color': app.primaryColor || '#6366f1' }"
          @click="selectApp(app)">
          <div class="app-icon">
            <i :class="app.icon || 'el-icon-menu'"></i>
          </div>
          <div class="app-title">{{ app.appName }}</div>
          <div class="app-desc">{{ app.title }}</div>
          <div class="app-arrow">
            <span>{{ $ts("进入") }}</span>
            <i class="el-icon-right"></i>
          </div>
        </div>
      </div>

      <div v-else class="empty-container">
        <i class="el-icon-warning-outline"></i>
        <div>{{ $ts("当前账号没有任何应用权限，请联系管理员分配") }}</div>
        <span class="logout-link" @click="logout">{{ $ts("返回登录") }}</span>
      </div>
    </div>
  </div>
</template>

<script>
import { defineComponent, ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { fetchAppList, saveAppId, removeSavedAppId } from '@/config/appConfig';
import store from '@/store/index';

export default defineComponent({
  name: 'Guide',
  setup() {
    const router = useRouter();
    const allApps = ref([]);
    const loading = ref(true);

    const isSuperAdmin = computed(() => {
      const userInfo = store.getters.getUserInfo();
      return !!(userInfo && userInfo.isSuperAdmin);
    });

    //根据用户权限过滤应用列表(GetEnabledApps服务端已按角色过滤，此处为双保险)
    const apps = computed(() => {
      const userAppIds = store.getters.getUserAppIds();
      const userInfo = store.getters.getUserInfo();

      //超级管理员显示所有应用
      if (userInfo && userInfo.isSuperAdmin) {
        return allApps.value;
      }
      //userAppIds为空(如刷新页面后)时显示服务端过滤结果
      if (!userAppIds || userAppIds.length === 0) {
        return allApps.value;
      }
      return allApps.value.filter((app) => userAppIds.includes(app.appId));
    });

    const loadApps = async () => {
      loading.value = true;
      try {
        const appList = await fetchAppList();
        allApps.value = appList;
        store.commit('setAppList', appList);
      } catch (error) {
        console.error('Failed to load apps:', error);
      } finally {
        loading.value = false;
      }
    };

    //选择应用：保存appId并进入首页
    const selectApp = (app) => {
      saveAppId(app.appId);
      store.commit('setCurrentApp', app.appId);
      window.location.href = '/'; //整页刷新，确保菜单/主题按新应用重新加载
    };

    //超管专属：清除已选应用，进入完整菜单视图
    const selectFullMenu = () => {
      removeSavedAppId();
      window.location.href = '/';
    };

    const logout = () => {
      store.commit('clearUserInfo', '');
      router.push({ path: '/login' });
    };

    onMounted(() => {
      loadApps();
    });

    return { apps, loading, selectApp, selectFullMenu, logout, isSuperAdmin };
  }
});
</script>

<style scoped lang="less">
.guide-page {
  position: relative;
  width: 100%;
  min-height: 100vh;
  overflow-y: auto;
  overflow-x: hidden;
  background: #0b1022;
  display: flex;
}

//背景光斑(与Login.vue一致)
.bg-glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(110px);
  pointer-events: none;
  animation: drift 16s ease-in-out infinite;
}

.glow-1 {
  width: 560px;
  height: 560px;
  background: rgba(79, 70, 229, 0.4);
  top: -160px;
  left: -120px;
}

.glow-2 {
  width: 480px;
  height: 480px;
  background: rgba(14, 165, 233, 0.28);
  bottom: -160px;
  right: -100px;
  animation-delay: 5s;
}

.glow-3 {
  width: 360px;
  height: 360px;
  background: rgba(168, 85, 247, 0.25);
  top: 30%;
  left: 55%;
  animation-delay: 9s;
}

@keyframes drift {

  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  50% {
    transform: translate(30px, -30px) scale(1.06);
  }
}

//背景网格(中心可见、四周淡出)
.bg-grid {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.05) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.05) 1px, transparent 1px);
  background-size: 54px 54px;
  -webkit-mask-image: radial-gradient(ellipse 70% 60% at 50% 45%, #000 20%, transparent 100%);
  mask-image: radial-gradient(ellipse 70% 60% at 50% 45%, #000 20%, transparent 100%);
}

//顶栏
.top-bar {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  z-index: 3;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 32px;
}

.brand-mini {
  display: flex;
  align-items: center;
  gap: 10px;

  .brand-mark {
    width: 34px;
    height: 34px;
    border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    box-shadow: 0 6px 16px rgba(99, 102, 241, 0.4);
    display: flex;
    align-items: center;
    justify-content: center;

    i {
      font-size: 17px;
      color: #fff;
    }
  }

  span {
    font-size: 15px;
    font-weight: 600;
    color: #fff;
    letter-spacing: 1px;
  }
}

.logout-link {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.55);
  cursor: pointer;
  transition: color 0.2s;

  &:hover {
    color: #fff;
  }
}

.content-wrapper {
  position: relative;
  z-index: 2;
  width: 100%;
  max-width: 1080px;
  margin: auto;
  padding: 110px 40px 60px;
}

.guide-header {
  text-align: center;
  margin-bottom: 44px;

  .guide-title {
    font-size: 30px;
    font-weight: 700;
    color: #fff;
    letter-spacing: 6px;
  }

  .guide-subtitle {
    margin-top: 10px;
    font-size: 12px;
    color: rgba(255, 255, 255, 0.4);
    letter-spacing: 4px;
  }
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  color: rgba(255, 255, 255, 0.7);

  .loading-spinner {
    width: 36px;
    height: 36px;
    border: 3px solid rgba(255, 255, 255, 0.15);
    border-top-color: #818cf8;
    border-radius: 50%;
    animation: spin 0.9s linear infinite;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
  color: rgba(255, 255, 255, 0.75);
  font-size: 14px;

  i {
    font-size: 42px;
    color: #fbbf24;
  }

  .logout-link {
    font-size: 14px;
    color: #a5b4fc;

    &:hover {
      color: #c7d2fe;
    }
  }
}

//应用卡片
.app-grid {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 22px;
}

.app-card {
  width: 236px;
  padding: 28px 22px 22px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.055);
  border: 1px solid rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(22px);
  text-align: center;
  cursor: pointer;
  animation: fadeInUp 0.5s ease both;
  animation-delay: var(--delay);
  transition: all 0.25s ease;

  .app-icon {
    width: 54px;
    height: 54px;
    margin: 0 auto;
    border-radius: 14px;
    background: var(--app-color);
    box-shadow: 0 10px 24px rgba(0, 0, 0, 0.35);
    display: flex;
    align-items: center;
    justify-content: center;

    i {
      font-size: 24px;
      color: #fff;
    }
  }

  .app-title {
    margin-top: 16px;
    font-size: 16px;
    font-weight: 600;
    color: #fff;
  }

  .app-desc {
    margin-top: 6px;
    font-size: 12px;
    color: rgba(255, 255, 255, 0.5);
    min-height: 17px;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .app-arrow {
    margin-top: 14px;
    font-size: 12px;
    color: var(--app-color);
    filter: brightness(1.6);
    opacity: 0;
    transform: translateY(4px);
    transition: all 0.25s ease;
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }

  &:hover {
    transform: translateY(-6px);
    border-color: var(--app-color);
    background: rgba(255, 255, 255, 0.08);
    box-shadow: 0 18px 44px rgba(3, 7, 24, 0.5);

    .app-arrow {
      opacity: 1;
      transform: translateY(0);
    }
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(24px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 768px) {
  .top-bar {
    padding: 16px 20px;
  }

  .content-wrapper {
    padding: 90px 20px 40px;
  }

  .guide-header {
    margin-bottom: 30px;

    .guide-title {
      font-size: 24px;
    }
  }

  .app-grid {
    gap: 14px;
  }

  .app-card {
    width: calc(50% - 7px);
    padding: 22px 14px 18px;
  }
}
</style>
