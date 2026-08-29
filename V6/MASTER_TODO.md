# ApplianceX V6 — MASTER TODO (tracking)

Status: `DONE` | `PARTIAL` | `TODO`

## Phase 1 — Core Architecture & Database
| ID | Item | Status |
|----|------|--------|
| 001 | Authoritative base unit | DONE — DOMAIN_MODEL.md |
| 002 | PackSize / pack qty / base qty | DONE — documented + PackMath |
| 003 | PurchasePrice / SalePrice meaning | DONE — pack prices |
| 004 | Never mix pack qty with ledger qty | PARTIAL — repos updated; enforce via InventoryService |
| 005 | InventoryTransaction = SoT | DONE — Sale/Purchase use ledger |
| 006 | No uncontrolled CurrentStock edits | PARTIAL — only via sync after ledger |
| 007 | CurrentStock as controlled cache | PARTIAL — InventoryService.SyncCache |
| 008 | Foreign keys | PARTIAL — migration 004 |
| 009 | Qty/price constraints | PARTIAL — migration 004 |
| 010 | Unique ProductCode | DONE (schema) |
| 011 | Unique Barcode | PARTIAL — filtered unique index 004 |
| 012 | Safe product code generation | TODO |
| 013 | Atomic multi-table saves | PARTIAL — already in Sale/Purchase |
| 014 | Audit columns | PARTIAL — migration 004 optional cols |
| 015 | Remove exception-based schema detect | TODO |

## Phase 2 — Service layer
| ID | Item | Status |
|----|------|--------|
| 016–026 | Services | PARTIAL — InventoryService, PackMath, TransactionTotals started |

## Phase 3–25
See full list in project discussion. Implement in order:
1. DB + unit/pack → 2. Ledger → 3. Services → 4. Product → 5. Purchase → 6. Sale → 7. Returns → 8. Accounts → 9. UI → 10. Reports → 11. Auth → 12. UAT

### Run after pull
```bat
sqlcmd -S . -E -i V6\Migrations\003_Uom_PackSize.sql
sqlcmd -S . -E -i V6\Migrations\004_Phase1_Integrity.sql
dotnet build V6\ApplianceManagement
```
