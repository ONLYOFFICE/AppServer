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


using System.Collections.Generic;
using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Models;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class CrmLinkDao : BaseMailDao, ICrmLinkDao
    {
        public CrmLinkDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public List<CrmContactData> GetLinkedCrmContactEntities(string chainId, int mailboxId)
        {
            var list = MailDbContext.MailChainXCrmEntity
                .Where(x => x.IdMailbox == mailboxId && x.IdTenant == Tenant && x.IdChain == chainId)
                .Select(x => new CrmContactData
                {
                    Id = x.EntityId,
                    Type = (CrmContactData.EntityTypes)x.EntityType
                })
                .ToList();

            return list;
        }

        public int SaveCrmLinks(string chainId, int mailboxId, IEnumerable<CrmContactData> crmContactEntities)
        {
            var list = crmContactEntities.Select(x => 
                new MailChainXCrmEntity { 
                    IdChain = chainId,
                    IdMailbox = mailboxId,
                    IdTenant = Tenant,
                    EntityId = x.Id,
                    EntityType = (int)x.Type
                })
                .ToList();

            MailDbContext.MailChainXCrmEntity.AddRange(list);

            var result = MailDbContext.SaveChanges();
            
            return result;
        }

        public int UpdateCrmLinkedMailboxId(string chainId, int oldMailboxId, int newMailboxId)
        {
            var chainEntities = MailDbContext.MailChainXCrmEntity
                .Where(x => x.IdChain == chainId && x.IdMailbox == oldMailboxId && x.IdTenant == Tenant)
                .ToList();

            foreach (var chainEntity in chainEntities)
            {
                chainEntity.IdMailbox = newMailboxId;
            }

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int UpdateCrmLinkedChainId(string chainId, int mailboxId, string newChainId)
        {
            var chainEntities = MailDbContext.MailChainXCrmEntity
               .Where(x => x.IdChain == chainId && x.IdMailbox == mailboxId && x.IdTenant == Tenant)
               .ToList();

            foreach (var chainEntity in chainEntities)
            {
                chainEntity.IdChain = newChainId;
            }

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public void RemoveCrmLinks(string chainId, int mailboxId, IEnumerable<CrmContactData> crmContactEntities)
        {
            var deleteItems = crmContactEntities.Select(x =>
                new MailChainXCrmEntity
                {
                    IdChain = chainId,
                    IdMailbox = mailboxId,
                    IdTenant = Tenant,
                    EntityId = x.Id,
                    EntityType = (int)x.Type
                })
                .ToList();

            MailDbContext.MailChainXCrmEntity.RemoveRange(deleteItems);

            MailDbContext.SaveChanges();
        }

        public int RemoveCrmLinks(int mailboxId)
        {
            var deleteQuery = MailDbContext.MailChainXCrmEntity
               .Where(x => x.IdMailbox == mailboxId && x.IdTenant == Tenant);

            MailDbContext.MailChainXCrmEntity.RemoveRange(deleteQuery);

            var result = MailDbContext.SaveChanges();

            return result;
        }
    }
}