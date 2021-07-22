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
using System.Linq;
using System.Net.Mail;
using System.Security;

using ASC.Common;
using ASC.Common.Utils;
using ASC.Core;
using ASC.Core.Common.Settings;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Models;
using ASC.Mail.Server.Core.Entities;
using ASC.Mail.Server.Utils;
using ASC.Mail.Utils;
using ASC.Web.Core;
using ASC.Web.Core.Users;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class ServerEngine : BaseEngine
    {
        private int Tenant => TenantManager.GetCurrentTenant().TenantId;
        private bool IsAdmin => WebItemSecurity.IsProductAdministrator(WebItemManager.MailProductID, SecurityContext.CurrentAccount.ID);

        private SecurityContext SecurityContext { get; }
        private TenantManager TenantManager { get; }
        private IMailDaoFactory MailDaoFactory { get; }
        private ServerDomainEngine ServerDomainEngine { get; }
        private CoreBaseSettings CoreBaseSettings { get; }
        private WebItemSecurity WebItemSecurity { get; }
        private SettingsManager SettingsManager { get; }
        private UserManagerWrapper UserManagerWrapper { get; }
        private IServiceProvider ServiceProvider { get; }

        public ServerEngine(
            SecurityContext securityContext,
            TenantManager tenantManager,
            IMailDaoFactory mailDaoFactory,
            ServerDomainEngine serverDomainEngine,
            CoreBaseSettings coreBaseSettings,
            WebItemSecurity webItemSecurity,
            SettingsManager settingsManager,
            UserManagerWrapper userManagerWrapper,
            MailSettings mailSettings,
            IServiceProvider serviceProvider) : base(mailSettings)
        {
            SecurityContext = securityContext;
            TenantManager = tenantManager;
            MailDaoFactory = mailDaoFactory;
            ServerDomainEngine = serverDomainEngine;
            CoreBaseSettings = coreBaseSettings;
            WebItemSecurity = webItemSecurity;
            SettingsManager = settingsManager;
            UserManagerWrapper = userManagerWrapper;
            ServiceProvider = serviceProvider;
        }

        public List<MailAddressInfo> GetAliases(int mailboxId)
        {
            var MailDbContext = MailDaoFactory.GetContext();

            var list = MailDbContext.MailServerAddress
                .Join(MailDbContext.MailServerDomain, a => a.IdDomain, d => d.Id,
                (a, d) => new
                {
                    Address = a,
                    Domain = d
                })
                .Where(x => x.Address.IdMailbox == mailboxId && x.Address.IsAlias)
                .Select(x => new MailAddressInfo(x.Address.Id, string.Format("{0}@{1}", x.Address.Name, x.Domain.Name), x.Domain.Id))
                .ToList();

            return list;
        }

        public List<MailAddressInfo> GetGroups(int mailboxId)
        {
            var MailDbContext = MailDaoFactory.GetContext();

            var list = MailDbContext.MailServerAddress
                .Join(MailDbContext.MailServerDomain, a => a.IdDomain, d => d.Id,
                (a, d) => new
                {
                    Address = a,
                    Domain = d
                })
                .Join(MailDbContext.MailServerMailGroupXMailServerAddress, a => a.Address.Id, x => x.IdAddress,
                (a, x) => new
                {
                    a.Address,
                    a.Domain,
                    XGroup = x
                })
                 .Join(MailDbContext.MailServerMailGroup, x => x.XGroup.IdMailGroup, g => g.Id,
                (x, g) => new
                {
                    x.Address,
                    x.Domain,
                    x.XGroup,
                    Group = g
                })
                .Where(x => x.Address.IdMailbox == mailboxId)
                .Select(x => new MailAddressInfo(x.Group.Id, x.Group.Address, x.Domain.Id))
                .ToList();

            return list;
        }

        public Entities.Server GetLinkedServer()
        {
            var linkedServer = MailDaoFactory.GetServerDao().Get(Tenant);

            return linkedServer;
        }

        private List<Entities.Server> GetAllServers()
        {
            var servers = MailDaoFactory.GetServerDao().GetList();

            return servers;
        }

        public void Link(Entities.Server server, int tenant)
        {
            if (server == null)
                return;

            var result = MailDaoFactory.GetServerDao().Link(server, Tenant);

            if (result <= 0)
                throw new Exception("Invalid insert operation");
        }

        public void UnLink(Entities.Server server, int tenant)
        {
            if (server == null)
                return;

            MailDaoFactory.GetServerDao().UnLink(server, Tenant);
        }

        public int Save(Entities.Server server)
        {
            if (server == null)
                throw new ArgumentNullException("server");

            var id = MailDaoFactory.GetServerDao().Save(server);

            return id;
        }

        public void Delete(int serverId)
        {
            if (serverId <= 0)
                throw new ArgumentOutOfRangeException("serverId");

            MailDaoFactory.GetServerDao().Delete(serverId);
        }

        public Entities.Server GetOrCreate()
        {
            var linkedServer = MailDaoFactory.GetServerDao().Get(Tenant);

            if (linkedServer != null)
                return linkedServer;

            var servers = GetAllServers();

            if (!servers.Any())
                throw new Exception("Mail Server not configured");

            var server = servers.First();

            Link(server, Tenant);

            linkedServer = server;

            return linkedServer;
        }

        public string GetMailServerMxDomain()
        {
            var server = GetMailServer();

            return server.Dns.MxRecord.Host;
        }

        public ServerData GetMailServer()
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            var linkedServer = GetOrCreate();

            var dns = ServerDomainEngine.GetOrCreateUnusedDnsData(linkedServer);

            var inServer = MailDaoFactory.GetMailboxServerDao().GetServer(linkedServer.ImapSettingsId);
            var outServer = MailDaoFactory.GetMailboxServerDao().GetServer(linkedServer.SmtpSettingsId);

            return new ServerData
            {
                Id = linkedServer.Id,
                Dns = dns,
                ServerLimits = new ServerLimitData
                {
                    MailboxMaxCountPerUser = (int)MailSettings.ServerDomainMailboxPerUserLimit
                },
                InServer = inServer,
                OutServer = outServer
            };
        }

        public ServerDomainDnsData GetOrCreateUnusedDnsData()
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            var server = GetOrCreate();
            return ServerDomainEngine.GetOrCreateUnusedDnsData(server);
        }

        public bool CheckDomainOwnership(string domain)
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

            var dns = GetOrCreateUnusedDnsData();

            var dnsLookup = new DnsLookup();

            return dnsLookup.IsDomainTxtRecordExists(domainName, dns.DomainCheckRecord.Value);
        }

        public ServerNotificationAddressData CreateNotificationAddress(string localPart, string password, int domainId)
        {
            if (!CoreBaseSettings.Standalone)
                throw new SecurityException("Only for standalone");

            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (string.IsNullOrEmpty(localPart))
                throw new ArgumentNullException("localPart", @"Invalid address username.");

            localPart = localPart.ToLowerInvariant().Trim();

            if (localPart.Length > 64)
                throw new ArgumentException(@"Local part of address exceed limitation of 64 characters.", "localPart");

            if (!Parser.IsEmailLocalPartValid(localPart))
                throw new ArgumentException(@"Incorrect address username.", "localPart");

            var trimPwd = Parser.GetValidPassword(password, SettingsManager, UserManagerWrapper);

            if (domainId < 0)
                throw new ArgumentException(@"Invalid domain id.", "domainId");

            var notificationAddressSettings = SettingsManager.LoadSettings<ServerNotificationAddressSettings>(Tenant);

            if (!string.IsNullOrEmpty(notificationAddressSettings.NotificationAddress))
            {
                RemoveNotificationAddress(notificationAddressSettings.NotificationAddress);
            }

            var utcNow = DateTime.UtcNow;

            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(domainId);

            if (localPart.Length + serverDomain.Name.Length > 318) // 318 because of @ sign
                throw new ArgumentException(@"Address of mailbox exceed limitation of 319 characters.", "localPart");

            var login = string.Format("{0}@{1}", localPart, serverDomain.Name);

            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            if (server == null)
                throw new ArgumentException("Server not configured");

            var engine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

            var maildir = PostfixMaildirUtil.GenerateMaildirPath(serverDomain.Name, localPart, utcNow);

            var serverMailbox = new Server.Core.Entities.Mailbox
            {
                Name = localPart,
                Password = trimPwd,
                Username = login,
                LocalPart = localPart,
                Domain = serverDomain.Name,
                Active = true,
                Quota = 0,
                Maildir = maildir,
                Modified = utcNow,
                Created = utcNow,
            };

            var serverAddress = new Alias
            {
                Name = localPart,
                Address = login,
                Goto = login,
                Domain = serverDomain.Name,
                Active = true,
                Islist = false,
                Modified = utcNow,
                Created = utcNow
            };

            engine.SaveMailbox(serverMailbox, serverAddress, false);

            notificationAddressSettings = new ServerNotificationAddressSettings { NotificationAddress = login };

            SettingsManager.SaveSettings(notificationAddressSettings, Tenant);

            var smtpSettings = MailDaoFactory.GetMailboxServerDao()
                .GetServer(server.SmtpSettingsId);

            var address = new MailAddress(login);

            var notifyAddress = new ServerNotificationAddressData
            {
                Email = address.ToString(),
                SmtpPort = smtpSettings.Port,
                SmtpServer = smtpSettings.Hostname,
                SmtpAccount = address.ToLogin(smtpSettings.Username),
                SmptEncryptionType = smtpSettings.SocketType,
                SmtpAuth = true,
                SmtpAuthenticationType = smtpSettings.Authentication
            };

            return notifyAddress;
        }

        public void RemoveNotificationAddress(string address)
        {
            if (!CoreBaseSettings.Standalone)
                throw new SecurityException("Only for standalone");

            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException("address");

            var deleteAddress = address.ToLowerInvariant().Trim();
            var notificationAddressSettings = SettingsManager.LoadSettings<ServerNotificationAddressSettings>(Tenant);

            if (notificationAddressSettings.NotificationAddress != deleteAddress)
                throw new ArgumentException("Mailbox not exists");

            var mailAddress = new MailAddress(deleteAddress);

            var serverDomain = MailDaoFactory.GetServerDomainDao()
                .GetDomains()
                .FirstOrDefault(d => d.Name == mailAddress.Host);

            if (serverDomain == null)
                throw new ArgumentException("Domain not exists");

            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            if (server == null)
                throw new ArgumentException("Server not configured");

            var engine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

            engine.RemoveMailbox(deleteAddress);

            var addressSettings = notificationAddressSettings.GetDefault(ServiceProvider) as ServerNotificationAddressSettings;
            if (addressSettings != null && !SettingsManager.SaveSettings(addressSettings, Tenant))
            {
                throw new Exception("Could not delete notification address setting.");
            }
        }

        public ServerFullData GetMailServerFullInfo()
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            var fullServerInfo = new ServerFullData();
            var mailboxDataList = new List<ServerMailboxData>();
            var mailgroupDataList = new List<ServerDomainGroupData>();

            var server = GetMailServer();

            var domains = ServerDomainEngine.GetDomains();

            var mailboxes = MailDaoFactory.GetMailboxDao().GetMailBoxes(new TenantServerMailboxesExp(Tenant));

            var addresses = MailDaoFactory.GetServerAddressDao().GetList();

            foreach (var mailbox in mailboxes)
            {
                var address =
                    addresses.FirstOrDefault(
                        a => a.MailboxId == mailbox.Id && a.IsAlias == false && a.IsMailGroup == false);

                if (address == null)
                    continue;

                var domain = domains.FirstOrDefault(d => d.Id == address.DomainId);

                if (domain == null)
                    continue;

                var serverAddressData = ServerMailboxEngine.ToServerDomainAddressData(address, domain);

                var aliases =
                    addresses.Where(a => a.MailboxId == mailbox.Id && a.IsAlias && !a.IsMailGroup)
                        .ToList()
                        .ConvertAll(a => ServerMailboxEngine.ToServerDomainAddressData(a, domain));

                mailboxDataList.Add(ServerMailboxEngine.ToMailboxData(mailbox, serverAddressData, aliases));
            }

            var groups = MailDaoFactory.GetServerGroupDao().GetList();

            foreach (var serverGroup in groups)
            {
                var address =
                    addresses.FirstOrDefault(
                        a => a.Id == serverGroup.AddressId && !a.IsAlias && a.IsMailGroup);

                if (address == null)
                    continue;

                var domain = domains.FirstOrDefault(d => d.Id == address.DomainId);

                if (domain == null)
                    continue;

                var email = string.Format("{0}@{1}", address.AddressName, domain.Name);

                var serverGroupAddress = ServerMailboxEngine.ToServerDomainAddressData(address, email);

                var serverGroupAddresses =
                    MailDaoFactory.GetServerAddressDao().GetGroupAddresses(serverGroup.Id)
                        .ConvertAll(a => ServerMailboxEngine.ToServerDomainAddressData(a,
                            string.Format("{0}@{1}", a.AddressName, domain.Name)));

                mailgroupDataList.Add(ServerMailgroupEngine.ToServerDomainGroupData(serverGroup.Id, serverGroupAddress, serverGroupAddresses));
            }

            fullServerInfo.Server = server;
            fullServerInfo.Domains = domains;
            fullServerInfo.Mailboxes = mailboxDataList;
            fullServerInfo.Mailgroups = mailgroupDataList;

            return fullServerInfo;
        }

        public string GetServerVersion()
        {
            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            if (server == null)
                return null;

            var engine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);
            var version = engine.GetVersion();
            return version;
        }
    }
}
