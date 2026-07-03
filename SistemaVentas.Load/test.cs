using System;
using System.IO;
using System.Linq;

class Test {
    static void Main() {
        var orders = File.ReadAllLines("C:\\Adrian\\ITLA_Materias ETC\\FrancisElec1\\Archivo CSV Análisis de Ventas-20260603\\orders.csv").Skip(1).Select(x => x.Split(',')[0]).ToHashSet();
        var products = File.ReadAllLines("C:\\Adrian\\ITLA_Materias ETC\\FrancisElec1\\Archivo CSV Análisis de Ventas-20260603\\products.csv").Skip(1).Select(x => x.Split(',')[0]).ToHashSet();
        var details = File.ReadAllLines("C:\\Adrian\\ITLA_Materias ETC\\FrancisElec1\\Archivo CSV Análisis de Ventas-20260603\\order_details.csv").Skip(1).Select(x => x.Split(',')).ToList();
        
        int invalidFk = 0;
        foreach (var d in details) {
            if (!orders.Contains(d[0]) || !products.Contains(d[1])) invalidFk++;
        }
        
        var duplicates = details.GroupBy(x => new { O = x[0], P = x[1] }).Where(g => g.Count() > 1).Sum(g => g.Count() - 1);
        
        Console.WriteLine($"Invalid FKs: {invalidFk}");
        Console.WriteLine($"Duplicates: {duplicates}");
    }
}
