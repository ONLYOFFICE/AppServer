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


using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security;

using ASC.Common;
using ASC.Common.Utils;
using ASC.Core;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Core.Entities;
using ASC.Mail.Extensions;
using ASC.Mail.Models;
using ASC.Mail.Server.Utils;
using ASC.Mail.Utils;
using ASC.Web.Core;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class ServerDomainEngine : BaseEngine
    {
        public int Tenant => TenantManager.GetCurrentTenant().TenantId;

        public string User => SecurityContext.CurrentAccount.ID.ToString();

        private bool IsAdmin => WebItemSecurity.IsProductAdministrator(WebItemManager.MailProductID, SecurityContext.CurrentAccount.ID);

        private SecurityContext SecurityContext { get; }
        private TenantManager TenantManager { get; }
        private CoreBaseSettings CoreBaseSettings { get; }
        private WebItemSecurity WebItemSecurity { get; }
        private IMailDaoFactory MailDaoFactory { get; }

        public ServerDomainEngine(
            SecurityContext securityContext,
            TenantManager tenantManager,
            IMailDaoFactory mailDaoFactory,
            CoreBaseSettings coreBaseSettings,
            WebItemSecurity webItemSecurity,
            MailSettings mailSettings) : base(mailSettings)
        {
            SecurityContext = securityContext;
            TenantManager = tenantManager;
            CoreBaseSettings = coreBaseSettings;
            WebItemSecurity = webItemSecurity;
            MailDaoFactory = mailDaoFactory;
        }

        public ServerDomainData GetDomain(int id)
        {
            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(id);

            if (serverDomain == null)
                throw new Exception("Domain not found");

            var dnsData = UpdateDnsStatus(serverDomain);

            return ToServerDomainData(serverDomain, dnsData);
        }

        public List<ServerDomainData> GetDomains()
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            var listDomains = MailDaoFactory.GetServerDomainDao().GetDomains();

            if (!listDomains.Any())
                return new List<ServerDomainData>();

            var list = new List<ServerDomainData>();

            foreach (var domain in listDomains)
            {
                var dnsData = UpdateDnsStatus(domain);

                var serverDomain = ToServerDomainData(domain, dnsData);

                list.Add(serverDomain);
            }

            return list;
        }

        public List<ServerDomain> GetAllDomains()
        {
            var domains = MailDaoFactory.GetServerDomainDao().GetAllDomains();

            return domains;
        }

        public ServerDomainData GetCommonDomain()
        {
            var domainCommon = MailDaoFactory.GetServerDomainDao().GetDomains()
                .SingleOrDefault(x => x.Tenant == DefineConstants.SHARED_TENANT_ID);

            if (domainCommon == null)
                return null;

            var serverDomain = ToServerDomainData(domainCommon, null);

            return serverDomain;
        }

        public ServerDomainDnsData GetDnsData(int domainId)
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (domainId < 0)
                throw new ArgumentException(@"Invalid domain id.", "domainId");

            var domain = MailDaoFactory.GetServerDomainDao().GetDomain(domainId);

            if (domain == null)
                return null;

            var dnsData = UpdateDnsStatus(domain, true);

            return dnsData;
        }

        public bool IsDomainExists(string name)
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(@"Invalid domain name.", "name");

            if (name.Length > 255)
                throw new ArgumentException(@"Domain name exceed limitation of 255 characters.", "name");

            if (!Parser.IsDomainValid(name))
                throw new ArgumentException(@"Incorrect domain name.", "name");

            var domainName = name.ToLowerInvariant();

            return MailDaoFactory.GetServerDomainDao().IsDomainExists(domainName);
        }

        public ServerDomainDnsData GetOrCreateUnusedDnsData(Entities.Server server)
        {
            var dnsSettings = MailDaoFactory.GetServerDnsDao().GetFree();

            if (dnsSettings == null)
            {
                string privateKey, publicKey;
                CryptoUtil.GenerateDkimKeys(out privateKey, out publicKey);

                var domainCheckValue = PasswordGenerator.GenerateNewPassword(16);
                var domainCheck = MailSettings.ServerDnsDomainCheckPrefix + ": " + domainCheckValue;

                var serverDns = new ServerDns
                {
                    Id = 0,
                    Tenant = Tenant,
                    User = User,
                    DomainId = DefineConstants.UNUSED_DNS_SETTING_DOMAIN_ID,
                    DomainCheck = domainCheck,
                    DkimSelector = MailSettings.ServerDnsDkimSelector,
                    DkimPrivateKey = privateKey,
                    DkimPublicKey = publicKey,
                    DkimTtl = MailSettings.ServerDnsDefaultTtl,
                    DkimVerified = false,
                    DkimDateChecked = null,
                    Spf = MailSettings.ServerDnsSpfRecordValue,
                    SpfTtl = MailSettings.ServerDnsDefaultTtl,
                    SpfVerified = false,
                    SpfDateChecked = null,
                    Mx = server.MxRecord,
                    MxTtl = MailSettings.ServerDnsDefaultTtl,
                    MxVerified = false,
                    MxDateChecked = null,
                    TimeModified = DateTime.UtcNow
                };

                serverDns.Id = MailDaoFactory.GetServerDnsDao().Save(serverDns);

                dnsSettings = serverDns;
            }

            var dnsData = new ServerDomainDnsData
            {
                Id = dnsSettings.Id,
                MxRecord = new ServerDomainMxRecordData
                {
                    Host = dnsSettings.Mx,
                    IsVerified = false,
                    Priority = (int)MailSettings.ServerDnsMxRecordPriority
                },
                DkimRecord = new ServerDomainDkimRecordData
                {
                    Selector = dnsSettings.DkimSelector,
                    IsVerified = false,
                    PublicKey = dnsSettings.DkimPublicKey
                },
                DomainCheckRecord = new ServerDomainDnsRecordData
                {
                    Name = DefineConstants.DNS_DEFAULT_ORIGIN,
                    IsVerified = false,
                    Value = dnsSettings.DomainCheck
                },
                SpfRecord = new ServerDomainDnsRecordData
                {
                    Name = DefineConstants.DNS_DEFAULT_ORIGIN,
                    IsVerified = false,
                    Value = dnsSettings.Spf
                }
            };

            return dnsData;
        }

        public ServerDomainData AddDomain(string domain, int dnsId)
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (string.IsNullOrEmpty(domain))
                throw new ArgumentException(@"Invalid domain name.", "domain");

            if (domain.Length > 255)
                throw new ArgumentException(@"Domain name exceed limitation of 255 characters.", "domain");

            if (!Parser.IsDomainValid(domain))
                throw new ArgumentException(@"Incorrect domain name.", "domain");

            var domainName = domain.ToLowerInvariant();

            var dnsLookup = new DnsLookup();

            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            var freeDns = GetOrCreateUnusedDnsData(server);

            if (freeDns.Id != dnsId)
                throw new InvalidDataException("This dkim public key is already in use. Please reopen wizard again.");

            if (!CoreBaseSettings.Standalone &&
                !dnsLookup.IsDomainTxtRecordExists(domainName, freeDns.DomainCheckRecord.Value))
            {
                throw new InvalidOperationException("txt record is not correct.");
            }

            var isVerified = freeDns.CheckDnsStatus(domainName);

            using var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted);

            var utcNow = DateTime.UtcNow;

            var mailServerEngine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

            var mailServerDomain = new Server.Core.Entities.Domain
            {
                DomainName = domainName,
                Active = true,
                Description = string.Format("Domain created in UtcTime: {0}, for tenant: {1}", utcNow, Tenant),
                Created = utcNow,
                Modified = utcNow
            };

            mailServerEngine.SaveDomain(mailServerDomain);

            var serverDomain = new ServerDomain
            {
                Id = 0,
                Tenant = Tenant,
                Name = domainName,
                IsVerified = isVerified,
                DateAdded = utcNow,
                DateChecked = utcNow
            };

            serverDomain.Id = MailDaoFactory.GetServerDomainDao()
                .Save(serverDomain);

            var serverDns = MailDaoFactory.GetServerDnsDao()
                .GetById(freeDns.Id);

            var mailServerDkim = new Server.Core.Entities.Dkim
            {
                DomainName = domainName,
                Selector = serverDns.DkimSelector,
                PrivateKey = serverDns.DkimPrivateKey,
                PublicKey = serverDns.DkimPublicKey
            };

            mailServerEngine.SaveDkim(mailServerDkim);

            serverDns.DomainId = serverDomain.Id;
            serverDns.TimeModified = utcNow;

            MailDaoFactory.GetServerDnsDao().Save(serverDns);

            tx.Commit();

            return ToServerDomainData(serverDomain, freeDns);
        }

        public MailOperationStatus RemoveDomain(int id)
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (id < 0)
                throw new ArgumentException(@"Invalid domain id.", "id");

            var domain = GetDomain(id);

            if (domain.IsSharedDomain)
                throw new SecurityException("Can not remove shared domain.");

            //TODO: Fix return OperationEngine.RemoveServerDomain(domain);

            throw new NotImplementedException();
        }

        private ServerDomainDnsData UpdateDnsStatus(ServerDomain domain, bool force = false)
        {
            var serverDns = MailDaoFactory.GetServerDnsDao().Get(domain.Id);

            //TODO fix
            /*if (serverDns.UpdateRecords(OperationEngine, domain.Name, force))
            {
                DaoFactory.ServerDnsDao.Save(serverDns);
            }*/

            var dnsData = ToServerDomainDnsData(serverDns);

            if (dnsData != null && domain.IsVerified != dnsData.IsVerified)
            {
                MailDaoFactory.GetServerDomainDao().SetVerified(domain.Id, dnsData.IsVerified);
            }

            return dnsData;
        }

        protected ServerDomainData ToServerDomainData(ServerDomain domain, ServerDomainDnsData dns)
        {
            var serverDomain = new ServerDomainData
            {
                Id = domain.Id,
                Name = domain.Name,
                Dns = dns,
                IsSharedDomain = domain.Tenant == DefineConstants.SHARED_TENANT_ID
            };

            return serverDomain;
        }

        protected ServerDomainDnsData ToServerDomainDnsData(ServerDns serverDns)
        {
            if (serverDns == null)
                return null;

            var dnsData = new ServerDomainDnsData
            {
                Id = serverDns.Id,
                MxRecord = new ServerDomainMxRecordData
                {
                    Host = serverDns.Mx,
                    IsVerified = serverDns.MxVerified,
                    Priority = (int)MailSettings.ServerDnsMxRecordPriority
                },
                SpfRecord = new ServerDomainDnsRecordData
                {
                    Name = DefineConstants.DNS_DEFAULT_ORIGIN,
                    IsVerified = serverDns.SpfVerified,
                    Value = serverDns.Spf
                },
                DkimRecord = new ServerDomainDkimRecordData
                {
                    Selector = serverDns.DkimSelector,
                    IsVerified = serverDns.DkimVerified,
                    PublicKey = serverDns.DkimPublicKey
                },
                DomainCheckRecord = new ServerDomainDnsRecordData
                {
                    Name = DefineConstants.DNS_DEFAULT_ORIGIN,
                    IsVerified = true,
                    Value = serverDns.DomainCheck
                }
            };

            return dnsData;
        }
    }
}
