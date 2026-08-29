# ApplianceX implementation status (enterprise TODO map)

## Completed / solid
- Domain model (base unit, pack, prices) — DOMAIN_MODEL.md
- InventoryTransaction as stock SoT + InventoryService
- PackMath, TransactionTotals
- SaleService, PurchaseService, SaleReturnService (invoice-linked)
- PurchaseReturnService + form
- ProductService (safe code counter, validation)
- CustomerAccountService / SupplierAccountService + payments forms
- StockOpsService (opening / adjust / damage) + form
- AppSession (login/logout)
- PBKDF2 password hashing (UserRepository)
- Sale/Purchase forms wired to services
- Migrations 003–006
- Menu: Transactions (Sale/Return/Purchase/Purchase Return), Inventory (ops), Accounts, Reports

## Partial
- Customer/Supplier ledgers post on every sale/purchase (helpers exist; SaleRepository not fully wired to CustomerAccountService.PostSale yet)
- Reports profit COGS depth
- Full permission matrix beyond AppSettings menu keys
- Reusable TransactionForm layout component
- DPI formal testing (needs Windows client)
- Authenticator standalone app (license tooling) — login lives in ApplianceManagement.LoginForm
- Concurrent multi-user stress tests

## Not complete (environment / scope)
- Physical Windows 7/10 matrix testing in CI
- Backup/restore automated test
- Full UAT sign-off with real shop data
- Export Excel on all reports
- Branch multi-company

## Migrations to run
```
sqlcmd -S . -E -i V6\Migrations\003_Uom_PackSize.sql
sqlcmd -S . -E -i V6\Migrations\004_Phase1_Integrity.sql
sqlcmd -S . -E -i V6\Migrations\005_SaleReturn.sql
sqlcmd -S . -E -i V6\Migrations\006_Accounts_PurchaseReturn_StockOps.sql
dotnet build V6\ApplianceManagement
```
