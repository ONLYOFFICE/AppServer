/*
 *
 * (c) Copyright Ascensio System Limited 2010-2020
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


using ASC.Core.Common.EF;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.Mail.Server.Core.Entities
{

    [Table("domain")]
    public partial class Domain : BaseEntity
    {
        [Key]
        [Column("domain", TypeName = "varchar(255)")]
        public string DomainName { get; set; }
        [Column("description", TypeName = "text")]
        public string Description { get; set; }
        [Column("disclaimer", TypeName = "text")]
        public string Disclaimer { get; set; }
        [Column("aliases", TypeName = "int(10)")]
        public int Aliases { get; set; }
        [Column("mailboxes", TypeName = "int(10)")]
        public int Mailboxes { get; set; }
        [Column("maxquota", TypeName = "bigint(20)")]
        public long Maxquota { get; set; }
        [Column("quota", TypeName = "bigint(20)")]
        public long Quota { get; set; }
        [Required]
        [Column("transport", TypeName = "varchar(255)")]
        public string Transport { get; set; }
        [Column("backupmx")]
        public bool Backupmx { get; set; }
        [Column("settings", TypeName = "text")]
        public string Settings { get; set; }
        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Column("modified", TypeName = "datetime")]
        public DateTime Modified { get; set; }
        [Column("expired", TypeName = "datetime")]
        public DateTime Expired { get; set; }
        [Required]
        [Column("active")]
        public bool? Active { get; set; }

        public override object[] GetKeys()
        {
            return new object[] { DomainName };
        }
    }
}
