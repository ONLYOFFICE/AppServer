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
using System.Net.Mail;
using System.Threading;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Common.Settings;
using ASC.Mail.Authorization;
using ASC.Mail.Clients;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Enums;
using ASC.Mail.Models;
using ASC.Mail.Utils;

using Microsoft.Extensions.Options;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class AccountEngine
    {
        private int Tenant => TenantManager.GetCurrentTenant().TenantId;
        private string UserId => SecurityContext.CurrentAccount.ID.ToString();

        private SecurityContext SecurityContext { get; }
        private ILog Log { get; }
        private MailboxEngine MailboxEngine { get; }
        private IMailDaoFactory MailDaoFactory { get; }
        private TenantManager TenantManager { get; }
        private CacheEngine CacheEngine { get; }
        private ServerEngine ServerEngine { get; }
        private MailBoxSettingEngine MailBoxSettingEngine { get; }
        private CoreBaseSettings CoreBaseSettings { get; }
        private SettingsManager SettingsManager { get; }
        private MailSettings MailSettings { get; }

        public List<ServerFolderAccessInfo> ServerFolderAccessInfos { get; set; }

        public AccountEngine(
            TenantManager tenantManager,
            SecurityContext securityContext,
            IMailDaoFactory daoFactory,
            MailboxEngine mailboxEngine,
            CacheEngine cacheEngine,
            ServerEngine serverEngine,
            MailBoxSettingEngine mailBoxSettingEngine,
            CoreBaseSettings coreBaseSettings,
            SettingsManager settingsManager,
            MailSettings mailSettings,
            IOptionsMonitor<ILog> option)
        {
            SecurityContext = securityContext;

            MailboxEngine = mailboxEngine;

            MailSettings = mailSettings;

            MailDaoFactory = daoFactory;
            TenantManager = tenantManager;
            CacheEngine = cacheEngine;
            ServerEngine = serverEngine;
            MailBoxSettingEngine = mailBoxSettingEngine;
            CoreBaseSettings = coreBaseSettings;
            SettingsManager = settingsManager;
            ServerFolderAccessInfos = MailDaoFactory.GetImapSpecialMailboxDao().GetServerFolderAccessInfoList();

            Log = option.Get("ASC.Mail.AccountEngine");
        }

        public List<AccountInfo> GetAccountInfoList()
        {
            var accountInfoList = CacheEngine.Get(UserId);

            if (accountInfoList != null && accountInfoList.Any())
                return accountInfoList;

            accountInfoList = new List<AccountInfo>();

            var accounts = MailDaoFactory.GetAccountDao().GetAccounts();

            foreach (var account in accounts)
            {
                var mailboxId = account.MailboxId;
                var accountIndex = accountInfoList.FindIndex(a => a.Id == mailboxId);

                var isAlias = account.ServerAddressIsAlias;

                if (!isAlias)
                {
                    var groupAddress = account.ServerMailGroupAddress;
                    MailAddressInfo group = null;

                    if (!string.IsNullOrEmpty(groupAddress))
                    {
                        group = new MailAddressInfo(account.ServerMailGroupId,
                            groupAddress,
                            account.ServerDomainId);
                    }

                    if (accountIndex == -1)
                    {
                        var authErrorType = MailBoxData.AuthProblemType.NoProblems;

                        if (account.MailboxDateAuthError.HasValue)
                        {
                            var authErrorDate = account.MailboxDateAuthError.Value;

                            if (DateTime.UtcNow - authErrorDate > MailSettings.AuthErrorDisableMailboxTimeout)
                                authErrorType = MailBoxData.AuthProblemType.TooManyErrors;
                            else if (DateTime.UtcNow - authErrorDate > MailSettings.AuthErrorWarningTimeout)
                                authErrorType = MailBoxData.AuthProblemType.ConnectError;
                        }

                        var accountInfo = new AccountInfo(
                            mailboxId,
                            account.MailboxAddress,
                            account.MailboxAddressName,
                            account.MailboxEnabled,
                            account.MailboxQuotaError,
                            authErrorType,
                            account.MailboxSignature,
                            account.MailboxAutoreply,
                            !string.IsNullOrEmpty(account.MailboxOAuthToken),
                            account.MailboxEmailInFolder,
                            account.MailboxIsServerMailbox,
                            account.ServerDomainTenant == DefineConstants.SHARED_TENANT_ID);

                        if (group != null) accountInfo.Groups.Add(group);

                        accountInfoList.Add(accountInfo);
                    }
                    else if (group != null)
                    {
                        accountInfoList[accountIndex].Groups.Add(group);
                    }
                }
                else
                {
                    var alias = new MailAddressInfo(account.ServerAddressId,
                        string.Format("{0}@{1}", account.ServerAddressName, account.ServerDomainName),
                        account.ServerDomainId);

                    accountInfoList[accountIndex].Aliases.Add(alias);
                }
            }

            CacheEngine.Set(UserId, accountInfoList);

            return accountInfoList;
        }

        public MailBoxData GetAccount(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException(@"Email empty", "email");

            var mailbox =
                MailboxEngine.GetMailboxData(new СoncreteUserMailboxExp(new MailAddress(email), Tenant,
                    UserId));

            if (mailbox == null)
                throw new NullReferenceException(string.Format("Account wasn't found by email: {0}", email));

            if (mailbox.IsTeamlab)
            {
                if (!CoreBaseSettings.Standalone)
                    throw new ArgumentException("Access to this account restricted");

                string mxHost = null;

                try
                {
                    mxHost = ServerEngine.GetMailServerMxDomain();
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("GetMailServerMxDomain() failed. Exception: {0}", ex.ToString());
                }

                if (!string.IsNullOrEmpty(mxHost))
                {
                    mailbox.Server = mxHost;
                    mailbox.SmtpServer = mxHost;
                }
            }

            mailbox.Password = "";
            mailbox.SmtpPassword = "";

            return mailbox;
        }

        public MailBoxData GetAccountDefaults(string email, string action)
        {
            switch (action)
            {
                case DefineConstants.GET_IMAP_POP_SETTINGS:
                    return
                        MailboxEngine.GetDefaultMailboxData(email, "",
                            AuthorizationServiceType.None, true, true) ??
                        MailboxEngine.GetDefaultMailboxData(email, "",
                            AuthorizationServiceType.None, false, false);
                case DefineConstants.GET_IMAP_SERVER:
                case DefineConstants.GET_IMAP_SERVER_FULL:
                    return MailboxEngine.GetDefaultMailboxData(email, "",
                        AuthorizationServiceType.None, true, false);
                case DefineConstants.GET_POP_SERVER:
                case DefineConstants.GET_POP_SERVER_FULL:
                    return MailboxEngine.GetDefaultMailboxData(email, "",
                        AuthorizationServiceType.None, false, false);
            }

            return MailboxEngine.GetDefaultMailboxData(email, "",
                AuthorizationServiceType.None, null, false);
        }

        public AccountInfo TryCreateAccount(AccountModel accountModel, out LoginResult loginResult)
        {
            if (accountModel == null)
                throw new NullReferenceException("accountModel");

            var mbox = new MailBoxData
            {
                Name = accountModel.Name,
                EMail = new MailAddress(accountModel.Email),
                Account = accountModel.Login,
                Password = accountModel.Password,
                Port = accountModel.Port,
                Server = accountModel.Server,
                SmtpAccount = accountModel.SmtpLogin,
                SmtpPassword = accountModel.SmtpPassword,
                SmtpPort = accountModel.SmtpPort,
                SmtpServer = accountModel.SmtpServer,
                Imap = accountModel.Imap,
                TenantId = Tenant,
                UserId = UserId,
                BeginDate = accountModel.Restrict
                    ? DateTime.Now.Subtract(new TimeSpan(MailBoxData.DefaultMailLimitedTimeDelta))
                    : new DateTime(MailBoxData.DefaultMailBeginTimestamp),
                Encryption = accountModel.IncomingEncryptionType,
                SmtpEncryption = accountModel.OutcomingEncryptionType,
                Authentication = accountModel.IncomingAuthenticationType,
                SmtpAuthentication = accountModel.SmtpAuth ? accountModel.OutcomingAuthenticationType : SaslMechanism.None,
                Enabled = true
            };

            using (var client = new MailClient(mbox, CancellationToken.None, ServerFolderAccessInfos,
                    certificatePermit: MailSettings.SslCertificatesErrorsPermit, log: Log))
            {
                loginResult = client.TestLogin();
            }

            if (!loginResult.IngoingSuccess || !loginResult.OutgoingSuccess)
                return null;

            if (!MailboxEngine.SaveMailBox(mbox))
                throw new Exception(string.Format("SaveMailBox {0} failed", mbox.EMail));

            CacheEngine.Clear(UserId);

            var account = new AccountInfo(mbox.MailBoxId, mbox.EMailView, mbox.Name, mbox.Enabled, mbox.QuotaError,
                MailBoxData.AuthProblemType.NoProblems, new MailSignatureData(mbox.MailBoxId, Tenant, "", false),
                new MailAutoreplyData(mbox.MailBoxId, Tenant, false, false, false, DateTime.MinValue,
                    DateTime.MinValue, string.Empty, string.Empty), false, mbox.EMailInFolder, false, false);

            return account;
        }

        public AccountInfo TryCreateAccount(MailBoxData mbox, out LoginResult loginResult)
        {
            if (mbox == null)
                throw new NullReferenceException("mbox");

            using (var client = new MailClient(mbox, CancellationToken.None, ServerFolderAccessInfos,
                    certificatePermit: MailSettings.SslCertificatesErrorsPermit, log: Log))
            {
                loginResult = client.TestLogin();
            }

            if (!loginResult.IngoingSuccess || !loginResult.OutgoingSuccess)
                return null;

            if (!MailboxEngine.SaveMailBox(mbox))
                throw new Exception(string.Format("SaveMailBox {0} failed", mbox.EMail));

            CacheEngine.Clear(UserId);

            var account = new AccountInfo(mbox.MailBoxId, mbox.EMailView, mbox.Name, mbox.Enabled, mbox.QuotaError,
                MailBoxData.AuthProblemType.NoProblems, new MailSignatureData(mbox.MailBoxId, Tenant, "", false),
                new MailAutoreplyData(mbox.MailBoxId, Tenant, false, false, false, DateTime.MinValue,
                    DateTime.MinValue, string.Empty, string.Empty), false, mbox.EMailInFolder, false, false);

            return account;
        }

        public AccountInfo CreateAccountSimple(string email, string password, out List<LoginResult> loginResults)
        {
            MailBoxData mbox = null;

            var domain = email.Substring(email.IndexOf('@') + 1);

            var mailboxSettings = MailBoxSettingEngine.GetMailBoxSettings(domain);

            if (mailboxSettings == null)
            {
                throw new Exception("Unknown mail provider settings.");
            }

            var testMailboxes = mailboxSettings.ToMailboxList(email, password, Tenant, UserId);

            loginResults = new List<LoginResult>();

            foreach (var mb in testMailboxes)
            {
                LoginResult loginResult;

                using (var client = new MailClient(mb, CancellationToken.None, ServerFolderAccessInfos,
                    MailSettings.TcpTimeout, MailSettings.SslCertificatesErrorsPermit, log: Log))
                {
                    loginResult = client.TestLogin();
                }

                loginResults.Add(loginResult);

                if (!loginResult.IngoingSuccess || !loginResult.OutgoingSuccess)
                    continue;

                mbox = mb;
                break;
            }

            if (mbox == null)
                return null;

            if (!MailboxEngine.SaveMailBox(mbox))
                throw new Exception(string.Format("SaveMailBox {0} failed", email));

            CacheEngine.Clear(UserId);

            var account = new AccountInfo(mbox.MailBoxId, mbox.EMailView, mbox.Name, mbox.Enabled, mbox.QuotaError,
                MailBoxData.AuthProblemType.NoProblems, new MailSignatureData(mbox.MailBoxId, Tenant, "", false),
                new MailAutoreplyData(mbox.MailBoxId, Tenant, false, false, false, DateTime.MinValue,
                    DateTime.MinValue, string.Empty, string.Empty), false, mbox.EMailInFolder, false, false);

            return account;
        }

        public AccountInfo CreateAccountOAuth(string code, byte type)
        {
            //TODO: Fix
            /*var oAuthToken = OAuth20TokenHelper.GetAccessToken<GoogleLoginProvider>(ConsumerFactory, code);

            if (oAuthToken == null)
                throw new Exception(@"Empty oauth token");

            var loginProfile = GoogleLoginProvider.Instance.GetLoginProfile(oAuthToken.AccessToken);
            var email = loginProfile.EMail;

            if (string.IsNullOrEmpty(email))
                throw new Exception(@"Empty email");

            var beginDate = DateTime.UtcNow.Subtract(new TimeSpan(MailBoxData.DefaultMailLimitedTimeDelta));

            var mboxImap = MailboxEngine.GetDefaultMailboxData(email, "", (AuthorizationServiceType)type,
                true, false);

            mboxImap.OAuthToken = oAuthToken.ToJson();
            mboxImap.BeginDate = beginDate; // Apply restrict for download

            if (!MailboxEngine.SaveMailBox(mboxImap, (AuthorizationServiceType)type))
                throw new Exception(string.Format("SaveMailBox {0} failed", email));

            CacheEngine.Clear(UserId);

            if (Defines.IsSignalRAvailable)
            {
                SetAccountsActivity();
            }

            var account = new AccountInfo(mboxImap.MailBoxId, mboxImap.EMailView, mboxImap.Name, mboxImap.Enabled,
                mboxImap.QuotaError,
                MailBoxData.AuthProblemType.NoProblems, new MailSignatureData(mboxImap.MailBoxId, Tenant, "", false),
                new MailAutoreplyData(mboxImap.MailBoxId, Tenant, false, false, false, DateTime.MinValue,
                    DateTime.MinValue, string.Empty, string.Empty), true, mboxImap.EMailInFolder, false, false);

            return account;*/

            throw new NotImplementedException();
        }

        public AccountInfo UpdateAccount(MailBoxData newMailBoxData, out LoginResult loginResult)
        {
            if (newMailBoxData == null)
                throw new NullReferenceException("mbox");

            var mbox =
                MailDaoFactory.GetMailboxDao().GetMailBox(
                    new СoncreteUserMailboxExp(
                        newMailBoxData.EMail,
                        Tenant, UserId));

            if (null == mbox)
                throw new ArgumentException("Mailbox with specified email doesn't exist.");

            if (mbox.IsTeamlabMailbox)
                throw new ArgumentException("Mailbox with specified email can't be updated");

            if (!string.IsNullOrEmpty(mbox.OAuthToken))
            {
                var needSave = false;

                if (!mbox.Name.Equals(newMailBoxData.Name))
                {
                    mbox.Name = newMailBoxData.Name;
                    needSave = true;
                }

                if (!mbox.BeginDate.Equals(newMailBoxData.BeginDate))
                {
                    mbox.BeginDate = newMailBoxData.BeginDate;
                    mbox.ImapIntervals = null;
                    needSave = true;
                }

                if (needSave)
                {
                    MailDaoFactory.GetMailboxDao().SaveMailBox(mbox);

                    CacheEngine.Clear(UserId);
                }

                var accountInfo = new AccountInfo(mbox.Id, mbox.Address, mbox.Name, mbox.Enabled, mbox.QuotaError,
                    MailBoxData.AuthProblemType.NoProblems, new MailSignatureData(mbox.Id, Tenant, "", false),
                    new MailAutoreplyData(mbox.Id, Tenant, false, false, false, DateTime.MinValue,
                        DateTime.MinValue, string.Empty, string.Empty), false, mbox.EmailInFolder, false, false);

                loginResult = new LoginResult
                {
                    Imap = mbox.Imap,
                    IngoingSuccess = true,
                    OutgoingSuccess = true
                };

                return accountInfo;
            }

            newMailBoxData.Password = string.IsNullOrEmpty(newMailBoxData.Password)
                ? mbox.Password
                : newMailBoxData.Password;

            newMailBoxData.SmtpPassword = string.IsNullOrEmpty(newMailBoxData.SmtpPassword)
                ? mbox.SmtpPassword
                : newMailBoxData.SmtpPassword;

            newMailBoxData.Imap = mbox.Imap;

            return TryCreateAccount(newMailBoxData, out loginResult);
        }

        public AccountInfo UpdateAccountOAuth(int mailboxId, string code, byte type)
        {
            //TODO: Fix
            /*if (string.IsNullOrEmpty(code))
                throw new ArgumentException(@"Empty OAuth code", "code");

            var oAuthToken = OAuth20TokenHelper.GetAccessToken<GoogleLoginProvider>(ConsumerFactory, code);

            if (oAuthToken == null)
                throw new Exception(@"Empty OAuth token");

            if (string.IsNullOrEmpty(oAuthToken.AccessToken))
                throw new Exception(@"Empty OAuth AccessToken");

            if (string.IsNullOrEmpty(oAuthToken.RefreshToken))
                throw new Exception(@"Empty OAuth RefreshToken");

            if (oAuthToken.IsExpired)
                throw new Exception(@"OAuth token is expired");

            var loginProfile = GoogleLoginProvider.Instance.GetLoginProfile(oAuthToken.AccessToken);
            var email = loginProfile.EMail;

            if (string.IsNullOrEmpty(email))
                throw new Exception(@"Empty email");

            var mbox = DaoFactory.MailboxDao.GetMailBox(
                new СoncreteUserMailboxExp(
                    mailboxId,
                    Tenant, UserId));

            if (null == mbox)
                throw new ArgumentException("Mailbox with specified email doesn't exist.");

            if (mbox.IsTeamlabMailbox || string.IsNullOrEmpty(mbox.OAuthToken))
                throw new ArgumentException("Mailbox with specified email can't be updated");

            if (!mbox.Address.Equals(email, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Mailbox with specified email can't be updated");

            mbox.OAuthToken = oAuthToken.ToJson();

            var result = DaoFactory.MailboxDao.SaveMailBox(mbox);

            mbox.Id = result;

            CacheEngine.Clear(UserId);

            if (Defines.IsSignalRAvailable)
            {
                SetAccountsActivity();
            }

            var accountInfo = new AccountInfo(mbox.Id, mbox.Address, mbox.Name, mbox.Enabled, mbox.QuotaError,
                MailBoxData.AuthProblemType.NoProblems, new MailSignatureData(mbox.Id, Tenant, "", false),
                new MailAutoreplyData(mbox.Id, Tenant, false, false, false, DateTime.MinValue,
                    DateTime.MinValue, string.Empty, string.Empty), true, mbox.EmailInFolder, false, false);

            return accountInfo;*/

            throw new NotImplementedException();
        }

        public int SetAccountEnable(MailAddress address, bool enabled, out LoginResult loginResult)
        {
            if (address == null)
                throw new ArgumentNullException("address");


            var tuple = MailboxEngine.GetMailboxFullInfo(new СoncreteUserMailboxExp(address, Tenant, UserId));

            if (tuple == null)
                throw new NullReferenceException(string.Format("Account wasn't found by email: {0}", address.Address));

            if (enabled)
            {
                // Check account connection setting on activation
                using (var client = new MailClient(tuple.Item1, CancellationToken.None, ServerFolderAccessInfos,
                        certificatePermit: MailSettings.SslCertificatesErrorsPermit, log: Log))
                {
                    loginResult = client.TestLogin();
                }

                if (!loginResult.IngoingSuccess || !loginResult.OutgoingSuccess)
                {
                    return -1;
                }
            }

            loginResult = null;
            var mailboxId =
                MailDaoFactory.GetMailboxDao().Enable(
                    new СoncreteUserMailboxExp(tuple.Item2.Id, tuple.Item2.Tenant, tuple.Item2.User),
                    enabled)
                    ? tuple.Item2.Id
                    : -1;

            if (mailboxId == -1)
                return mailboxId;

            CacheEngine.Clear(UserId);

            return mailboxId;
        }

        public bool SetAccountEmailInFolder(int mailboxId, string emailInFolder)
        {
            if (mailboxId < 0)
                throw new ArgumentNullException("mailboxId");

            bool saved;

            var mailbox = MailDaoFactory.GetMailboxDao().GetMailBox(
                new СoncreteUserMailboxExp(
                    mailboxId,
                    Tenant, UserId)
                );

            if (mailbox == null)
                return false;

            saved = MailDaoFactory.GetMailboxDao().SetMailboxEmailIn(mailbox, emailInFolder);

            if (!saved)
                return saved;

            CacheEngine.Clear(UserId);

            return saved;
        }

        public bool SetAccountsActivity(bool userOnline = true)
        {
            return MailDaoFactory.GetMailboxDao().SetMailboxesActivity(Tenant, UserId, userOnline);
        }

        public List<string> SearchAccountEmails(string searchText)
        {
            var accounts = GetAccountInfoList();
            var emails = new List<string>();

            foreach (var account in accounts)
            {
                var email = string.IsNullOrEmpty(account.Name)
                                ? account.Email
                                : MailUtil.CreateFullEmail(account.Name, account.Email);
                emails.Add(email);

                foreach (var alias in account.Aliases)
                {
                    email = string.IsNullOrEmpty(account.Name)
                                ? account.Email
                                : MailUtil.CreateFullEmail(account.Name, alias.Email);
                    emails.Add(email);
                }

                foreach (var group in account.Groups.Where(group => emails.IndexOf(group.Email) == -1))
                {
                    emails.Add(group.Email);
                }
            }

            return emails.Where(e => e.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1).ToList();
        }

        public string SetDefaultAccount(string email, bool isDefault)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("email");

            email = email.ToLowerInvariant();

            if (isDefault)
            {
                var accounts = GetAccountInfoList();

                var emailExist = false;

                foreach (var account in accounts)
                {
                    if (account.Email == email)
                    {
                        emailExist = true;
                        break;
                    }
                    if (account.Aliases.Any(address => address.Email == email))
                    {
                        emailExist = true;
                    }
                }

                if (!emailExist)
                    throw new ArgumentException("Account not found");
            }

            var settings = new MailBoxAccountSettings { DefaultEmail = isDefault ? email : string.Empty };

            SettingsManager.SaveForCurrentUser(settings);

            return email;
        }
    }
}
