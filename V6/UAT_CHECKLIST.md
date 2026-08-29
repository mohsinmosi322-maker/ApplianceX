# ApplianceX V6 — UAT checklist (run on your PC)

Mark each item after testing on SQL Server 2008+ / Windows 7 or 10.

## Setup
- [ ] Migrations 002–006 applied
- [ ] `connectionstring.txt` or license.dat works
- [ ] Login shows **Database: connected**
- [ ] Default admin can sign in

## Product / pack
- [ ] New Item: PackSize 50, Sale price 5000 → unit preview 100
- [ ] Edit existing by code works
- [ ] Category / UOM optional (right-click enable)

## Purchase
- [ ] Qty = packs; stock increases by packs × PackSize
- [ ] F9 opens purchase history (ESC closes, no confirm dialog)
- [ ] F8 removes line; Up/Down moves grid
- [ ] Discount % → amount → net → paid → save confirm

## Sale
- [ ] Unit price = SalePrice / PackSize
- [ ] Stock blocks oversell
- [ ] F9 sale history; F8 remove; arrows
- [ ] Max discount enforced for User role

## Returns
- [ ] Sale return cannot exceed sold qty on invoice path
- [ ] Sale return unit pricing same as sale
- [ ] Purchase return reduces stock by packs × PackSize
- [ ] Sales report: green SALE / red RETURN

## Accounts
- [ ] Customer payment posts ledger credit
- [ ] Supplier payment posts ledger debit
- [ ] Ledgers show running balance

## Stock ops
- [ ] Opening / Adjustment / Damage update ledger
- [ ] Stock Position filters Low / Out

## Security / ops
- [ ] Settings password gate
- [ ] Create user + menu rights (re-login required)
- [ ] Backup DB (path writable by SQL service)
- [ ] Help → Application Logs shows entries

## UI
- [ ] Dashboard fills after Close All
- [ ] Child forms cascade; only one Purchase window
- [ ] Status bar user / shop / clock

## Known environment limits
- Concurrent multi-user stress not automated here
- DPI on every Win7 machine must be checked locally
