<template>
  <div class="message-container">
    <div style="padding: 0 0 20px 10px">
      <el-alert title="站内消息发送" type="success" show-icon
        ><p>1、仅 admin 可发送站内通知，支持单人或多人；消息会写入数据库，离线用户登录后可在首页右上角铃铛查看。</p>
        <p>2、在线用户会实时收到通知弹窗，并支持已读/未读状态。</p>
        <p>3、可参照 Index.vue 与 HomePageMessageHub.cs</p></el-alert
      >
    </div>

    <el-alert
      v-if="!isAdmin"
      title="当前账号没有发送权限，仅 admin 可以发送站内消息"
      type="warning"
      show-icon
      :closable="false"
      style="margin: 0 0 20px 10px"
    />

    <vol-form
      ref="form"
      :formRules="formOptions"
      :labelWidth="90"
      :formFields="formFields"
    >
    </vol-form>
    <div class="btns">
      <el-button type="default" :disabled="!isAdmin" @click="openSentMessages">
        <i class="el-icon-document"></i>查看发送记录
      </el-button>
      <el-button
        type="primary"
        :disabled="!isAdmin || sending || !connected"
        :loading="sending"
        icon="el-icon-chat-line-round"
        @click="sendMessage"
        >发送消息</el-button
      >
    </div>

    <el-dialog v-model="sentDialogVisible" width="720px" destroy-on-close align-center class="vol-msg-dialog">
      <template #header>
        <div class="dlg-head">
          <span class="dlg-title">发送记录</span>
          <span class="dlg-count" v-if="sentMessages.length">共 {{ sentMessages.length }} 条</span>
        </div>
      </template>
      <div class="sent-list" v-if="sentMessages.length">
        <div class="sent-item" v-for="row in sentMessages" :key="row.id" @click="viewRecipientStatus(row)">
          <div class="sent-main">
            <div class="sent-title-row">
              <span class="sent-title">{{ row.title }}</span>
              <span class="read-pill" :class="pillClass(row)">
                <span class="dot"></span>{{ pillText(row) }}
              </span>
            </div>
            <div class="sent-content">{{ row.content }}</div>
            <div class="sent-meta">
              <span><i class="el-icon-user"></i>{{ row.recipientCount }} 位收件人</span>
              <span><i class="el-icon-time"></i>{{ row.createDate }}</span>
            </div>
          </div>
          <div class="sent-action">详情<i class="el-icon-arrow-right"></i></div>
        </div>
      </div>
      <el-empty v-else description="还没有发送过消息" :image-size="80" />
    </el-dialog>

    <el-dialog v-model="recipientDialogVisible" width="560px" destroy-on-close align-center class="vol-msg-dialog">
      <template #header>
        <div class="dlg-head">
          <span class="dlg-title">阅读状态</span>
        </div>
      </template>
      <!-- 消息内容 -->
      <div class="detail-msg" v-if="currentMessageInfo">
        <div class="detail-title">{{ currentMessageInfo.title }}</div>
        <div class="detail-content">{{ currentMessageInfo.content }}</div>
        <div class="detail-meta">{{ currentMessageInfo.createDate }}</div>
      </div>
      <!-- 阅读统计 -->
      <div class="read-summary">
        <div class="rs-item">
          <div class="rs-num">{{ recipientStatusList.length }}</div>
          <div class="rs-label">收件人</div>
        </div>
        <div class="rs-divider"></div>
        <div class="rs-item ok">
          <div class="rs-num">{{ readCountOfList }}</div>
          <div class="rs-label">已读</div>
        </div>
        <div class="rs-divider"></div>
        <div class="rs-item warn">
          <div class="rs-num">{{ recipientStatusList.length - readCountOfList }}</div>
          <div class="rs-label">未读</div>
        </div>
        <div class="rs-progress">
          <div class="rs-bar">
            <div class="rs-bar-inner" :class="{ done: readPercentOfList === 100 }" :style="{ width: readPercentOfList + '%' }"></div>
          </div>
          <div class="rs-percent">{{ readPercentOfList }}%</div>
        </div>
      </div>
      <!-- 收件人列表 -->
      <div class="recipient-list">
        <div class="recipient-row" v-for="r in recipientStatusList" :key="r.id">
          <span class="avatar">{{ (r.userName || '?').substring(0, 1).toUpperCase() }}</span>
          <span class="r-name">{{ r.userName }}</span>
          <span class="r-status" :class="{ ok: r.isRead }">{{ r.isRead ? '已读' : '未读' }}</span>
          <span class="r-date">{{ r.readDate || '—' }}</span>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script>
