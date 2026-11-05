SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE [dbo].[Migrations]
(
	[MigrationId] nvarchar(150) NOT NULL,
	[AppliedAt] datetime2(7) NOT NULL
		CONSTRAINT [DF_Migrations_AppliedAt] DEFAULT SYSUTCDATETIME(),
	CONSTRAINT [PK_Migrations] PRIMARY KEY ([MigrationId])
);

INSERT INTO [dbo].[Migrations] ([MigrationId])
VALUES (N'000_CreateMigrationsTable');

COMMIT TRANSACTION;
