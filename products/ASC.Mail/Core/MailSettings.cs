using System;
using System.Collections.Generic;

using ASC.Common;
using ASC.Common.Utils;
using ASC.Mail.Models;
using ASC.Web.Studio.Core;

using static System.TimeSpan;

namespace ASC.Mail.Configuration
{
    [Singletone]
    public class MailSettings
    {
        #region From configs

        #region MailGarbageEraserConfig

        public string CleanerHttpContextScheme { get; set; }
        public int? CleanerGarbageOverdueDays { get; set; }
        public int? CleanerMaxTasksAtOnce { get; set; }
        public int? CleanerMaxFilesToRemoveAtOnce { get; set; }
        public int? CleanerTenantCacheDays { get; set; }
        public int? CleanerTenantOverdueDays { get; set; }
        public int? CleanerTimerWaitMinutes { get; set; }
        #endregion

        #region from defines

        /// <summary>
        /// mail.auth-error-warning-in-minutes
        /// </summary>
        public TimeSpan AuthErrorWarningTimeout { get; set; }

        /// <summary>
        /// mail.auth-error-disable-mailbox-in-minutes
        /// </summary>
        public TimeSpan AuthErrorDisableMailboxTimeout { get; set; }

        /// <summary>
        /// earlier mail.check-work-timer-seconds
        /// </summary>
        public TimeSpan CheckTimerInterval { get; set; }

        /// <summary>
        /// earlier mail.activity-timeout-seconds
        /// </summary>
        public TimeSpan ActiveInterval { get; set; }

        /// <summary>
        /// earlier mail.one-user-mode
        /// </summary>
        public List<string> WorkOnUsersOnlyList { get; set; }

        public string WorkOnUsersOnly { get; set; }

        /// <summary>
        /// mail.aggregate-mode
        /// </summary>
        public AggregateModeType AggregateMode { get; set; }

        public string AggregateModeValue { get; set; }

        /// <summary>
        /// mail.overdue-account-delay-seconds
        /// </summary>
        public TimeSpan OverdueAccountDelay { get; set; }

        /// <summary>
        /// mail.quota-ended-delay-seconds
        /// </summary>
        public TimeSpan QuotaEndedDelay { get; set; }

        /// <summary>
        /// mail.tenant-cache-lifetime-seconds
        /// </summary>
        public TimeSpan TenantCachingPeriod { get; set; }

        /// <summary>
        /// mail.queue-lifetime-seconds
        /// </summary>
        public TimeSpan QueueLifetime { get; set; }

        /// <summary>
        /// mail.save-original-message
        /// </summary>
        public bool SaveOriginalMessage { get; set; }

        /// <summary>
        /// mail.certificate-permit
        /// </summary>
        public bool SslCertificatesErrorsPermit { get; set; }

        /// <summary>
        /// mail.default-api-scheme
        /// </summary>
        public string DefaultApiSchema { get; set; }

        /// <summary>
        /// mail.default-login-delay
        /// </summary>
        public int DefaultServerLoginDelay { get; set; }

        /// <summary>
        /// mail.autoreply-days-interval
        /// </summary>
        public int AutoreplyDaysInterval { get; set; }

        public bool NeedProxyHttp { get; set; }

        public string DefaultServerLoginDelayStr { get; set; }

        /// <summary>
        /// "web.hub"
        /// </summary>
        public string WebHub { get; set; }

        /// <summary>
        /// "web.hub.internal"
        /// </summary>
        public string WebHubInternal { get; set; }

        public bool IsSignalRAvailable { get; set; }

        /// <summary>
        /// mail.daemon-email
        /// </summary>
        public string MailDaemonEmail { get; set; }

        /// <summary>
        /// mail.recalculate-folders-timeout
        /// </summary>
        public int? RecalculateFoldersTimeout { get; set; }

        /// <summary>
        /// mail.remove-domain-timeout
        /// </summary>
        public int? RemoveDomainTimeout { get; set; }

        /// <summary>
        /// mail.remove-mailbox-timeout
        /// </summary>
        public int? RemoveMailboxTimeout { get; set; }

        /// <summary>
        /// mail.server-mailbox-limit-per-user
        /// </summary>
        public int? ServerDomainMailboxPerUserLimit { get; set; }

        /// <summary>
        /// mail.server.spf-record-value
        /// </summary>
        public string ServerDnsSpfRecordValue { get; set; }

        /// <summary>
        /// mail.server.mx-record-priority
        /// </summary>
        public int? ServerDnsMxRecordPriority { get; set; }

        /// <summary>
        /// mail.server-dkim-selector
        /// </summary>
        public string ServerDnsDkimSelector { get; set; }

        /// <summary>
        /// mail.server-dns-check-prefix
        /// </summary>
        public string ServerDnsDomainCheckPrefix { get; set; }

        /// <summary>
        /// mail.server-dns-default-ttl
        /// </summary>
        public int ServerDnsDefaultTtl { get; set; }

