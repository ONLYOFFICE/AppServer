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
using System.Linq;

using ASC.Common.Logging;
using ASC.Core;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Models;
using ASC.Mail.Storage;

using Microsoft.Extensions.Options;

namespace ASC.Mail.Core.Engine.Operations
{
    public class MailRemoveMailserverDomainOperation : MailOperation
    {
        private readonly ServerDomainData _domain;

        public override MailOperationType OperationType
        {
            get { return MailOperationType.RemoveDomain; }
        }

        public MailboxEngine MailboxEngine { get; }
        public CacheEngine CacheEngine { get; }
        public IndexEngine IndexEngine { get; }

        public MailRemoveMailserverDomainOperation(
            TenantManager tenantManager,
            SecurityContext securityContext,
            IMailDaoFactory mailDaoFactory,
            MailboxEngine mailboxEngine,
            CacheEngine cacheEngine,
            IndexEngine indexEngine,
            CoreSettings coreSettings,
            StorageManager storageManager,
            IOptionsMonitor<ILog> optionsMonitor,
            ServerDomainData domain)
            : base(tenantManager, securityContext, mailDaoFactory, coreSettings, storageManager, optionsMonitor)
        {
            MailboxEngine = mailboxEngine;
            CacheEngine = cacheEngine;
            IndexEngine = indexEngine;
            _domain = domain;

            SetSource(_domain.Id.ToString());
        }

        protected override void Do()
        {
            try
            {
                SetProgress((int?)MailOperationRemoveDomainProgress.Init, "Setup tenant and user");

                TenantManager.SetCurrentTenant(CurrentTenant);

                try
                {
                    SecurityContext.AuthenticateMe(CurrentUser);
                }
                catch
                {
                    // User was removed
                    SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);
                }

                SetProgress((int?)MailOperationRemoveDomainProgress.RemoveFromDb, "Remove domain from Db");

                var tenant = CurrentTenant.TenantId;

                var mailboxes = new List<MailBoxData>();

                // using (var db = new DbManager(Defines.CONNECTION_STRING_NAME, Defines.RemoveDomainTimeout))

                using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    var groups = MailDaoFactory.GetServerGroupDao().GetList(_domain.Id);

                    foreach (var serverGroup in groups)
                    {
                        MailDaoFactory.GetServerAddressDao().DeleteAddressesFromMailGroup(serverGroup.Id);
                        MailDaoFactory.GetServerAddressDao().Delete(serverGroup.AddressId);
                        MailDaoFactory.GetServerGroupDao().Delete(serverGroup.Id);
                    }

                    var serverAddresses = MailDaoFactory.GetServerAddressDao().GetDomainAddresses(_domain.Id);

                    var serverMailboxAddresses = serverAddresses.Where(a => a.MailboxId > -1 && !a.IsAlias);

                    foreach (var serverMailboxAddress in serverMailboxAddresses)
                    {
                        var mailbox =
                            MailboxEngine.GetMailboxData(
                                new ConcreteTenantServerMailboxExp(serverMailboxAddress.MailboxId, tenant, false));

                        if (mailbox == null)
                            continue;

                        mailboxes.Add(mailbox);

                        MailboxEngine.RemoveMailBox(mailbox, false);
                    }

                    MailDaoFactory.GetServerAddressDao().Delete(serverAddresses.Select(a => a.Id).ToList());

                    MailDaoFactory.GetServerDomainDao().Delete(_domain.Id);

                    var server = MailDaoFactory.GetServerDao().Get(tenant);

                    var serverEngine = new Server.Core.ServerEngine(server.Id, server.ConnectionString);

                    serverEngine.RemoveDomain(_domain.Name);

                    tx.Commit();
                }

                SetProgress((int?)MailOperationRemoveDomainProgress.ClearCache, "Clear accounts cache");

                CacheEngine.ClearAll();

                SetProgress((int?)MailOperationRemoveDomainProgress.RemoveIndex, "Remove Elastic Search index by messages");

                foreach (var mailbox in mailboxes)
                {
                    IndexEngine.Remove(mailbox);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Mail operation error -> Remove mailbox: {0}", e);
                Error = "InternalServerError";
            }
        }
    }
}
