-- =============================================================
-- Sys_QuartzOptions 设置频率生成cron - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-07-31
-- 内容: Sys_QuartzOptions 增加 CronDescr(执行频率中文描述)、CronStr(表达式副本) 列
-- 说明: 脚本可重复执行(幂等)
-- =============================================================

IF COL_LENGTH('Sys_QuartzOptions', 'CronDescr') IS NULL
BEGIN
    ALTER TABLE Sys_QuartzOptions ADD CronDescr NVARCHAR(255) NULL;
    PRINT 'Sys_QuartzOptions.CronDescr added';
END
ELSE
    PRINT 'Sys_QuartzOptions.CronDescr exists, skip';
GO

IF COL_LENGTH('Sys_QuartzOptions', 'CronStr') IS NULL
BEGIN
    ALTER TABLE Sys_QuartzOptions ADD CronStr NVARCHAR(100) NULL;
    PRINT 'Sys_QuartzOptions.CronStr added';
END
ELSE
    PRINT 'Sys_QuartzOptions.CronStr exists, skip';
GO
