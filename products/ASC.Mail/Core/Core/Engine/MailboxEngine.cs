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

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Mail.Authorization;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao;
using ASC.Mail.Core.Dao.Expressions.Attachment;
using ASC.Mail.Core.Dao.Expressions.Conversation;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Core.Dao.Expressions.Message;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;
using ASC.Mail.Models;
using ASC.Mail.Utils;

using Microsoft.Extensions.Options;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class MailboxEngine : BaseEngine
    {
        private int Tenant => TenantManager.GetCurrentTenant().TenantId;
        private string UserId => SecurityContext.CurrentAccount.ID.ToString();

        private TenantManager TenantManager { get; }
        private SecurityContext SecurityContext { get; }
        private ILog Log { get; }

        private MailDbContext MailDbContext { get; }

        private IMailDaoFactory MailDaoFactory { get; }
        private AlertEngine AlertEngine { get; }
        private MailBoxSettingEngine MailBoxSettingEngine { get; }
        private QuotaEngine QuotaEngine { get; }
        private CacheEngine CacheEngine { get; }
        private IndexEngine IndexEngine { get; }

        public MailboxEngine(
            TenantManager tenantManager,
            SecurityContext securityContext,
            IMailDaoFactory mailDaoFactory,
            AlertEngine alertEngine,
            MailBoxSettingEngine mailBoxSettingEngine,
            QuotaEngine quotaEngine,
            CacheEngine cacheEngine,
            IndexEngine indexEngine,
            IOptionsMonitor<ILog> option,
            MailSettings mailSettings) : base(mailSettings)
        {
            TenantManager = tenantManager;
            SecurityContext = securityContext;
            MailDaoFactory = mailDaoFactory;

            MailDbContext = MailDaoFactory.GetContext();

            AlertEngine = alertEngine;
            MailBoxSettingEngine = mailBoxSettingEngine;
            QuotaEngine = quotaEngine;
            CacheEngine = cacheEngine;
            IndexEngine = indexEngine;

            Log = option.Get("ASC.Mail.MailboxEngine");
        }

        public MailBoxData GetMailboxData(IMailboxExp exp)
        {
            var tuple = GetMailboxFullInfo(exp);
            return tuple == null ? null : tuple.Item1;
        }

        public List<MailBoxData> GetMailboxDataList(IMailboxesExp exp)
        {
            var tuples = GetMailboxFullInfoList(exp);
            return tuples.Select(t => t.Item1).ToList();
        }

        public List<Tuple<MailBoxData, Mailbox>> GetMailboxFullInfoList(IMailboxesExp exp)
        {
            var list = new List<Tuple<MailBoxData, Mailbox>>();

            var mailboxes = MailDaoFactory.GetMailboxDao().GetMailBoxes(exp);

            list.AddRange(mailboxes.Select(GetMailbox).Where(tuple => tuple != null));

            return list;
        }

        public Tuple<MailBoxData, Mailbox> GetMailboxFullInfo(IMailboxExp exp)
        {
            var mailbox = MailDaoFactory.GetMailboxDao().GetMailBox(exp);

            if (mailbox == null)
                return null;

            var tuple = GetMailbox(mailbox);

            return tuple;
        }

        public Tuple<int, int> GetRangeMailboxes(IMailboxExp exp)
        {
            return MailDaoFactory.GetMailboxDao().GetRangeMailboxes(exp);
        }

        public bool TryGetNextMailboxData(IMailboxExp exp, out MailBoxData mailBoxData, out int failedId)
        {
            failedId = -1;

            try
            {
                var mailbox = MailDaoFactory.GetMailboxDao().GetNextMailBox(exp);

                if (mailbox == null)
                {
                    mailBoxData = null;
                    return false;
                }

                var tuple = GetMailbox(mailbox);

                if (tuple == null)
                {
                    Log.WarnFormat("Mailbox id = {0} is not well-formated.", mailbox.Id);

                    mailBoxData = null;
                    failedId = mailbox.Id;
                    return false;
                }

                mailBoxData = tuple.Item1;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("TryGetNextMailboxData failed", ex);
            }

            mailBoxData = null;
            return false;
        }

        public MailBoxData GetDefaultMailboxData(string email, string password,
            AuthorizationServiceType type, bool? imap, bool isNullNeeded)
        {
            var address = new MailAddress(email);

            var host = address.Host.ToLowerInvariant();

            if (type == AuthorizationServiceType.Google) host = DefineConstants.GOOGLE_HOST;

            MailBoxData initialMailbox = null;

            if (imap.HasValue)
            {
                try
                {
                    var settings = MailBoxSettingEngine.GetMailBoxSettings(host);

                    if (settings != null)
                    {
                        var outgoingServerLogin = "";

                        var incommingType = imap.Value ? "imap" : "pop3";

                        var incomingServer =
                            settings.EmailProvider.IncomingServer
                            .FirstOrDefault(serv =>
                                serv.Type
                                .ToLowerInvariant()
                                .Equals(incommingType));

                        var outgoingServer = settings.EmailProvider.OutgoingServer.FirstOrDefault() ?? new ClientConfigEmailProviderOutgoingServer();

                        if (incomingServer != null && !string.IsNullOrEmpty(incomingServer.Username))
                        {
                            var incomingServerLogin = address.ToLogin(incomingServer.Username);

                            if (!string.IsNullOrEmpty(outgoingServer.Username))
                            {
                                outgoingServerLogin = address.ToLogin(outgoingServer.Username);
                            }

                            initialMailbox = new MailBoxData
                            {
                                EMail = address,
                                Name = "",

                                Account = incomingServerLogin,
                                Password = password,
                                Server = host.ToHost(incomingServer.Hostname),
                                Port = incomingServer.Port,
                                Encryption = incomingServer.SocketType.ToEncryptionType(),
                                SmtpEncryption = outgoingServer.SocketType.ToEncryptionType(),
                                Authentication = incomingServer.Authentication.ToSaslMechanism(),
                                SmtpAuthentication = outgoingServer.Authentication.ToSaslMechanism(),
                                Imap = imap.Value,

                                SmtpAccount = outgoingServerLogin,
                                SmtpPassword = password,
                                SmtpServer = host.ToHost(outgoingServer.Hostname),
                                SmtpPort = outgoingServer.Port,
                                Enabled = true,
                                TenantId = Tenant,
                                UserId = UserId,
                                BeginDate = DateTime.UtcNow.Subtract(new TimeSpan(MailBoxData.DefaultMailLimitedTimeDelta)),
                                OAuthType = (byte)type
                            };
                        }
                    }
                }
                catch (Exception)
                {
                    initialMailbox = null;
                }
            }

            if (initialMailbox != null || isNullNeeded)
            {
                return initialMailbox;
            }

            var isImap = imap.GetValueOrDefault(true);
            return new MailBoxData
            {
                EMail = address,
                Name = "",
                Account = email,
                Password = password,
                Server = string.Format((isImap ? "imap.{0}" : "pop.{0}"), host),
                Port = (isImap ? 993 : 110),
                Encryption = isImap ? EncryptionType.SSL : EncryptionType.None,
                SmtpEncryption = EncryptionType.None,
                Imap = isImap,
                SmtpAccount = email,
                SmtpPassword = password,
                SmtpServer = string.Format("smtp.{0}", host),
                SmtpPort = 25,
                Enabled = true,
                TenantId = Tenant,
                UserId = UserId,
                BeginDate = DateTime.UtcNow.Subtract(new TimeSpan(MailBoxData.DefaultMailLimitedTimeDelta)),
                Authentication = SaslMechanism.Login,
                SmtpAuthentication = SaslMechanism.Login
            };
        }

        public MailboxStatus GetMailboxStatus(IMailboxExp exp)
        {
            var status = MailDaoFactory.GetMailboxDao().GetMailBoxStatus(exp);

            return status;
        }

        public bool SaveMailBox(MailBoxData mailbox, AuthorizationServiceType authType = AuthorizationServiceType.None)
        {
            if (mailbox == null)
                throw new ArgumentNullException("mailbox");

            if (mailbox.IsTeamlab)
                throw new ArgumentException("Mailbox with specified email can't be updated");

            using var tx = MailDbContext.Database.BeginTransaction();

            var existingMailbox = MailDaoFactory.GetMailboxDao().GetMailBox(
                new СoncreteUserMailboxExp(
                    mailbox.EMail,
                    mailbox.TenantId, mailbox.UserId));

            int newInServerId, newOutServerId;

            var mailboxId = 0;
            var dateCreated = DateTime.UtcNow;
            var enabled = true;
            var host = authType == AuthorizationServiceType.Google ? DefineConstants.GOOGLE_HOST : mailbox.EMail.Host;

            // Get new imap/pop3 server from MailBoxData
            var newInServer = new MailboxServer
            {
                Hostname = mailbox.Server,
                Port = mailbox.Port,
                Type = mailbox.Imap ? DefineConstants.IMAP : DefineConstants.POP3,
                Username = mailbox.EMail.ToLoginFormat(mailbox.Account) ?? mailbox.Account,
                SocketType = mailbox.Encryption.ToNameString(),
                Authentication = mailbox.Authentication.ToNameString()
            };

            // Get new smtp server from MailBoxData
            var newOutServer = new MailboxServer
            {
                Hostname = mailbox.SmtpServer,
                Port = mailbox.SmtpPort,
                Type = DefineConstants.SMTP,
                Username =
                    mailbox.SmtpAuthentication != SaslMechanism.None
                        ? mailbox.EMail.ToLoginFormat(mailbox.SmtpAccount) ?? mailbox.SmtpAccount
                        : "",
                SocketType = mailbox.SmtpEncryption.ToNameString(),
                Authentication = mailbox.SmtpAuthentication.ToNameString()
            };

            if (existingMailbox != null)
            {
                mailboxId = existingMailbox.Id;
                enabled = existingMailbox.Enabled;
                dateCreated = existingMailbox.DateCreated;

                // Get existing settings by existing ids
                var dbInServer = MailDaoFactory.GetMailboxServerDao().GetServer(existingMailbox.ServerId);
                var dbOutServer = MailDaoFactory.GetMailboxServerDao().GetServer(existingMailbox.SmtpServerId);

                // Compare existing settings with new
                if (!dbInServer.Equals(newInServer) || !dbOutServer.Equals(newOutServer))
                {
                    var domain = MailDaoFactory.GetMailboxDomainDao().GetDomain(host);

                    List<MailboxServer> trustedServers = null;
                    if (domain != null)
                        trustedServers = MailDaoFactory.GetMailboxServerDao().GetServers(domain.ProviderId);

                    newInServerId = GetMailboxServerId(dbInServer, newInServer, trustedServers);
                    newOutServerId = GetMailboxServerId(dbOutServer, newOutServer,
                        trustedServers);
                }
                else
                {
                    newInServerId = existingMailbox.ServerId;
                    newOutServerId = existingMailbox.SmtpServerId;
                }
            }
            else
            {
                //Find settings by host

                var domain = MailDaoFactory.GetMailboxDomainDao().GetDomain(host);

                if (domain != null)
                {
                    //Get existing servers with isUserData = 0
                    var trustedServers = MailDaoFactory.GetMailboxServerDao().GetServers(domain.ProviderId);

                    //Compare existing settings with new

                    var foundInServer = trustedServers.FirstOrDefault(ts => ts.Equals(newInServer));
                    var foundOutServer = trustedServers.FirstOrDefault(ts => ts.Equals(newOutServer));

                    //Use existing or save new servers
                    newInServerId = foundInServer != null
                        ? foundInServer.Id
                        : SaveMailboxServer(newInServer, domain.ProviderId);

                    newOutServerId = foundOutServer != null
                        ? foundOutServer.Id
                        : SaveMailboxServer(newOutServer, domain.ProviderId);
                }
                else
                {
                    //Save new servers
                    var newProvider = new MailboxProvider
                    {
                        Id = 0,
                        Name = host,
                        DisplayShortName = "",
                        DisplayName = "",
                        Url = ""
                    };

                    newProvider.Id = MailDaoFactory.GetMailboxProviderDao().SaveProvider(newProvider);

                    var newDomain = new MailboxDomain
                    {
                        Id = 0,
                        Name = host,
                        ProviderId = newProvider.Id
                    };

                    MailDaoFactory.GetMailboxDomainDao().SaveDomain(newDomain);

                    newInServerId = SaveMailboxServer(newInServer, newProvider.Id);
                    newOutServerId = SaveMailboxServer(newOutServer, newProvider.Id);
                }
            }

            var loginDelayTime = GetLoginDelayTime(mailbox);

            //Save Mailbox to DB
            var mb = new Mailbox
            {
                Id = mailboxId,
                Tenant = mailbox.TenantId,
                User = mailbox.UserId,
                Address = mailbox.EMail.Address.ToLowerInvariant(),
                Name = mailbox.Name,
                Password = mailbox.Password,
                MsgCountLast = mailbox.MessagesCount,
                SmtpPassword = mailbox.SmtpPassword,
                SizeLast = mailbox.Size,
                LoginDelay = loginDelayTime,
                Enabled = enabled,
                Imap = mailbox.Imap,
                BeginDate = mailbox.BeginDate,
                OAuthType = mailbox.OAuthType,
                OAuthToken = mailbox.OAuthToken,
                ServerId = newInServerId,
                SmtpServerId = newOutServerId,
                DateCreated = dateCreated
            };

            var mailBoxId = MailDaoFactory.GetMailboxDao().SaveMailBox(mb);

            mailbox.MailBoxId = mailBoxId;

            if (mailBoxId < 1)
            {
                tx.Rollback();
                return false;
            }

            tx.Commit();

            return true;
        }

        public List<MailBoxData> GetMailboxesForProcessing(MailSettings mailSettings, int needTasks)
        {
            var mailboxes = new List<MailBoxData>();

            var boundaryRatio = !(mailSettings.InactiveMailboxesRatio > 0 && mailSettings.InactiveMailboxesRatio < 100);

            if (needTasks > 1 || boundaryRatio)
            {
                var inactiveCount = (int)Math.Round(needTasks * mailSettings.InactiveMailboxesRatio / 100, MidpointRounding.AwayFromZero);

                var activeCount = needTasks - inactiveCount;

                if (activeCount == needTasks)
                {
                    mailboxes.AddRange(GetActiveMailboxesForProcessing(mailSettings, activeCount));
                }
                else if (inactiveCount == needTasks)
                {
                    mailboxes.AddRange(GetInactiveMailboxesForProcessing(mailSettings, inactiveCount));
                }
                else
                {
                    mailboxes.AddRange(GetActiveMailboxesForProcessing(mailSettings, activeCount));

                    var difference = inactiveCount + activeCount - mailboxes.Count;

                    if (difference > 0)
                        mailboxes.AddRange(GetInactiveMailboxesForProcessing(mailSettings, difference));
                }
            }
            else
            {
                mailboxes.AddRange(GetActiveMailboxesForProcessing(mailSettings, 1));

                var difference = needTasks - mailboxes.Count;

                if (difference > 0)
                    mailboxes.AddRange(GetInactiveMailboxesForProcessing(mailSettings, difference));
            }


            foreach (var box in mailboxes)
            {
                Log.Debug($"{box.MailBoxId}:\tIsEnabled={box.Enabled}\tIsRemoved={box.IsRemoved}\tTenant={box.TenantId}\tId={box.UserId}");
            }

            return mailboxes;
        }

        public bool LockMaibox(int id)
        {
            var status = MailDaoFactory.GetMailboxDao().SetMailboxInProcess(id);

            return status;
        }

        public bool ReleaseMaibox(MailBoxData account, MailSettings mailSettings)
        {
            var disableMailbox = false;

            if (account.AuthErrorDate.HasValue)
            {
                var difference = DateTime.UtcNow - account.AuthErrorDate.Value;

                if (difference > mailSettings.AuthErrorDisableMailboxTimeout)
                {
                    disableMailbox = true;

                    AlertEngine.CreateAuthErrorDisableAlert(account.TenantId, account.UserId,
                        account.MailBoxId);
                }
                else if (difference > mailSettings.AuthErrorWarningTimeout)
                {
                    AlertEngine.CreateAuthErrorWarningAlert(account.TenantId, account.UserId,
                        account.MailBoxId);
                }
            }

            if (account.QuotaErrorChanged)
            {
                if (account.QuotaError)
                {
                    AlertEngine.CreateQuotaErrorWarningAlert(account.TenantId, account.UserId);
                }
                else
                {
                    AlertEngine.DeleteAlert(MailAlertTypes.QuotaError);
                }
            }

            var exp = new СoncreteUserMailboxExp(account.MailBoxId, account.TenantId, account.UserId);

            var mailbox = MailDaoFactory.GetMailboxDao().GetMailBox(exp);

            if (mailbox == null) // Mailbox has been removed
                return true;

            bool? enabled = null;
            int? messageCount = null;
            long? size = null;
            bool? quotaError = null;
            string oAuthToken = null;
            string imapIntervalsJson = null;
            bool? resetImapIntervals = null;

            if (account.AuthErrorDate.HasValue)
            {
                if (disableMailbox)
                {
                    enabled = false;
                }
            }

            if (mailbox.MsgCountLast != account.MessagesCount)
            {
                messageCount = account.MessagesCount;
            }

            if (mailbox.SizeLast != account.Size)
            {
                size = account.Size;
            }

            if (account.QuotaErrorChanged)
            {
                quotaError = account.QuotaError;
            }

            if (account.AccessTokenRefreshed)
            {
                oAuthToken = account.OAuthToken;
            }

            if (account.Imap && account.ImapFolderChanged)
            {
                if (account.BeginDateChanged)
                {
                    resetImapIntervals = true;
                }
                else
                {
                    imapIntervalsJson = account.ImapIntervalsJson;
                }
            }

            return MailDaoFactory.GetMailboxDao().SetMailboxProcessed(mailbox, account.ServerLoginDelay, enabled, messageCount, size,
                quotaError, oAuthToken, imapIntervalsJson, resetImapIntervals);
        }

        public bool SetMaiboxAuthError(int id, DateTime? authErroDate)
        {
            return MailDaoFactory.GetMailboxDao().SetMailboxAuthError(id, authErroDate);
        }

        public List<int> ReleaseMailboxes(int timeoutInMinutes)
        {
            return MailDaoFactory.GetMailboxDao().SetMailboxesProcessed(timeoutInMinutes);
        }

        public List<Tuple<int, string>> GetMailUsers(IMailboxExp exp)
        {
            return MailDaoFactory.GetMailboxDao().GetMailUsers(exp);
        }

        public bool DisableMailboxes(IMailboxExp exp)
        {
            return MailDaoFactory.GetMailboxDao().Enable(exp, false);
        }

        public bool LoggedDisableMailboxes(IMailboxExp exp, ILog log)
        {
            return MailDaoFactory.GetMailboxDao().LoggedEnable(exp, false, log);
        }

        public bool SetNextLoginDelay(IMailboxExp exp, TimeSpan delay)
        {
            return MailDaoFactory.GetMailboxDao().SetNextLoginDelay(exp, delay);
        }

        public void RemoveMailBox(MailBoxData mailbox, bool needRecalculateFolders = true)
        {
            if (mailbox.MailBoxId <= 0)
                throw new Exception("MailBox id is 0");

            long freedQuotaSize;

            using (var tx = MailDaoFactory.BeginTransaction())
            {
                if (mailbox.MailBoxId <= 0)
                    throw new Exception("MailBox id is 0");

                freedQuotaSize = RemoveMailBoxInfo(mailbox);

                QuotaEngine.QuotaUsedDelete(freedQuotaSize);

                if (!needRecalculateFolders)
                    return;

                //TODO: Fix OperationEngine.RecalculateFolders();

                tx.Commit();
            }

            QuotaEngine.QuotaUsedDelete(freedQuotaSize);

            CacheEngine.Clear(mailbox.UserId);

            IndexEngine.Remove(mailbox);

            if (!needRecalculateFolders)
                return;

            //TODO: Fix OperationEngine.RecalculateFolders();
        }

        /// <summary>
        /// Set mailbox removed
        /// </summary>
        /// <param name="mailBoxData"></param>
        /// <returns>Return freed quota value</returns>
        public long RemoveMailBoxInfo(MailBoxData mailBoxData)
        {
            long totalAttachmentsSize;

            //TODO: Check timeout on big mailboxes
            //using (var db = new DbManager(Defines.CONNECTION_STRING_NAME, Defines.RemoveMailboxTimeout))

            using (var tx = MailDaoFactory.BeginTransaction())
            {
                if (mailBoxData.MailBoxId <= 0)
                    throw new Exception("MailBox id is 0");

                var mailbox = MailDaoFactory.GetMailboxDao().GetMailBox(
                    new СoncreteUserMailboxExp(mailBoxData.MailBoxId, mailBoxData.TenantId, mailBoxData.UserId, null));

                if (mailbox == null)
                {
                    throw new Exception(string.Format("MailBox with id = {0} (Tenant={1}, User='{2}') not found",
                        mailBoxData.MailBoxId, mailBoxData.TenantId, mailBoxData.UserId));
                }

                MailDaoFactory.GetMailboxDao().SetMailboxRemoved(mailbox);

                var folderTypes = Enum.GetValues(typeof(FolderType)).Cast<int>().ToList();

                var exp = SimpleConversationsExp.CreateBuilder(mailBoxData.TenantId, mailBoxData.UserId)
                        .SetFoldersIds(folderTypes)
                        .SetMailboxId(mailBoxData.MailBoxId)
                        .Build();

                MailDaoFactory.GetChainDao().Delete(exp);

                MailDaoFactory.GetCrmLinkDao().RemoveCrmLinks(mailBoxData.MailBoxId);

                var exp1 = SimpleMessagesExp.CreateBuilder(mailBoxData.TenantId, mailBoxData.UserId)
                        .SetMailboxId(mailBoxData.MailBoxId)
                        .Build();

                MailDaoFactory.GetMailInfoDao().SetFieldValue(exp1,
                    "IsRemoved",
                    true);

                var exp2 = new ConcreteMailboxAttachmentsExp(mailBoxData.MailBoxId, mailBoxData.TenantId, mailBoxData.UserId,
                    onlyEmbedded: null);

                totalAttachmentsSize = MailDaoFactory.GetAttachmentDao().GetAttachmentsSize(exp2);

                MailDaoFactory.GetAttachmentDao().SetAttachmnetsRemoved(exp2);

                var tagIds = MailDaoFactory.GetTagMailDao().GetTagIds(mailBoxData.MailBoxId);

                MailDaoFactory.GetTagMailDao().DeleteByMailboxId(mailBoxData.MailBoxId);

                foreach (var tagId in tagIds)
                {
                    var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

                    if (tag == null)
                        continue;

                    var count = MailDaoFactory.GetTagMailDao().CalculateTagCount(tag.Id);

                    tag.Count = count;

                    MailDaoFactory.GetTagDao().SaveTag(tag);
                }

                MailDaoFactory.GetMailboxSignatureDao()
                    .DeleteSignature(mailBoxData.MailBoxId);

                MailDaoFactory.GetMailboxAutoreplyDao()
                    .DeleteAutoreply(mailBoxData.MailBoxId);

                MailDaoFactory.GetMailboxAutoreplyHistoryDao()
                    .DeleteAutoreplyHistory(mailBoxData.MailBoxId);

                MailDaoFactory.GetAlertDao()
                    .DeleteAlerts(mailBoxData.MailBoxId);

                MailDaoFactory.GetUserFolderXMailDao()
                    .RemoveByMailbox(mailBoxData.MailBoxId);

                tx.Commit();
            }

            return totalAttachmentsSize;
        }

        private List<MailBoxData> GetActiveMailboxesForProcessing(MailSettings mailSettings, int tasksLimit)
        {
            if (tasksLimit <= 0)
                return new List<MailBoxData>();

            Log.Debug("GetActiveMailboxForProcessing()");

            var mailboxes = GetMailboxDataList(new MailboxesForProcessingExp(mailSettings, tasksLimit, true));

            Log.DebugFormat("Found {0} active tasks", mailboxes.Count);

            return mailboxes;
        }

        private IEnumerable<MailBoxData> GetInactiveMailboxesForProcessing(MailSettings mailSettings, int tasksLimit)
        {
            if (tasksLimit <= 0)
                return new List<MailBoxData>();

            Log.Debug("GetInactiveMailboxForProcessing()");

            var mailboxes = GetMailboxDataList(new MailboxesForProcessingExp(mailSettings, tasksLimit, false));

            Log.DebugFormat("Found {0} inactive tasks", mailboxes.Count);

            return mailboxes;
        }

        private int GetMailboxServerId(MailboxServer dbServer,
            MailboxServer newServer, List<MailboxServer> trustedServers)
        {
            int serverId;

            if (!dbServer.Equals(newServer))
            {
                // Server settings have been changed
                if (dbServer.IsUserData)
                {
                    if (trustedServers != null)
                    {
                        var foundInServer = trustedServers.FirstOrDefault(ts => ts.Equals(newServer));
                        if (foundInServer != null)
                        {
                            MailDaoFactory.GetMailboxServerDao().DeleteServer(dbServer.Id);
                            newServer.Id = foundInServer.Id;
                            newServer.IsUserData = false;
                        }
                        else
                        {
                            newServer.Id = dbServer.Id;
                            newServer.Id = SaveMailboxServer(newServer, dbServer.ProviderId);
                        }
                    }
                    else
                    {
                        newServer.Id = dbServer.Id;
                        newServer.Id = SaveMailboxServer(newServer, dbServer.ProviderId);
                    }
                }
                else
                {
                    if (trustedServers != null)
                    {
                        var foundInServer = trustedServers.FirstOrDefault(ts => ts.Equals(newServer));
                        if (foundInServer != null)
                        {
                            newServer.Id = foundInServer.Id;
                            newServer.IsUserData = false;
                        }
                        else
                        {
                            newServer.Id = SaveMailboxServer(newServer, dbServer.ProviderId);
                        }
                    }
                    else
                    {
                        newServer.Id = SaveMailboxServer(newServer, dbServer.ProviderId);

                    }
                }

                serverId = newServer.Id;
            }
            else
            {
                serverId = dbServer.Id;
            }

            return serverId;
        }

        private int SaveMailboxServer(MailboxServer server,
            int providerId)
        {
            server.IsUserData = true;
            server.ProviderId = providerId;
            return MailDaoFactory.GetMailboxServerDao().SaveServer(server);
        }

        private static int GetLoginDelayTime(MailBoxData mailbox)
        {
            //Todo: This hardcode inserted because pop3.live.com doesn't support CAPA command.
            //Right solution for that collision type:
            //1) Create table in DB: mail_login_delays. With REgexs and delays
            //1.1) Example of mail_login_delays data:
            //    .*@outlook.com    900
            //    .*@hotmail.com    900
            //    .*                30
            //1.2) Load this table to aggregator cache. Update it on changing.
            //1.3) Match email addreess of account with regexs from mail_login_delays
            //1.4) If email matched then set delay from that record.
            if (mailbox.Server == "pop3.live.com")
                return DefineConstants.HARDCODED_LOGIN_TIME_FOR_MS_MAIL;

            return mailbox.ServerLoginDelay < MailBoxData.DefaultServerLoginDelay
                       ? MailBoxData.DefaultServerLoginDelay
                       : mailbox.ServerLoginDelay;
        }

        private Tuple<MailBoxData, Mailbox> GetMailbox(Mailbox mailbox)
        {
            var inServer = MailDaoFactory.GetMailboxServerDao().GetServer(mailbox.ServerId);

            if (inServer == null)
                return null;

            var outServer = MailDaoFactory.GetMailboxServerDao().GetServer(mailbox.SmtpServerId);

            if (outServer == null)
                return null;

            var autoreply = MailDaoFactory.GetMailboxAutoreplyDao().GetAutoreply(mailbox);

            return new Tuple<MailBoxData, Mailbox>(ToMailBoxData(mailbox, inServer, outServer, autoreply), mailbox);
        }

        public static MailBoxData ToMailBoxData(Mailbox mailbox, MailboxServer inServer, MailboxServer outServer,
            MailboxAutoreply autoreply)
        {
            var address = new MailAddress(mailbox.Address);

            var mailAutoReply = autoreply != null
                ? new MailAutoreplyData(autoreply.MailboxId, autoreply.Tenant, autoreply.TurnOn, autoreply.OnlyContacts,
                    autoreply.TurnOnToDate, autoreply.FromDate, autoreply.ToDate, autoreply.Subject, autoreply.Html)
                : null;

            var inServerOldFormat = string.Format("{0}:{1}", inServer.Hostname, inServer.Port);
            var outServerOldFormat = string.Format("{0}:{1}", outServer.Hostname, outServer.Port);

            var mailboxData = new MailBoxData(mailbox.Tenant, mailbox.User, mailbox.Id, mailbox.Name, address,
                address.ToLogin(inServer.Username), mailbox.Password, inServerOldFormat,
                inServer.SocketType.ToEncryptionType(), inServer.Authentication.ToSaslMechanism(), mailbox.Imap,
                address.ToLogin(outServer.Username), mailbox.SmtpPassword, outServerOldFormat,
                outServer.SocketType.ToEncryptionType(), outServer.Authentication.ToSaslMechanism(),
                Convert.ToByte(mailbox.OAuthType), mailbox.OAuthToken)
            {
                Size = mailbox.SizeLast,
                MessagesCount = mailbox.MsgCountLast,
                ServerLoginDelay = mailbox.LoginDelay,
                BeginDate = mailbox.BeginDate,
                QuotaError = mailbox.QuotaError,
                AuthErrorDate = mailbox.DateAuthError,
                ImapIntervalsJson = mailbox.ImapIntervals,
                SmtpServerId = mailbox.SmtpServerId,
                InServerId = mailbox.ServerId,
                EMailInFolder = mailbox.EmailInFolder,
                MailAutoreply = mailAutoReply,
                AccessTokenRefreshed = false, //TODO: ???

                Enabled = mailbox.Enabled,
                IsRemoved = mailbox.IsRemoved,
                IsTeamlab = mailbox.IsTeamlabMailbox
            };

            return mailboxData;
        }
    }
}