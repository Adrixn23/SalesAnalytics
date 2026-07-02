# 🛒 Sistema de Análisis de Ventas - Proceso ETL

Sistema ETL desarrollado en C# .NET 9 para procesar y cargar datos de ventas masivos desde archivos CSV hacia una base de datos relacional SQL Server utilizando Entity Framework Core, CsvHelper y LINQ.

---

## 🏗️ Arquitectura
CSV (Dataset Raw) ➔ Limpieza en Memoria (LINQ) ➔ Validación de Integridad y Duplicados ➔ Carga Transaccional ➔ BD SQL Server

## 🗄️ Modelo de Datos

**Entidades Principales y Esquemas:**
* `Geo.Countries` / `Geo.Cities` - Catálogo de locaciones geográficas.
* `Catalog.Categories` / `Catalog.Products` - Inventario y categorización.
* `People.Customers` - Información de clientes.
* `Sales.OrderStatus` - Catálogo de estados de facturación.
* `Sales.Orders` - Cabecera de pedidos.
* `Sales.OrderDetails` - Líneas de pedido (cantidades y precios).

**Relaciones:**
* `Countries` 1 ➔ N `Cities`
* `Cities` 1 ➔ N `Customers`
* `Customers` 1 ➔ N `Orders`
* `Orders` 1 ➔ N `OrderDetails`
* `Products` 1 ➔ N `OrderDetails`

---

## ⚡ Características Principales
* **Procesamiento de Alto Rendimiento:** Inserciones masivas utilizando Entity Framework Core `AddRange`, procesando +87,000 registros en ~6 segundos.
* **Idempotencia (Cero Duplicados):** Uso intensivo de colecciones `HashSet` para memoria caché en RAM (Complejidad O(1)), garantizando que si se corre el proceso múltiples veces, no se insertarán duplicados.
* **Validación de Integridad:** Rechazo automático de datos huérfanos que violen reglas de llaves foráneas.
* **Cero `foreach`:** Todo el procesamiento y proyección de objetos se realizó respetando el uso estricto de LINQ (`Select`, `Where`, `ToList`).
* **Polimorfismo:** Implementación de la interfaz `IEtlService` orquestada genéricamente por la aplicación.

---

🚀 Uso Rápido (Instrucciones para Ejecución Local)

Para ejecutar este proyecto en su entorno local, por favor siga los siguientes pasos:

### 1. Preparar la Base de Datos
Ejecute el script adjunto para crear el esquema estructural:
```sql
-- Ejecute el archivo 01_CreateSchema.sql en su SQL Server Management Studio
-- Esto generará la base de datos SalesAnalyticsDB y todas sus relaciones.
```

 2. Configurar Rutas Locales
Abra el archivo `Configuration/AppSettings.cs` y modifique las siguientes 2 variables para que coincidan con su equipo:
```csharp
// Cambie esta línea al nombre de su Servidor SQL local
internal static readonly string ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=SalesAnalyticsDB;Integrated Security=True;Trust Server Certificate=True";

// Cambie esta ruta a la carpeta donde usted tiene guardados los archivos CSV
internal static readonly string CsvDirectory = @"C:\Ruta\Hacia\Sus\Archivos_CSV";
```

### 3. Ejecutar
Compile y ejecute la aplicación (F5). El sistema realizará la inyección de dependencias y comenzará la carga masiva automatizada, reportando los registros procesados, insertados y rechazados por consola.
