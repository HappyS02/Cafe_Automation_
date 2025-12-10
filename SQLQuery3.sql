-- 1. Eski tabloyu sil (Hata vermemesi için kontrol ederek)
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
DROP TABLE dbo.Users
GO

-- 2. Yeni tabloyu istediğin sütunlarla oluştur
CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Username] NVARCHAR(MAX) NOT NULL,
    [Email] NVARCHAR(MAX) NOT NULL,
    [Password] NVARCHAR(MAX) NOT NULL,
    [Role] NVARCHAR(MAX) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- 3. Sisteme girebilmen için 1 tane Yönetici ekle
INSERT INTO [dbo].[Users] ([Name], [Username], [Email], [Password], [Role], [IsActive])
VALUES ('Süper Admin', 'admin', 'admin@cafe.com', '123', 'Yönetici', 1);
GO