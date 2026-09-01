/*
  Repair Products.PurchasePrice if an older build stored UNIT cost instead of PACK price.

  Symptom: after each purchase, product cost kept getting smaller (divided by PackSize again).
  Correct rule: Products.PurchasePrice = price of ONE PACK. Unit cost = PurchasePrice / PackSize.

  This script multiplies PurchasePrice back by PackSize ONLY when the last purchase
  detail pack price is about PackSize times the current product price (corruption pattern).

  Review results before running the UPDATE. Safe to re-run checks.
*/
USE APPLIANCE_DB;
GO
SET NOCOUNT ON;

-- Preview candidates (read-only)
SELECT p.ProductID, p.ProductCode, p.ProductName, p.PackSize,
       p.PurchasePrice AS CurrentStoredPrice,
       d.LastPackPrice,
       CASE WHEN ISNULL(p.PackSize,1) > 1 AND p.PurchasePrice > 0
                 AND d.LastPackPrice IS NOT NULL
                 AND ABS(p.PurchasePrice * p.PackSize - d.LastPackPrice) < 0.05
            THEN p.PurchasePrice * p.PackSize
            ELSE p.PurchasePrice END AS SuggestedPackPrice
FROM Products p
OUTER APPLY (
  SELECT TOP 1 pd.PurchasePrice AS LastPackPrice
  FROM PurchaseDetail pd
  INNER JOIN PurchaseHeader ph ON pd.PurchaseID = ph.PurchaseID
  WHERE pd.ProductID = p.ProductID
  ORDER BY ph.PurchaseDate DESC, pd.PurchaseDetailID DESC
) d
WHERE ISNULL(p.PackSize,1) > 1;
GO

-- Uncomment to apply repair for clear corruption cases:
/*
UPDATE p
SET PurchasePrice = d.LastPackPrice
FROM Products p
OUTER APPLY (
  SELECT TOP 1 pd.PurchasePrice AS LastPackPrice
  FROM PurchaseDetail pd
  INNER JOIN PurchaseHeader ph ON pd.PurchaseID = ph.PurchaseID
  WHERE pd.ProductID = p.ProductID
  ORDER BY ph.PurchaseDate DESC, pd.PurchaseDetailID DESC
) d
WHERE ISNULL(p.PackSize,1) > 1
  AND d.LastPackPrice IS NOT NULL
  AND d.LastPackPrice > 0
  AND p.PurchasePrice > 0
  AND ABS(p.PurchasePrice * p.PackSize - d.LastPackPrice) < 0.05;
*/

PRINT '007 preview complete. Review SuggestedPackPrice then uncomment UPDATE if needed.';
GO