        /// <summary>
        /// mail.maximum-message-body-size
        /// </summary>
        public int? MaximumMessageBodySize { get; set; }

        /// <summary>
        /// mail.attachments-group-operations
        /// </summary>
        public bool IsAttachmentsGroupOperationsAvailable { get; set; }

        #endregion

        #region from taskConfig

        public int? ApiPort { get; set; }

        public bool UseDump { get; set; }

        /// <summary>
        /// mail.aggregate-mode
        /// </summary>
        public enum AggregateModeType
        {
            All,
            External,
            Internal
        };

        /// <summary>
        /// web.enable-signalr
        /// </summary>
        public bool EnableSignalr { get; set; }

        /// <summary>
        /// mail.check-pop3-uidl-chunk
        /// </summary>
        public int ChunkOfPop3Uidl { get; set; }

        /// <summary>
        /// mail.max-messages-per-mailbox
        /// </summary>
        public int MaxMessagesPerSession { get; set; }

        /// <summary>
        /// mail.max-tasks-count
        /// </summary>
        public int MaxTasksAtOnce { get; set; }

        /// <summary>
        /// mail.inactive-mailboxes-ratio
        /// </summary>
        public double InactiveMailboxesRatio { get; set; }

        /// <summary>
        /// mail.tenant-overdue-days
        /// </summary>
        public int? TenantOverdueDays { get; set; }

        /// <summary>
        /// mail.tenant-min-quota-balance
        /// </summary>
        public long? TenantMinQuotaBalance { get; set; }

        /// <summary>
        /// mail.task-process-lifetime-seconds
        /// </summary>
        public TimeSpan TaskLifetime { get; set; }

        /// <summary>
        /// mail.task-check-state-seconds
        /// </summary>
        public TimeSpan TaskCheckState { get; set; }

        /// <summary>
        /// mail.tcp-timeout
        /// </summary>
        public int TcpTimeout { get; set; }

        /// <summary>
        /// mail.protocol-log-path
        /// </summary>
        public string ProtocolLogPath { get; set; }

        /// <summary>
        /// mail.collect-statistics
        /// </summary>
        public bool CollectStatistics { get; set; }

        public bool ShowMailEngineLogs { get; set; }

        /// <summary>
        /// "mail.quota-rest": 26214400,
        /// </summary>
        public int? QuotaRest { get; set; }
        #endregion

        #endregion

        #region Inner

        public Dictionary<string, int> ImapFlags { get; set; }
        public string[] SkipImapFlags { get; set; }
        public Dictionary<string, Dictionary<string, MailBoxData.MailboxInfo>> SpecialDomainFolders { get; set; }
        public Dictionary<string, int> DefaultFolders { get; set; }

        #endregion

        private int _defaultTimeout = 99999;

        public static MailSettings Current;

        public MailSettings()
        {
            Current = this;
        }

