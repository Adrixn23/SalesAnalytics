use SalesAnalyticsDB;
go

select 
    c.CategoryName as categoria,
    count(od.ProductID) as cantidad_productos_vendidos,
    sum(od.TotalPrice) as ingresos_totales
from Catalog.Categories c
inner join Catalog.Products p on c.CategoryID = p.CategoryID
inner join Sales.OrderDetails od on p.ProductID = od.ProductID
group by c.CategoryName
order by ingresos_totales desc;

select top 10
    c.FirstName + ' ' + c.LastName as cliente,
    c.Email as email,
    count(o.OrderID) as cantidad_ordenes,
    sum(od.TotalPrice) as total_gastado
from People.Customers c
inner join Sales.Orders o on c.CustomerID = o.CustomerID
inner join Sales.OrderDetails od on o.OrderID = od.OrderID
group by c.CustomerID, c.FirstName, c.LastName, c.Email
order by total_gastado desc;

select 
    s.StatusName as estado_orden,
    count(o.OrderID) as total_ordenes,
    cast(count(o.OrderID) * 100.0 / (select count(*) from Sales.Orders) as decimal(5,2)) as porcentaje
from Sales.OrderStatus s
left join Sales.Orders o on s.StatusID = o.StatusID
group by s.StatusName
order by total_ordenes desc;

select 
    co.CountryName as pais,
    ci.CityName as ciudad,
    count(distinct c.CustomerID) as clientes_unicos,
    count(distinct o.OrderID) as total_ordenes_generadas
from Geo.Countries co
inner join Geo.Cities ci on co.CountryID = ci.CountryID
inner join People.Customers c on ci.CityID = c.CityID
inner join Sales.Orders o on c.CustomerID = o.CustomerID
group by co.CountryName, ci.CityName
order by total_ordenes_generadas desc;

select 
    p.ProductID as producto_id,
    p.ProductName as nombre_producto,
    c.CategoryName as categoria,
    p.Stock as stock_actual,
    sum(od.Quantity) as cantidad_vendida_historica
from Catalog.Products p
inner join Catalog.Categories c on p.CategoryID = c.CategoryID
inner join Sales.OrderDetails od on p.ProductID = od.ProductID
where p.Stock < 10
group by p.ProductID, p.ProductName, c.CategoryName, p.Stock
order by p.Stock asc;
