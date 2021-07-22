using System;
using System.Configuration;

using ASC.Api.Core;
using ASC.Common;
using ASC.Common.Logging;
using ASC.Common.Threading;
using ASC.Core;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Engine;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Core.Resources;
using ASC.Web.Api.Routing;
using ASC.Web.Core.Users;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ASC.Mail.Controllers
{
    [DefaultRoute]
    [ApiController]
    [Scope]
    public partial class MailController : ControllerBase
    {
        private int TenantId => TenantManager.GetCurrentTenant().TenantId;

        private string UserId => SecurityContext.CurrentAccount.ID.ToString();

        private TenantManager TenantManager { get; }
        private SecurityContext SecurityContext { get; }
        private UserManager UserManager { get; }
        private DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }
        private ApiContext ApiContext { get; }
        private AccountEngine AccountEngine { get; }
        private AlertEngine AlertEngine { get; }
        private DisplayImagesAddressEngine DisplayImagesAddressEngine { get; }
        private SignatureEngine SignatureEngine { get; }
        private TagEngine TagEngine { get; }
        private MailboxEngine MailboxEngine { get; }
        private DocumentsEngine DocumentsEngine { get; }
        private AutoreplyEngine AutoreplyEngine { get; }
        private ContactEngine ContactEngine { get; }
        private MessageEngine MessageEngine { get; }
        private CrmLinkEngine CrmLinkEngine { get; }
        private SpamEngine SpamEngine { get; }
        private FilterEngine FilterEngine { get; }
        private UserFolderEngine UserFolderEngine { get; }
        private FolderEngine FolderEngine { get; }
        private DraftEngine DraftEngine { get; }
        private TemplateEngine TemplateEngine { get; }
        private SettingEngine SettingEngine { get; }
        private ServerEngine ServerEngine { get; }
        private ServerDomainEngine ServerDomainEngine { get; }
        private ServerMailboxEngine ServerMailboxEngine { get; }
        private ServerMailgroupEngine ServerMailgroupEngine { get; }
        private OperationEngine OperationEngine { get; }
        private TestEngine TestEngine { get; }
        private CoreBaseSettings CoreBaseSettings { get; }
        private IServiceProvider ServiceProvider { get; }
        private ILog Log { get; }

        private MailSettings MailSettings { get; }

        public MailController(
            TenantManager tenantManager,
            SecurityContext securityContext,
            UserManager userManager,
            DisplayUserSettingsHelper displayUserSettingsHelper,
            ApiContext apiContext,
            AccountEngine accountEngine,
            AlertEngine alertEngine,
            DisplayImagesAddressEngine displayImagesAddressEngine,
            SignatureEngine signatureEngine,
            TagEngine tagEngine,
            MailboxEngine mailboxEngine,
            DocumentsEngine documentsEngine,
            AutoreplyEngine autoreplyEngine,
            ContactEngine contactEngine,
            MessageEngine messageEngine,
            CrmLinkEngine crmLinkEngine,
            SpamEngine spamEngine,
            FilterEngine filterEngine,
            UserFolderEngine userFolderEngine,
            FolderEngine folderEngine,
            DraftEngine draftEngine,
            TemplateEngine templateEngine,
            SettingEngine settingEngine,
            ServerEngine serverEngine,
            ServerDomainEngine serverDomainEngine,
            ServerMailboxEngine serverMailboxEngine,
            ServerMailgroupEngine serverMailgroupEngine,
            OperationEngine operationEngine,
            TestEngine testEngine,
            CoreBaseSettings coreBaseSettings,
            MailSettings mailSettings,
            IServiceProvider serviceProvider,
            IOptionsMonitor<ILog> option)
        {
            TenantManager = tenantManager;
            SecurityContext = securityContext;
            MailSettings = mailSettings;
            UserManager = userManager;
            DisplayUserSettingsHelper = displayUserSettingsHelper;
            ApiContext = apiContext;
            AccountEngine = accountEngine;
            AlertEngine = alertEngine;
            DisplayImagesAddressEngine = displayImagesAddressEngine;
            SignatureEngine = signatureEngine;
            TagEngine = tagEngine;
            MailboxEngine = mailboxEngine;
            DocumentsEngine = documentsEngine;
            AutoreplyEngine = autoreplyEngine;
            ContactEngine = contactEngine;
            MessageEngine = messageEngine;
            CrmLinkEngine = crmLinkEngine;
            SpamEngine = spamEngine;
            FilterEngine = filterEngine;
            UserFolderEngine = userFolderEngine;
            FolderEngine = folderEngine;
            DraftEngine = draftEngine;
            TemplateEngine = templateEngine;
            SettingEngine = settingEngine;
            ServerEngine = serverEngine;
            ServerDomainEngine = serverDomainEngine;
            ServerMailboxEngine = serverMailboxEngine;
            ServerMailgroupEngine = serverMailgroupEngine;
            OperationEngine = operationEngine;
            TestEngine = testEngine;
            CoreBaseSettings = coreBaseSettings;
            ServiceProvider = serviceProvider;
            Log = option.Get("ASC.Api.Mail");
        }

        [Read("info")]
        public Module GetModule()
        {
            var product = new MailProduct();
            product.Init();
            return new Module(product);
        }


        /// <summary>
        /// Method for translation mail operation statuses
        /// </summary>
        /// <param name="op">instance of DistributedTask</param>
        /// <returns>translated status text</returns>
        private static string TranslateMailOperationStatus(DistributedTask op)
        {
            var type = op.GetProperty<MailOperationType>(MailOperation.OPERATION_TYPE);
            var status = op.GetProperty<string>(MailOperation.STATUS);
            //TODO: Move strings to Resource file
            switch (type)
            {
                case MailOperationType.DownloadAllAttachments:
                {
                    var progress = op.GetProperty<MailOperationDownloadAllAttachmentsProgress>(MailOperation.PROGRESS);
                    switch (progress)
                    {
                        case MailOperationDownloadAllAttachmentsProgress.Init:
                            return MailApiResource.SetupTenantAndUserHeader;
                        case MailOperationDownloadAllAttachmentsProgress.GetAttachments:
                            return MailApiResource.GetAttachmentsHeader;
                        case MailOperationDownloadAllAttachmentsProgress.Zipping:
                            return MailApiResource.ZippingAttachmentsHeader;
                        case MailOperationDownloadAllAttachmentsProgress.ArchivePreparation:
                            return MailApiResource.PreparationArchiveHeader;
                        case MailOperationDownloadAllAttachmentsProgress.CreateLink:
                            return MailApiResource.CreatingLinkHeader;
                        case MailOperationDownloadAllAttachmentsProgress.Finished:
                            return MailApiResource.FinishedHeader;
                        default:
                            return status;
                    }
                }
                case MailOperationType.RemoveMailbox:
                {
                    var progress = op.GetProperty<MailOperationRemoveMailboxProgress>(MailOperation.PROGRESS);
                    switch (progress)
                    {
                        case MailOperationRemoveMailboxProgress.Init:
                            return "Setup tenant and user";
                        case MailOperationRemoveMailboxProgress.RemoveFromDb:
                            return "Remove mailbox from Db";
                        case MailOperationRemoveMailboxProgress.FreeQuota:
                            return "Decrease newly freed quota space";
                        case MailOperationRemoveMailboxProgress.RecalculateFolder:
                            return "Recalculate folders counters";
                        case MailOperationRemoveMailboxProgress.ClearCache:
                            return "Clear accounts cache";
                        case MailOperationRemoveMailboxProgress.Finished:
                            return "Finished";
                        default:
                            return status;
                    }
                }
                case MailOperationType.RecalculateFolders:
                {
                    var progress = op.GetProperty<MailOperationRecalculateMailboxProgress>(MailOperation.PROGRESS);
                    switch (progress)
                    {
                        case MailOperationRecalculateMailboxProgress.Init:
                            return "Setup tenant and user";
                        case MailOperationRecalculateMailboxProgress.CountUnreadMessages:
                            return "Calculate unread messages";
                        case MailOperationRecalculateMailboxProgress.CountTotalMessages:
                            return "Calculate total messages";
                        case MailOperationRecalculateMailboxProgress.CountUreadConversation:
                            return "Calculate unread conversations";
                        case MailOperationRecalculateMailboxProgress.CountTotalConversation:
                            return "Calculate total conversations";
                        case MailOperationRecalculateMailboxProgress.UpdateFoldersCounters:
                            return "Update folders counters";
                        case MailOperationRecalculateMailboxProgress.CountUnreadUserFolderMessages:
                            return "Calculate unread messages in user folders";
                        case MailOperationRecalculateMailboxProgress.CountTotalUserFolderMessages:
                            return "Calculate total messages in user folders";
                        case MailOperationRecalculateMailboxProgress.CountUreadUserFolderConversation:
                            return "Calculate unread conversations in user folders";
                        case MailOperationRecalculateMailboxProgress.CountTotalUserFolderConversation:
                            return "Calculate total conversations in user folders";
                        case MailOperationRecalculateMailboxProgress.UpdateUserFoldersCounters:
                            return "Update user folders counters";
                        case MailOperationRecalculateMailboxProgress.Finished:
                            return "Finished";
                        default:
                            return status;
                    }
                }
                case MailOperationType.RemoveUserFolder:
                {
                    var progress = op.GetProperty<MailOperationRemoveUserFolderProgress>(MailOperation.PROGRESS);
                    switch (progress)
                    {
                        case MailOperationRemoveUserFolderProgress.Init:
                            return "Setup tenant and user";
                        case MailOperationRemoveUserFolderProgress.MoveMailsToTrash:
                            return "Move mails into Trash folder";
                        case MailOperationRemoveUserFolderProgress.DeleteFolders:
                            return "Delete folder";
                        case MailOperationRemoveUserFolderProgress.Finished:
                            return "Finished";
                        default:
                            return status;
                    }
                }
                default:
                    return status;
            }
        }

        /// <summary>
        /// Limit result per Contact System
        /// </summary>
        private static int MailAutocompleteMaxCountPerSystem
        {
            get
            {
                var count = 20;
                if (ConfigurationManager.AppSettings["mail.autocomplete-max-count"] == null)
                    return count;

                int.TryParse(ConfigurationManager.AppSettings["mail.autocomplete-max-count"], out count);
                return count;
            }
        }

        /// <summary>
        /// Timeout in milliseconds
        /// </summary>
        private static int MailAutocompleteTimeout
        {
            get
            {
                var count = 3000;
                if (ConfigurationManager.AppSettings["mail.autocomplete-timeout"] == null)
                    return count;

                int.TryParse(ConfigurationManager.AppSettings["mail.autocomplete-timeout"], out count);
                return count;
            }
        }
    }
}