        public MailSettings(ConfigurationExtension configuration)
        {
            var c = configuration.GetSetting<MailSettings>("mail");

            //ApiPort = c.ApiPort ?? 0;

            UseDump = c.UseDump;

            CheckTimerInterval = c.CheckTimerInterval == Zero ? FromSeconds(10) : c.CheckTimerInterval;

            ActiveInterval = c.ActiveInterval == Zero ? FromSeconds(90) : c.ActiveInterval;

            TaskLifetime = c.TaskLifetime == Zero ? FromSeconds(300) : c.TaskLifetime;

            TaskCheckState = c.TaskCheckState == Zero ? FromSeconds(30) : c.TaskCheckState;

            OverdueAccountDelay = c.OverdueAccountDelay == Zero ? FromSeconds(600) : c.OverdueAccountDelay;

            QuotaEndedDelay = c.QuotaEndedDelay == Zero ? FromSeconds(600) : c.QuotaEndedDelay;

            TenantCachingPeriod = c.TenantCachingPeriod == Zero ? FromSeconds(86400) : c.TenantCachingPeriod;

            QueueLifetime = c.QueueLifetime == Zero ? FromSeconds(30) : c.QueueLifetime;

            Guid userId;
            WorkOnUsersOnlyList = Guid.TryParse(c.WorkOnUsersOnly, out userId)
                ? new List<string> { userId.ToString() }
                : new List<string>();

            AggregateMode = c.AggregateModeValue == string.Empty
                ? AggregateModeType.All
                : c.AggregateModeValue == "external" ? AggregateModeType.External
                : c.AggregateModeValue == "internal" ? AggregateModeType.Internal
                : AggregateModeType.All;

            QuotaRest = c.QuotaRest ?? 26214400;

            EnableSignalr = c.EnableSignalr;

            ShowMailEngineLogs = c.ShowMailEngineLogs;

            ChunkOfPop3Uidl = c.ChunkOfPop3Uidl < 1 || c.ChunkOfPop3Uidl > 100 || c.ChunkOfPop3Uidl == 0
                ? 100
                : c.ChunkOfPop3Uidl;

            MaxMessagesPerSession = c.MaxMessagesPerSession < 1
                ? -1
                : c.MaxMessagesPerSession == 0
                ? 10
                : c.MaxMessagesPerSession;

            MaxTasksAtOnce = c.MaxTasksAtOnce < 1 //Вытянуть конфиг!
                ? 1
                : c.MaxTasksAtOnce == 0
                ? 10
                : c.MaxTasksAtOnce;


            InactiveMailboxesRatio = c.InactiveMailboxesRatio < 0
                ? 0
                : c.InactiveMailboxesRatio > 100
                ? 100
                : c.InactiveMailboxesRatio == 0
                ? 25
                : c.InactiveMailboxesRatio;

            AuthErrorWarningTimeout = c.AuthErrorWarningTimeout == Zero ? FromHours(1) : c.AuthErrorWarningTimeout;

            AuthErrorDisableMailboxTimeout = c.AuthErrorDisableMailboxTimeout == Zero ? FromDays(3) : c.AuthErrorDisableMailboxTimeout;

            TenantOverdueDays = c.TenantOverdueDays ?? 10;

            TenantMinQuotaBalance = c.TenantMinQuotaBalance ?? 26214400;

            SaveOriginalMessage = c.SaveOriginalMessage;

            SslCertificatesErrorsPermit = c.SslCertificatesErrorsPermit;

            TcpTimeout = c.TcpTimeout == default(int) ? 30000 : c.TcpTimeout;

            ProtocolLogPath = c.ProtocolLogPath ?? "";

            CollectStatistics = c.CollectStatistics;

            DefaultApiSchema = c.DefaultApiSchema == "https" ? c.DefaultApiSchema : Uri.UriSchemeHttp;

            DefaultServerLoginDelay = c.DefaultServerLoginDelay == default(int) ? 30 : c.DefaultServerLoginDelay;

            AutoreplyDaysInterval = c.AutoreplyDaysInterval == default(int) ? 1 : c.AutoreplyDaysInterval;

            NeedProxyHttp = SetupInfo.IsVisibleSettings("ProxyHttpContent");

            DefaultServerLoginDelayStr = DefaultServerLoginDelay.ToString();

            WebHub = c.WebHub ?? "";

            IsSignalRAvailable = !string.IsNullOrEmpty(WebHub);

            MailDaemonEmail = c.MailDaemonEmail ?? "mail-daemon@onlyoffice.com";

            RecalculateFoldersTimeout = c.RecalculateFoldersTimeout ?? _defaultTimeout;

            RemoveDomainTimeout = c.RemoveDomainTimeout ?? _defaultTimeout;

            RemoveMailboxTimeout = c.RemoveMailboxTimeout ?? _defaultTimeout;

            ServerDomainMailboxPerUserLimit = c.ServerDomainMailboxPerUserLimit ?? 2;

            ServerDnsSpfRecordValue = c.ServerDnsSpfRecordValue ?? "v=spf1 +mx ~all";

            ServerDnsMxRecordPriority = c.ServerDnsMxRecordPriority ?? 0;

            ServerDnsDkimSelector = c.ServerDnsDkimSelector ?? "dkim";

            ServerDnsDomainCheckPrefix = c.ServerDnsDomainCheckPrefix ?? "onlyoffice-domain";

            ServerDnsDefaultTtl = c.ServerDnsDefaultTtl == default(int) ? 300 : c.ServerDnsDefaultTtl;

            MaximumMessageBodySize = c.MaximumMessageBodySize ?? 524288;

            IsAttachmentsGroupOperationsAvailable = c.IsAttachmentsGroupOperationsAvailable;

            CleanerHttpContextScheme = DefaultApiSchema == Uri.UriSchemeHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

            CleanerGarbageOverdueDays = c.CleanerGarbageOverdueDays ?? 30;

            CleanerMaxTasksAtOnce = c.CleanerMaxTasksAtOnce ?? 1;

            CleanerMaxFilesToRemoveAtOnce = c.CleanerMaxFilesToRemoveAtOnce ?? 100;

            CleanerTenantCacheDays = c.CleanerTenantCacheDays ?? 1;

            CleanerTenantOverdueDays = c.CleanerTenantOverdueDays ?? 30;

            CleanerTimerWaitMinutes = c.CleanerTimerWaitMinutes ?? 60;

            WebHubInternal = c.WebHubInternal == string.Empty ? "http://localhost:9899" : c.WebHubInternal;

            //Set values from MailQueueItemSettings later
            ImapFlags = new Dictionary<string, int>();
            SkipImapFlags = new string[] { };
            SpecialDomainFolders = new Dictionary<string, Dictionary<string, MailBoxData.MailboxInfo>>();
            DefaultFolders = new Dictionary<string, int>();
        }

    }
}
