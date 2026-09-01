using System;
using VOL.Core.Enums;

namespace VOL.Core.Utilities
{
    /// <summary>
    /// 控制台输出+日志落盘二合一：
    /// CustomConsole.WriteLine(NlogLoggerType.Quartz, "xxx") 同时输出到控制台并写入 Logs/Quartz/yyyy-MM-dd.txt
    /// </summary>
    public class CustomConsole
    {
        public static void WriteLine(NlogLoggerType loggerType, string message)
        {
            LogHelper.Info(loggerType.ToString(), message);
            Console.WriteLine(message);
        }
    }
}
