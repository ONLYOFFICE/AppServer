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
using ASC.Mail.Core.Entities;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class ServerGroupDao : BaseMailDao, IServerGroupDao
    {
        public ServerGroupDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public ServerGroup Get(int id)
        {
            var group = MailDbContext.MailServerMailGroup
                .Where(g => g.IdTenant == Tenant)
                .Where(g => g.Id == id)
                .Select(ToServerGroup)
                .SingleOrDefault();

            return group;
        }

        public List<ServerGroup> GetList()
        {
            var groups = MailDbContext.MailServerMailGroup
                .Where(g => g.IdTenant == Tenant)
                .Select(ToServerGroup)
                .ToList();

            return groups;
        }

        public List<ServerGroup> GetList(int domainId)
        {
            var groups = MailDbContext.MailServerMailGroup
                .Join(MailDbContext.MailServerAddress, g => g.IdAddress, a => a.Id, 
                    (g, a) => new { 
                        Group = g,
                        Address = a
                    })
                .Where(o => o.Group.IdTenant == Tenant)
                .Where(o => o.Address.IdDomain == domainId && o.Address.IsMailGroup == true)
                .Select(o => ToServerGroup(o.Group))
                .ToList();

            return groups;
        }

        public int Save(ServerGroup @group)
        {
            var mailServerGroup = new MailServerMailGroup { 
                Id = group.Id,
                IdTenant = group.Tenant,
                Address = group.Address,
                IdAddress = group.AddressId,
                DateCreated = group.DateCreated
            };

            var entry = MailDbContext.AddOrUpdate(t => t.MailServerMailGroup, mailServerGroup);

            MailDbContext.SaveChanges();

            return entry.Id;
        }

        public int Delete(int id)
        {
            var mailServerGroup = new MailServerMailGroup
            {
                Id = id
            };

            MailDbContext.MailServerMailGroup.Remove(mailServerGroup);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected ServerGroup ToServerGroup(MailServerMailGroup r)
        {
            var group = new ServerGroup
            {
                Id = r.Id,
                Tenant = r.IdTenant,
                AddressId = r.IdAddress,
                Address = r.Address,
                DateCreated = r.DateCreated
            };

            return group;
        }
    }
}