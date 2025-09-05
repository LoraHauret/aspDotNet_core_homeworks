--    CREATE DATABASE testdb
--    ON PRIMARY
--    (
--        NAME = testdb_Data,
--        FILENAME = 'D:\ASP.net core\homework2\testdb.mdf',
--        SIZE = 10MB,
--        MAXSIZE = 100MB,
--        FILEGROWTH = 5MB
--    )
--    LOG ON
--    (
--        NAME = testdb_Log,
--        FILENAME = 'D:\ASP.net core\homework2\testdb_log.ldf',
--        SIZE = 5MB,
--        MAXSIZE = 25MB,
--        FILEGROWTH = 5MB
--    );
--    
--    GO
--    
--    CREATE TABLE [dbo].[Users]
--    (
--    [Id]	INT		IDENTITY (1,1) NOT NULL,
--    [Name]	NVARCHAR(Max)	NOT NULL,
--    [Age]	INT		NOT NULL,
--    CONSTRAINT [Pk_Users]	PRIMARY KEY CLUSTERED ([Id] ASC)
--    );


--Включение доступа к расширенным настройкам, чтобы иметь возможность использовать команды, связанные с системными настройками
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

--Для того чтобы использовать команду удаления xp_cmdshell, необходимо разрешить её выполнение
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;

IF EXISTS (SELECT* FROM sys.databases WHERE name = 'testdb')
BEGIN
USE master; -- переключение на другую базу
ALTER DATABASE testdb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; --прервет все соединения и переведет бд в режим одного пользователя --SET OFFLINE; --отключение базы, которую хочу удалить
DROP DATABASE testdb;

DECLARE @fileExists INT;

EXEC xp_fileexist 'D:\ASP.net core\homework2\testdb.mdf', @fileExists OUTPUT; -- Проверка наличия файла на жд
IF @fileExists = 1
BEGIN
	EXEC xp_cmdshell 'del D:\ASP.net core\homework2\testdb.mdf';
END

EXEC xp_fileexist 'D:\ASP.net core\homework2\testdb.ldf', @fileExists OUTPUT;
IF @fileExists = 1
BEGIN
	EXEC xp_cmdshell 'del D:\ASP.net core\homework2\testdb.ldf';
END

END
GO


IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'testdb')
BEGIN
CREATE DATABASE testdb
ON
(
NAME = 'testdb',
FILENAME = 'D:\ASP.net core\homework2\testdb.mdf',
SIZE = 10MB,
MAXSIZE = 100MB,
FILEGROWTH=100MB
)
LOG ON
(
NAME = 'testdb_log',
FILENAME = 'D:\ASP.net core\homework2\testdb.ldf',
SIZE = 5MB,
MAXSIZE = 50MB,
FILEGROWTH=50MB
)
END

GO

USE testdb;

GO 
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
CREATE TABLE [dbo].[Users]
(
[Id]	INT		IDENTITY (1,1) NOT NULL,
[Name]	NVARCHAR(Max)	NOT NULL,
[Age]	INT		NOT NULL,
CONSTRAINT [Pk_Users]	PRIMARY KEY CLUSTERED ([Id] ASC)
);
END

GO
--SET IDENTITY_INSERT [dbo].[Users] ON
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Алексей', 28)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Мария', 34)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Иван', 22)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Светлана', 30)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Дмитрий', 41)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Екатерина', 26)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Николай', 37)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Анастасия', 25)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Андрей', 33)
INSERT [dbo].[Users]([Name], [Age]) VALUES (N'Ольга', 29)
GO
--Отключение xp_cmdshell после завершения операций
EXEC sp_configure 'xp_cmdshell', 0;
RECONFIGURE;
--Отключение доступа к расширенным настройкам
EXEC sp_configure 'show advanced options', 0;
RECONFIGURE;