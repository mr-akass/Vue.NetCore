<template>
  <div class="data-panel-default">
    <div class="panel-header">
      <div class="app-indicator" :style="{ '--app-color': appColor }">
        <span class="app-dot"></span>
        <span class="app-name">{{ appName }}</span>
      </div>
      <div class="panel-title">{{ appTitle }}</div>
      <div class="panel-desc">
        这是应用【{{ appName }}】的定制首页面板。在"应用管理"中把 DataPanel 字段配置为组件名(如
        DataPanelDefault)，系统会自动加载 src/views/home/{DataPanel}.vue 作为该应用的首页。
      </div>
    </div>
    <div class="panel-cards">
      <div class="p-card" v-for="(item, i) in cards" :key="i" :style="{ '--c': item.color }">
        <div class="p-card-icon"><i :class="item.icon"></i></div>
        <div class="p-card-body">
          <div class="p-card-name">{{ item.name }}</div>
          <div class="p-card-value">{{ item.value }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import store from '@/store/index';

const appConfig = computed(() => store.getters.getAppConfig() || {});
const appName = computed(() => appConfig.value.appName || '应用');
const appTitle = computed(() => appConfig.value.title || '定制首页示例');
const appColor = computed(() => appConfig.value.primaryColor || '#409eff');

const cards = [
  { name: '今日待办', value: 12, icon: 'el-icon-bell', color: '#409eff' },
  { name: '进行中流程', value: 5, icon: 'el-icon-share', color: '#67c23a' },
  { name: '本月新增', value: 328, icon: 'el-icon-data-line', color: '#e6a23c' },
  { name: '异常提醒', value: 2, icon: 'el-icon-warning-outline', color: '#f56c6c' }
];
</script>

<style scoped lang="less">
.data-panel-default {
  padding: 24px;
}
.panel-header {
  background: linear-gradient(135deg, #f8fbff 0%, #ffffff 100%);
  border: 1px solid #e8eef5;
  border-radius: 10px;
  padding: 22px 24px;
}
.app-indicator {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 4px 12px;
  border-radius: 20px;
  background: color-mix(in srgb, var(--app-color) 12%, #fff);
  .app-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--app-color);
  }
  .app-name {
    font-size: 12px;
    font-weight: 600;
    color: var(--app-color);
  }
}
.panel-title {
  margin-top: 12px;
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}
.panel-desc {
  margin-top: 8px;
  color: #909399;
  font-size: 13px;
  line-height: 1.7;
}
.panel-cards {
  margin-top: 18px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
  gap: 16px;
}
.p-card {
  display: flex;
  align-items: center;
  gap: 14px;
  background: #fff;
  border: 1px solid #eef1f6;
  border-radius: 10px;
  padding: 18px;
  transition: all 0.2s ease;
  &:hover {
    transform: translateY(-3px);
    box-shadow: 0 8px 20px rgba(0, 0, 0, 0.06);
  }
  .p-card-icon {
    width: 46px;
    height: 46px;
    border-radius: 10px;
    background: color-mix(in srgb, var(--c) 12%, #fff);
    display: flex;
    align-items: center;
    justify-content: center;
    i {
      font-size: 22px;
      color: var(--c);
    }
  }
  .p-card-name {
    font-size: 13px;
    color: #909399;
  }
  .p-card-value {
    margin-top: 2px;
    font-size: 22px;
    font-weight: 700;
    color: #303133;
  }
}
</style>
