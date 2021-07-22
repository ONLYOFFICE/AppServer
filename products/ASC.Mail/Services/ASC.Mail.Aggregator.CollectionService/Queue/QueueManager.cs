using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Mail.Aggregator.CollectionService.Queue.Data;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Core.Engine;
using ASC.Mail.Extensions;
using ASC.Mail.Models;
using ASC.Mail.Utils;

using LiteDB;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ASC.Mail.Aggregator.CollectionService.Queue
{
    [Singletone]
    public class QueueManager : IDisposable
    {
        private readonly int _maxItemsLimit;
        private readonly Queue<MailBoxData> _mailBoxQueue;
        private List<MailBoxData> _lockedMailBoxList;

        private DateTime _loadQueueTime;
        private MemoryCache _tenantMemCache;
        private const string DBC_MAILBOXES = "mailboxes";
        private const string DBC_TENANTS = "tenants";
        private static string _dbcFile;
        private static string _dbcJournalFile;
        private readonly object _locker = new object();
        private LiteDatabase _db;
        private ILiteCollection<MailboxData> _mailboxes;
        private ILiteCollection<TenantData> _tenants;

        private readonly ILog Log;
        private MailSettings MailSettings { get; }
        private IServiceProvider ServiceProvider { get; }

        public ManualResetEvent CancelHandler { get; set; }

        public QueueManager(
            MailSettings mailSettings,
            IOptionsMonitor<ILog> optionsMonitor,
            IServiceProvider serviceProvider)
        {
            _maxItemsLimit = mailSettings.MaxTasksAtOnce;
            _mailBoxQueue = new Queue<MailBoxData>();
            _lockedMailBoxList = new List<MailBoxData>();

            MailSettings = mailSettings;
            ServiceProvider = serviceProvider;

            Log = optionsMonitor.Get("ASC.Mail.MainThread");
            _loadQueueTime = DateTime.UtcNow;
            _tenantMemCache = new MemoryCache("QueueManagerTenantCache");

            CancelHandler = new ManualResetEvent(false);

            if (MailSettings.UseDump)
            {
                _dbcFile = Path.Combine(Environment.CurrentDirectory, "dump.db");
                _dbcJournalFile = Path.Combine(Environment.CurrentDirectory, "dump-journal.db");

                Log.DebugFormat($"Dump file path: {_dbcFile}");

                LoadDump();
            }
        }

        #region - public methods -

        public IEnumerable<MailBoxData> GetLockedMailboxes(int needTasks)
        {
            var mbList = new List<MailBoxData>();
            do
            {
                var mailBox = GetLockedMailbox();
                if (mailBox == null)
                    break;

                mbList.Add(mailBox);

            } while (mbList.Count < needTasks);

            return mbList;
        }

        public MailBoxData GetLockedMailbox()
        {
            MailBoxData mailBoxData;

            do
            {
                mailBoxData = GetQueuedMailbox();

            } while (mailBoxData != null && !TryLockMailbox(mailBoxData));

            if (mailBoxData == null)
                return null;

            _lockedMailBoxList.Add(mailBoxData);

            CancelHandler.Reset();

            AddMailboxToDumpDb(mailBoxData.ToMailboxData());

            return mailBoxData;
        }

        public void ReleaseAllProcessingMailboxes()
        {
            if (!_lockedMailBoxList.Any())
                return;

            var cloneCollection = new List<MailBoxData>(_lockedMailBoxList);

            Log.Info("QueueManager -> ReleaseAllProcessingMailboxes()");

            foreach (var mailbox in cloneCollection)
            {
                ReleaseMailbox(mailbox);
            }
        }

        public void ReleaseMailbox(MailBoxData mailBoxData)
        {
            try
            {
                if (!_lockedMailBoxList.Contains(mailBoxData))
                {
                    Log.WarnFormat($"QueueManager -> ReleaseMailbox(Tenant = {mailBoxData.TenantId} MailboxId = {mailBoxData.MailBoxId}, Address = '{mailBoxData.EMail}') mailbox not found");
                    return;
                }

                Log.InfoFormat($"QueueManager -> ReleaseMailbox(MailboxId = {mailBoxData.MailBoxId} Address '{mailBoxData.EMail}')");

                var scope = ServiceProvider.CreateScope();

                var tenantManager = scope.ServiceProvider.GetService<TenantManager>();

                tenantManager.SetCurrentTenant(mailBoxData.TenantId);

                var mailboxEngine = scope.ServiceProvider.GetService<MailboxEngine>();

                mailboxEngine.ReleaseMaibox(mailBoxData, MailSettings);

                _lockedMailBoxList.Remove(mailBoxData);

                DeleteMailboxFromDumpDb(mailBoxData.MailBoxId);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> ReleaseMailbox(Tenant = {mailBoxData.TenantId} MailboxId = {mailBoxData.MailBoxId}, Address = '{mailBoxData.Account}')\r\nException: {ex} \r\n");
            }
        }

        public int ProcessingCount
        {
            get { return _lockedMailBoxList.Count; }
        }

        public void LoadMailboxesFromDump()
        {
            if (!MailSettings.UseDump)
                return;

            if (_lockedMailBoxList.Any())
                return;

            try
            {
                Log.Debug("QueueManager -> LoadMailboxesFromDump()");

                lock (_locker)
                {
                    var list = _mailboxes.FindAll().ToList();

                    _lockedMailBoxList = list.ConvertAll(m => m.ToMailbox()).ToList();
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> LoadMailboxesFromDump: {ex}");

                ReCreateDump();
            }
        }

        public void LoadTenantsFromDump()
        {
            if (!MailSettings.UseDump)
                return;

            try
            {
                Log.Debug("QueueManager -> LoadTenantsFromDump()");

                lock (_locker)
                {
                    var list = _tenants.FindAll().ToList();

                    foreach (var tenantData in list)
                    {
                        AddTenantToCache(tenantData, false);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> LoadTenantsFromDump: {ex}");

                ReCreateDump();
            }
        }

        #endregion

        #region - private methods -

        private void ReCreateDump()
        {
            if (!MailSettings.UseDump)
                return;

            try
            {
                if (File.Exists(_dbcFile))
                {
                    Log.DebugFormat($"Dump file '{_dbcFile}' exists, trying delete");

                    File.Delete(_dbcFile);

                    Log.DebugFormat($"Dump file '{_dbcFile}' deleted");
                }

                if (File.Exists(_dbcJournalFile))
                {
                    Log.DebugFormat($"Dump journal file '{_dbcJournalFile}' exists, trying delete");

                    File.Delete(_dbcJournalFile);

                    Log.DebugFormat($"Dump journal file '{_dbcJournalFile}' deleted");
                }

                _db = new LiteDatabase(_dbcFile);

                lock (_locker)
                {
                    _mailboxes = _db.GetCollection<MailboxData>(DBC_MAILBOXES);
                    _tenants = _db.GetCollection<TenantData>(DBC_TENANTS);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> ReCreateDump() failed Exception: {ex}");
            }
        }

        private void AddMailboxToDumpDb(MailboxData mailboxData)
        {
            if (!MailSettings.UseDump)
                return;

            try
            {
                lock (_locker)
                {
                    var mailbox = _mailboxes.FindOne(Query.EQ("MailboxId", mailboxData.MailboxId));

                    if (mailbox != null)
                        return;

                    _mailboxes.Insert(mailboxData);

                    // Create, if not exists, new index on Name field
                    _mailboxes.EnsureIndex(x => x.MailboxId);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> AddMailboxToDumpDb(Id = {mailboxData.MailboxId}) Exception: {ex}");

                ReCreateDump();
            }
        }

        private void DeleteMailboxFromDumpDb(int mailBoxId)
        {
            if (!MailSettings.UseDump)
                return;

            try
            {
                lock (_locker)
                {
                    var mailbox = _mailboxes.FindOne(Query.EQ("MailboxId", mailBoxId));

                    if (mailbox == null)
                        return;

                    _mailboxes.DeleteMany(Query.EQ("MailboxId", mailBoxId));
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> DeleteMailboxFromDumpDb(MailboxId = {mailBoxId}) Exception: {ex}");

                ReCreateDump();
            }
        }

        private void LoadDump()
        {
            if (!MailSettings.UseDump)
                return;

            try
            {
                if (File.Exists(_dbcJournalFile))
                    throw new Exception($"Temp dump journal file exists in {_dbcJournalFile}");

                _db = new LiteDatabase(_dbcFile);

                lock (_locker)
                {
                    _tenants = _db.GetCollection<TenantData>(DBC_TENANTS);
                    _mailboxes = _db.GetCollection<MailboxData>(DBC_MAILBOXES);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> LoadDump() failed Exception: {ex}");

                ReCreateDump();
            }
        }

        private void AddTenantToDumpDb(TenantData tenantData)
        {
            if (!MailSettings.UseDump)
                return;

            try
            {
                lock (_locker)
                {
                    var tenant = _tenants.FindOne(Query.EQ("Tenant", tenantData.Tenant));

                    if (tenant != null)
                        return;

                    _tenants.Insert(tenantData);

                    // Create, if not exists, new index on Name field
                    _tenants.EnsureIndex(x => x.Tenant);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> AddTenantToDumpDb(TenantId = {tenantData.Tenant}) Exception: {ex}");

                ReCreateDump();
            }
        }

        private void DeleteTenantFromDumpDb(int tenantId)
        {
            if (!MailSettings.UseDump)
                return;

            try
            {
                lock (_locker)
                {
                    var tenant = _tenants.FindOne(Query.EQ("Tenant", tenantId));

                    if (tenant == null)
                        return;

                    _tenants.DeleteMany(Query.EQ("Tenant", tenantId));
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> DeleteTenantFromDumpDb(TenantId = {tenantId}) Exception: {ex}");

                ReCreateDump();
            }
        }

        private void AddTenantToCache(TenantData tenantData, bool needDump = true)
        {
            var now = DateTime.UtcNow;

            if (tenantData.Expired < now)
            {
                DeleteTenantFromDumpDb(tenantData.Tenant);
                return; // Skip Expired tenant
            }

            var cacheItem = new CacheItem(tenantData.Tenant.ToString(CultureInfo.InvariantCulture), tenantData);

            var nowOffset = tenantData.Expired - now;

            var absoluteExpiration = DateTime.UtcNow.Add(nowOffset);

            var cacheItemPolicy = new CacheItemPolicy
            {
                RemovedCallback = CacheEntryRemove,
                AbsoluteExpiration = absoluteExpiration
            };

            _tenantMemCache.Add(cacheItem, cacheItemPolicy);

            if (!needDump)
                return;

            AddTenantToDumpDb(tenantData);
        }

        private bool QueueIsEmpty
        {
            get { return !_mailBoxQueue.Any(); }
        }

        private bool QueueLifetimeExpired
        {
            get { return DateTime.UtcNow - _loadQueueTime >= MailSettings.QueueLifetime; }
        }

        private void LoadQueue()
        {
            try
            {
                var scope = ServiceProvider.CreateScope();
                var mailboxEngine = scope.ServiceProvider.GetService<MailboxEngine>();
                var mbList = mailboxEngine.GetMailboxesForProcessing(MailSettings, _maxItemsLimit).ToList();

                ReloadQueue(mbList);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat($"QueueManager -> LoadQueue()\r\nException: \r\n {ex}");
            }
        }

        private MailBoxData GetQueuedMailbox()
        {
            if (QueueIsEmpty || QueueLifetimeExpired)
            {
                Log.DebugFormat("Queue is {0}. Load new queue.", QueueIsEmpty ? "EMPTY" : "EXPIRED");

                LoadQueue();
            }

            return !QueueIsEmpty ? _mailBoxQueue.Dequeue() : null;
        }

        private void RemoveFromQueue(int tenant)
        {
            var mbList = _mailBoxQueue.Where(mb => mb.TenantId != tenant).Select(mb => mb).ToList();
            ReloadQueue(mbList);
        }

        private void RemoveFromQueue(int tenant, string user)
        {

            Log.Debug("RemoveFromQueue()");
            var list = _mailBoxQueue.ToList();

            foreach (var b in list)
            {
                if (b.UserId == user)
                    Log.Debug($"Next mailbox will be removed from queue: {b.MailBoxId}");
            }

            var mbList = _mailBoxQueue.Where(mb => mb.UserId != user).Select(mb => mb).ToList();

            foreach (var box in list.Except(mbList))
            {
                Log.Debug($"Mailbox with id |{box.MailBoxId}| for user {box.UserId} from tenant {box.TenantId} was removed from queue");
            }

            string boxes = "";
            foreach (var b in mbList)
            {
                boxes += $"{b.MailBoxId} | ";
            }

            Log.Debug($"Now in queue next mailboxes: {boxes}");

            ReloadQueue(mbList);
        }

        private void ReloadQueue(IEnumerable<MailBoxData> mbList)
        {
            _mailBoxQueue.Clear();
            _mailBoxQueue.PushRange(mbList);
            _loadQueueTime = DateTime.UtcNow;
        }

        private bool TryLockMailbox(MailBoxData mailbox)
        {
            Log.DebugFormat("TryLockMailbox(MailboxId={0} is {1})", mailbox.MailBoxId, mailbox.Active ? "active" : "inactive");

            try
            {
                var contains = _tenantMemCache.Contains(mailbox.TenantId.ToString(CultureInfo.InvariantCulture));

                var scope = ServiceProvider.CreateScope();
                var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
                var securityContext = scope.ServiceProvider.GetService<SecurityContext>();
                var apiHelper = scope.ServiceProvider.GetService<ApiHelper>();
                var mailboxEngine = scope.ServiceProvider.GetService<MailboxEngine>();
                var alertEngine = scope.ServiceProvider.GetService<AlertEngine>();
                var userManager = scope.ServiceProvider.GetService<UserManager>();

                if (!contains)
                {
                    Log.DebugFormat($"Tenant {mailbox.TenantId} isn't in cache");
                    try
                    {
                        var type = mailbox.GetTenantStatus(tenantManager, securityContext, apiHelper, (int)MailSettings.TenantOverdueDays, Log);

                        switch (type)
                        {
                            case DefineConstants.TariffType.LongDead:
                                Log.InfoFormat("Tenant {0} is not paid. Disable mailboxes.", mailbox.TenantId);

                                mailboxEngine.DisableMailboxes(
                                    new TenantMailboxExp(mailbox.TenantId));

                                var userIds =
                                    mailboxEngine.GetMailUsers(new TenantMailboxExp(mailbox.TenantId))
                                        .ConvertAll(t => t.Item2);

                                alertEngine.CreateDisableAllMailboxesAlert(mailbox.TenantId, userIds);

                                RemoveFromQueue(mailbox.TenantId);

                                return false;

                            case DefineConstants.TariffType.Overdue:
                                Log.InfoFormat("Tenant {0} is not paid. Stop processing mailboxes.", mailbox.TenantId);

                                mailboxEngine.SetNextLoginDelay(new TenantMailboxExp(mailbox.TenantId),
                                    MailSettings.OverdueAccountDelay);

                                RemoveFromQueue(mailbox.TenantId);

                                return false;

                            case DefineConstants.TariffType.Active:
                                Log.InfoFormat("Tenant {0} is paid.", mailbox.TenantId);

                                var expired = DateTime.UtcNow.Add(MailSettings.TenantCachingPeriod);

                                var tenantData = new TenantData
                                {
                                    Tenant = mailbox.TenantId,
                                    TariffType = type,
                                    Expired = expired
                                };

                                AddTenantToCache(tenantData);

                                break;
                            default:
                                Log.InfoFormat($"Cannot get tariff type for {mailbox.MailBoxId} mailbox");
                                mailboxEngine.SetNextLoginDelay(new TenantMailboxExp(mailbox.TenantId),
                                    MailSettings.OverdueAccountDelay);

                                RemoveFromQueue(mailbox.TenantId);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorFormat($"QueueManager -> TryLockMailbox(): GetTariffType \r\nException:{e}\r\n");
                    }
                }
                else
                {
                    Log.DebugFormat($"Tenant {mailbox.TenantId} is in cache");
                }

                var isUserTerminated = mailbox.IsUserTerminated(tenantManager, userManager, Log);
                var isUserRemoved = mailbox.IsUserRemoved(tenantManager, userManager, Log);

                if (isUserTerminated || isUserRemoved)
                {
                    string userStatus = "";
                    if (isUserRemoved) userStatus = "removed";
                    else if (isUserTerminated) userStatus = "terminated";

                    Log.InfoFormat($"User '{mailbox.UserId}' was {userStatus}. Tenant = {mailbox.TenantId}. Disable mailboxes for user.");

                    mailboxEngine.LoggedDisableMailboxes(
                        new UserMailboxExp(mailbox.TenantId, mailbox.UserId), Log);

                    alertEngine.CreateDisableAllMailboxesAlert(mailbox.TenantId,
                        new List<string> { mailbox.UserId });

                    RemoveFromQueue(mailbox.TenantId, mailbox.UserId);

                    return false;
                }

                if (mailbox.IsTenantQuotaEnded(tenantManager, (int)MailSettings.TenantMinQuotaBalance, Log))
                {
                    Log.InfoFormat($"Tenant = {mailbox.TenantId} User = {mailbox.UserId}. Quota is ended.");

                    if (!mailbox.QuotaError)
                        alertEngine.CreateQuotaErrorWarningAlert(mailbox.TenantId, mailbox.UserId);

                    mailboxEngine.SetNextLoginDelay(new UserMailboxExp(mailbox.TenantId, mailbox.UserId),
                                    MailSettings.QuotaEndedDelay);

                    RemoveFromQueue(mailbox.TenantId, mailbox.UserId);

                    return false;
                }

                return mailboxEngine.LockMaibox(mailbox.MailBoxId);

            }
            catch (Exception ex)
            {
                Log.ErrorFormat("QueueManager -> TryLockMailbox(MailboxId={0} is {1})\r\nException:{2}\r\n", mailbox.MailBoxId,
                           mailbox.Active ? "active" : "inactive", ex.ToString());

                return false;
            }

        }

        private void CacheEntryRemove(CacheEntryRemovedArguments arguments)
        {
            if (arguments.RemovedReason == CacheEntryRemovedReason.CacheSpecificEviction)
                return;

            var tenantId = Convert.ToInt32(arguments.CacheItem.Key);

            Log.InfoFormat($"Tenant {tenantId} payment cache is expired.");

            DeleteTenantFromDumpDb(tenantId);
        }

        #endregion

        public void Dispose()
        {
            _tenantMemCache.Dispose();
            _tenantMemCache = null;

            if (MailSettings.UseDump)
            {
                _db.Dispose();
                _db = null;
            }
        }
    }
}
