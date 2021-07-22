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
    [Table("mailbox")]
    public partial class Mailbox : BaseEntity
    {
        [Key]
        [Column("username", TypeName = "varchar(255)")]
        public string Username { get; set; }
        [Required]
        [Column("password", TypeName = "varchar(255)")]
        public string Password { get; set; }
        [Required]
        [Column("name", TypeName = "varchar(255)")]
        public string Name { get; set; }
        [Required]
        [Column("language", TypeName = "varchar(5)")]
        public string Language { get; set; }
        [Required]
        [Column("storagebasedirectory", TypeName = "varchar(255)")]
        public string Storagebasedirectory { get; set; }
        [Required]
        [Column("storagenode", TypeName = "varchar(255)")]
        public string Storagenode { get; set; }
        [Required]
        [Column("maildir", TypeName = "varchar(255)")]
        public string Maildir { get; set; }
        [Column("quota", TypeName = "bigint(20)")]
        public long Quota { get; set; }
        [Required]
        [Column("domain", TypeName = "varchar(255)")]
        public string Domain { get; set; }
        [Required]
        [Column("transport", TypeName = "varchar(255)")]
        public string Transport { get; set; }
        [Required]
        [Column("department", TypeName = "varchar(255)")]
        public string Department { get; set; }
        [Required]
        [Column("rank", TypeName = "varchar(255)")]
        public string Rank { get; set; }
        [Column("employeeid", TypeName = "varchar(255)")]
        public string Employeeid { get; set; }
        [Column("isadmin")]
        public bool Isadmin { get; set; }
        [Column("isglobaladmin")]
        public bool Isglobaladmin { get; set; }
        [Required]
        [Column("enablesmtp")]
        public bool? Enablesmtp { get; set; }
        [Required]
        [Column("enablesmtpsecured")]
        public bool? Enablesmtpsecured { get; set; }
        [Required]
        [Column("enablepop3")]
        public bool? Enablepop3 { get; set; }
        [Required]
        [Column("enablepop3secured")]
        public bool? Enablepop3secured { get; set; }
        [Required]
        [Column("enableimap")]
        public bool? Enableimap { get; set; }
        [Required]
        [Column("enableimapsecured")]
        public bool? Enableimapsecured { get; set; }
        [Required]
        [Column("enabledeliver")]
        public bool? Enabledeliver { get; set; }
        [Required]
        [Column("enablelda")]
        public bool? Enablelda { get; set; }
        [Required]
        [Column("enablemanagesieve")]
        public bool? Enablemanagesieve { get; set; }
        [Required]
        [Column("enablemanagesievesecured")]
        public bool? Enablemanagesievesecured { get; set; }
        [Required]
        [Column("enablesieve")]
        public bool? Enablesieve { get; set; }
        [Required]
        [Column("enablesievesecured")]
        public bool? Enablesievesecured { get; set; }
        [Required]
        [Column("enableinternal")]
        public bool? Enableinternal { get; set; }
        [Required]
        [Column("enabledoveadm")]
        public bool? Enabledoveadm { get; set; }
        [Required]
        [Column("enablelib-storage")]
        public bool? EnablelibStorage { get; set; }
        [Required]
        [Column("enablelmtp")]
        public bool? Enablelmtp { get; set; }
        [Column("allow_nets", TypeName = "text")]
        public string AllowNets { get; set; }
        [Column("lastlogindate", TypeName = "datetime")]
        public DateTime Lastlogindate { get; set; }
        [Column("lastloginipv4", TypeName = "int(4) unsigned")]
        public uint Lastloginipv4 { get; set; }
        [Required]
        [Column("lastloginprotocol", TypeName = "char(255)")]
        public string Lastloginprotocol { get; set; }
        [Column("disclaimer", TypeName = "text")]
        public string Disclaimer { get; set; }
        [Column("allowedsenders", TypeName = "text")]
        public string Allowedsenders { get; set; }
        [Column("rejectedsenders", TypeName = "text")]
        public string Rejectedsenders { get; set; }
        [Column("allowedrecipients", TypeName = "text")]
        public string Allowedrecipients { get; set; }
        [Column("rejectedrecipients", TypeName = "text")]
        public string Rejectedrecipients { get; set; }
        [Column("settings", TypeName = "text")]
        public string Settings { get; set; }
        [Column("passwordlastchange", TypeName = "datetime")]
        public DateTime Passwordlastchange { get; set; }
        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Column("modified", TypeName = "datetime")]
        public DateTime Modified { get; set; }
        [Column("expired", TypeName = "datetime")]
        public DateTime Expired { get; set; }
        [Required]
        [Column("active")]
        public bool? Active { get; set; }
        [Required]
        [Column("local_part", TypeName = "varchar(255)")]
        public string LocalPart { get; set; }

        public override object[] GetKeys()
        {
            return new object[] { Username };
        }
    }
}