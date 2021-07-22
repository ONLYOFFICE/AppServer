using System;
using Microsoft.EntityFrameworkCore;

namespace ASC.Core.Common.EF.Model
{
    public class DbWebstudioSettings : BaseEntity
    {
        public int TenantId { get; set; }
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Data { get; set; }
        public override object[] GetKeys()
        {
            return new object[] { TenantId, Id, UserId };
        }
    }

    public static class WebstudioSettingsExtension
    {
        public static ModelBuilderWrapper AddWebstudioSettings(this ModelBuilderWrapper modelBuilder)
        {
            modelBuilder
                .Add(MySqlAddWebstudioSettings, Provider.MySql)
                .Add(PgSqlAddWebstudioSettings, Provider.Postgre)
                .HasData(
                new DbWebstudioSettings
                { 
                    TenantId = 1, 
                    Id = Guid.Parse("9a925891-1f92-4ed7-b277-d6f649739f06"), 
                    UserId = Guid.Parse("00000000-0000-0000-0000-000000000000"), 
                    Data = "{\"Completed\":false}"
                });
            return modelBuilder;
        }

        public static void MySqlAddWebstudioSettings(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbWebstudioSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id, e.UserId })
                    .HasName("PRIMARY");

                entity.ToTable("webstudio_settings");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("ID");

                entity.Property(e => e.TenantId).HasColumnName("TenantID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.UserId)
                    .HasColumnName("UserID")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Data)
                    .IsRequired()
                    .HasColumnType("mediumtext")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }
        public static void PgSqlAddWebstudioSettings(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbWebstudioSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id, e.UserId })
                    .HasName("webstudio_settings_pkey");

                entity.ToTable("webstudio_settings", "onlyoffice");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("ID");

                entity.Property(e => e.TenantId).HasColumnName("TenantID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasMaxLength(64);

                entity.Property(e => e.UserId)
                    .HasColumnName("UserID")
                    .HasMaxLength(64);

                entity.Property(e => e.Data).IsRequired();
            });
        }
    }
}
