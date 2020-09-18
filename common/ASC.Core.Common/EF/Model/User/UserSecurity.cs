﻿using ASC.Core.Common.EF.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.Core.Common.EF
{
    [Table("core_usersecurity")]
    public class UserSecurity : BaseEntity
    {
        public int Tenant { get; set; }

        [Key]
        public Guid UserId { get; set; }

        public string PwdHash { get; set; }

        public string PwdHashSha512 { get; set; }

        public override object[] GetKeys()
        {
            return new object[] { UserId };
        }
    }
    public static class UserSecurityExtension
    {
        public static ModelBuilderWrapper AddUserSecurity(this ModelBuilderWrapper modelBuilder)
        {
            modelBuilder
                .Add(MySqlAddUserSecurity, Provider.MySql)
                .Add(PgSqlAddUserSecurity, Provider.Postrge);
            return modelBuilder;
        }
        public static void UserSecurityData(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSecurity>().HasData(
                new UserSecurity { Tenant = 1, UserId = Guid.Parse("66faa6e4 - f133 - 11ea - b126 - 00ffeec8b4ef"), PwdHash = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", PwdHashSha512 = "l/DFJ5yg4oh1F6Qp7uDhBw==" }
                );
        }
        public static void MySqlAddUserSecurity(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSecurity>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("PRIMARY");

                entity.ToTable("core_usersecurity");

                entity.HasIndex(e => e.PwdHash)
                    .HasName("pwdhash");

                entity.HasIndex(e => e.Tenant)
                    .HasName("tenant");

                entity.Property(e => e.UserId)
                    .HasColumnName("userid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.PwdHash)
                    .HasColumnName("pwdhash")
                    .HasColumnType("varchar(512)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.PwdHashSha512)
                    .HasColumnName("pwdhashsha512")
                    .HasColumnType("varchar(512)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Tenant).HasColumnName("tenant");
            });
        }
        public static void PgSqlAddUserSecurity(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSecurity>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("core_usersecurity_pkey");

                entity.ToTable("core_usersecurity", "onlyoffice");

                entity.HasIndex(e => e.PwdHash)
                    .HasName("pwdhash");

                entity.HasIndex(e => e.Tenant)
                    .HasName("tenant_core_usersecurity");

                entity.Property(e => e.UserId)
                    .HasColumnName("userid")
                    .HasMaxLength(38);

                entity.Property(e => e.PwdHash)
                    .HasColumnName("pwdhash")
                    .HasMaxLength(512)
                    .HasDefaultValueSql("NULL::character varying");

                entity.Property(e => e.PwdHashSha512)
                    .HasColumnName("pwdhashsha512")
                    .HasMaxLength(512)
                    .HasDefaultValueSql("NULL::character varying");

                entity.Property(e => e.Tenant).HasColumnName("tenant");
            });
        }
    }
}
