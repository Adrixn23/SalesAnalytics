using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVentas.Models;
using System;
using System.Collections.Generic;

namespace SistemaVentas.Models.Configurations
{
    public partial class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
    {
        public void Configure(EntityTypeBuilder<OrderStatus> entity)
        {
            entity.HasKey(e => e.StatusId);

            entity.ToTable("OrderStatus", "Sales");

            entity.HasIndex(e => e.StatusName, "UQ_OrderStatus_StatusName").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.StatusName)
                .IsRequired()
                .HasMaxLength(50);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<OrderStatus> entity);
    }
}
