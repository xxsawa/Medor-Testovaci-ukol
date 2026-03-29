using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medor.Api.Migrations;

/// <summary>Initial schema: stored Bitcoin price snapshots.</summary>
public partial class Initial : Migration
{
    /// <summary>
    /// Creates <c>BitcoinPrices</c> only if missing. Supports DBs created via <c>Database/CreateTables.sql</c>
    /// where the table exists but <c>__EFMigrationsHistory</c> was not yet updated for this migration.
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[BitcoinPrices]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[BitcoinPrices] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [BtcEur] decimal(18,8) NOT NULL,
                    [BtcCzk] decimal(18,2) NOT NULL,
                    [EurCzkRate] decimal(18,6) NOT NULL,
                    [CnbRateValidFor] date NOT NULL,
                    [FetchedAtUtc] datetime2 NOT NULL,
                    [Note] nvarchar(500) NOT NULL,
                    CONSTRAINT [PK_BitcoinPrices] PRIMARY KEY ([Id])
                );
            END
            """);
    }

    /// <summary>Drops <c>BitcoinPrices</c> when present.</summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[BitcoinPrices]', N'U') IS NOT NULL
                DROP TABLE [dbo].[BitcoinPrices];
            """);
    }
}
