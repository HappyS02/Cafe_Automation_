-- Mevcut Users tablosuna eksik olan sütunları ekliyoruz
ALTER TABLE [dbo].[Users] ADD [TC] NVARCHAR(MAX) NULL;
ALTER TABLE [dbo].[Users] ADD [Email] NVARCHAR(MAX) NULL;
ALTER TABLE [dbo].[Users] ADD [PhoneNumber] NVARCHAR(MAX) NULL;
GO