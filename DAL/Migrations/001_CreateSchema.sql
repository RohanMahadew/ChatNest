SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE [dbo].[MessageStatuses]
(
	[Id] int NOT NULL,
	[Value] nvarchar(50) NOT NULL,
	CONSTRAINT [PK_MessageStatuses] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_MessageStatuses_Value] UNIQUE ([Value])
);

INSERT INTO [dbo].[MessageStatuses] ([Id], [Value])
VALUES
	(1, N'Send'),
	(2, N'Read');

CREATE TABLE [dbo].[ChannelRoles]
(
	[Id] int NOT NULL,
	[Value] nvarchar(50) NOT NULL,
	CONSTRAINT [PK_ChannelRoles] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_ChannelRoles_Value] UNIQUE ([Value])
);

INSERT INTO [dbo].[ChannelRoles] ([Id], [Value])
VALUES
	(1, N'DirectMessage'),
	(2, N'Blocked'),
	(3, N'Member'),
	(4, N'Admin'),
	(5, N'SuperAdmin'),
	(6, N'Banned'),
	(7, N'Invited');

CREATE TABLE [dbo].[ChannelTypes]
(
	[Id] int NOT NULL,
	[Value] nvarchar(50) NOT NULL,
	CONSTRAINT [PK_ChannelTypes] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_ChannelTypes_Value] UNIQUE ([Value])
);

INSERT INTO [dbo].[ChannelTypes] ([Id], [Value])
VALUES
	(1, N'DIRECT'),
	(2, N'PRIVATE'),
	(3, N'PUBLIC');

CREATE TABLE [dbo].[Users]
(
	[Id] int IDENTITY(1,1) NOT NULL,
	[UserName] nvarchar(256) NOT NULL,
	[Email] nvarchar(320) NOT NULL,
	[PasswordHash] nvarchar(512) NOT NULL,
	[DateOfBirth] date NOT NULL,
	[CreatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
	[UpdatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_Users_UpdatedAt] DEFAULT SYSUTCDATETIME(),
	CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_Users_UserName] UNIQUE ([UserName]),
	CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);

CREATE TABLE [dbo].[Channels]
(
	[Id] int IDENTITY(1,1) NOT NULL,
	[Uuid] nvarchar(36) NOT NULL,
	[Type] int NOT NULL,
	[Name] nvarchar(256) NULL,
	[CreatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_Channels_CreatedAt] DEFAULT SYSUTCDATETIME(),
	[UpdatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_Channels_UpdatedAt] DEFAULT SYSUTCDATETIME(),
	CONSTRAINT [PK_Channels] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_Channels_Uuid] UNIQUE ([Uuid]),
	CONSTRAINT [FK_Channels_ChannelTypes] FOREIGN KEY ([Type])
		REFERENCES [dbo].[ChannelTypes] ([Id])
);

CREATE TABLE [dbo].[ChannelUsers]
(
	[Id] int IDENTITY(1,1) NOT NULL,
	[UserId] int NOT NULL,
	[ChannelId] int NOT NULL,
	[ChannelRole] int NOT NULL,
	[CreatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_ChannelUsers_CreatedAt] DEFAULT SYSUTCDATETIME(),
	[UpdatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_ChannelUsers_UpdatedAt] DEFAULT SYSUTCDATETIME(),
	CONSTRAINT [PK_ChannelUsers] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ChannelUsers_Users] FOREIGN KEY ([UserId])
		REFERENCES [dbo].[Users] ([Id]),
	CONSTRAINT [FK_ChannelUsers_Channels] FOREIGN KEY ([ChannelId])
		REFERENCES [dbo].[Channels] ([Id]),
	CONSTRAINT [FK_ChannelUsers_ChannelRoles] FOREIGN KEY ([ChannelRole])
		REFERENCES [dbo].[ChannelRoles] ([Id]),
	CONSTRAINT [UQ_ChannelUsers_UserId_ChannelId] UNIQUE ([UserId], [ChannelId])
);

CREATE TABLE [dbo].[Messages]
(
	[Id] int IDENTITY(1,1) NOT NULL,
	[UserId] int NOT NULL,
	[ChannelId] int NOT NULL,
	[MessageStatus] int NOT NULL,
	[Content] nvarchar(max) NOT NULL,
	[CreatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_Messages_CreatedAt] DEFAULT SYSUTCDATETIME(),
	[UpdatedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_Messages_UpdatedAt] DEFAULT SYSUTCDATETIME(),
	CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_Messages_Users] FOREIGN KEY ([UserId])
		REFERENCES [dbo].[Users] ([Id]),
	CONSTRAINT [FK_Messages_Channels] FOREIGN KEY ([ChannelId])
		REFERENCES [dbo].[Channels] ([Id]),
	CONSTRAINT [FK_Messages_MessageStatuses] FOREIGN KEY ([MessageStatus])
		REFERENCES [dbo].[MessageStatuses] ([Id])
);

INSERT INTO [dbo].[Migrations] ([MigrationId])
VALUES (N'001_CreateSchema');

COMMIT TRANSACTION;
