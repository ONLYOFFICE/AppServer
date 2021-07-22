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

using ASC.Common.Logging;
using ASC.Core;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Models;
using ASC.Mail.Storage;

using Microsoft.Extensions.Options;

namespace ASC.Mail.Core.Engine.Operations
{
    public class MailRemoveMailboxOperation : MailOperation
    {
        private readonly MailBoxData _mailBoxData;

        public ILog Log { get; set; }

        public override MailOperationType OperationType
        {
            get { return MailOperationType.RemoveMailbox; }
        }

        public MailboxEngine MailboxEngine { get; }
        public QuotaEngine QuotaEngine { get; }
        public FolderEngine FolderEngine { get; }
        public CacheEngine CacheEngine { get; }
        public IndexEngine IndexEngine { get; }

        public MailRemoveMailboxOperation(
            TenantManager tenantManager,
            SecurityContext securityContext,
            MailboxEngine mailboxEngine,
            QuotaEngine quotaEngine,
            FolderEngine folderEngine,
            CacheEngine cacheEngine,
            IndexEngine indexEngine,
            IMailDaoFactory mailDaoFactory,
            CoreSettings coreSettings,
            StorageManager storageManager,
            IOptionsMonitor<ILog> optionsMonitor,
            MailBoxData mailBoxData)
            : base(tenantManager, securityContext, mailDaoFactory, coreSettings, storageManager, optionsMonitor)
        {
            MailboxEngine = mailboxEngine;
            QuotaEngine = quotaEngine;
            FolderEngine = folderEngine;
            CacheEngine = cacheEngine;
            IndexEngine = indexEngine;
            _mailBoxData = mailBoxData;

            SetSource(_mailBoxData.MailBoxId.ToString());
        }

        protected override void Do()
        {
            try
            {
                SetProgress((int?)MailOperationRemoveMailboxProgress.Init, "Setup tenant and user");

                TenantManager.SetCurrentTenant(CurrentTenant);

                SecurityContext.AuthenticateMe(CurrentUser);

                SetProgress((int?)MailOperationRemoveMailboxProgress.RemoveFromDb, "Remove mailbox from Db");

                var freedQuotaSize = MailboxEngine.RemoveMailBoxInfo(_mailBoxData);

                SetProgress((int?)MailOperationRemoveMailboxProgress.FreeQuota, "Decrease newly freed quota space");

                QuotaEngine.QuotaUsedDelete(freedQuotaSize);

                SetProgress((int?)MailOperationRemoveMailboxProgress.RecalculateFolder, "Recalculate folders counters");

                FolderEngine.RecalculateFolders();

                SetProgress((int?)MailOperationRemoveMailboxProgress.ClearCache, "Clear accounts cache");

                CacheEngine.Clear(_mailBoxData.UserId);

                SetProgress((int?)MailOperationRemoveMailboxProgress.RemoveIndex, "Remove Elastic Search index by messages");

                IndexEngine.Remove(_mailBoxData);

                SetProgress((int?)MailOperationRemoveMailboxProgress.Finished);
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Mail operation error -> Remove mailbox: {0}", e.ToString());
                Error = "InternalServerError";
            }
        }


    }
}
