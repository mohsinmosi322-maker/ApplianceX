# ApplianceX V6 — Implementation status

**Last updated:** 2026-08-29

## Production-ready (code on GitHub)

| Area | Status |
|------|--------|
| Architecture (Forms → Services → Repos) | Done |
| Inventory ledger + PackSize/UOM | Done |
| Sale / Purchase / Returns | Done |
| Customer / Supplier accounts | Done |
| Masters (product, customer, supplier, category) | Done |
| Reports + Profit COGS + CSV | Done |
| MDI dashboard, accents, status bar | Done |
| Permissions, settings lock, PBKDF2 | Done |
| connectionstring.txt + license + App.config | Done |
| Global exception log + Log viewer | Done |
| DB backup (Admin) | Done |
| Sale/Purchase F8, F9, arrows | Done |

## Your machine only

| Item | Notes |
|------|--------|
| SQL 2008 migrations | Run 002–006 |
| Printers | BillPrinter layout per model |
| Win7/10 DPI | Visual check |
| Multi-user concurrency | Stress test |
| UAT | See `UAT_CHECKLIST.md` |

## Repo

https://github.com/mohsinmosi322-maker/ApplianceX

```bat
git pull
dotnet build V6\ApplianceManagement
```
