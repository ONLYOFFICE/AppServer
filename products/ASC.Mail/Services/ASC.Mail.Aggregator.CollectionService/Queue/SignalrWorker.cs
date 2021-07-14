﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Notify.Signalr;
using ASC.Core.Users;
using ASC.Mail.Core.Engine;
using ASC.Mail.Enums;
using ASC.Mail.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ASC.Mail.Aggregator.CollectionService.Queue
{
    [Singletone]
    public class SignalrWorker : IDisposable
    {
        private readonly Queue<MailBoxData> _processingQueue;
        private Thread _worker;
        private volatile bool _workerTerminateSignal;
        private readonly EventWaitHandle _waitHandle;
        private readonly TimeSpan _timeSpan;
        public bool StartImmediately { get; set; } = true;

        private ILog _log { get; }
        private SignalrServiceClient SignalrServiceClient { get; }
        private IServiceProvider ServiceProvider { get; }

        public SignalrWorker(
            IOptionsMonitor<ILog> optionsMonitor,
            IOptionsSnapshot<SignalrServiceClient> optionsSnapshot,
            IServiceProvider serviceProvider)
        {
            _log = optionsMonitor.Get("ASC.Mail.SignalrWorker");
            SignalrServiceClient = optionsSnapshot.Get("mail");
            ServiceProvider = serviceProvider;

            _workerTerminateSignal = false;
            _processingQueue = new Queue<MailBoxData>();
            _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            _timeSpan = TimeSpan.FromSeconds(10);

            _worker = new Thread(ProcessQueue);

            if (StartImmediately)
                _worker.Start();
        }

        private void ProcessQueue()
        {
            while (!_workerTerminateSignal)
            {
                if (!HasQueuedMailbox)
                {
                    _log.Debug("No items, waiting.");
                    _waitHandle.WaitOne();
                    _log.Debug("Waking up...");
                }

                var mailbox = NextQueuedMailBoxData;
                if (mailbox == null)
                    continue;

                try
                {
                    _log.DebugFormat("signalrServiceClient.SendUnreadUser(UserId = {0} TenantId = {1})", mailbox.UserId,
                        mailbox.TenantId);

                    SendUnreadUser(mailbox.TenantId, mailbox.UserId);
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("signalrServiceClient.SendUnreadUser(UserId = {0} TenantId = {1}) Exception: {2}", mailbox.UserId,
                        mailbox.TenantId, ex.ToString());
                }

                _waitHandle.Reset();
            }
        }

        public int QueueCount
        {
            get
            {
                lock (_processingQueue)
                {
                    return _processingQueue.Count;
                }
            }
        }

        public bool HasQueuedMailbox
        {
            get
            {
                lock (_processingQueue)
                {
                    return _processingQueue.Any();
                }
            }
        }

        public MailBoxData NextQueuedMailBoxData
        {
            get
            {
                if (!HasQueuedMailbox)
                    return null;

                lock (_processingQueue)
                {
                    return _processingQueue.Dequeue();
                }
            }
        }

        public void Start()
        {
            if (!_worker.IsAlive)
                _worker.Start();
        }

        public void AddMailbox(MailBoxData item)
        {
            lock (_processingQueue)
            {
                if (!_processingQueue.Contains(item))
                    _processingQueue.Enqueue(item);
            }
            _waitHandle.Set();
        }

        public void Dispose()
        {
            if (_worker == null)
                return;

            _workerTerminateSignal = true;
            _waitHandle.Set();

            if (_worker.IsAlive)
            {
                _log.Info("Stop SignalrWorker.");

                if (!_worker.Join(_timeSpan))
                {
                    _log.Info("SignalrWorker busy, aborting the thread.");
                    _worker.Abort();
                }
            }

            _worker = null;
        }

        private void SendUnreadUser(int tenant, string userId)
        {
            try
            {
                var scope = ServiceProvider.CreateScope();

                var folderEngine = scope.ServiceProvider.GetService<FolderEngine>();
                var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
                var userManager = scope.ServiceProvider.GetService<UserManager>();

                _log.Debug($"SendUnreadUser(). Try set tenant |{tenant}| for user |{userId}|...");

                tenantManager.SetCurrentTenant(tenant);

                _log.Debug($"SendUnreadUser(). Now current tennant = {tenantManager.GetCurrentTenant().TenantId}");

                var mailFolderInfos = folderEngine.GetFolders();

                var count = (from mailFolderInfo in mailFolderInfos
                             where mailFolderInfo.id == FolderType.Inbox
                             select mailFolderInfo.unreadMessages)
                    .FirstOrDefault();

                var userInfo = userManager.GetUsers(Guid.Parse(userId));
                if (userInfo.ID != Constants.LostUser.ID)
                {
                    // sendMailsCount
                    SignalrServiceClient.SendUnreadUser(tenant, userId, count);
                }
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Unknown Error. {0}, {1}", e.ToString(),
                    e.InnerException != null ? e.InnerException.Message : string.Empty);
            }
        }
    }
}
