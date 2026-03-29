/*
  Medor – vytvoření tabulky pro uložené kurzy Bitcoinu (Microsoft SQL Server).
  Schéma odpovídá EF Core migraci Initial (tabulka BitcoinPrices).

  Použití: spusťte proti cílové databázi (např. sqlcmd, SSMS).
  Aplikace může databázi vytvářet i přes MigrateAsync(); tento skript slouží
  k ručnímu nasazení nebo kontrole schématu podle zadání (T-SQL).
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[BitcoinPrices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BitcoinPrices] (
        [Id]              INT            NOT NULL IDENTITY (1, 1),
        [BtcEur]          DECIMAL (18, 8) NOT NULL,
        [BtcCzk]          DECIMAL (18, 2) NOT NULL,
        [EurCzkRate]      DECIMAL (18, 6) NOT NULL,
        [CnbRateValidFor] DATE           NOT NULL,
        [FetchedAtUtc]    DATETIME2      NOT NULL,
        [Note]            NVARCHAR (500) NOT NULL,
        CONSTRAINT [PK_BitcoinPrices] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* Volitelné: urychlení dotazů řazených podle času (graf, seznam). */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[BitcoinPrices]', N'U')
      AND name = N'IX_BitcoinPrices_FetchedAtUtc'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_BitcoinPrices_FetchedAtUtc]
        ON [dbo].[BitcoinPrices] ([FetchedAtUtc] ASC);
END
GO