//默认不会自动重连，需手动调用withAutomaticReconnect
let connection;
import * as signalR from '@microsoft/signalr';
export default {
  components: {},
  computed: {
    readCountOfList() {
      return this.recipientStatusList.filter((x) => x.isRead).length;
    },
    readPercentOfList() {
      if (!this.recipientStatusList.length) return 0;
      return Math.round((this.readCountOfList / this.recipientStatusList.length) * 100);
    }
  },
  data() {
    return {
      sending: false,
      connected: false,
      currentUserName: '',
      isAdmin: false,
      sentDialogVisible: false,
      recipientDialogVisible: false,
      sentMessages: [],
      recipientStatusList: [],
      currentMessageInfo: null,
      formOptions: [
        [
          {
            title: '收件人',
            required: true,
            field: 'userNames',
            type: 'selectList',
            data: [],
            placeholder: '请选择一个或多个收件人'
          }
        ],
        [{ title: '消息标题', required: true, field: 'title' }],
        [
          {
            title: '消息内容',
            required: true,
            field: 'message',
            type: 'textarea',
            minRows: 10
          }
        ]
      ],
      formFields: {
        userNames: [],
        title: '系统通知',
        message: ''
      }
    };
  },
  methods: {
    //发送记录的阅读状态标签样式
    pillClass(row) {
      if (!row.recipientCount || !row.readCount) return 'none';
      return row.readCount >= row.recipientCount ? 'all' : 'part';
    },
    //发送记录的阅读状态标签文字
    pillText(row) {
      if (!row.recipientCount || !row.readCount) return '暂无人已读';
      return row.readCount >= row.recipientCount ? '全部已读' : `已读 ${row.readCount}/${row.recipientCount}`;
    },
    async openSentMessages() {
      if (!this.isAdmin) {
        return;
      }
      const result = await this.http.get('api/Sys_Message/GetSentMessages?page=1&rows=50');
      this.sentMessages = result?.rows || result?.data?.rows || [];
      this.sentDialogVisible = true;
    },
    async viewRecipientStatus(row) {
      const messageId = row?.messageId || row?.id;
      if (!messageId) {
        return;
      }
      const result = await this.http.get(`api/Sys_Message/GetRecipientStatus/${messageId}`);
      this.recipientStatusList = result?.recipients || result?.data?.recipients || [];
      this.currentMessageInfo = result?.message || result?.data?.message || null;
      this.recipientDialogVisible = true;
    },
    async loadRecipients() {
      const result = await this.http.get('api/User/GetMessageRecipients', {}, true);
      const recipientField = this.formOptions[0][0];
      const recipients = result?.data || result || [];
      recipientField.data = recipients.map((item) => ({
        key: item.key,
        value: item.value,
        label: item.value
      }));
    },
    async initCurrentUser() {
      const result = await this.http.post('api/user/GetCurrentUserInfo');
      const userName = result?.data?.userName || '';
      this.currentUserName = userName;
      this.isAdmin = userName.toLowerCase() === 'admin';
      if (this.isAdmin) {
        await this.loadRecipients();
      }
    },
    async initSignalR() {
      connection = new signalR.HubConnectionBuilder()
        .withAutomaticReconnect()
        .withUrl(`${this.http.ipAddress}message?userName=${this.currentUserName}`)
        .build();

      connection.onreconnected(() => {
        this.connected = true;
      });
      connection.onclose(() => {
        this.connected = false;
      });

      await connection.start();
      this.connected = true;
    },
    async sendMessage() {
      if (!this.isAdmin) {
        this.$message.error('只有admin可以发送站内消息');
        return;
      }
      if (!this.connected || !connection) {
        this.$message.error('消息服务未连接，请稍后重试');
        return;
      }
      if (!this.formFields.userNames.length) {
        this.$message.warning('请选择至少一个收件人');
        return;
      }
      if (!this.formFields.title || !this.formFields.title.trim()) {
        this.$message.warning('请输入消息标题');
        return;
      }
      if (!this.formFields.message || !this.formFields.message.trim()) {
        this.$message.warning('请输入消息内容');
        return;
      }

      this.sending = true;
      try {
        const result = await connection.invoke('SendHomeMessage', {
          userNames: this.formFields.userNames,
          title: this.formFields.title,
          message: this.formFields.message
        });
        if (!result?.success) {
          this.$message.error(result?.message || '发送失败');
          return;
        }
        this.$message.success(result.message || '消息发送成功');
        this.formFields.message = '';
        if (this.sentDialogVisible) {
          await this.openSentMessages();
        }
      } catch (error) {
        this.$message.error(error?.message || '发送失败');
      } finally {
        this.sending = false;
      }
    }
  },
  created() {},
  async mounted() {
    try {
      await this.initCurrentUser();
      if (this.isAdmin) {
        await this.initSignalR();
      }
    } catch (error) {
      this.connected = false;
      this.$message.error(error?.message || '消息服务初始化失败');
    }
  }
};
</script>
<style lang="less">
//el-dialog渲染在body下，内部结构样式需用全局样式
.vol-msg-dialog {
  border-radius: 12px !important;
  overflow: hidden;
  .el-dialog__header {
    margin: 0;
    padding: 16px 20px;
    border-bottom: 1px solid #f0f2f5;
  }
  .el-dialog__body {
    padding: 16px 20px 20px;
  }
  .el-dialog__headerbtn {
    top: 14px;
  }
}
</style>
<style scoped lang="less">
.message-container {
  margin: 20px;
  .btns {
    text-align: center;
    padding: 10px;
  }
}

