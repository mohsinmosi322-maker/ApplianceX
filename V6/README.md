# ApplianceX V6

Windows Forms POS + inventory for appliance / general stores.

## Projects

| Folder | Purpose |
|--------|---------|
| `ApplianceManagement` | Main POS application |
| `Authenticator` | License / admin tools |
| `Migrations` | SQL scripts (run in order) |
| `run` | Sample connectionstring.txt |

## Requirements

- .NET Framework 4.7.2+
- SQL Server 2008 R2 or later (Express OK)
- Windows 7 SP1+ / 10 / 11

## Quick setup

```bat
git pull

:: create DB (adjust server)
sqlcmd -S .\SQLEXPRESS -E -i Migrations\002_NewCustomer_FullDatabase.sql
sqlcmd -S .\SQLEXPRESS -E -i Migrations\003_Uom_PackSize.sql
sqlcmd -S .\SQLEXPRESS -E -i Migrations\004_Phase1_Integrity.sql
sqlcmd -S .\SQLEXPRESS -E -i Migrations\005_SaleReturn.sql
sqlcmd -S .\SQLEXPRESS -E -i Migrations\006_Accounts_PurchaseReturn_StockOps.sql

dotnet build ApplianceManagement
```

Connection string (pick one):

1. **license.dat** from Authenticator (preferred production)
2. **connectionstring.txt** next to the .exe (see `run/connectionstring.txt.sample`)
3. **App.config** key `ApplianceDb`

Default admin user depends on seed script (often `admin` / check seed).

## Domain rules (short)

- Stock truth = `InventoryTransaction` (units in − units out)
- **Sale / Sale Return**: quantity in **base units**; price = pack sale price ÷ PackSize
- **Purchase / Purchase Return**: quantity in **packs**; ledger posts packs × PackSize units
- See `DOMAIN_MODEL.md` and `SYSTEM_RULES.md`

## Keyboard

| Key | Action |
|-----|--------|
| F2 | Sale |
| F3 | Purchase |
| F4 | Close form |
| F5 | Refresh |
| F8 | Remove line |
| F9 | Product history |
| F12 | Discount / Save |

## Logs & backup

- Logs: `{exe}\logs\app-yyyyMMdd.log` — **Help → Application Logs**
- Backup: **Settings (Admin) → Backup DB**

## Status

See `IMPLEMENTATION_STATUS.md`.
