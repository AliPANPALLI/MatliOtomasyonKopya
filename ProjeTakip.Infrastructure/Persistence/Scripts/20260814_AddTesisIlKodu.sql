IF COL_LENGTH('dbo.Tesis', 'IlKodu') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.Tesis ADD IlKodu int NULL');
    EXEC('ALTER TABLE dbo.Tesis ADD CONSTRAINT CK_Tesis_IlKodu CHECK (IlKodu IS NULL OR IlKodu BETWEEN 1 AND 81)');
    EXEC('CREATE INDEX IX_Tesis_IlKodu ON dbo.Tesis (IlKodu)');
END;
