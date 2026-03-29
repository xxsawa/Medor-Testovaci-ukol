# Secrets

## Lokální vývoj

- Zkopírujte `Medor.Api/.env.example` do `Medor.Api/.env` a doplňte připojení k SQL Serveru.  
Soubor `.env` je ignorovaný — do Gitu ho necommitujte.
- Nebo v adresáři `Medor.Api`:  
`dotnet user-secrets set ConnectionStrings:DefaultConnection "..."`

