SET XACT_ABORT ON;
BEGIN TRANSACTION;

DROP TABLE IF EXISTS [dbo].[Messages];
DROP TABLE IF EXISTS [dbo].[ChannelUsers];
DROP TABLE IF EXISTS [dbo].[Channels];
DROP TABLE IF EXISTS [dbo].[Users];
DROP TABLE IF EXISTS [dbo].[MessageStatuses];
DROP TABLE IF EXISTS [dbo].[ChannelRoles];
DROP TABLE IF EXISTS [dbo].[ChannelTypes];
DROP TABLE IF EXISTS [dbo].[Migrations];

COMMIT TRANSACTION;
