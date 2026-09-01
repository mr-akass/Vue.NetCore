import * as signalR from '@microsoft/signalr';
import { ElNotification } from 'element-plus';
import { ElMessageBox } from 'element-plus';
import store from '@/store/index';
import { loadMessages, messageState } from './MessageState';

//浏览器桌面通知(需用户授权)
function showDesktopNotification(data) {
  if (typeof window === 'undefined' || !('Notification' in window)) {
    return;
  }
  const title = data?.title || '站内消息';
  const message = data?.message ? data.message + '' : '';

  if (Notification.permission === 'granted') {
    new Notification(title, { body: message });
    return;
  }
  if (Notification.permission === 'default') {
    Notification.requestPermission().then((permission) => {
      if (permission === 'granted') {
        new Notification(title, { body: message });
      }
    });
  }
}

async function getUnreadCount(http) {
  const result = await http.get('api/Sys_MessageUser/GetMyUnreadCount');
  return result?.unreadCount || result?.data?.unreadCount || 0;
}

async function getMessageList(http, page = 1, rows = 20) {
  const result = await http.get(`api/Sys_MessageUser/GetMyMessages?page=${page}&rows=${rows}`);
  return {
    total: result?.total || result?.data?.total || 0,
    rows: result?.rows || result?.data?.rows || []
  };
}

export default function (http, receive) {
  let connection;
  http.post('api/user/GetCurrentUserInfo').then(async (result) => {
    const userName = result.data.userName;
    if (!userName) {
      return;
    }
    //初始加载未读数量与消息列表(填充头部铃铛)
    try {
      const [unreadCount, messageResult] = await Promise.all([
        getUnreadCount(http),
        getMessageList(http)
      ]);
      loadMessages({
        unreadCount,
        list: messageResult.rows,
        total: messageResult.total
      });
    } catch (e) {
      console.log(e?.message || e);
    }

    connection = new signalR.HubConnectionBuilder()
      .withAutomaticReconnect()
      .withUrl(`${http.ipAddress}message?userName=${userName}`, {
        //withCredentials: true // 启用凭证模式
        // accessTokenFactory: () => store.getters.getToken()
      })
      //.withUrl(`${http.ipAddress}message`)
      .build();

    connection.start().catch((err) => console.log(err.message));
    //自动重连成功后重新拉取真实未读数
    connection.onreconnected(async (connectionId) => {
      try {
        messageState.unreadCount = await getUnreadCount(http);
      } catch (error) {
        console.log(error?.message || error);
      }
    });
    connection.on('ReceiveHomePageMessage', function (data) {
      switch (data.value) {
        case 'logout':
          showLogoutMessage(data);
          return;
        default:
          ElNotification.success({
            title: data.title,
            message: data.message + '',
            type: 'warning'
          });
          showDesktopNotification(data);
          //从服务端刷新消息列表与未读数(保证已读标记使用正确的记录ID)
          Promise.all([getUnreadCount(http), getMessageList(http)])
            .then(([unreadCount, messageResult]) => {
              loadMessages({
                unreadCount,
                list: messageResult.rows,
                total: messageResult.total
              });
            })
            .catch((e) => console.log(e?.message || e));
          receive && receive(data);
          break;
      }
    });
  });
}
//强制用户下线
function showLogoutMessage(data) {
  store.commit('clearUserInfo', '');
  const timerId = setTimeout(() => {
    clearTimeout(timerId);
    window.location.href = '/';
  }, 5000);
  ElMessageBox.confirm(data.msg, '警告', {
    center: true,
    showCancelButton: false,
    closeOnClickModal: false,
    closeOnPressEscape: false,
    showClose: false
  }).then(() => {
    clearTimeout(timerId);
    window.location.href = '/';
  });
}
