use SalesAnalyticsDB;
go

create or alter view Sales.vw_SalesSummaryByCountryAndCategory as
select 
    co.CountryName as pais,
    c.CategoryName as categoria,
    count(od.ProductID) as total_productos_vendidos,
    sum(od.TotalPrice) as ingresos_totales
from Geo.Countries co
inner join Geo.Cities ci on co.CountryID = ci.CountryID
inner join People.Customers cu on ci.CityID = cu.CityID
inner join Sales.Orders o on cu.CustomerID = o.CustomerID
inner join Sales.OrderDetails od on o.OrderID = od.OrderID
inner join Catalog.Products p on od.ProductID = p.ProductID
inner join Catalog.Categories c on p.CategoryID = c.CategoryID
group by co.CountryName, c.CategoryName;
go

create or alter procedure People.sp_GetCustomerPurchaseHistory
    @Email varchar(150)
as
begin
    set nocount on;

    select 
        o.OrderID as orden_id,
        o.OrderDate as fecha_orden,
        s.StatusName as estado,
        p.ProductName as producto,
        od.Quantity as cantidad,
        od.TotalPrice as total
    from People.Customers c
    inner join Sales.Orders o on c.CustomerID = o.CustomerID
    inner join Sales.OrderStatus s on o.StatusID = s.StatusID
    inner join Sales.OrderDetails od on o.OrderID = od.OrderID
    inner join Catalog.Products p on od.ProductID = p.ProductID
    where c.Email = @Email
    order by o.OrderDate desc;
end;
go
