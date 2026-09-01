-- =============================================================
-- SignalR站内消息入库+已读未读 - 数据库升级脚本 (SQL Server)
-- 生成时间: 2026-07-31
-- 内容:
--   1. Sys_Message      消息主表(一条消息一条记录)
--   2. Sys_MessageUser  收件人表(每个收件人一条已读状态记录)
-- 说明: 脚本可重复执行(幂等)
-- =============================================================

-- 1. 消息主表
IF OBJECT_ID('Sys_Message', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_Message (
        ID INT PRIMARY KEY IDENTITY(1,1),
        Title NVARCHAR(255) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        MessageType INT NOT NULL DEFAULT 1,
        SenderUserName NVARCHAR(100) NOT NULL,
        SenderUserId INT NOT NULL,
        RecipientCount INT NOT NULL DEFAULT 0,
        CreateDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Sys_Message_SenderUserId ON Sys_Message(SenderUserId);
    PRINT 'Sys_Message created';
END
ELSE
    PRINT 'Sys_Message exists, skip';
GO

-- 2. 收件人已读状态表
IF OBJECT_ID('Sys_MessageUser', 'U') IS NULL
BEGIN
    CREATE TABLE Sys_MessageUser (
        ID INT PRIMARY KEY IDENTITY(1,1),
        MessageId INT NOT NULL,
        UserName NVARCHAR(100) NOT NULL,
        UserId INT NOT NULL,
        IsRead BIT NOT NULL DEFAULT 0,
        ReadDate DATETIME NULL,
        CreateDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Sys_MessageUser_MessageId ON Sys_MessageUser(MessageId);
    CREATE INDEX IX_Sys_MessageUser_UserId_IsRead ON Sys_MessageUser(UserId, IsRead);
    PRINT 'Sys_MessageUser created';
END
ELSE
    PRINT 'Sys_MessageUser exists, skip';
GO
