-- 1. Eğer Users tablosu zaten varsa sil (Hata vermemesi için)
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
DROP TABLE dbo.Users
GO

-- 2. Users tablosunu CafeDb içinde oluştur
CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Username] NVARCHAR(MAX) NOT NULL,
    [Password] NVARCHAR(MAX) NOT NULL,
    [Role] NVARCHAR(MAX) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- 3. İlk Yöneticiyi ve İlk Müşteriyi Elle Ekle (Test edebilmen için)
INSERT INTO [dbo].[Users] ([Name], [Username], [Password], [Role], [IsActive])
VALUES ('Süper Yönetici', 'admin', '123', 'Yönetici', 1);

INSERT INTO [dbo].[Users] ([Name], [Username], [Password], [Role], [IsActive])
VALUES ('Test Müşteri', 'musteri', '123', 'Müşteri', 1);
GO