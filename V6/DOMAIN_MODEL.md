# ApplianceX — Domain Model (authoritative)

## Base unit (inventory unit)
Stock is always counted in **base units** (pieces / kg / litre as defined per product).

- `InventoryTransaction.QuantityIn` / `QuantityOut` = **base units only**
- Never store pack counts in the ledger

## Pack model
| Term | Meaning |
|------|--------|
| **PackSize** | How many base units in one pack (e.g. 50 for 50kg bag). Default **1** |
| **Pack quantity** | Number of whole packs (Purchase line qty) |
| **Base quantity** | Units = packs × PackSize (ledger) |
| **Loose quantity** | Partial units sold without full pack (Sale qty in units) |

```
BaseUnits = Packs × PackSize
Packs     = BaseUnits / PackSize   (when PackSize > 0)
```

## Prices
| Field | Meaning |
|-------|--------|
| **PurchasePrice** (on product / purchase line) | **Price of one pack** (or one unit if PackSize=1) |
| **SalePrice** (on product) | **Price of one pack** (list price) |
| **Unit cost** | `PurchasePrice / PackSize` — stored on inventory rows |
| **Unit sale price** | `SalePrice / PackSize` — charged on Sale / Sale Return lines |

## Transaction rules
| Document | Line qty means | Line price | Ledger effect |
|----------|----------------|------------|---------------|
| **Purchase** | Packs | Pack purchase price | `+ packs × PackSize` units IN |
| **Sale** | Base units | Unit sale price | units OUT |
| **Sale Return** | Base units | Unit sale price | units IN |
| **Purchase Return** | Packs | Pack purchase price | `− packs × PackSize` units OUT |
| **Adjustment / Opening / Damage** | Base units | optional unit cost | IN or OUT with reason |

## Stock truth
```
Available = SUM(QuantityIn) − SUM(QuantityOut)   -- InventoryTransaction
```
`Products.CurrentStock` is a **cache only**. After every inventory post, sync:
```sql
UPDATE Products SET CurrentStock = (ledger stock) WHERE ProductID = @P
```

## Posted documents
Posted Sale / Purchase headers are **immutable**. Corrections only via Return / Adjustment.
