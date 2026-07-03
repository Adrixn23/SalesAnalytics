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
            entity.HasKey(e => e.CityId).HasName("PK__Cities__F2D21A9652BE83E6");

            entity.ToTable("Cities", "Geo");

            entity.HasIndex(e => e.CountryId, "IX_Cities_CountryID");

            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.CityName)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CountryId).HasColumnName("CountryID");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<City> entity);
    }
}
