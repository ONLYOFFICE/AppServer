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
using System.Net.Mail;
using System.Security;

using ASC.Common;
using ASC.Common.Web;
using ASC.Core;
using ASC.Core.Common.Settings;
using ASC.Core.Users;
using ASC.Mail.Authorization;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Core.Entities;
using ASC.Mail.Models;
using ASC.Mail.Server.Core.Entities;
using ASC.Mail.Server.Utils;
using ASC.Mail.Utils;
using ASC.Web.Core;
using ASC.Web.Core.Users;
using ASC.Web.Studio.Core;

using Mailbox = ASC.Mail.Core.Entities.Mailbox;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class ServerMailboxEngine : BaseEngine
    {
        private int Tenant => TenantManager.GetCurrentTenant().TenantId;
        private string User => SecurityContext.CurrentAccount.ID.ToString();
        private bool IsAdmin => WebItemSecurity.IsProductAdministrator(WebItemManager.MailProductID, SecurityContext.CurrentAccount.ID);

        private SecurityContext SecurityContext { get; }
        private TenantManager TenantManager { get; }
        private IMailDaoFactory MailDaoFactory { get; }
        private AccountEngine AccountEngine { get; }
        private CacheEngine CacheEngine { get; }
        private ServerDomainEngine ServerDomainEngine { get; }
        private MailboxEngine MailboxEngine { get; }
        private CoreBaseSettings CoreBaseSettings { get; }
        private WebItemSecurity WebItemSecurity { get; }
        private SettingsManager SettingsManager { get; }
        private UserManagerWrapper UserManagerWrapper { get; }
        private AuthManager AuthManager { get; }
        private UserManager UserManager { get; }
        private DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }

        public ServerMailboxEngine(
            SecurityContext securityContext,
            TenantManager tenantManager,
            IMailDaoFactory mailDaoFactory,
            AccountEngine accountEngine,
            CacheEngine cacheEngine,
            ServerDomainEngine serverDomainEngine,
            MailboxEngine mailboxEngine,
            CoreBaseSettings coreBaseSettings,
            WebItemSecurity webItemSecurity,
            SettingsManager settingsManager,
            UserManagerWrapper userManagerWrapper,
            AuthManager authManager,
            UserManager userManager,
            DisplayUserSettingsHelper displayUserSettingsHelper,
            MailSettings mailSettings) : base(mailSettings)
        {
            SecurityContext = securityContext;
            TenantManager = tenantManager;
            MailDaoFactory = mailDaoFactory;
            AccountEngine = accountEngine;
            CacheEngine = cacheEngine;
            ServerDomainEngine = serverDomainEngine;
            MailboxEngine = mailboxEngine;
            CoreBaseSettings = coreBaseSettings;
            WebItemSecurity = webItemSecurity;
            SettingsManager = settingsManager;
            UserManagerWrapper = userManagerWrapper;
            AuthManager = authManager;
            UserManager = userManager;
            DisplayUserSettingsHelper = displayUserSettingsHelper;
        }

        public List<ServerMailboxData> GetMailboxes()
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            var mailboxes = MailDaoFactory.GetMailboxDao().GetMailBoxes(new TenantServerMailboxesExp(Tenant));

            var addresses = MailDaoFactory.GetServerAddressDao().GetList();

            var domains = MailDaoFactory.GetServerDomainDao().GetDomains();

            var list = from mailbox in mailboxes
                       let address =
                           addresses.FirstOrDefault(
                               a => a.MailboxId == mailbox.Id && a.IsAlias == false && a.IsMailGroup == false)
                       where address != null
                       let domain = domains.FirstOrDefault(d => d.Id == address.DomainId)
                       where domain != null
                       let serverAddressData = ToServerDomainAddressData(address, domain)
                       let aliases =
                           addresses.Where(a => a.MailboxId == mailbox.Id && a.IsAlias && !a.IsMailGroup)
                               .ToList()
                               .ConvertAll(a => ToServerDomainAddressData(a, domain))
                       select ToMailboxData(mailbox, serverAddressData, aliases);

            return list.ToList();
        }

        public bool IsAddressAlreadyRegistered(string localPart, int domainId)
        {
            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(domainId);

            var isSharedDomain = serverDomain.Tenant == DefineConstants.SHARED_TENANT_ID;

            if (!IsAdmin && !isSharedDomain)
                throw new SecurityException("Need admin privileges.");

            var tenantQuota = TenantManager.GetTenantQuota(Tenant);

            if (isSharedDomain
                && (tenantQuota.Trial
                    || tenantQuota.Free))
            {
                throw new SecurityException("Not available in unpaid version");
            }

            if (string.IsNullOrEmpty(localPart))
                throw new ArgumentException(@"Invalid local part.", "localPart");

            if (domainId < 0)
                throw new ArgumentException(@"Invalid domain id.", "domainId");

            var state = MailDaoFactory.GetServerAddressDao().IsAddressAlreadyRegistered(localPart, serverDomain.Name);

            return state;
        }

        public bool IsAddressValid(string localPart, int domainId)
        {
            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(domainId);

            var isSharedDomain = serverDomain.Tenant == DefineConstants.SHARED_TENANT_ID;

            if (!IsAdmin && !isSharedDomain)
                throw new SecurityException("Need admin privileges.");

            var tenantQuota = TenantManager.GetTenantQuota(Tenant);

            if (isSharedDomain
                && (tenantQuota.Trial
                    || tenantQuota.Free))
            {
                throw new SecurityException("Not available in unpaid version");
            }

            if (string.IsNullOrEmpty(localPart))
                return false;

            if (domainId < 0)
                return false;

            if (localPart.Length > 64)
                return false;

            if (!Parser.IsEmailLocalPartValid(localPart))
                return false;

            return true;
        }

        public ServerMailboxData CreateMailbox(string name, string localPart, int domainId, string userId)
        {
            ServerMailboxData mailboxData;

            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(domainId);

            var isSharedDomain = serverDomain.Tenant == DefineConstants.SHARED_TENANT_ID;

            if (!IsAdmin && !isSharedDomain)
                throw new SecurityException("Need admin privileges.");

            var tenantQuota = TenantManager.GetTenantQuota(Tenant);

            if (isSharedDomain
                && (tenantQuota.Trial
                    || tenantQuota.Free))
            {
                throw new SecurityException("Not available in unpaid version");
            }

            if (string.IsNullOrEmpty(localPart))
                throw new ArgumentException(@"Invalid local part.", "localPart");

            if (domainId < 0)
                throw new ArgumentException(@"Invalid domain id.", "domainId");

            if (name.Length > 255)
                throw new ArgumentException(@"Sender name exceed limitation of 64 characters.", "name");

            if (!Guid.TryParse(userId, out Guid user))
                throw new ArgumentException(@"Invalid user id.", "userId");

            if (isSharedDomain && !IsAdmin && user != SecurityContext.CurrentAccount.ID)
                throw new SecurityException(
                    "Creation of a shared mailbox is allowed only for the current account if user is not admin.");

            var teamlabAccount = AuthManager.GetAccountByID(Tenant, user);

            if (teamlabAccount == null)
                throw new InvalidDataException("Unknown user.");

            var userInfo = UserManager.GetUsers(user);

            if (userInfo.IsVisitor(UserManager))
                throw new InvalidDataException("User is visitor.");

            if (localPart.Length > 64)
                throw new ArgumentException(@"Local part of mailbox exceed limitation of 64 characters.",
                    "localPart");

            if (!Parser.IsEmailLocalPartValid(localPart))
                throw new ArgumentException("Incorrect local part of mailbox.");

            if (MailDaoFactory.GetServerAddressDao().IsAddressAlreadyRegistered(localPart, serverDomain.Name))
            {
                throw new DuplicateNameException("You want to create a mailbox with already existing address.");
            }

            if (MailSettings.ServerDomainMailboxPerUserLimit > 0)
            {
                var accounts = AccountEngine.GetAccountInfoList();

                var countDomainMailboxes =
                    accounts.Count(a =>
                        a.IsTeamlabMailbox &&
                        Parser.ParseAddress(a.Email)
                            .Domain.Equals(serverDomain.Name, StringComparison.InvariantCultureIgnoreCase));

                if (countDomainMailboxes >= MailSettings.ServerDomainMailboxPerUserLimit)
                {
                    throw new ArgumentOutOfRangeException(
                        string.Format("Count of user's mailboxes must be less or equal {0}.",
                            MailSettings.ServerDomainMailboxPerUserLimit));
                }
            }

            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            var mailboxLocalPart = localPart.ToLowerInvariant();

            var login = string.Format("{0}@{1}", mailboxLocalPart, serverDomain.Name);

            var existMailbox = MailDaoFactory.GetMailboxDao().GetMailBox(new СoncreteUserMailboxExp(new MailAddress(login), Tenant, userId));

            if (existMailbox != null)
            {
                throw new DuplicateNameException("You want to create a mailbox that is already connected.");
            }

            var password = PasswordGenerator.GenerateNewPassword(12);

            var utcNow = DateTime.UtcNow;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var mailbox = new Mailbox
                {
                    Id = 0,
                    Tenant = Tenant,
                    User = userId,
                    Name = name,
                    Address = login,
                    OAuthToken = null,
                    OAuthType = (int)AuthorizationServiceType.None,
                    ServerId = server.ImapSettingsId,
                    Password = password,
                    SmtpServerId = server.SmtpSettingsId,
                    SmtpPassword = password,
                    SizeLast = 0,
                    MsgCountLast = 0,
                    BeginDate = DefineConstants.MinBeginDate,
                    Imap = true,
                    Enabled = true,
                    IsTeamlabMailbox = true,
                    IsRemoved = false,
                    DateCreated = utcNow
                };

                mailbox.Id = MailDaoFactory.GetMailboxDao().SaveMailBox(mailbox);

                var address = new ServerAddress
                {
                    Id = 0,
                    Tenant = Tenant,
                    MailboxId = mailbox.Id,
                    DomainId = serverDomain.Id,
                    AddressName = localPart,
                    IsAlias = false,
                    IsMailGroup = false,
                    DateCreated = utcNow
                };

                address.Id = MailDaoFactory.GetServerAddressDao().Save(address);

                var engine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

                var maildir = PostfixMaildirUtil.GenerateMaildirPath(serverDomain.Name, localPart, utcNow);

                var serverMailbox = new Server.Core.Entities.Mailbox
                {
                    Name = name,
                    Password = password,
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
                    Name = name,
                    Address = login,
                    Goto = login,
                    Domain = serverDomain.Name,
                    Active = true,
                    Islist = false,
                    Modified = utcNow,
                    Created = utcNow
                };

                engine.SaveMailbox(serverMailbox, serverAddress);

                tx.Commit();

                CacheEngine.Clear(userId);

                mailboxData = ToMailboxData(mailbox, ToServerDomainAddressData(address, login),
                    new List<ServerDomainAddressData>());
            }

            return mailboxData;
        }

        public ServerMailboxData CreateMyCommonDomainMailbox(string name)
        {
            if (!SetupInfo.IsVisibleSettings("AdministrationPage") || !SetupInfo.IsVisibleSettings("MailCommonDomain") ||
                CoreBaseSettings.Standalone)
            {
                throw new Exception("Common domain is not available");
            }

            var domain = ServerDomainEngine.GetCommonDomain();

            if (domain == null)
                throw new SecurityException("Domain not found.");

            var userInfo = UserManager.GetUsers(SecurityContext.CurrentAccount.ID);

            return CreateMailbox(userInfo.DisplayUserName(DisplayUserSettingsHelper), name, domain.Id, userInfo.ID.ToString());
        }

        public ServerMailboxData UpdateMailboxDisplayName(int mailboxId, string name)
        {
            var serverMailboxAddresses = MailDaoFactory.GetServerAddressDao().GetList(mailboxId);

            if (!serverMailboxAddresses.Any())
                throw new ArgumentException("Mailbox not found");

            var serverMailboxAddress = serverMailboxAddresses.FirstOrDefault(a => !a.IsAlias && !a.IsMailGroup);

            if (serverMailboxAddress == null)
                throw new ArgumentException("Mailbox not found");

            var serverMailboxAliases = serverMailboxAddresses.Where(a => a.IsAlias).ToList();

            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(serverMailboxAddress.DomainId);

            var isSharedDomain = serverDomain.Tenant == DefineConstants.SHARED_TENANT_ID;

            if (!IsAdmin && !isSharedDomain)
                throw new SecurityException("Need admin privileges.");

            var tenantQuota = TenantManager.GetTenantQuota(Tenant);

            if (isSharedDomain
                && (tenantQuota.Trial
                    || tenantQuota.Free))
            {
                throw new SecurityException("Not available in unpaid version");
            }

            if (name.Length > 255)
                throw new ArgumentException(@"Sender name exceed limitation of 64 characters.", "name");

            var serverMailbox =
                MailDaoFactory.GetMailboxDao().GetMailBox(new ConcreteTenantServerMailboxExp(mailboxId, Tenant, false));

            serverMailbox.Name = name;

            MailDaoFactory.GetMailboxDao().SaveMailBox(serverMailbox);

            CacheEngine.Clear(serverMailbox.User);

            var address = ToServerDomainAddressData(serverMailboxAddress, serverDomain);

            var aliases = serverMailboxAliases.ConvertAll(a => ToServerDomainAddressData(a, serverDomain));

            var mailboxData = ToMailboxData(serverMailbox, address, aliases);

            return mailboxData;
        }

        public ServerDomainAddressData AddAlias(int mailboxId, string aliasName)
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (string.IsNullOrEmpty(aliasName))
                throw new ArgumentException(@"Invalid alias name.", "aliasName");

            if (mailboxId < 0)
                throw new ArgumentException(@"Invalid mailbox id.", "mailboxId");

            if (aliasName.Length > 64)
                throw new ArgumentException(@"Local part of mailbox alias exceed limitation of 64 characters.", "aliasName");

            if (!Parser.IsEmailLocalPartValid(aliasName))
                throw new ArgumentException("Incorrect mailbox alias.");

            var mailboxAliasName = aliasName.ToLowerInvariant();

            var mailbox = MailDaoFactory.GetMailboxDao().GetMailBox(new ConcreteTenantMailboxExp(mailboxId, Tenant));

            if (mailbox == null)
                throw new ArgumentException("Mailbox not exists");

            if (!mailbox.IsTeamlabMailbox)
                throw new ArgumentException("Invalid mailbox type");

            if (mailbox.Tenant == DefineConstants.SHARED_TENANT_ID)
                throw new InvalidOperationException("Adding mailbox alias is not allowed for shared domain.");

            var mailAddress = new MailAddress(mailbox.Address);

            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomains().FirstOrDefault(d => d.Name == mailAddress.Host);

            if (serverDomain == null)
                throw new ArgumentException("Domain not exists");

            var mailboxAddress = mailAddress.Address;

            if (MailDaoFactory.GetServerAddressDao().IsAddressAlreadyRegistered(mailboxAliasName, serverDomain.Name))
            {
                throw new DuplicateNameException("You want to create a mailbox with already existing address.");
            }

            var utcNow = DateTime.UtcNow;

            var address = new ServerAddress
            {
                Id = 0,
                Tenant = Tenant,
                MailboxId = mailbox.Id,
                DomainId = serverDomain.Id,
                AddressName = mailboxAliasName,
                IsAlias = true,
                IsMailGroup = false,
                DateCreated = utcNow
            };

            var aliasEmail = string.Format("{0}@{1}", mailboxAliasName, serverDomain.Name);

            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            var engine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                address.Id = MailDaoFactory.GetServerAddressDao().Save(address);

                var serverAddress = new Alias
                {
                    Name = mailbox.Name,
                    Address = aliasEmail,
                    Goto = mailboxAddress,
                    Domain = serverDomain.Name,
                    Active = true,
                    Islist = false,
                    Modified = utcNow,
                    Created = utcNow
                };

                engine.SaveAlias(serverAddress);

                tx.Commit();
            }

            CacheEngine.Clear(mailbox.User);

            return new ServerDomainAddressData
            {
                Id = address.Id,
                DomainId = address.DomainId,
                Email = aliasEmail
            };
        }

        public void RemoveAlias(int mailboxId, int addressId)
        {
            if (!IsAdmin)
                throw new SecurityException("Need admin privileges.");

            if (mailboxId < 0)
                throw new ArgumentException(@"Invalid address id.", "mailboxId");


            var mailbox = MailDaoFactory.GetMailboxDao().GetMailBox(new ConcreteTenantServerMailboxExp(mailboxId, Tenant, false));

            if (mailbox == null)
                throw new ArgumentException("Mailbox not exists");

            if (!mailbox.IsTeamlabMailbox)
                throw new ArgumentException("Invalid mailbox type");

            var alias = MailDaoFactory.GetServerAddressDao().Get(addressId);

            if (!alias.IsAlias)
                throw new ArgumentException("Address is not alias");

            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(alias.DomainId);

            if (serverDomain == null)
                throw new ArgumentException("Domain not exists");

            var aliasEmail = string.Format("{0}@{1}", alias.AddressName, serverDomain.Name);

            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            var engine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                MailDaoFactory.GetServerAddressDao().Delete(addressId);
                engine.RemoveAlias(aliasEmail);

                tx.Commit();
            }

            CacheEngine.Clear(mailbox.User);
        }

        public void RemoveMailbox(MailBoxData mailBox)
        {
            var serverMailboxAddresses = MailDaoFactory.GetServerAddressDao().GetList(mailBox.MailBoxId);

            var serverMailboxAddress = serverMailboxAddresses.FirstOrDefault(a => !a.IsAlias && !a.IsMailGroup);

            if (serverMailboxAddress == null)
                throw new InvalidDataException("Mailbox address not found");

            var serverDomain = MailDaoFactory.GetServerDomainDao().GetDomain(serverMailboxAddress.DomainId);

            if (serverDomain == null)
                throw new InvalidDataException("Domain not found");

            var serverGroups = MailDaoFactory.GetServerGroupDao().GetList();

            var server = MailDaoFactory.GetServerDao().Get(mailBox.TenantId);

            var serverEngine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

            var utcNow = DateTime.UtcNow;

            using var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted);

            foreach (var serverGroup in serverGroups)
            {
                var addresses = MailDaoFactory.GetServerAddressDao().GetGroupAddresses(serverGroup.Id);

                var index = addresses.FindIndex(a => a.Id == serverMailboxAddress.Id);

                if (index < 0)
                    continue;

                addresses.RemoveAt(index);

                if (addresses.Count == 0)
                {
                    MailDaoFactory.GetServerGroupDao().Delete(serverGroup.Id);

                    MailDaoFactory.GetServerAddressDao().DeleteAddressesFromMailGroup(serverGroup.Id);

                    serverEngine.RemoveAlias(serverGroup.Address);
                }
                else
                {
                    MailDaoFactory.GetServerAddressDao().DeleteAddressFromMailGroup(serverGroup.Id, serverMailboxAddress.Id);

                    var goTo = string.Join(",",
                        addresses.Select(m => string.Format("{0}@{1}", m.AddressName, serverDomain.Name)));

                    var serverAddress = new Alias
                    {
                        Name = "",
                        Address = serverGroup.Address,
                        Goto = goTo,
                        Domain = serverDomain.Name,
                        Active = true,
                        Islist = true,
                        Modified = utcNow,
                        Created = serverGroup.DateCreated
                    };

                    serverEngine.SaveAlias(serverAddress);
                }
            }

            MailDaoFactory.GetServerAddressDao().Delete(serverMailboxAddresses.Select(a => a.Id).ToList());

            foreach (var mailboxAddress in serverMailboxAddresses)
            {
                serverEngine.RemoveAlias(string.Format("{0}@{1}", mailboxAddress.AddressName, serverDomain.Name));
            }

            MailboxEngine.RemoveMailBox(mailBox, false);

            serverEngine.RemoveMailbox(string.Format("{0}@{1}", serverMailboxAddress.AddressName,
                serverDomain.Name));

            tx.Commit();
        }

        public MailOperationStatus RemoveMailbox(int id)
        {
            if (id < 0)
                throw new ArgumentException(@"Invalid server mailbox id.", "id");

            var mailbox = MailboxEngine.GetMailboxData(new ConcreteTenantServerMailboxExp(id, Tenant, false));

            if (mailbox == null)
                throw new ItemNotFoundException("Mailbox not found.");

            var isSharedDomain = mailbox.TenantId == DefineConstants.SHARED_TENANT_ID;

            if (!IsAdmin && !isSharedDomain)
                throw new SecurityException("Need admin privileges.");

            if (isSharedDomain && !IsAdmin &&
                !mailbox.UserId.Equals(SecurityContext.CurrentAccount.ID.ToString(),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityException(
                    "Removing of a shared mailbox is allowed only for the current account if user is not admin.");
            }

            //TODO fix return OperationEngine.RemoveServerMailbox(mailbox);

            throw new NotImplementedException();
        }

        public void ChangePassword(int mailboxId, string password)
        {
            if (!CoreBaseSettings.Standalone)
                throw new SecurityException("Not allowed in this version");

            if (mailboxId < 0)
                throw new ArgumentException(@"Invalid mailbox id.", "mailboxId");

            var trimPwd = Parser.GetValidPassword(password, SettingsManager, UserManagerWrapper);


            var serverMailboxAddresses = MailDaoFactory.GetServerAddressDao().GetList(mailboxId);

            if (!serverMailboxAddresses.Any())
                throw new ArgumentException("Mailbox not found");

            var serverMailboxAddress = serverMailboxAddresses.FirstOrDefault(a => !a.IsAlias && !a.IsMailGroup);

            if (serverMailboxAddress == null)
                throw new ArgumentException("Mailbox not found");

            var exp = IsAdmin
                ? (IMailboxExp)new ConcreteTenantMailboxExp(mailboxId, Tenant)
                : new СoncreteUserMailboxExp(mailboxId, Tenant, User);

            var mailbox =
                    MailDaoFactory.GetMailboxDao().GetMailBox(exp);

            if (mailbox == null) // Mailbox has been removed
                throw new ArgumentException("Mailbox not found");

            var server = MailDaoFactory.GetServerDao().Get(Tenant);

            using var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted);

            var engine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

            engine.ChangePassword(mailbox.Address, trimPwd);

            mailbox.Password = trimPwd;
            mailbox.SmtpPassword = trimPwd;

            MailDaoFactory.GetMailboxDao().SaveMailBox(mailbox);

            tx.Commit();
        }

        public static ServerMailboxData ToMailboxData(Mailbox mailbox, ServerDomainAddressData address,
            List<ServerDomainAddressData> aliases)
        {
            return new ServerMailboxData
            {
                Id = mailbox.Id,
                UserId = mailbox.User,
                Address = address,
                Name = mailbox.Name,
                Aliases = aliases
            };
        }

        public static ServerDomainAddressData ToServerDomainAddressData(ServerAddress address, ServerDomain domain)
        {
            var result = new ServerDomainAddressData
            {
                Id = address.Id,
                DomainId = address.DomainId,
                Email = string.Format("{0}@{1}", address.AddressName, domain.Name)
            };

            return result;
        }

        public static ServerDomainAddressData ToServerDomainAddressData(ServerAddress address, ServerDomainData domain)
        {
            var result = new ServerDomainAddressData
            {
                Id = address.Id,
                DomainId = address.DomainId,
                Email = string.Format("{0}@{1}", address.AddressName, domain.Name)
            };

            return result;
        }

        public static ServerDomainAddressData ToServerDomainAddressData(ServerAddress address, string email)
        {
            var result = new ServerDomainAddressData
            {
                Id = address.Id,
                DomainId = address.DomainId,
                Email = email
            };

            return result;
        }
    }
}
