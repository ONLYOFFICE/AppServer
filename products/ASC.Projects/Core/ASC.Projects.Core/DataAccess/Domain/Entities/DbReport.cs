﻿#region License agreement statement

/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/

#endregion License agreement statement

using System;
using ASC.Projects.Core.DataAccess.Domain.Entities.Interfaces;
using ASC.Projects.Core.DataAccess.Domain.Enums;
using ASC.Projects.Core.DataAccess.EF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ASC.Projects.Core.DataAccess.Domain.Entities
{
    /// <summary>
    /// Represents a report.
    /// </summary>
    public class DbReport : BaseDbEntity<int>, ITenantEntity<int>
    {
        /// <summary>
        /// Name of report.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of report.
        /// </summary>
        public ReportType ReportType { get; set; }

        /// <summary>
        /// Id of report file.
        /// </summary>
        public int FileId { get; set; }

        /// <summary>
        /// Date when this report was created.
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Id of user who created this report.
        /// </summary>
        public Guid? CreatorId { get; set; }

        /// <summary>
        /// Id of tenant.
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Describes a set of entity-to-table mapping rules.
        /// </summary>
        public class MySqlMappingConfig : IEntityTypeConfiguration<DbReport>
        {
            public void Configure(EntityTypeBuilder<DbReport> builder)
            {
                builder.ToTable("reports");

                builder.HasKey(r => r.Id)
                    .HasName(MySqlMappingConfigurationConstants.PrimaryKeyDefaultName);

                builder.Property(r => r.Name)
                    .HasColumnType("varchar(1024)")
                    .HasMaxLength(1024);

                builder.Property(r => r.ReportType)
                    .HasColumnName("type")
                    .HasColumnType("int");

                builder.Property(r => r.FileId)
                    .HasColumnName("fileId")
                    .HasDefaultValue();

                builder.Property(r => r.CreationDate)
                    .HasColumnName("create_on")
                    .HasDefaultValue()
                    .ValueGeneratedOnAdd();

                builder.Property(r => r.CreatorId)
                    .HasColumnName("create_by")
                    .HasColumnType(MySqlMappingConfigurationConstants.GuidDbType);
            }
        }
    }
}
