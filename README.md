# Medor Testovací úkol – sledování kurzu Bitcoinu (BTC/EUR → BTC/CZK)

## Stručný popis projektu

Medor Testovací úkol je webová aplikace v .NET, která zobrazuje **aktuální kurz Bitcoinu** (zdroj CoinDesk pro BTC/EUR, přepočet přes kurz EUR/CZK z ČNB), umožní **ukládat snímky kurzu** do databáze Microsoft SQL Server a **spravovat uložené záznamy** (poznámky, mazání, filtrování). Součástí je **REST API** (ASP.NET Core) a **frontend** (Razor Pages + JavaScript moduly) s živým grafem a grafem uložených hodnot.

## Návod ke spuštění aplikace

### Požadavky

- [.NET SDK](https://dotnet.microsoft.com/download) (projekt cílí na **.NET 9**)
- **Microsoft SQL Server** (lokální instance nebo Docker) a platný **connection string**
- (Volitelně) **Node.js** pro E2E testy ve složce `e2e`

### 1. Databáze a API

**Microsoft SQL Server** – ukládání probíhá do tabulky uložených kurzů. Schéma definuje EF Core (migrace `Initial`).

**T-SQL (součást zadání):** v souboru [`Medor.Api/Database/CreateTables.sql`](Medor.Api/Database/CreateTables.sql) je skript pro vytvoření tabulky `BitcoinPrices` (a volitelného indexu). Struktura odpovídá migraci. Běžné spuštění API vytvoří / aktualizuje schéma přes **`MigrateAsync()`** – skript slouží k dokumentaci a k ručnímu nasazení.

**Spuštění T-SQL skriptu** (nejprve vytvořte prázdnou databázi, např. `Medor`, a nahraďte server a cesty):

```powershell
# Z kořene klonu repozitáře — Windows Integrated Security (-E)
sqlcmd -S localhost -d Medor -E -i "Medor.Api\Database\CreateTables.sql"
```

```powershell
# SQL přihlášení (např. Docker / sa)
sqlcmd -S localhost,1433 -d Medor -U sa -P "VASE_HESLO" -C -i "Medor.Api\Database\CreateTables.sql"
```

V **SQL Server Management Studio (SSMS)** otevřete `CreateTables.sql`, v horní liště zvolte cílovou databázi a spusťte (F5).

1. Vytvořte databázi (nebo použijte existující) a nastavte připojení k SQL Serveru.
2. Zkopírujte `Medor.Api/.env.example` na `Medor.Api/.env` (pokud existuje) **nebo** nastavte proměnnou prostředí / user secrets:
  ```text
   ConnectionStrings__DefaultConnection=Server=...;Database=Medor;Trusted_Connection=True;TrustServerCertificate=True;
  ```
3. V kořeni řešení spusťte API (výchozí URL dle `launchSettings`: **[http://localhost:5298](http://localhost:5298)**):
  ```powershell
   dotnet run --project Medor.Api/Medor.Api
  ```
   Při startu se provedou **EF Core migrace** (`MigrateAsync`).

### 2. Web (frontend)

V **druhém** terminálu (API musí běžet, pokud chcete živá data z API):

```powershell
dotnet run --project Medor.Web/Medor.Web
```

Výchozí adresa webu je typicky **[http://localhost:5192](http://localhost:5192)** (viz `Medor.Web/Properties/launchSettings.json`).  
Ve `Medor.Web/appsettings.json` je `**MedorApi:BaseUrl`** nastaven na `http://localhost:5298` – musí odpovídat běžícímu API.

### 3. Testy (volitelné)

- **Unit / integrační testy API** (xUnit):
  ```powershell
  dotnet test Medor.Api.Tests/Medor.Api.Tests.csproj
  ```
- **Playwright E2E** (složka `e2e`, mockuje API – samotné Medor.Api spouštět nemusíte):
  ```powershell
  cd e2e
  npm install
  npx playwright install chromium
  npm test
  ```
  V PowerShellu pro **headless** režim: `$env:HEADLESS='1'; npm test`

## Popis použitých technologií


| Oblast      | Technologie                                                           |
| ----------- | --------------------------------------------------------------------- |
| Backend API | ASP.NET Core Web API (.NET 9), kontrolery, Serilog                    |
| Databáze    | Microsoft SQL Server, **Entity Framework Core** (Code First, migrace), T-SQL skript `Medor.Api/Database/CreateTables.sql` |
| HTTP        | `HttpClient` – CoinDesk (BTC/EUR), ČNB (kurz EUR/CZK)                 |
| Web UI      | ASP.NET Core **Razor Pages**, Bootstrap                               |
| Frontend JS | Moduly ES (import/export), **Chart.js**, **DataTables**               |
| Testy API   | xUnit, Moq, EF Core InMemory                                          |
| E2E         | **Playwright** (Chromium), mock REST API v testech                    |


## Ukázková API volání

Níže je základ **[http://localhost:5298](http://localhost:5298)** (upravte host/port podle vašeho prostředí).  
Tělo požadavků je **JSON**; hlavička `Content-Type: application/json` u POST/PUT/DELETE.

### Živý kurz (GET)

```http
GET /api/LivePrice
```

**cURL**

```bash
curl -s http://localhost:5298/api/LivePrice
```

**PowerShell (`Invoke-RestMethod`)**

```powershell
Invoke-RestMethod -Uri "http://localhost:5298/api/LivePrice" -Method Get
```

### Uložené záznamy (GET)

```http
GET /api/SavedPrices
```

```bash
curl -s http://localhost:5298/api/SavedPrices
```

### Graf uložených cen (GET)

```http
GET /api/SavedPrices/chart
```

```bash
curl -s http://localhost:5298/api/SavedPrices/chart
```

### Uložení aktuálního kurzu nebo dávky ze stránky Live (POST)

Jedna poznámka + aktuální stav z API (bez `items`):

```http
POST /api/SavedPrices
Content-Type: application/json

{"note":"Moje poznámka"}
```

Dávka pozorovaných snímků (stejná poznámka pro každý řádek):

```http
POST /api/SavedPrices
Content-Type: application/json

{
  "note": "Společná poznámka",
  "items": [
    {
      "btcEur": 95000.0,
      "btcCzk": 2400000.0,
      "eurCzkRate": 25.3,
      "cnbRateValidFor": "2026-03-29",
      "fetchedAtUtc": "2026-03-29T12:00:00Z"
    }
  ]
}
```

```bash
curl -s -X POST http://localhost:5298/api/SavedPrices ^
  -H "Content-Type: application/json" ^
  -d "{\"note\":\"Test\"}"
```

(PowerShell jednodušeji s `Invoke-RestMethod -Body (@{ note = "Test" } | ConvertTo-Json)`.)

### Hromadná úprava poznámek (PUT)

```http
PUT /api/SavedPrices/notes
Content-Type: application/json

{
  "items": [
    { "id": 1, "note": "Nová poznámka A" },
    { "id": 2, "note": "Nová poznámka B" }
  ]
}
```

### Smazání záznamů (DELETE)

```http
DELETE /api/SavedPrices
Content-Type: application/json

{ "ids": [1, 2] }
```

```bash
curl -s -X DELETE http://localhost:5298/api/SavedPrices ^
  -H "Content-Type: application/json" ^
  -d "{\"ids\":[1]}"
```

### Postman

1. Vytvořte kolekci a proměnnou `baseUrl` = `http://localhost:5298`.
2. Přidejte požadavky: `GET {{baseUrl}}/api/LivePrice`, `GET {{baseUrl}}/api/SavedPrices`, atd.
3. U POST/PUT/DELETE nastavte **Body → raw → JSON** podle výše uvedených příkladů.

---

*Medor Testovací úkol –  portfolio projekt (Bitcoin kurz, ČNB, SQL Server, EF Core).*
