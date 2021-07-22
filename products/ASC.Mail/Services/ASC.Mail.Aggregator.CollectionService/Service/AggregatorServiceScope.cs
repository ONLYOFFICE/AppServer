using ASC.Common;
using ASC.Core;
using ASC.Data.Storage;
using ASC.Mail.Aggregator.CollectionService.Queue;
using ASC.Mail.Core;
using ASC.Mail.Core.Engine;
using ASC.Mail.Utils;

using Microsoft.Extensions.DependencyInjection;

namespace ASC.Mail.Aggregator.CollectionService.Service
{
    [Scope]
    public class AggregatorServiceScope
    {
        #region AggSrv

        private TenantManager TenantManager { get; }
        private CoreBaseSettings CoreBaseSettings { get; }
        private MailQueueItemSettings MailQueueItemSettings { get; }
        private StorageFactory StorageFactory { get; }
        private MailEnginesFactory MailEnginesFactory { get; }
        private SecurityContext SecurityContext { get; }
        private ApiHelper ApiHelper { get; }
        private IMailDaoFactory MailDaoFactory { get; }

        #endregion

        #region QueueManagerScope

        private UserManager UserManager { get; }
        private MailboxEngine MailboxEngine { get; }
        private AlertEngine AlertEngine { get; }

        #endregion

        #region SignalrWorkerScope

        private FolderEngine FolderEngine { get; }

        #endregion

        private ServiceProvider ServiceProvider { get; }

        public AggregatorServiceScope(
            ServiceProvider serviceProvider,
            TenantManager tenantManager,
            CoreBaseSettings coreBaseSettings,
            MailQueueItemSettings mailQueueItemSettings,
            StorageFactory storageFactory,
            MailEnginesFactory mailEnginesFactory,
            SecurityContext securityContext,
            ApiHelper apiHelper,
            IMailDaoFactory mailDaoFactory,
            UserManager userManager,
            MailboxEngine mailboxEngine,
            AlertEngine alertEngine,
            FolderEngine folderEngine)
        {
            ServiceProvider = serviceProvider;

            TenantManager = tenantManager;
            MailQueueItemSettings = mailQueueItemSettings;
            CoreBaseSettings = coreBaseSettings;
            StorageFactory = storageFactory;
            MailEnginesFactory = mailEnginesFactory;
            SecurityContext = securityContext;
            ApiHelper = apiHelper;
            MailDaoFactory = mailDaoFactory;
            UserManager = userManager;
            MailboxEngine = mailboxEngine;
            AlertEngine = alertEngine;
            FolderEngine = folderEngine;
        }

        public TenantManager GetTenantManager() => TenantManager;
    }
}