//弹窗标题
.dlg-head {
  display: flex;
  align-items: baseline;
  gap: 10px;
  .dlg-title {
    font-size: 16px;
    font-weight: 600;
    color: #1f2d3d;
  }
  .dlg-count {
    font-size: 12px;
    color: #a0a6b1;
  }
}

//========== 发送记录：卡片列表 ==========
.sent-list {
  max-height: 460px;
  overflow: auto;
  margin: -4px;
  padding: 4px;
}
.sent-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  border: 1px solid #eef0f4;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.18s ease;
  & + .sent-item {
    margin-top: 10px;
  }
  &:hover {
    border-color: #d3e3fd;
    background: #f8fbff;
    .sent-action {
      color: #409eff;
      transform: translateX(2px);
    }
  }
}
.sent-main {
  flex: 1;
  min-width: 0;
}
.sent-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  .sent-title {
    font-size: 14px;
    font-weight: 600;
    color: #303133;
    max-width: 340px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}
.read-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  padding: 1px 10px;
  border-radius: 10px;
  flex-shrink: 0;
  .dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: currentColor;
  }
  &.all {
    color: #2ba471;
    background: #e8f7f0;
  }
  &.part {
    color: #2f7de0;
    background: #ecf3fe;
  }
  &.none {
    color: #8a919f;
    background: #f2f3f5;
  }
}
.sent-content {
  margin-top: 5px;
  font-size: 12px;
  color: #8a919f;
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
  word-break: break-all;
}
.sent-meta {
  margin-top: 8px;
  display: flex;
  gap: 18px;
  font-size: 12px;
  color: #a0a6b1;
  i {
    margin-right: 4px;
  }
}
.sent-action {
  flex-shrink: 0;
  font-size: 12px;
  color: #c0c4cc;
  display: inline-flex;
  align-items: center;
  gap: 2px;
  transition: all 0.18s ease;
}

//========== 阅读状态 ==========
//消息内容(左侧强调条)
.detail-msg {
  border-left: 3px solid #409eff;
  background: #f7f9fc;
  border-radius: 0 8px 8px 0;
  padding: 12px 16px;
  .detail-title {
    font-size: 14px;
    font-weight: 600;
    color: #303133;
  }
  .detail-content {
    margin-top: 6px;
    font-size: 13px;
    color: #606266;
    line-height: 1.7;
    white-space: pre-wrap;
    word-break: break-word;
    max-height: 96px;
    overflow: auto;
  }
  .detail-meta {
    margin-top: 8px;
    font-size: 12px;
    color: #a0a6b1;
  }
}

//阅读统计条
.read-summary {
  display: flex;
  align-items: center;
  margin: 14px 2px 12px;
  .rs-item {
    text-align: center;
    padding: 0 18px;
    .rs-num {
      font-size: 20px;
      font-weight: 700;
      color: #303133;
      line-height: 1.2;
    }
    .rs-label {
      margin-top: 2px;
      font-size: 12px;
      color: #a0a6b1;
    }
    &.ok .rs-num {
      color: #2ba471;
    }
    &.warn .rs-num {
      color: #e6a23c;
    }
  }
  .rs-divider {
    width: 1px;
    height: 26px;
    background: #f0f2f5;
  }
  .rs-progress {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 10px;
    padding-left: 18px;
    .rs-bar {
      flex: 1;
      height: 6px;
      border-radius: 3px;
      background: #f0f2f5;
      overflow: hidden;
      .rs-bar-inner {
        height: 100%;
        border-radius: 3px;
        background: #409eff;
        transition: width 0.3s ease;
        &.done {
          background: #2ba471;
        }
      }
    }
    .rs-percent {
      font-size: 12px;
      color: #8a919f;
      width: 34px;
      text-align: right;
    }
  }
}

//收件人列表
.recipient-list {
  max-height: 280px;
  overflow: auto;
  border-top: 1px solid #f0f2f5;
}
.recipient-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 4px;
  border-bottom: 1px solid #f5f6f8;
  &:hover {
    background: #fafbfc;
  }
  .avatar {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background: linear-gradient(135deg, #6db3ff, #409eff);
    color: #fff;
    font-size: 12px;
    font-weight: 600;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }
  .r-name {
    flex: 1;
    font-size: 13px;
    color: #303133;
  }
  .r-status {
    font-size: 12px;
    color: #e6a23c;
    background: #fdf4e7;
    padding: 1px 10px;
    border-radius: 10px;
    &.ok {
      color: #2ba471;
      background: #e8f7f0;
    }
  }
  .r-date {
    width: 150px;
    text-align: right;
    font-size: 12px;
    color: #a0a6b1;
  }
}
</style>
