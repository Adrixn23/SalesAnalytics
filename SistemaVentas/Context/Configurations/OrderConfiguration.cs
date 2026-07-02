using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVentas.Models;
using System;
using System.Collections.Generic;

namespace SistemaVentas.Models.Configurations
{
    public partial class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> entity)
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAFBD79285E");

            entity.ToTable("Orders", "Sales");

            entity.HasIndex(e => e.StatusId, "IX_Orders_StatusID");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Order> entity);
    }
}
