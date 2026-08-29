# ApplianceX — system rules (single source of truth)

## Stock (units only)
- **Authoritative stock** = `SUM(QuantityIn) - SUM(QuantityOut)` from `InventoryTransaction`.
- `Products.CurrentStock` is kept in sync as a cache, but UI/sale checks use the ledger formula.

## Pack size
| Screen | Qty means | Price | Stock change |
|--------|-----------|-------|--------------|
| **Purchase** | Packs | Full pack `PurchasePrice` | `+ packs × PackSize` units; unit cost = pack÷size |
| **Sale** | Units | `SalePrice ÷ PackSize` | `− units` |
| **Sale Return** | Units | Unit sale price | `+ units` |

If `PackSize` is missing/≤0 treat as **1**.

## Forms UX
- Banner docked top (add banner **last** among Dock.Top controls).
- Footer: Disc% → Enter → Discount → Net → Paid → Save.
- **F9** = product history (Sale / Purchase / Return).
- History form: **ESC / F4** closes with **no** confirm dialog (`AttachF4Close(form, false)` or `Tag=NOSAVECONFIRM`).
- Transaction forms: F4 / close may ask confirm unless `Tag=NOSAVECONFIRM` after successful save.

## MDI
- Dashboard = borderless maximized `homeHost` (not cascade).
- Other children open Normal + cascade offset.
- Close All → `ForceHomeFill()` so dashboard is never half-cut.

## Branding
- Login title = `license.dat` → `SoftwareName` via `UiHelper.AppName`.

## Migrations to run once
1. `V6/Migrations/003_Uom_PackSize.sql`

## Build
```bat
git pull
sqlcmd -S . -E -i V6\Migrations\003_Uom_PackSize.sql
dotnet build V6\ApplianceManagement
```
