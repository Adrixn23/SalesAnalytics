using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaVentas.Models;
using System;
using System.Collections.Generic;

namespace SistemaVentas.Models.Configurations
{
    public partial class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> entity)
        {
            entity.ToTable("Cities", "Geo");

            entity.HasIndex(e => e.CountryId, "IX_Cities_CountryID");

            entity.HasIndex(e => new { e.CityName, e.CountryId }, "UQ_Cities_CityName_CountryID").IsUnique();

            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.CityName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.CountryId).HasColumnName("CountryID");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<City> entity);
    }
}
