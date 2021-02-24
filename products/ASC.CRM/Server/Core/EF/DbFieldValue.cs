﻿using ASC.Core.Common.EF;
using ASC.Core.Common.EF.Model;
using ASC.CRM.Core.Enums;
using ASC.ElasticSearch;

using Microsoft.EntityFrameworkCore;

using Nest;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace ASC.CRM.Core.EF
{
    [ElasticsearchType(RelationName = "crm_field_value")]
    [Table("crm_field_value")]
    public partial class DbFieldValue : IDbCrm, ISearchItem
    {
        public int Id { get; set; }

        [Text(Analyzer = "whitespacecustom")]
        public string Value { get; set; }
        
        [Column("entity_id", TypeName = "int(11)")]
        public int EntityId { get; set; }
        
        [Column("tenant_id", TypeName = "int(11)")]
        public int TenantId { get; set; }

        [Column("field_id", TypeName = "int(11)")]
        public int FieldId { get; set; }
        
        [Column("entity_type", TypeName = "int(10)")]
        public EntityType EntityType { get; set; }
        
        [Column("last_modifed_on", TypeName = "datetime")]
        public DateTime? LastModifedOn { get; set; }
        
        [Column("last_modifed_by", TypeName = "char(38)")]
        public Guid LastModifedBy { get; set; }

        [NotMapped]
        [Ignore]
        public string IndexName
        {
            get
            {
                return "crm_field_value";
            }
        }

        [Ignore]
        public Expression<Func<ISearchItem, object[]>> SearchContentFields
        {
            get
            {
                return (a) => new[] { Value };
            }
        }
    }

    public static class DbFieldValueExtension
    {
        public static ModelBuilderWrapper AddDbFieldValue(this ModelBuilderWrapper modelBuilder)
        {
            modelBuilder
                .Add(MySqlAddDbDeal, Provider.MySql)
                .Add(PgSqlAddDbDeal, Provider.Postgre);

            return modelBuilder;
        }

        private static void MySqlAddDbDeal(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbFieldValue>(entity =>
            {
                entity.HasIndex(e => e.FieldId)
                    .HasName("field_id");

                entity.HasIndex(e => e.LastModifedOn)
                    .HasName("last_modifed_on");

                entity.HasIndex(e => new { e.TenantId, e.EntityId, e.EntityType, e.FieldId })
                    .HasName("tenant_id");

                entity.Property(e => e.LastModifedBy)
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Value)
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");
            });
        }

        private static void PgSqlAddDbDeal(this ModelBuilder modelBuilder)
        {
            throw new NotImplementedException();
        }

    }
}