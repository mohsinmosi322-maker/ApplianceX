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

```
BaseUnits = Packs × PackSize
```

## Prices (CRITICAL — do not mix)
| Field | Meaning |
|-------|--------|
| **Products.PurchasePrice** | **Price of ONE PACK** (never unit cost) |
| **Products.SalePrice** | **Price of ONE PACK** (list price) |
| **PurchaseDetail.PurchasePrice** | Pack purchase price as entered on the bill |
| **InventoryTransaction.UnitCost** | `PurchasePrice / PackSize` — only place unit cost is stored |
| **Sale line price** | `SalePrice / PackSize` (unit sale price) |

### Fatal bug (fixed)
Old code did `UPDATE Products SET PurchasePrice = unitCost` on every purchase.
That stored unit cost in a pack-price field. Profit then did `unitCost / PackSize` again → cost collapsed every time.

**Correct purchase update:**
```sql
UPDATE Products SET PurchasePrice = @PackPrice  -- same pack price as the purchase line
```
Ledger still posts `UnitCost = PackPrice / PackSize`.

## Transaction rules
| Document | Line qty means | Line price | Ledger effect |
|----------|----------------|------------|---------------|
| **Purchase** | Packs | Pack purchase price | `+ packs × PackSize` units IN at unit cost |
| **Sale** | Base units | Unit sale price | units OUT |
| **Sale Return** | Base units | Unit sale price | units IN |
| **Purchase Return** | Packs | Pack purchase price | `− packs × PackSize` units OUT |

## Profit / COGS
1. Prefer **weighted average UnitCost** from `InventoryTransaction` (IN rows)
2. Else last purchase unit cost from ledger
3. Else `Products.PurchasePrice / PackSize`

Never use pack price × unit qty for COGS.

## Stock truth
```
Available = SUM(QuantityIn) − SUM(QuantityOut)   -- InventoryTransaction
```
`Products.CurrentStock` is a **cache only**.
