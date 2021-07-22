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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Common.Threading;
using ASC.Core;
using ASC.Core.Tenants;
using ASC.Data.Storage;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Extensions;
using ASC.Mail.Iterators;
using ASC.Mail.Models;
using ASC.Mail.Storage;
using ASC.Mail.Utils;

using Microsoft.Extensions.Options;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class MailGarbageEngine : BaseEngine, IDisposable
    {
        private static MemoryCache TenantMemCache { get; set; }
        private SecurityContext SecurityContext { get; }
        private TenantManager TenantManager { get; }
        private UserManager UserManager { get; }
        private IMailDaoFactory MailDaoFactory { get; }
        private MailboxEngine MailboxEngine { get; }
        private ServerMailboxEngine ServerMailboxEngine { get; }
        private ServerDomainEngine ServerDomainEngine { get; }
        private UserFolderEngine UserFolderEngine { get; }
        private OperationEngine OperationEngine { get; }
        private ApiHelper ApiHelper { get; }
        private StorageFactory StorageFactory { get; }
        private static TaskFactory TaskFactory { get; set; }
        private static object Locker { get; set; }
        private ILog Log { get; }

        public MailGarbageEngine(
            SecurityContext securityContext,
            TenantManager tenantManager,
            UserManager userManager,
            IMailDaoFactory mailDaoFactory,
            MailboxEngine mailboxEngine,
            ServerMailboxEngine serverMailboxEngine,
            ServerDomainEngine serverDomainEngine,
            UserFolderEngine userFolderEngine,
            OperationEngine operationEngine,
            ApiHelper apiHelper,
            StorageFactory storageFactory,
            //MailGarbageEraserConfig config, //TODO: think about setup config
            MailSettings mailSettings,
            IOptionsMonitor<ILog> option) : base(mailSettings)
        {
            SecurityContext = securityContext;
            TenantManager = tenantManager;
            UserManager = userManager;
            MailDaoFactory = mailDaoFactory;
            MailboxEngine = mailboxEngine;
            ServerMailboxEngine = serverMailboxEngine;
            ServerDomainEngine = serverDomainEngine;
            UserFolderEngine = userFolderEngine;
            OperationEngine = operationEngine;
            ApiHelper = apiHelper;
            StorageFactory = storageFactory;

            //Config = MailGarbageEraserConfig.FromConfig();

            Log = option.Get("ASC.Mail.GarbageEngine");

            TenantMemCache = new MemoryCache("GarbageEraserTenantCache");

            var scheduler = new LimitedConcurrencyLevelTaskScheduler(MailSettings.MaxTasksAtOnce);

            TaskFactory = new TaskFactory(scheduler);

            Locker = new object();
        }

        #region - Public methods -

        public void ClearMailGarbage(CancellationToken cancelToken)
        {
            Log.Debug("Begin ClearMailGarbage()");

            var tasks = new List<Task>();

            var mailboxIterator = new MailboxIterator(MailboxEngine, isRemoved: null, log: Log);

            var mailbox = mailboxIterator.First();

            while (!mailboxIterator.IsDone)
            {
                try
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    var mb = mailbox;

                    var task = Queue(() => ClearGarbage(mb), cancelToken);

                    tasks.Add(task);

                    if (tasks.Count == MailSettings.MaxTasksAtOnce)
                    {
                        Log.Info("Wait all tasks to complete");

                        Task.WaitAll(tasks.ToArray());

                        tasks = new List<Task>();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                if (!cancelToken.IsCancellationRequested)
                {
                    mailbox = mailboxIterator.Next();
                    continue;
                }

                Log.Debug("ClearMailGarbage: IsCancellationRequested. Quit.");
                break;
            }

            RemoveUselessMsDomains();

            Log.Debug("End ClearMailGarbage()\r\n");
        }

        public void RemoveUselessMsDomains()
        {
            Log.Debug("Start RemoveUselessMsDomains()\r\n");

            try
            {
                var domains = ServerDomainEngine.GetAllDomains();

                foreach (var domain in domains)
                {
                    if (domain.Tenant == -1)
                        continue;

                    var status = GetTenantStatus(domain.Tenant);

                    if (status != TenantStatus.RemovePending)
                        continue;

                    var exp = new TenantServerMailboxesExp(domain.Tenant, null);

                    var mailboxes = MailboxEngine.GetMailboxDataList(exp);

                    if (mailboxes.Any())
                    {
                        Log.WarnFormat("Domain's '{0}' Tenant={1} is removed, but it has unremoved server mailboxes (count={2}). Skip it.",
                            domain.Name, domain.Tenant, mailboxes.Count);

                        continue;
                    }

                    Log.InfoFormat("Domain's '{0}' Tenant={1} is removed. Lets remove domain.", domain.Name, domain.Tenant);

                    var count = domains.Count(d => d.Name.Equals(domain.Name, StringComparison.InvariantCultureIgnoreCase));

                    var skipMS = count > 1;

                    if (skipMS)
                    {
                        Log.InfoFormat("Domain's '{0}' has duplicated entry for another tenant. Remove only current entry.", domain.Name);
                    }

                    RemoveDomain(domain, skipMS);
                }

            }
            catch (Exception ex)
            {
                Log.Error(string.Format("RemoveUselessMsDomains failed. Exception: {0}", ex.ToString()));
            }

            Log.Debug("End RemoveUselessMsDomains()\r\n");
        }

        public TenantStatus GetTenantStatus(int tenant)
        {
            try
            {
                TenantManager.SetCurrentTenant(tenant);

                var tenantInfo = TenantManager.GetCurrentTenant();

                return tenantInfo.Status;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTenantStatus(tenant='{tenant}') failed. Exception: {ex}");
            }

            return TenantStatus.Active;
        }

        public void RemoveDomain(Entities.ServerDomain domain, bool skipMS = false)
        {
            try
            {
                using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    MailDaoFactory.GetServerDomainDao().Delete(domain.Id);

                    if (!skipMS)
                    {

                        var server = MailDaoFactory.GetServerDao().Get(domain.Tenant);

                        if (server == null)
                            throw new Exception(string.Format("Information for Tenant's Mail Server not found (Tenant = {0})", domain.Tenant));

                        var serverEngine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

                        serverEngine.RemoveDomain(domain.Name);
                    }

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("RemoveDomainIfUseless(Domain: '{0}', ID='{1}') failed. Exception: {2}", domain.Name, domain.Id, ex.ToString()));
            }
        }

        public void ClearUserMail(Guid userId, Tenant tenantId = null)
        {
            var tenant = tenantId != null ? tenantId.TenantId : TenantManager.GetCurrentTenant().TenantId;

            Log.InfoFormat("ClearUserMail(userId: '{0}' tenant: {1})", userId, tenant);

            var user = userId.ToString();

            RemoveUserFolders(Log);

            RemoveUserMailboxes(tenant, user, Log);

            //TODO: RemoveUserTags

            //TODO: RemoveUserContacts

            //TODO: RemoveUserAlerts

            //TODO: RemoveUserDisplayImagesAddresses

            //TODO: RemoveUserFolderCounters
        }

        #endregion

        #region - Private methods -

        private Task Queue(Action action, CancellationToken cancelToken)
        {
            var task = TaskFactory.StartNew(action, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            task.ConfigureAwait(false)
                .GetAwaiter()
                .OnCompleted(() =>
                {
                    Log.DebugFormat("End Task {0} with status = '{1}'.", task.Id, task.Status);
                });

            return task;
        }

        private bool NeedRemove(MailBoxData mailbox, ILog taskLog)
        {
            var needRemove = false;

            lock (Locker)
            {
                DefineConstants.TariffType type;

                var memTenantItem = TenantMemCache.Get(mailbox.TenantId.ToString(CultureInfo.InvariantCulture));

                if (memTenantItem == null)
                {
                    taskLog.InfoFormat("Tenant {0} isn't in cache", mailbox.TenantId);

                    taskLog.DebugFormat("GetTenantStatus(OverdueDays={0})", MailSettings.TenantOverdueDays);

                    type = mailbox.GetTenantStatus(TenantManager, SecurityContext, ApiHelper, (int)MailSettings.TenantOverdueDays, Log);

                    var cacheItem = new CacheItem(mailbox.TenantId.ToString(CultureInfo.InvariantCulture), type);

                    var cacheItemPolicy = new CacheItemPolicy
                    {
                        AbsoluteExpiration =
                            DateTimeOffset.UtcNow.AddDays((int)MailSettings.CleanerTenantCacheDays)
                    };

                    TenantMemCache.Add(cacheItem, cacheItemPolicy);
                }
                else
                {
                    taskLog.InfoFormat("Tenant {0} is in cache", mailbox.TenantId);

                    type = (DefineConstants.TariffType)memTenantItem;
                }

                taskLog.InfoFormat("Tenant {0} has status '{1}'", mailbox.TenantId, type.ToString());

                if (type == DefineConstants.TariffType.LongDead)
                {
                    needRemove = true;
                }
                else
                {
                    var isUserRemoved = mailbox.IsUserRemoved(TenantManager, UserManager);

                    taskLog.InfoFormat("User '{0}' status is '{1}'", mailbox.UserId, isUserRemoved ? "Terminated" : "Not terminated");

                    if (isUserRemoved)
                    {
                        needRemove = true;
                    }
                }

            }

            return needRemove;
        }

        private void ClearGarbage(MailBoxData mailbox)
        {
            Log.InfoFormat("Processing MailboxId = {0}, email = '{1}', tenant = '{2}', user = '{3}'",
                mailbox.MailBoxId, mailbox.EMail.Address, mailbox.TenantId, mailbox.UserId);

            try
            {
                if (NeedRemove(mailbox, Log))
                {
                    RemoveMailboxData(mailbox, true, Log);
                }
                else if (mailbox.IsRemoved)
                {
                    Log.Info("Mailbox is removed.");
                    RemoveMailboxData(mailbox, false, Log);
                }
                else
                {
                    RemoveGarbageMailData(mailbox, (int)MailSettings.CleanerGarbageOverdueDays, Log);
                }

                Log.InfoFormat("Mailbox {0} processing complete.", mailbox.MailBoxId);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Mailbox {0} processed with error : {1}", mailbox.MailBoxId, ex.ToString());
            }
        }

        private void RemoveMailboxData(MailBoxData mailbox, bool totalMailRemove, ILog log)
        {
            log.InfoFormat("RemoveMailboxData(id: {0} address: {1})", mailbox.MailBoxId, mailbox.EMail.ToString());

            try
            {
                if (!mailbox.IsRemoved)
                {
                    log.Info("Mailbox is't removed.");

                    var needRecalculateFolders = !totalMailRemove;

                    if (mailbox.IsTeamlab)
                    {
                        log.Info("RemoveTeamlabMailbox()");

                        TenantManager.SetCurrentTenant(mailbox.TenantId);
                        SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

                        RemoveTeamlabMailbox(mailbox, log);
                    }

                    log.Info("SetMailboxRemoved()");

                    MailboxEngine.RemoveMailBox(mailbox, needRecalculateFolders);

                    mailbox.IsRemoved = true;
                }

                log.DebugFormat("MailDataStore.GetDataStore(Tenant = {0})", mailbox.TenantId);

                var dataStorage = StorageFactory.GetMailStorage(mailbox.TenantId);

                dataStorage.QuotaController = null;

                log.Debug("GetMailboxAttachsCount()");


                var countAttachs = MailDaoFactory.GetMailGarbageDao().GetMailboxAttachsCount(mailbox);

                log.InfoFormat("Found {0} garbage attachments", countAttachs);

                if (countAttachs > 0)
                {
                    var sumCount = 0;

                    log.DebugFormat("GetMailboxAttachsGarbage(limit = {0})", MailSettings.CleanerMaxFilesToRemoveAtOnce);

                    var attachGrbgList = MailDaoFactory.GetMailGarbageDao().GetMailboxAttachs(mailbox, (int)MailSettings.CleanerMaxFilesToRemoveAtOnce);

                    sumCount += attachGrbgList.Count;

                    log.InfoFormat("Clearing {0} garbage attachments ({1}/{2})", attachGrbgList.Count, sumCount, countAttachs);

                    while (attachGrbgList.Any())
                    {
                        foreach (var attachGrbg in attachGrbgList)
                        {
                            RemoveFile(dataStorage, attachGrbg.Path, log);
                        }

                        log.Debug("CleanupMailboxAttachs()");

                        MailDaoFactory.GetMailGarbageDao().CleanupMailboxAttachs(attachGrbgList);

                        log.Debug("GetMailboxAttachs()");

                        attachGrbgList = MailDaoFactory.GetMailGarbageDao().GetMailboxAttachs(mailbox, (int)MailSettings.CleanerMaxFilesToRemoveAtOnce);

                        if (!attachGrbgList.Any()) continue;

                        sumCount += attachGrbgList.Count;

                        log.InfoFormat("Found {0} garbage attachments ({1}/{2})", attachGrbgList.Count, sumCount,
                                 countAttachs);
                    }
                }

                log.Debug("GetMailboxMessagesCount()");

                var countMessages = MailDaoFactory.GetMailGarbageDao().GetMailboxMessagesCount(mailbox);

                log.InfoFormat("Found {0} garbage messages", countMessages);

                if (countMessages > 0)
                {
                    var sumCount = 0;

                    log.DebugFormat("GetMailboxMessagesGarbage(limit = {0})", MailSettings.CleanerMaxFilesToRemoveAtOnce);

                    var messageGrbgList = MailDaoFactory.GetMailGarbageDao().GetMailboxMessages(mailbox, (int)MailSettings.CleanerMaxFilesToRemoveAtOnce);

                    sumCount += messageGrbgList.Count;

                    log.InfoFormat("Clearing {0} garbage messages ({1}/{2})", messageGrbgList.Count, sumCount, countMessages);

                    while (messageGrbgList.Any())
                    {
                        foreach (var mailMessageGarbage in messageGrbgList)
                        {
                            RemoveFile(dataStorage, mailMessageGarbage.Path, log);
                        }

                        log.Debug("CleanupMailboxMessages()");

                        MailDaoFactory.GetMailGarbageDao().CleanupMailboxMessages(messageGrbgList);

                        log.Debug("GetMailboxMessages()");

                        messageGrbgList = MailDaoFactory.GetMailGarbageDao().GetMailboxMessages(mailbox, (int)MailSettings.CleanerMaxFilesToRemoveAtOnce);

                        if (!messageGrbgList.Any()) continue;

                        sumCount += messageGrbgList.Count;

                        log.InfoFormat("Found {0} garbage messages ({1}/{2})", messageGrbgList.Count, sumCount,
                                 countMessages);
                    }
                }

                log.Debug("ClearMailboxData()");

                CleanupMailboxData(mailbox, totalMailRemove);

                log.DebugFormat("Garbage mailbox '{0}' was totaly removed.", mailbox.EMail.Address);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("RemoveMailboxData(mailboxId = {0}) Failure\r\nException: {1}", mailbox.MailBoxId, ex.ToString());

                throw;
            }
        }

        public void CleanupMailboxData(MailBoxData mailbox, bool totalRemove)
        {
            if (!mailbox.IsRemoved)
                throw new Exception("Mailbox is not removed.");

            var MailDbContext = MailDaoFactory.GetContext();

            using var tx = MailDaoFactory.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);

            var exp = new СoncreteUserMailboxExp(mailbox.MailBoxId, mailbox.TenantId, mailbox.UserId, true);

            var mb = MailDaoFactory.GetMailboxDao().GetMailBox(exp);

            var deleteMailboxMessagesQuery = MailDbContext.MailMail
                .Where(m => m.IdMailbox == mb.Id && m.TenantId == mb.Tenant && m.IdUser == mb.User);

            MailDbContext.MailMail.RemoveRange(deleteMailboxMessagesQuery);

            MailDbContext.SaveChanges();

            var deleteMailboxAttachmentsQuery = MailDbContext.MailAttachment
                .Where(a => a.IdMailbox == mb.Id && a.Tenant == mb.Tenant);

            MailDbContext.MailAttachment.RemoveRange(deleteMailboxAttachmentsQuery);

            MailDbContext.SaveChanges();

            MailDaoFactory.GetMailboxDao().RemoveMailbox(mb);

            if (totalRemove)
            {
                MailDaoFactory.GetFolderDao().Delete();

                var deleteContactInfoQuery = MailDbContext.MailContactInfo
                    .Where(c => c.IdUser == mb.User && c.TenantId == mb.Tenant);

                MailDbContext.MailContactInfo.RemoveRange(deleteContactInfoQuery);

                MailDbContext.SaveChanges();

                var deleteContactsQuery = MailDbContext.MailContacts
                    .Where(c => c.IdUser == mb.User && c.TenantId == mb.Tenant);

                MailDbContext.MailContacts.RemoveRange(deleteContactsQuery);

                MailDbContext.SaveChanges();

                var deleteDisplayImagesQuery = MailDbContext.MailDisplayImages
                   .Where(c => c.IdUser == mb.User && c.Tenant == mb.Tenant);

                MailDbContext.MailDisplayImages.RemoveRange(deleteDisplayImagesQuery);

                MailDbContext.SaveChanges();
            }

            tx.Commit();
        }

        private void RemoveFile(IDataStore dataStorage, string path, ILog log)
        {
            try
            {
                log.DebugFormat("Removing file: {0}", path);

                dataStorage.Delete(string.Empty, path);

                log.InfoFormat("File: '{0}' removed successfully", path);
            }
            catch (FileNotFoundException)
            {
                log.WarnFormat("File: {0} not found", path);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("RemoveFile(path: {0}) failed. Error: {1}", path, ex.ToString());
            }
        }

        private void RemoveUserMailDirectory(int tenant, string userId, ILog log)
        {
            log.DebugFormat("MailDataStore.GetDataStore(Tenant = {0})", tenant);

            var dataStorage = StorageFactory.GetMailStorage(tenant);

            var userMailDir = MailStoragePathCombiner.GetUserMailsDirectory(userId);

            try
            {
                log.InfoFormat("RemoveUserMailDirectory(Path: {0}, Tenant = {1} User = '{2}')", userMailDir, tenant, userId);

                dataStorage.DeleteDirectory(userMailDir);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("MailDataStore.DeleteDirectory(path: {0}) failed. Error: {1}", userMailDir, ex.ToString());

                throw;
            }
        }

        public bool RemoveGarbageMailData(MailBoxData mailbox, int garbageDaysLimit, ILog log)
        {
            //TODO: Implement cleanup data marked as removed and trash messages exceeded garbageDaysLimit

            return true;
        }

        private void RemoveTeamlabMailbox(MailBoxData mailbox, ILog log)
        {
            if (mailbox == null)
                throw new ArgumentNullException("mailbox");

            if (!mailbox.IsTeamlab)
                return;

            try
            {
                ServerMailboxEngine.RemoveMailbox(mailbox);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("RemoveTeamlabMailbox(mailboxId = {0}) Failure\r\nException: {1}", mailbox.MailBoxId,
                    ex.ToString());
            }
        }

        private void RemoveUserFolders(ILog log)
        {
            try
            {
                var folders = UserFolderEngine
                    .GetList(parentId: 0);

                foreach (var folder in folders)
                {
                    OperationEngine
                        .RemoveUserFolder(folder.Id);
                }

            }
            catch (Exception ex)
            {
                log.ErrorFormat("RemoveUserFolders() Failure\r\nException: {0}", ex.ToString());
            }
        }

        private void RemoveUserMailboxes(int tenant, string user, ILog log)
        {
            var mailboxIterator = new MailboxIterator(MailboxEngine, tenant, user);

            var mailbox = mailboxIterator.First();

            if (mailboxIterator.IsDone)
            {
                log.Info("There are no user's mailboxes for deletion");
                return;
            }

            while (!mailboxIterator.IsDone)
            {
                try
                {
                    if (!mailbox.UserId.Equals(user))
                        throw new Exception(
                            string.Format("Mailbox (id:{0}) user '{1}' not equals to removed user: '{2}'",
                                mailbox.MailBoxId, mailbox.UserId, user));

                    RemoveMailboxData(mailbox, true, log);
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("RemoveMailboxData(MailboxId: {0}) failed. Error: {1}", mailbox.MailBoxId, ex);
                }

                mailbox = mailboxIterator.Next();
            }

            RemoveUserMailDirectory(tenant, user, log);
        }

        #endregion

        public void Dispose()
        {
            if (TenantMemCache != null)
            {
                TenantMemCache.Dispose();
            }
        }
    }
}