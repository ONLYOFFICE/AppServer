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
using ASC.Mail.Core.Dao.Expressions.Attachment;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class AttachmentDao : BaseMailDao, IAttachmentDao
    {
        public AttachmentDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext) 
        { 
        }

        public Attachment GetAttachment(IAttachmentExp exp)
        {
            var attachemnt = MailDbContext.MailAttachment
                    .Include(a => a.Mail)
                    .Where(exp.GetExpression())
                    .Select(ToAttachment)
                    .FirstOrDefault();

            return attachemnt;
        }

        public List<Attachment> GetAttachments(IAttachmentsExp exp)
        {
            var attachemnts = MailDbContext.MailAttachment
                    .Include(a => a.Mail)
                    .Where(exp.GetExpression())
                    .Select(ToAttachment)
                    .ToList();

            return attachemnts;
        }

        public long GetAttachmentsSize(IAttachmentsExp exp)
        {
            var size = MailDbContext.MailAttachment
                   .Where(exp.GetExpression())
                   .Sum(a => a.Size);

            return size;
        }

        public int GetAttachmentsMaxFileNumber(IAttachmentsExp exp)
        {
            var list = MailDbContext.MailAttachment
                   .Where(exp.GetExpression())
                   .Select(a => a.FileNumber)
                   .ToList();

            var max = list.Any() ? list.Max() : 0;

            return max;
        }

        public int GetAttachmentsCount(IAttachmentsExp exp)
        {
            var count = MailDbContext.MailAttachment
                   .Where(exp.GetExpression())
                   .Count();

            return count;
        }

        public bool SetAttachmnetsRemoved(IAttachmentsExp exp)
        {
            var attachments = MailDbContext.MailAttachment.Where(exp.GetExpression());

            foreach (var att in attachments)
            {
                att.NeedRemove = true;
            }

            MailDbContext.UpdateRange(attachments);

            var result = MailDbContext.SaveChanges();

            return result > 0;
        }

        public int SaveAttachment(Attachment attachment)
        {
            var mailAttachment = new MailAttachment
            {
                Id = attachment.Id,
                Tenant = attachment.Tenant,
                IdMail = attachment.MailId,
                IdMailbox = attachment.MailboxId,
                Name = attachment.Name,
                StoredName = attachment.StoredName,
                Type = attachment.Type,
                Size = attachment.Size,
                FileNumber = attachment.FileNumber,
                NeedRemove = attachment.IsRemoved,
                ContentId = attachment.ContentId
            };

            var entry = MailDbContext.MailAttachment.Add(mailAttachment);

            MailDbContext.SaveChanges();

            return entry.Entity.Id;
        }

        protected Attachment ToAttachment(MailAttachment r)
        {
            var a = new Attachment
            {
                Id = r.Id,
                MailId = r.IdMail,
                Name = r.Name,
                StoredName = r.StoredName,
                Type = r.Type,
                Size = r.Size,
                IsRemoved = r.NeedRemove,
                FileNumber = r.FileNumber,
                ContentId = r.ContentId,
                Tenant = r.Tenant,
                MailboxId = r.IdMailbox,
                Stream = r.Mail.Stream,
                User = r.Mail.IdUser
            };

            return a;
        }
    }
}