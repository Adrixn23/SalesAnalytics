USE SalesAnalyticsDB;
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Dim')
BEGIN
    EXEC('CREATE SCHEMA Dim');
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Fact')
BEGIN
    EXEC('CREATE SCHEMA Fact');
END
GO

IF OBJECT_ID('Fact.Sales', 'U') IS NOT NULL DROP TABLE Fact.Sales;
IF OBJECT_ID('Dim.Customer', 'U') IS NOT NULL DROP TABLE Dim.Customer;
IF OBJECT_ID('Dim.Product', 'U') IS NOT NULL DROP TABLE Dim.Product;
IF OBJECT_ID('Dim.Seller', 'U') IS NOT NULL DROP TABLE Dim.Seller;
IF OBJECT_ID('Dim.Status', 'U') IS NOT NULL DROP TABLE Dim.Status;
IF OBJECT_ID('Dim.Date', 'U') IS NOT NULL DROP TABLE Dim.Date;
GO

CREATE TABLE Dim.Customer (
    CustomerKey INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    CityName NVARCHAR(100) NOT NULL,
    RegionName NVARCHAR(100) NOT NULL,
    CountryName NVARCHAR(100) NOT NULL,
    Segment NVARCHAR(50) NOT NULL
);

CREATE TABLE Dim.Product (
    ProductKey INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT NOT NULL,
    ProductName NVARCHAR(150) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    StandardPrice DECIMAL(18,2) NOT NULL
);

CREATE TABLE Dim.Seller (
    SellerKey INT IDENTITY(1,1) PRIMARY KEY,
    SellerID INT NOT NULL,
    SellerName NVARCHAR(150) NOT NULL,
    SellerRegion NVARCHAR(100) NOT NULL
);

CREATE TABLE Dim.Status (
    StatusKey INT IDENTITY(1,1) PRIMARY KEY,
    StatusID INT NOT NULL,
    StatusName NVARCHAR(50) NOT NULL
);

CREATE TABLE Dim.Date (
    DateKey INT PRIMARY KEY,
    FullDate DATE NOT NULL,
    Year INT NOT NULL,
    Month INT NOT NULL,
    MonthName NVARCHAR(20) NOT NULL,
    Quarter INT NOT NULL
);

CREATE TABLE Fact.Sales (
    SalesKey INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    OrderDetailID INT NOT NULL,
    CustomerKey INT NOT NULL,
    ProductKey INT NOT NULL,
    SellerKey INT NOT NULL,
    StatusKey INT NOT NULL,
    DateKey INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Fact_Sales_Customer FOREIGN KEY (CustomerKey) REFERENCES Dim.Customer(CustomerKey),
    CONSTRAINT FK_Fact_Sales_Product FOREIGN KEY (ProductKey) REFERENCES Dim.Product(ProductKey),
    CONSTRAINT FK_Fact_Sales_Seller FOREIGN KEY (SellerKey) REFERENCES Dim.Seller(SellerKey),
    CONSTRAINT FK_Fact_Sales_Status FOREIGN KEY (StatusKey) REFERENCES Dim.Status(StatusKey),
    CONSTRAINT FK_Fact_Sales_Date FOREIGN KEY (DateKey) REFERENCES Dim.Date(DateKey)
);
GO

SET IDENTITY_INSERT Dim.Seller ON;
INSERT INTO Dim.Seller (SellerKey, SellerID, SellerName, SellerRegion)
VALUES (-1, -1, 'Desconocido', 'No disponible');
SET IDENTITY_INSERT Dim.Seller OFF;

INSERT INTO Dim.Status (StatusID, StatusName)
SELECT StatusID, StatusName FROM Sales.OrderStatus;

INSERT INTO Dim.Product (ProductID, ProductName, CategoryName, StandardPrice)
SELECT p.ProductID, p.ProductName, c.CategoryName, p.Price
FROM Catalog.Products p
INNER JOIN Catalog.Categories c ON p.CategoryID = c.CategoryID;

INSERT INTO Dim.Customer (CustomerID, FullName, Email, CityName, RegionName, CountryName, Segment)
SELECT cu.CustomerID, cu.FirstName + ' ' + cu.LastName, cu.Email, ci.CityName, 'No disponible', co.CountryName, 'No disponible'
FROM People.Customers cu
INNER JOIN Geo.Cities ci ON cu.CityID = ci.CityID
INNER JOIN Geo.Countries co ON ci.CountryID = co.CountryID;

DECLARE @StartDate DATE = '2023-01-01';
DECLARE @EndDate DATE = '2025-12-31';

WITH DateCTE AS (
    SELECT @StartDate AS DateValue
    UNION ALL
    SELECT DATEADD(day, 1, DateValue)
    FROM DateCTE
    WHERE DateValue < @EndDate
)
INSERT INTO Dim.Date (DateKey, FullDate, Year, Month, MonthName, Quarter)
SELECT 
    CAST(FORMAT(DateValue, 'yyyyMMdd') AS INT),
    DateValue,
    YEAR(DateValue),
    MONTH(DateValue),
    FORMAT(DateValue, 'MMMM', 'es-ES'),
    DATEPART(quarter, DateValue)
FROM DateCTE
OPTION (MAXRECURSION 0);

INSERT INTO Fact.Sales (OrderID, OrderDetailID, CustomerKey, ProductKey, SellerKey, StatusKey, DateKey, Quantity, UnitPrice, Total)
SELECT 
    o.OrderID,
    od.OrderDetailID,
    dc.CustomerKey,
    dp.ProductKey,
    -1,
    ds.StatusKey,
    CAST(FORMAT(o.OrderDate, 'yyyyMMdd') AS INT),
    od.Quantity,
    od.TotalPrice / od.Quantity,
    od.TotalPrice
FROM Sales.Orders o
INNER JOIN Sales.OrderDetails od ON o.OrderID = od.OrderID
INNER JOIN People.Customers c ON o.CustomerID = c.CustomerID
INNER JOIN Dim.Customer dc ON c.CustomerID = dc.CustomerID
INNER JOIN Catalog.Products p ON od.ProductID = p.ProductID
INNER JOIN Dim.Product dp ON p.ProductID = dp.ProductID
INNER JOIN Sales.OrderStatus s ON o.StatusID = s.StatusID
INNER JOIN Dim.Status ds ON s.StatusID = ds.StatusID;
GO
