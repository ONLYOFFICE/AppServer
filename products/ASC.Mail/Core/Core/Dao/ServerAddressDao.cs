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


using System;
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
    public class ServerAddressDao : BaseMailDao, IServerAddressDao
    {
        public ServerAddressDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public ServerAddress Get(int id)
        {
            var address = MailDbContext.MailServerAddress
                .Where(a => a.Tenant == Tenant && a.Id == id)
                .Select(ToServerAddress)
                .SingleOrDefault();

            return address;
        }

        public List<ServerAddress> GetList(List<int> ids = null)
        {
            var query = MailDbContext.MailServerAddress
                .Where(a => a.Tenant == Tenant);

            if (ids != null && ids.Any())
            {
                query.Where(a => ids.Contains(a.Id));
            }

            var list = query
                .Select(ToServerAddress)
                .ToList();

            return list;
        }

        public List<ServerAddress> GetList(int mailboxId)
        {
            var list = MailDbContext.MailServerAddress
                .Where(a => a.Tenant == Tenant && a.IdMailbox == mailboxId)
                .Select(ToServerAddress)
                .ToList();

            return list;
        }

        public List<ServerAddress> GetGroupAddresses(int groupId)
        {
            var list = MailDbContext.MailServerAddress
                .Where(a => a.Tenant == Tenant)
               .Join(MailDbContext.MailServerMailGroupXMailServerAddress, a => a.Id, g => g.IdAddress, 
                (a, g) => new { 
                    Address = a,
                    Xgroup = g
                }
               )
               .Where(o => o.Xgroup.IdMailGroup == groupId)
               .Select(o => ToServerAddress(o.Address))
               .ToList();

            return list;
        }

        public List<ServerAddress> GetDomainAddresses(int domainId)
        {
            var list = MailDbContext.MailServerAddress
                .Where(a => a.Tenant == Tenant && a.IdDomain == domainId)
                .Select(ToServerAddress)
                .ToList();

            return list;
        }

        public void AddAddressesToMailGroup(int groupId, List<int> addressIds)
        {
            var list = addressIds.Select(id =>
                new MailServerMailGroupXMailServerAddress
                {
                    IdAddress = id,
                    IdMailGroup = groupId
                });

            MailDbContext.MailServerMailGroupXMailServerAddress.AddRange(list);

            MailDbContext.SaveChanges();
        }

        public void DeleteAddressFromMailGroup(int groupId, int addressId)
        {
            var deleteItem = new MailServerMailGroupXMailServerAddress
            {
                IdAddress = addressId,
                IdMailGroup = groupId
            };

            MailDbContext.MailServerMailGroupXMailServerAddress.Remove(deleteItem);

            MailDbContext.SaveChanges();
        }

        public void DeleteAddressesFromMailGroup(int groupId)
        {
            var deleteQuery = MailDbContext.MailServerMailGroupXMailServerAddress
                .Where(x => x.IdMailGroup == groupId);

            MailDbContext.MailServerMailGroupXMailServerAddress.RemoveRange(deleteQuery);

            MailDbContext.SaveChanges();
        }

        public void DeleteAddressesFromAnyMailGroup(List<int> addressIds)
        {
            var deleteQuery = MailDbContext.MailServerMailGroupXMailServerAddress
                .Where(x => addressIds.Contains(x.IdAddress));

            MailDbContext.MailServerMailGroupXMailServerAddress.RemoveRange(deleteQuery);

            MailDbContext.SaveChanges();
        }

        public int Save(ServerAddress address)
        {
            var mailServerAddress = new MailServerAddress { 
                Id = address.Id,
                Name = address.AddressName,
                Tenant = address.Tenant,
                IdDomain = address.DomainId,
                IdMailbox = address.MailboxId,
                IsMailGroup = address.IsMailGroup,
                IsAlias = address.IsAlias
            };

            if (address.Id <= 0)
            {
                mailServerAddress.DateCreated = DateTime.UtcNow;
            }

            var entry = MailDbContext.AddOrUpdate(t => t.MailServerAddress, mailServerAddress);

            MailDbContext.SaveChanges();

            return entry.Id;
        }

        public int Delete(int id)
        {
            var deleteItem = new MailServerAddress
            {
                Id = id,
                Tenant = Tenant
            };

            MailDbContext.MailServerAddress.Remove(deleteItem);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int Delete(List<int> ids)
        {
            var queryDelete = MailDbContext.MailServerAddress
                .Where(a => a.Tenant == Tenant && ids.Contains(a.Id));

            MailDbContext.MailServerAddress.RemoveRange(queryDelete);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public bool IsAddressAlreadyRegistered(string addressName, string domainName)
        {
            if (string.IsNullOrEmpty(addressName))
                throw new ArgumentNullException("addressName");

            if (string.IsNullOrEmpty(domainName))
                throw new ArgumentNullException("domainName");

            var tenants = new List<int> { Tenant, DefineConstants.SHARED_TENANT_ID };

            var exists = MailDbContext.MailServerAddress
                .Join(MailDbContext.MailServerDomain, a => a.IdDomain, d => d.Id,
                (a, d) => new
                {
                    Address = a,
                    Domain = d
                })
                .Where(ad => ad.Address.Name == addressName && tenants.Contains(ad.Address.Tenant))
                .Where(ad => ad.Domain.Name == domainName)
                .Select(ad => ad.Address.Id)
                .Any();

            return exists;
        }

        protected ServerAddress ToServerAddress(MailServerAddress r)
        {
            var s = new ServerAddress
            {
                Id = r.Id,
                AddressName = r.Name,
                Tenant = r.Tenant,
                DomainId = r.IdDomain,
                MailboxId = r.IdMailbox,
                IsMailGroup = r.IsMailGroup,
                IsAlias = r.IsAlias,
                DateCreated = r.DateCreated
            };

            return s;
        }
    }
}