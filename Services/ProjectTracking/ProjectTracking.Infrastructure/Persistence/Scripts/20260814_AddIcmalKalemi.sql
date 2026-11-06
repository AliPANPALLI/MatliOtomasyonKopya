IF OBJECT_ID('dbo.IcmalKalemi', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IcmalKalemi
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_IcmalKalemi PRIMARY KEY,
        ProjeDokumaniId int NOT NULL,
        IsAdimiId int NULL,
        SiraNo int NOT NULL,
        PozNo nvarchar(100) NOT NULL CONSTRAINT DF_IcmalKalemi_PozNo DEFAULT N'',
        Tanim nvarchar(500) NOT NULL,
        Birim nvarchar(20) NOT NULL,
        Miktar decimal(18,3) NOT NULL,
        BirimFiyat decimal(18,4) NOT NULL,
        ParaBirimi char(3) NOT NULL,
        Tutar decimal(18,2) NOT NULL,
        Aciklama nvarchar(500) NOT NULL CONSTRAINT DF_IcmalKalemi_Aciklama DEFAULT N'',
        OlusturulmaUtc datetime2 NOT NULL CONSTRAINT DF_IcmalKalemi_OlusturulmaUtc DEFAULT SYSUTCDATETIME(),
        GuncellemeUtc datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_IcmalKalemi_ProjeDokumani FOREIGN KEY (ProjeDokumaniId)
            REFERENCES dbo.ProjeDokumani(Id) ON DELETE CASCADE,
        CONSTRAINT FK_IcmalKalemi_IsAdimi FOREIGN KEY (IsAdimiId)
            REFERENCES dbo.IsAdimi(Id),
        CONSTRAINT CK_IcmalKalemi_SiraNo CHECK (SiraNo > 0),
        CONSTRAINT CK_IcmalKalemi_Miktar CHECK (Miktar > 0),
        CONSTRAINT CK_IcmalKalemi_BirimFiyat CHECK (BirimFiyat >= 0),
        CONSTRAINT CK_IcmalKalemi_ParaBirimi CHECK (ParaBirimi IN ('TRY', 'USD', 'EUR', 'GBP'))
    );
    CREATE INDEX IX_IcmalKalemi_ProjeDokumaniId_SiraNo ON dbo.IcmalKalemi (ProjeDokumaniId, SiraNo);
    CREATE INDEX IX_IcmalKalemi_IsAdimiId ON dbo.IcmalKalemi (IsAdimiId);
END;
