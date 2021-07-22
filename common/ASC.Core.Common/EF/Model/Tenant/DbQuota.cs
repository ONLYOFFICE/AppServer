using ASC.Core.Common.EF.Model;

using Microsoft.EntityFrameworkCore;

namespace ASC.Core.Common.EF
{
    public class DbQuota : BaseEntity
    {
        public int Tenant { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long MaxFileSize { get; set; }
        public long MaxTotalSize { get; set; }
        public int ActiveUsers { get; set; }
        public string Features { get; set; }
        public decimal Price { get; set; }
        public decimal Price2 { get; set; }
        public string AvangateId { get; set; }
        public bool Visible { get; set; }
        public override object[] GetKeys()
        {
            return new object[] { Tenant };
        }
    }
    public static class DbQuotaExtension
    {
        public static ModelBuilderWrapper AddDbQuota(this ModelBuilderWrapper modelBuilder)
        {
            modelBuilder
                .Add(MySqlAddDbQuota, Provider.MySql)
                .Add(PgSqlAddDbQuota, Provider.Postgre)
                .HasData(
                    new DbQuota 
                    { 
                        Tenant = -1, 
                        Name = "default", 
                        Description = null, 
                        MaxFileSize = 102400, 
                        MaxTotalSize = 10995116277760, 
                        ActiveUsers = 10000, 
                        Features = "domain,audit,controlpanel,healthcheck,ldap,sso,whitelabel,branding,ssbranding,update,support,portals:10000,discencryption,privacyroom,restore", 
                        Price = decimal.Parse("0,00"), 
                        Price2 = decimal.Parse("0,00"), 
                        AvangateId = "0", Visible = false 
                    });

            return modelBuilder;
        }

        public static void MySqlAddDbQuota(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbQuota>(entity =>
            {
                entity.HasKey(e => e.Tenant)
                    .HasName("PRIMARY");

                entity.ToTable("tenants_quota");

                entity.Property(e => e.Tenant).HasColumnName("tenant");

                entity.Property(e => e.ActiveUsers).HasColumnName("active_users");

                entity.Property(e => e.AvangateId)
                    .HasColumnName("avangate_id")
                    .HasColumnType("varchar(128)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("varchar(128)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Features)
                    .HasColumnName("features")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.MaxFileSize).HasColumnName("max_file_size");

                entity.Property(e => e.MaxTotalSize).HasColumnName("max_total_size");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(128)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Price)
                    .HasColumnName("price")
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.Price2)
                    .HasColumnName("price2")
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.Visible).HasColumnName("visible");
            });
        }
        public static void PgSqlAddDbQuota(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbQuota>(entity =>
            {
                entity.HasKey(e => e.Tenant)
                    .HasName("tenants_quota_pkey");

                entity.ToTable("tenants_quota", "onlyoffice");

                entity.Property(e => e.Tenant)
                    .HasColumnName("tenant")
                    .ValueGeneratedNever();

                entity.Property(e => e.ActiveUsers).HasColumnName("active_users");

                entity.Property(e => e.AvangateId)
                    .HasColumnName("avangate_id")
                    .HasMaxLength(128)
                    .HasDefaultValueSql("NULL");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("character varying");

                entity.Property(e => e.Features).HasColumnName("features");

                entity.Property(e => e.MaxFileSize)
                    .HasColumnName("max_file_size")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.MaxTotalSize)
                    .HasColumnName("max_total_size")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("character varying");

                entity.Property(e => e.Price)
                    .HasColumnName("price")
                    .HasColumnType("numeric(10,2)")
                    .HasDefaultValueSql("0.00");

                entity.Property(e => e.Price2)
                    .HasColumnName("price2")
                    .HasColumnType("numeric(10,2)")
                    .HasDefaultValueSql("0.00");

                entity.Property(e => e.Visible).HasColumnName("visible");
            });
        }
    }
}
