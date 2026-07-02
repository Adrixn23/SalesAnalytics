-- =========================================================================
-- PARTE 7: CONSULTAS PARA VALIDACIÓN DE INTEGRIDAD Y ANÁLISIS DE DATOS
-- Base de Datos: SalesAnalytics
-- =========================================================================

USE SalesAnalytics;
GO

-- 1. Total de ingresos generados por Categoría (Demuestra INNER JOIN múltiple)
SELECT 
    c.CategoryName AS Categoria,
    COUNT(od.ProductID) AS CantidadProductosVendidos,
    SUM(od.TotalPrice) AS IngresosTotales
FROM Catalog.Categories c
INNER JOIN Catalog.Products p ON c.CategoryID = p.CategoryID
INNER JOIN Sales.OrderDetails od ON p.ProductID = od.ProductID
GROUP BY c.CategoryName
ORDER BY IngresosTotales DESC;

-- 2. Top 10 Clientes con mayor volumen de compras (Demuestra agrupación y filtrado)
SELECT TOP 10
    c.FirstName + ' ' + c.LastName AS Cliente,
    c.Email,
    COUNT(o.OrderID) AS CantidadOrdenes,
    SUM(od.TotalPrice) AS TotalGastado
FROM People.Customers c
INNER JOIN Sales.Orders o ON c.CustomerID = o.CustomerID
INNER JOIN Sales.OrderDetails od ON o.OrderID = od.OrderID
GROUP BY c.CustomerID, c.FirstName, c.LastName, c.Email
ORDER BY TotalGastado DESC;

-- 3. Resumen de Órdenes por Estado (Demuestra el catálogo de estatus)
SELECT 
    s.StatusName AS EstadoOrden,
    COUNT(o.OrderID) AS TotalOrdenes,
    CAST(COUNT(o.OrderID) * 100.0 / (SELECT COUNT(*) FROM Sales.Orders) AS DECIMAL(5,2)) AS Porcentaje
FROM Sales.OrderStatus s
LEFT JOIN Sales.Orders o ON s.StatusID = o.StatusID
GROUP BY s.StatusName
ORDER BY TotalOrdenes DESC;

-- 4. Ventas por País y Ciudad (Demuestra la integridad geográfica)
SELECT 
    co.CountryName AS Pais,
    ci.CityName AS Ciudad,
    COUNT(DISTINCT c.CustomerID) AS ClientesUnicos,
    COUNT(DISTINCT o.OrderID) AS TotalOrdenesGeneradas
FROM Geo.Countries co
INNER JOIN Geo.Cities ci ON co.CountryID = ci.CountryID
INNER JOIN People.Customers c ON ci.CityID = c.CityID
INNER JOIN Sales.Orders o ON c.CustomerID = o.CustomerID
GROUP BY co.CountryName, ci.CityName
ORDER BY TotalOrdenesGeneradas DESC;

-- 5. Consulta de Auditoría: Productos con bajo Stock (Menor a 10) que tienen demanda reciente
SELECT 
    p.ProductID,
    p.ProductName,
    c.CategoryName,
    p.Stock AS StockActual,
    SUM(od.Quantity) AS CantidadVendidaHistorica
FROM Catalog.Products p
INNER JOIN Catalog.Categories c ON p.CategoryID = c.CategoryID
INNER JOIN Sales.OrderDetails od ON p.ProductID = od.ProductID
WHERE p.Stock < 10
GROUP BY p.ProductID, p.ProductName, c.CategoryName, p.Stock
ORDER BY p.Stock ASC;
