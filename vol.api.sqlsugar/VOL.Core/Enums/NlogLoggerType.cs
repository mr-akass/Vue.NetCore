namespace VOL.Core.Enums
{
    /// <summary>
    /// CustomConsole日志类别
    /// 每个枚举值对应Config/Log/nlog.config中的同名logger规则，日志写入 Logs/{枚举名}/yyyy-MM-dd.txt
    /// 新增枚举值时需要在nlog.config中添加对应的target与rule，否则日志只输出控制台不落盘
    /// </summary>
    public enum NlogLoggerType
    {
        Info,
        Login,
        Error,
        Quartz,
        SignalR,
        Job
    }
}
