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


using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class MailboxSignatureDao : BaseMailDao, IMailboxSignatureDao
    {
        public MailboxSignatureDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public MailboxSignature GetSignature(int mailboxId)
        {
            var query = MailDbContext.MailMailboxSignature
                .Join(MailDbContext.MailMailbox,
                    s => s.IdMailbox,
                    mb => (int)mb.Id,
                    (s, mb) => new MailboxSignature
                    {
                        MailboxId = (int)mb.Id,
                        Tenant = mb.Tenant,
                        Html = s.Html,
                        IsActive = s.IsActive

                    })
                .Where(r => r.MailboxId == mailboxId).FirstOrDefault();

            if (query == null)
            {
                return new MailboxSignature
                {
                    MailboxId = mailboxId,
                    Tenant = Tenant,
                    Html = "",
                    IsActive = false
                };
            }
            return query;
        }

        public List<MailboxSignature> GetSignatures(List<int> mailboxIds)
        {
            var query = MailDbContext.MailMailboxSignature
                .Join(MailDbContext.MailMailbox,
                    s => s.IdMailbox,
                    mb => (int)mb.Id,
                    (s, mb) => new MailboxSignature
                    {
                        MailboxId = (int)mb.Id,
                        Tenant = mb.Tenant,
                        Html = s.Html,
                        IsActive = s.IsActive

                    })
                .Where(r => mailboxIds.Contains(r.MailboxId));

            return (from mailboxId in mailboxIds
                    let sig = query.FirstOrDefault(s => s.MailboxId == mailboxId)
                    select sig ?? new MailboxSignature
                    {
                        MailboxId = mailboxId,
                        Tenant = Tenant,
                        Html = "",
                        IsActive = false
                    })
                    .ToList();
        }

        public int SaveSignature(MailboxSignature signature)
        {
            var dbSignature = new MailMailboxSignature()
            {
                Html = signature.Html,
                IsActive = signature.IsActive,
                Tenant = signature.Tenant,
                IdMailbox = signature.MailboxId
            };

            MailDbContext.MailMailboxSignature.Add(dbSignature);

            var result = MailDbContext.Entry(dbSignature);

            result.State = dbSignature.IdMailbox == 0
                ? EntityState.Added
                : EntityState.Modified;

            return MailDbContext.SaveChanges();
        }

        public int DeleteSignature(int mailboxId)
        {
            var query = MailDbContext.MailMailboxSignature
                .Where(r => r.Tenant == Tenant && r.IdMailbox == mailboxId);

            MailDbContext.MailMailboxSignature.RemoveRange(query);

            return MailDbContext.SaveChanges();
        }
    }
}