import { reactive } from 'vue';

/**
 * 站内消息共享状态：Index.vue 头部铃铛(Message.vue)与 SignalR 推送(MessageConfig.js)共用
 * unreadCount: 未读数量(红点)
 * total: 消息总数
 * list: 我的消息列表(最近的在前)
 */
export const messageState = reactive({
  unreadCount: 0,
  total: 0,
  list: []
});

/** 初始加载/刷新列表 */
export function loadMessages({ unreadCount, list, total }) {
  messageState.unreadCount = unreadCount || 0;
  messageState.total = total || 0;
  messageState.list.splice(0, messageState.list.length, ...(list || []));
}

