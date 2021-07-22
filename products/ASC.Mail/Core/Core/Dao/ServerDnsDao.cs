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
    public class ServerDnsDao : BaseMailDao, IServerDnsDao
    {
        public ServerDnsDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public ServerDns Get(int domainId)
        {
            var tenants = new List<int> { Tenant, DefineConstants.SHARED_TENANT_ID };

            var dns = MailDbContext.MailServerDns
                .Where(d => tenants.Contains(d.Tenant))
                .Where(d => d.IdDomain == domainId)
                .Select(ToServerDns)
                .SingleOrDefault();

            return dns;
        }

        public ServerDns GetById(int id)
        {
            var dns = MailDbContext.MailServerDns
               .Where(d => d.Tenant == Tenant)
               .Where(d => d.Id == id)
               .Select(ToServerDns)
               .SingleOrDefault();

            return dns;
        }

        public ServerDns GetFree()
        {
            var dns = MailDbContext.MailServerDns
               .Where(d => d.Tenant == Tenant)
               .Where(d => d.IdUser == UserId)
               .Where(d => d.IdDomain == DefineConstants.UNUSED_DNS_SETTING_DOMAIN_ID)
               .Select(ToServerDns)
               .SingleOrDefault();

            return dns;
        }

        public int Save(ServerDns dns)
        {
            var mailDns = new MailServerDns { 
                Id = (uint)dns.Id,
                Tenant = dns.Tenant,
                IdUser = dns.User,
                IdDomain = dns.DomainId,
                DomainCheck = dns.DomainCheck,
                DkimSelector = dns.DkimSelector,
                DkimPrivateKey = dns.DkimPrivateKey,
                DkimPublicKey = dns.DkimPublicKey,
                DkimTtl = dns.DkimTtl,
                DkimVerified = dns.DkimVerified,
                DkimDateChecked = dns.DkimDateChecked,
                Spf = dns.Spf,
                SpfTtl = dns.SpfTtl,
                SpfVerified = dns.SpfVerified,
                SpfDateChecked = dns.SpfDateChecked,
                Mx = dns.Mx,
                MxTtl = dns.MxTtl,
                MxVerified = dns.MxVerified,
                MxDateChecked = dns.MxDateChecked,
                TimeModified = dns.TimeModified
            };

            var entry = MailDbContext.AddOrUpdate(t => t.MailServerDns, mailDns);

            MailDbContext.SaveChanges();

            return (int)entry.Id;
        }

        public int Delete(int id)
        {
            var mailDns = new MailServerDns
            {
                Id = (uint)id,
                Tenant = Tenant,
                IdUser = UserId
            };

            MailDbContext.MailServerDns.Remove(mailDns);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected ServerDns ToServerDns(MailServerDns r)
        {
            var s = new ServerDns
            {
                Id = (int)r.Id,
                Tenant = r.Tenant,
                User = r.IdUser,
                DomainId = r.IdDomain,
                DomainCheck = r.DomainCheck,
                DkimSelector = r.DkimSelector,
                DkimPrivateKey = r.DkimPrivateKey,
                DkimPublicKey = r.DkimPublicKey,
                DkimTtl = r.DkimTtl,
                DkimVerified = r.DkimVerified,
                DkimDateChecked = r.DkimDateChecked,
                Spf = r.Spf,
                SpfTtl = r.SpfTtl,
                SpfVerified = r.SpfVerified,
                SpfDateChecked = r.SpfDateChecked,
                Mx = r.Mx,
                MxTtl = r.MxTtl,
                MxVerified = r.MxVerified,
                MxDateChecked = r.MxDateChecked,
                TimeModified = r.TimeModified
            };

            return s;
        }
    }
}