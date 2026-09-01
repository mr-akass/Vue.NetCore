<template>
  <vol-box
    :lazy="true"
    v-model="visible"
    title="设置执行频率"
    :height="400"
    :width="520"
    :padding="15"
  >
    <div style="padding: 20px">
      <el-form label-width="100px" size="default">
        <el-form-item label="执行频率">
          <el-select v-model="frequency" placeholder="请选择执行频率" @change="generateCron" style="width: 100%">
            <el-option label="每小时" value="hourly"></el-option>
            <el-option label="每天" value="daily"></el-option>
            <el-option label="每周" value="weekly"></el-option>
            <el-option label="每月" value="monthly"></el-option>
          </el-select>
        </el-form-item>

        <el-form-item label="星期" v-if="frequency === 'weekly'">
          <el-select v-model="dayOfWeek" placeholder="请选择星期" @change="generateCron" style="width: 100%">
            <el-option label="星期一" value="MON"></el-option>
            <el-option label="星期二" value="TUE"></el-option>
            <el-option label="星期三" value="WED"></el-option>
            <el-option label="星期四" value="THU"></el-option>
            <el-option label="星期五" value="FRI"></el-option>
            <el-option label="星期六" value="SAT"></el-option>
            <el-option label="星期日" value="SUN"></el-option>
          </el-select>
        </el-form-item>

        <el-form-item label="日期" v-if="frequency === 'monthly'">
          <el-input-number v-model="dayOfMonth" :min="1" :max="28" @change="generateCron" style="width: 100%"></el-input-number>
        </el-form-item>

        <el-form-item label="执行时间" v-if="frequency && frequency !== 'hourly'">
          <el-time-picker v-model="time" format="HH:mm" value-format="HH:mm" placeholder="选择时间" @change="generateCron" style="width: 100%"></el-time-picker>
        </el-form-item>

        <el-form-item label="分钟" v-if="frequency === 'hourly'">
          <el-input-number v-model="minute" :min="0" :max="59" @change="generateCron" style="width: 100%"></el-input-number>
        </el-form-item>

        <el-form-item label="Cron表达式">
          <el-input v-model="cronExpression" disabled></el-input>
        </el-form-item>

        <el-form-item label="说明">
          <el-input v-model="cronDescr" disabled></el-input>
        </el-form-item>
      </el-form>
    </div>

    <template #footer>
      <div>
        <el-button @click="visible = false">取消</el-button>
        <el-button type="primary" @click="confirm">确定</el-button>
      </div>
    </template>
  </vol-box>
</template>

<script>
import VolBox from '@/components/basic/VolBox.vue';

const dayOfWeekMap = {
  'MON': '星期一',
  'TUE': '星期二',
  'WED': '星期三',
  'THU': '星期四',
  'FRI': '星期五',
  'SAT': '星期六',
  'SUN': '星期日'
};

export default {
  components: { 'vol-box': VolBox },
  emits: ['confirm'],
  data() {
    return {
      visible: false,
      frequency: 'daily',
      dayOfWeek: 'MON',
      dayOfMonth: 1,
      time: '08:00',
      minute: 0,
      cronExpression: '',
      cronDescr: '',
      callback: null
    };
  },
  methods: {
    open(cronExpr, cb) {
      this.callback = cb;
      this.visible = true;
      if (cronExpr) {
        this.parseCron(cronExpr);
      } else {
        this.frequency = 'daily';
        this.time = '08:00';
        this.minute = 0;
        this.dayOfWeek = 'MON';
        this.dayOfMonth = 1;
      }
      this.$nextTick(() => {
        this.generateCron();
      });
    },
    parseCron(expr) {
      // Cron格式: 秒 分 时 日 月 周
      const parts = expr.trim().split(/\s+/);
      if (parts.length < 6) {
        this.frequency = 'daily';
        this.time = '08:00';
        this.generateCron();
        return;
      }
      const [, min, hour, dayOfMonth, , dayOfWeek] = parts;

      if (hour === '*') {
        // 每小时: 0 M * * * ?
        this.frequency = 'hourly';
        this.minute = parseInt(min) || 0;
      } else if (dayOfWeek !== '?' && dayOfWeek !== '*') {
        // 每周: 0 M H ? * DOW
        this.frequency = 'weekly';
        this.dayOfWeek = dayOfWeek;
        this.time = `${String(parseInt(hour)).padStart(2, '0')}:${String(parseInt(min)).padStart(2, '0')}`;
      } else if (dayOfMonth !== '*' && dayOfMonth !== '?') {
        // 每月: 0 M H D * ?
        this.frequency = 'monthly';
        this.dayOfMonth = parseInt(dayOfMonth) || 1;
        this.time = `${String(parseInt(hour)).padStart(2, '0')}:${String(parseInt(min)).padStart(2, '0')}`;
      } else {
        // 每天: 0 M H * * ?
        this.frequency = 'daily';
        this.time = `${String(parseInt(hour)).padStart(2, '0')}:${String(parseInt(min)).padStart(2, '0')}`;
      }
    },
    generateCron() {
      if (!this.frequency) return;

      let cron = '';
      let descr = '';

      if (this.frequency === 'hourly') {
        const m = this.minute || 0;
        cron = `0 ${m} * * * ?`;
        descr = `每小时第${m}分钟执行`;
      } else {
        const timeParts = (this.time || '08:00').split(':');
        const h = parseInt(timeParts[0]);
        const m = parseInt(timeParts[1]);
        const timeStr = `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;

        if (this.frequency === 'daily') {
          cron = `0 ${m} ${h} * * ?`;
          descr = `每天${timeStr}执行`;
        } else if (this.frequency === 'weekly') {
          cron = `0 ${m} ${h} ? * ${this.dayOfWeek}`;
          descr = `每周${dayOfWeekMap[this.dayOfWeek] || this.dayOfWeek} ${timeStr}执行`;
        } else if (this.frequency === 'monthly') {
          cron = `0 ${m} ${h} ${this.dayOfMonth} * ?`;
          descr = `每月${this.dayOfMonth}日 ${timeStr}执行`;
        }
      }

      this.cronExpression = cron;
      this.cronDescr = descr;
    },
    confirm() {
      if (!this.cronExpression) {
        return;
      }
      if (this.callback) {
        this.callback(this.cronExpression, this.cronDescr);
      }
      this.visible = false;
    }
  }
};
</script>
