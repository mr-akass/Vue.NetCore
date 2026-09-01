<template>
  <div class="notification" @click="showMsg">
    <el-badge :is-dot="messageState.unreadCount > 0" :max="99" :show-zero="false" class="item" :offset="[3, -3]">
      <el-icon size="15">
        <Bell />
      </el-icon>
    </el-badge>
  </div>
  <vol-box v-model="model" :width="420" :padding="5" title="站内消息">
    <div style="height: 445px">
      <el-tabs v-model="activeName" class="msg-tabs">
        <el-tab-pane name="unread">
          <template #label>
            <span class="custom-tabs-label">
              <el-badge :value="messageState.unreadCount" :show-zero="false" :offset="[-2, 4]"
                badge-style="background-color: #ff1b0b;width: 18px; ">
                未读消息<el-icon size="15"> </el-icon>
              </el-badge>
            </span>
          </template>
          <div class="msg-toolbar" v-if="messageState.unreadCount > 0">
            <el-button link type="primary" size="small" @click="markAllRead">全部已读</el-button>
          </div>
          <el-scrollbar :height="messageState.unreadCount > 0 ? 370 : 400">
            <template v-if="unreadList.length">
              <div class="msg-item unread" v-for="item in unreadList" :key="item.id" @click="openMessage(item)">
                <div class="title">
                  {{ item.title }}
                  <el-tag size="small" type="danger" style="margin-left: 6px">未读</el-tag>
                </div>
                <div class="desc">{{ item.message || item.content }}</div>
                <div class="bottom">
                  <div class="tag">{{ item.senderUserName || '系统' }}</div>
                  <div class="date">{{ item.date }}</div>
                </div>
              </div>
            </template>
            <vol-empty v-else description="暂无未读消息"></vol-empty>
          </el-scrollbar>
        </el-tab-pane>
        <el-tab-pane label="已读消息" name="read">
          <el-scrollbar :height="400">
            <template v-if="readList.length">
              <div class="msg-item" v-for="item in readList" :key="item.id" @click="openMessage(item)">
                <div class="title">{{ item.title }}</div>
                <div class="desc">{{ item.message || item.content }}</div>
                <div class="bottom">
                  <div class="tag">{{ item.senderUserName || '系统' }}</div>
                  <div class="date">{{ item.date }}</div>
                </div>
              </div>
            </template>
            <vol-empty v-else description="暂无已读消息"></vol-empty>
          </el-scrollbar>
        </el-tab-pane>
      </el-tabs>
    </div>
  </vol-box>
  <!-- 消息详情 -->
  <el-dialog title="站内消息" v-model="detailVisible" width="520px" append-to-body destroy-on-close>
    <div v-if="currentMessage">
      <div class="message-detail-title">{{ currentMessage.title }}</div>
      <div class="message-detail-meta">
        {{ currentMessage.date }} · {{ currentMessage.senderUserName || currentMessage.fromUser || '系统' }}
      </div>
      <div class="message-detail-content">{{ currentMessage.message || currentMessage.content }}</div>
    </div>
  </el-dialog>
</template>

<script setup>
import VolEmpty from "@/components/basic/VolEmpty.vue";
import { ref, computed, getCurrentInstance } from "vue";
import { messageState } from "./MessageState";

const props = defineProps({
  list: {
    type: Array,
    default: () => {
      return [];
    },
  },
});
const { proxy } = getCurrentInstance();
const model = ref(false);
const activeName = ref("unread");
const detailVisible = ref(false);
const currentMessage = ref(null);

const unreadList = computed(() => messageState.list.filter((x) => !x.isRead));
const readList = computed(() => messageState.list.filter((x) => x.isRead));

const showMsg = () => {
  model.value = true;
};

//点开消息：显示详情并标记已读
const openMessage = async (item) => {
  currentMessage.value = item;
  detailVisible.value = true;
  if (!item || item.isRead || !item.id) {
    return;
  }
  const result = await proxy.http.post(`api/Sys_MessageUser/MarkAsRead/${item.id}`, {});
  if (result?.status === false) {
    return;
  }
  item.isRead = true;
  messageState.unreadCount = Math.max(0, messageState.unreadCount - 1);
};

const markAllRead = async () => {
  if (!messageState.unreadCount) {
    return;
  }
  const result = await proxy.http.post("api/Sys_MessageUser/MarkAllAsRead", {});
  if (result?.status === false) {
    return;
  }
  messageState.list.forEach((item) => {
    item.isRead = true;
  });
  messageState.unreadCount = 0;
};
</script>
<style scoped lang="less">
.notification {
  outline: none;
  color: #000;
  cursor: pointer;
}

.msg-toolbar {
  display: flex;
  justify-content: flex-end;
  padding: 4px 10px 0;
}

.msg-item {
  border-bottom: 1px solid #eee;
  padding: 10px;

  .title {
    font-weight: bolder;
    font-size: 13px;
    color: #000;
    display: flex;
    align-items: center;
  }

  .desc {
    margin-top: 5px;
    line-height: 1.3;
    font-size: 12px;
    color: #676565;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .bottom {
    display: flex;
    margin-top: 5px;
    font-size: 12px;
    color: #676565;
  }

  .tag {
    flex: 1;
  }
}

.msg-item.unread {
  background: #f8fbff;
}

.msg-item:hover {
  cursor: pointer;
  background: #f9f9f9;
}

.message-detail-title {
  font-size: 16px;
  font-weight: 600;
  color: #333;
  margin-bottom: 10px;
}

.message-detail-meta {
  color: #999;
  font-size: 12px;
  margin-bottom: 16px;
}

.message-detail-content {
  color: #555;
  line-height: 1.8;
  white-space: pre-wrap;
  word-break: break-word;
}

::v-deep(.el-tabs__header) {
  margin: 0;
}

::v-deep(.el-tabs__content) {
  min-height: 200px;
}

::v-deep(.el-tabs__nav) {
  width: 100%;
  padding: 0 10px;
}

::v-deep(.el-tabs__item) {
  padding: 0 6px;
  flex: 1;
}
</style>
