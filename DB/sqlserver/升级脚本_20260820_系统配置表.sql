/*
 *系统配置表(键值对)：存放需要跨机器/跨浏览器保持一致的项目级配置
 *首个用途：代码生成器的Vue路径(vuePath)、App路径(appPath)——原来存localStorage，
 *复制框架做新项目时同一个localhost域名下会读到旧项目的路径，导致代码生成到错误的目录
 */
IF OBJECT_ID('Sys_ConfigSetting', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_ConfigSetting (
        ID INT PRIMARY KEY IDENTITY(1,1),
        ConfigKey NVARCHAR(100) NOT NULL,
        ConfigValue NVARCHAR(500) NULL,
        Remark NVARCHAR(200) NULL,
        CreateDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifyDate DATETIME NULL
    );
    CREATE UNIQUE INDEX UX_Sys_ConfigSetting_Key ON Sys_ConfigSetting(ConfigKey);
    PRINT 'Sys_ConfigSetting created';
END
ELSE
    PRINT 'Sys_ConfigSetting exists, skip';
GO

--代码生成器路径的两个初始行(值留空，界面上填写后保存)
IF NOT EXISTS (SELECT 1 FROM Sys_ConfigSetting WHERE ConfigKey = 'builder.vuePath')
    INSERT INTO Sys_ConfigSetting (ConfigKey, ConfigValue, Remark)
    VALUES ('builder.vuePath', NULL, N'代码生成器:Vue项目views目录绝对路径');
GO

IF NOT EXISTS (SELECT 1 FROM Sys_ConfigSetting WHERE ConfigKey = 'builder.appPath')
    INSERT INTO Sys_ConfigSetting (ConfigKey, ConfigValue, Remark)
    VALUES ('builder.appPath', NULL, N'代码生成器:uniapp pages目录绝对路径');
GO
