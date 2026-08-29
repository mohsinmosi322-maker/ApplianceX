# ApplianceX V6 — Implementation status

Last updated: 2026-08-29

## Done (production-ready core)

### Architecture
- Forms → Services → Repositories → SQL
- AppSession after login / clear on logout
- InventoryTransaction = stock source of truth; CurrentStock cache synced

### Database migrations
- 003 UOM / PackSize
- 004 integrity (constraints, unique codes)
- 005 SaleReturn tables
- 006 PurchaseReturn + CustomerLedger + SupplierLedger

### Transactions
- SaleService / PurchaseService (validation, totals, stock check)
- Invoice-linked SaleReturnService (over-return blocked)
- PurchaseReturnService (packs → units out)
- StockOpsService (opening / adjustment / damage)

### Accounts
- Customer / Supplier ledger posting on sale, purchase, returns, payments
- Payment forms + ledger statement forms

### Masters
- Products (New Item + ProductService)
- Customers / Suppliers master forms
- Categories master form

### UI
- Form accent banners, MDI dashboard ForceHomeFill
- Reports: Sales (green/red), Purchase, Stock, Profit (Revenue−COGS), CSV export
- Inventory filters: All / Low / Out
- DialogHelpers, CsvExport
- Status bar (user, role, shop, clock) on MainForm

### Security
- PBKDF2 password hashes (legacy SHA256 upgrade on login)
- Menu + OpenChild permission keys
- Settings password gate
- Parameterized SQL in repositories/services

### Authenticator
- Separate tool under V6/Authenticator (license / admin)
- App login is ApplianceManagement.LoginForm

## Partial / not fully verified in this environment
- Multi-branch company model
- Concurrent multi-user stress test
- Windows 7/10 DPI lab matrix
- Automated backup/restore pipeline
- Excel native export (CSV available)
- Print layout polish per printer model

## Install order
```
git pull
sqlcmd -S . -E -i V6\Migrations\003_Uom_PackSize.sql
sqlcmd -S . -E -i V6\Migrations\004_Phase1_Integrity.sql
sqlcmd -S . -E -i V6\Migrations\005_SaleReturn.sql
sqlcmd -S . -E -i V6\Migrations\006_Accounts_PurchaseReturn_StockOps.sql
dotnet build V6\ApplianceManagement
```
