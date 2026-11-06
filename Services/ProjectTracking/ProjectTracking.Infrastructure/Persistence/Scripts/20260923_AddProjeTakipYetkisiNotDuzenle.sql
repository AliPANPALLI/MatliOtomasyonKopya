IF COL_LENGTH('dbo.ProjeTakipYetkisi', 'NotDuzenle') IS NULL
BEGIN
    EXEC('ALTER TABLE dbo.ProjeTakipYetkisi ADD NotDuzenle bit NOT NULL CONSTRAINT DF_ProjeTakipYetkisi_NotDuzenle DEFAULT (0)');
END;
