USE SalesAnalyticsDB;
GO

-- =========================================================================
-- VISTA REQUERIDA (Entregable 2)
-- =========================================================================
-- Esta vista resume las ventas totales por país y categoría, facilitando reportes rápidos
CREATE OR ALTER VIEW Sales.vw_SalesSummaryByCountryAndCategory AS
SELECT 
    co.CountryName AS Pais,
    c.CategoryName AS Categoria,
    COUNT(od.ProductID) AS TotalProductosVendidos,
    SUM(od.TotalPrice) AS IngresosTotales
FROM Geo.Countries co
INNER JOIN Geo.Cities ci ON co.CountryID = ci.CountryID
INNER JOIN People.Customers cu ON ci.CityID = cu.CityID
INNER JOIN Sales.Orders o ON cu.CustomerID = o.CustomerID
INNER JOIN Sales.OrderDetails od ON o.OrderID = od.OrderID
INNER JOIN Catalog.Products p ON od.ProductID = p.ProductID
INNER JOIN Catalog.Categories c ON p.CategoryID = c.CategoryID
GROUP BY co.CountryName, c.CategoryName;
GO

-- =========================================================================
-- PROCEDIMIENTO ALMACENADO REQUERIDO (Entregable 2)
-- =========================================================================
-- Este procedimiento permite buscar el historial de compras de un cliente por su correo
CREATE OR ALTER PROCEDURE People.sp_GetCustomerPurchaseHistory
    @Email VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        o.OrderID,
        o.OrderDate,
        s.StatusName,
        p.ProductName,
        od.Quantity,
        od.TotalPrice
    FROM People.Customers c
    INNER JOIN Sales.Orders o ON c.CustomerID = o.CustomerID
    INNER JOIN Sales.OrderStatus s ON o.StatusID = s.StatusID
    INNER JOIN Sales.OrderDetails od ON o.OrderID = od.OrderID
    INNER JOIN Catalog.Products p ON od.ProductID = p.ProductID
    WHERE c.Email = @Email
    ORDER BY o.OrderDate DESC;
END;
GO

-- Ejemplo de cómo ejecutar el procedimiento (opcional para el maestro):
-- EXEC People.sp_GetCustomerPurchaseHistory @Email = 'kmarshall@chavez-lane.biz';
