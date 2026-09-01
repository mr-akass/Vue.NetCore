using NLog;

namespace VOL.Core.Utilities
{
    /// <summary>
    /// 日志帮助类：按logger名称路由到nlog.config配置的对应文件
    /// </summary>
    public class LogHelper
    {
        /// <summary>
        /// 日志记录
        /// </summary>
        /// <param name="logFileName">日志存放文件夹名(nlog.config中的logger名称)</param>
        /// <param name="message">日志消息</param>
        public static void Info(string logFileName, string message)
        {
            Logger logger = LogManager.GetLogger(logFileName);
            logger.Info(message);
        }

        /// <summary>
        /// 错误日志记录
        /// </summary>
        /// <param name="logFileName">日志存放文件夹名(nlog.config中的logger名称)</param>
        /// <param name="message">日志消息</param>
        public static void Error(string logFileName, string message)
        {
            Logger logger = LogManager.GetLogger(logFileName);
            logger.Error(message);
        }
    }
}
