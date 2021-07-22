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
using System.Linq;

using ASC.Common.Logging;
using ASC.Core;
using ASC.Data.Storage;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Enums.Filter;
using ASC.Mail.Models;
using ASC.Mail.Storage;

using Microsoft.Extensions.Options;

namespace ASC.Mail.Core.Engine.Operations
{
    public class ApplyFilterOperation : MailOperation
    {
        public MailSieveFilterData Filter { get; set; }

        public override MailOperationType OperationType
        {
            get { return MailOperationType.ApplyFilter; }
        }

        public FilterEngine FilterEngine { get; }
        public MessageEngine MessageEngine { get; }

        public ApplyFilterOperation(
            TenantManager tenantManager,
            SecurityContext securityContext,
            IMailDaoFactory mailDaoFactory,
            FilterEngine filterEngine,
            MessageEngine messageEngine,
            CoreSettings coreSettings,
            StorageManager storageManager,
            StorageFactory storageFactory,
            IOptionsMonitor<ILog> optionsMonitor,
            int filterId)
            : base(tenantManager, securityContext, mailDaoFactory, coreSettings, storageManager, optionsMonitor, storageFactory)
        {
            FilterEngine = filterEngine;
            MessageEngine = messageEngine;
            var filter = FilterEngine.Get(filterId);

            Filter = filter ?? throw new ArgumentException("Filter not found");

            SetSource(filter.Id.ToString());
        }

        protected override void Do()
        {
            try
            {
                SetProgress((int?)MailOperationApplyFilterProgress.Init, "Setup tenant and user");

                TenantManager.SetCurrentTenant(CurrentTenant);

                SecurityContext.AuthenticateMe(CurrentUser);

                SetProgress((int?)MailOperationApplyFilterProgress.Filtering, "Filtering");

                const int size = 100;
                var page = 0;

                var messages = MessageEngine.GetFilteredMessages(Filter, page, size, out long total);

                while (messages.Any())
                {
                    SetProgress((int?)MailOperationApplyFilterProgress.FilteringAndApplying, "Filtering and applying action");

                    var ids = messages.Select(m => m.Id).ToList();

                    foreach (var action in Filter.Actions)
                    {
                        FilterEngine.ApplyAction(ids, action);
                    }

                    if (messages.Count < size)
                        break;

                    if (!Filter.Actions.Exists(a => a.Action == ActionType.DeleteForever || a.Action == ActionType.MoveTo))
                    {
                        page++;
                    }

                    messages = MessageEngine.GetFilteredMessages(Filter, page, size, out total);
                }

                SetProgress((int?)MailOperationApplyFilterProgress.Finished);
            }
            catch (Exception e)
            {
                Logger.Error("Mail operation error -> Remove user folder: {0}", e);
                Error = "InternalServerError";
            }
        }
    }
}
