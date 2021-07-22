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


using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class TagDao : BaseMailDao, ITagDao
    {
        public TagDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public Tag GetTag(int id)
        {
            if (id < 0)
                return GetCrmTag(id);

            var tag = MailDbContext.MailTag
                .Where(r => r.TenantId == Tenant)
                .Where(r => r.IdUser == UserId)
                .Where(r => r.Id == id)
                .Select(r => new Tag
                {
                    Id = r.Id,
                    Tenant = r.TenantId,
                    User = r.IdUser,
                    TagName = r.Name,
                    Style = r.Style,
                    Addresses = r.Addresses,
                    Count = r.Count,
                    CrmId = r.CrmId

                }).SingleOrDefault();

            return tag;
        }

        public Tag GetCrmTag(int id)
        {
            var crmTagId = id < 0 ? -id : id;

            var crmTag = MailDbContext.CrmTag
                .Where(r => r.IdTenant == Tenant)
                .Where(r => r.EntityType == (int)EntityType.Contact)
                .Where(r => r.Id == crmTagId)
                .Select(r => new Tag
                {
                    Id = -r.Id,
                    TagName = r.Title,
                    Tenant = Tenant,
                    User = UserId,
                    Style = "",
                    Addresses = "",
                    Count = 0,
                    CrmId = 0
                }).SingleOrDefault();

            return crmTag;
        }

        public Tag GetTag(string name)
        {
            var tag = MailDbContext.MailTag
                .Where(r => r.TenantId == Tenant)
                .Where(r => r.IdUser == UserId)
                .Where(r => r.Name == name)
                .Select(r => new Tag
                {
                    Id = r.Id,
                    Tenant = r.TenantId,
                    User = r.IdUser,
                    TagName = r.Name,
                    Style = r.Style,
                    Addresses = r.Addresses,
                    Count = r.Count,
                    CrmId = r.CrmId

                }).SingleOrDefault();

            return tag;
        }

        public List<Tag> GetTags()
        {
            var tags = MailDbContext.MailTag
                .Where(r => r.TenantId == Tenant)
                .Where(r => r.IdUser == UserId)
                .Select(r => new Tag
                {
                    Id = r.Id,
                    TagName = r.Name,
                    Tenant = r.TenantId,
                    User = r.IdUser,
                    Style = r.Style,
                    Addresses = r.Addresses,
                    Count = r.Count,
                    CrmId = r.CrmId
                }).ToList();

            return tags;
        }

        public enum EntityType //TODO: Get from CRM.Core
        {
            Any = -1,
            Contact = 0,
            Opportunity = 1,
            RelationshipEvent = 2,
            Task = 3,
            Company = 4,
            Person = 5,
            File = 6,
            Case = 7,
            Invoice = 8
        }

        public List<Tag> GetCrmTags()
        {
            var crmTags = MailDbContext.CrmTag
                .Where(r => r.IdTenant == Tenant)
                .Where(r => r.EntityType == (int)EntityType.Contact)
                .Select(r => new Tag
                {
                    Id = -r.Id,
                    TagName = r.Title,
                    Tenant = Tenant,
                    User = UserId,
                    Style = "",
                    Addresses = "",
                    Count = 0,
                    CrmId = 0
                }).ToList();

            return crmTags;
        }

        public List<Tag> GetCrmTags(List<int> contactIds)
        {
            var query = MailDbContext.CrmEntityTag
                .Join(MailDbContext.CrmTag,
                    cet => cet.TagId,
                    ct => (int)ct.Id,
                    (cet, ct) => new Tag
                    {
                        Id = ct.Id,
                        TagName = ct.Title,
                        CrmId = cet.EntityId
                    })
                .Where(r => r.CrmId == (int)EntityType.Contact)
                .Where(r => contactIds.Contains(r.CrmId))
                .ToList();

            return query;

            //var query = new SqlQuery(CrmEntityTagTable.TABLE_NAME.Alias(cet))
            //    .InnerJoin(CrmTagTable.TABLE_NAME.Alias(ct),
            //        Exp.EqColumns(CrmEntityTagTable.Columns.TagId.Prefix(cet), CrmTagTable.Columns.Id.Prefix(ct)))
            //    .Distinct()
            //    .Select(CrmTagTable.Columns.Id.Prefix(ct), CrmTagTable.Columns.Title.Prefix(ct))
            //    .Where(CrmTagTable.Columns.Tenant.Prefix(ct), Tenant)
            //    .Where(CrmEntityTagTable.Columns.EntityType.Prefix(cet), (int) EntityType.Contact)
            //    .Where(Exp.In(CrmEntityTagTable.Columns.EntityId.Prefix(cet), contactIds));

            //return Db.ExecuteList(query)
            //    .ConvertAll(ToCrmTag);
        }
        public int SaveTag(Tag tag)
        {
            var dbTag = new MailTag()
            {
                Id = tag.Id,
                TenantId = Tenant,
                IdUser = UserId,
                Name = tag.TagName,
                Style = tag.Style,
                Addresses = tag.Addresses,
                Count = tag.Count,
                CrmId = tag.CrmId
            };

            var entry = MailDbContext.AddOrUpdate(t => t.MailTag, dbTag);

            MailDbContext.SaveChanges();

            return entry.Id;
        }

        public int DeleteTag(int id)
        {
            var range = MailDbContext.MailTag
                .Where(r => r.TenantId == Tenant && r.IdUser == UserId && r.Id == id);

            MailDbContext.MailTag.RemoveRange(range);

            return MailDbContext.SaveChanges();
        }

        public int DeleteTags(List<int> tagIds)
        {
            var range = MailDbContext.MailTag
                .Where(r => r.TenantId == Tenant && r.IdUser == UserId)
                .Where(r => tagIds.Contains(r.Id)); ;

            MailDbContext.MailTag.RemoveRange(range);

            return MailDbContext.SaveChanges();
        }
    }
}