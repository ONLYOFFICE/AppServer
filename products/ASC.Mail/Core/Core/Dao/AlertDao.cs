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
using System.Linq;
using System.Collections.Generic;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;
using ASC.Mail.Core.Dao.Entities;
using ASC.Common;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class AlertDao : BaseMailDao, IAlertDao
    {
        public AlertDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public Alert GetAlert(long id)
        {
            var alert = MailDbContext.MailAlerts
                .Where(r => r.Tenant == Tenant)
                .Where(r => r.IdUser == UserId)
                .Where(r => r.Id == id)
                .Select(r => new Alert
                {
                    Id = r.Id,
                    MailboxId = r.IdMailbox,
                    Type = r.Type,
                    Data = r.Data
                }).SingleOrDefault();

            return alert;
        }

        public List<Alert> GetAlerts(int mailboxId = -1, MailAlertTypes type = MailAlertTypes.Empty)
        {
            var alerts = MailDbContext.MailAlerts
                .Where(r => r.Tenant == Tenant)
                .Where(r => r.IdUser == UserId)
                .Select(r => new Alert
                {
                    Id = r.Id,
                    MailboxId = r.IdMailbox,
                    Type = r.Type,
                    Data = r.Data
                }).ToList();

            return alerts;
        }

        public int SaveAlert(Alert alert, bool unique = false)
        {
            if (unique)
            {
                var alerts = GetAlerts(alert.MailboxId, alert.Type);

                if (alerts.Any())
                {
                    var result = DeleteAlerts(alerts.Select(a => a.Id).ToList());

                    if (result <= 0)
                        throw new Exception("Delete old alerts failed");
                }
            }

            using var tr = MailDbContext.Database.BeginTransaction();

            var dbAlert = new MailAlert()
            {
                Id = alert.Id,
                Tenant = Tenant,
                IdUser = UserId,
                IdMailbox = alert.MailboxId,
                Type = alert.Type,
                Data = alert.Data
            };

            var saveResult = MailDbContext.MailAlerts.Add(dbAlert).Entity;

            MailDbContext.SaveChanges();

            tr.Commit();

            return saveResult.Id;
        }

        public int DeleteAlert(long id)
        {
            using var tr = MailDbContext.Database.BeginTransaction();

            var range = MailDbContext.MailAlerts.Where(r => r.Tenant == Tenant && r.IdUser == UserId && r.Id == id);

            MailDbContext.MailAlerts.RemoveRange(range);

            var count = MailDbContext.SaveChanges();

            tr.Commit();

            return count;
        }

        public int DeleteAlerts(int mailboxId)
        {
            using var tr = MailDbContext.Database.BeginTransaction();

            var range = MailDbContext.MailAlerts
                .Where(r => r.Tenant == Tenant && r.IdUser == UserId && r.IdMailbox == mailboxId);

            MailDbContext.MailAlerts.RemoveRange(range);

            var count = MailDbContext.SaveChanges();

            tr.Commit();

            return count;
        }

        public int DeleteAlerts(List<int> ids)
        {
            using var tr = MailDbContext.Database.BeginTransaction();

            var range = MailDbContext.MailAlerts
                .Where(r => r.Tenant == Tenant && r.IdUser == UserId)
                .Where(r => ids.Contains(r.Id));

            MailDbContext.MailAlerts.RemoveRange(range);

            var count = MailDbContext.SaveChanges();

            tr.Commit();

            return count;
        }

    }
}