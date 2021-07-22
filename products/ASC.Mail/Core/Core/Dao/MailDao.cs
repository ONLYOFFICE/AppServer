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
using System.Collections.Generic;
using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.Message;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Enums;
using ASC.Mail.Utils;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class MailDao : BaseMailDao, IMailDao
    {
        public MailDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public int Save(Core.Entities.Mail mail)
        {
            var mailMail = new MailMail
            {
                Id = mail.Id,
                IdMailbox = mail.MailboxId,
                TenantId = mail.Tenant,
                IdUser = mail.User,
                Address = mail.Address,
                Uidl = mail.Uidl,
                Md5 = mail.Md5,
                FromText = MailUtil.NormalizeStringForMySql(mail.From),
                ToText = MailUtil.NormalizeStringForMySql(mail.To),
                ReplyTo = mail.Reply,
                Subject = MailUtil.NormalizeStringForMySql(mail.Subject),
                Cc = mail.Cc,
                Bcc = mail.Bcc,
                Importance = mail.Importance,
                DateReceived = mail.DateReceived,
                DateSent = mail.DateSent,
                Size = (int)mail.Size,
                AttachmentsCount = mail.AttachCount,
                Unread = mail.Unread,
                IsAnswered = mail.IsAnswered,
                IsForwarded = mail.IsForwarded,
                Stream = mail.Stream,
                Folder = (int)mail.Folder,
                FolderRestore = (int)mail.FolderRestore,
                Spam = mail.Spam,
                MimeMessageId = mail.MimeMessageId,
                MimeInReplyTo = mail.MimeInReplyTo,
                ChainId = mail.ChainId,
                Introduction = MailUtil.NormalizeStringForMySql(mail.Introduction),
                ChainDate = mail.DateSent,
                IsTextBodyOnly = mail.IsTextBodyOnly
            };

            if (mail.HasParseError)
                mailMail.HasParseError = mail.HasParseError;

            if (!string.IsNullOrEmpty(mail.CalendarUid))
                mailMail.CalendarUid = mail.CalendarUid;

            var entry = MailDbContext.AddOrUpdate(m => m.MailMail, mailMail);

            MailDbContext.SaveChanges();

            return entry.Id;
        }

        public Core.Entities.Mail GetMail(IMessageExp exp)
        {
            var mail = MailDbContext.MailMail.Where(exp.GetExpression())
                .Select(ToMail)
                .SingleOrDefault();

            return mail;
        }

        public Core.Entities.Mail GetNextMail(IMessageExp exp)
        {
            var mail = MailDbContext.MailMail.Where(exp.GetExpression())
                .OrderBy(m => m.Id)
                .Take(1)
                .Select(ToMail)
                .SingleOrDefault();

            return mail;
        }

        public List<string> GetExistingUidls(int mailboxId, List<string> uidlList)
        {
            var existingUidls = MailDbContext.MailMail
                .Where(m => m.IdMailbox == mailboxId && uidlList.Contains(m.Uidl))
                .Select(m => m.Uidl)
                .ToList();

            return existingUidls;
        }

        public int SetMessagesChanged(List<int> ids)
        {
            var now = DateTime.UtcNow;

            var mails = MailDbContext.MailMail.Where(m => ids.Contains(m.Id));

            foreach (var mail in mails)
            {
                mail.TimeModified = now;
            }

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected Core.Entities.Mail ToMail(MailMail r)
        {
            var mail = new Core.Entities.Mail
            {
                Id = r.Id,
                MailboxId = r.IdMailbox,
                User = r.IdUser,
                Tenant = r.TenantId,
                Address = r.Address,
                Uidl = r.Uidl,
                Md5 = r.Md5,
                From = r.FromText,
                To = r.ToText,
                Reply = r.ReplyTo,
                Cc = r.Cc,
                Bcc = r.Bcc,
                Subject = r.Subject,
                Introduction = r.Introduction,
                Importance = r.Importance,
                DateReceived = r.DateReceived,
                DateSent = r.DateSent,
                Size = r.Size,
                AttachCount = r.AttachmentsCount,
                Unread = r.Unread,
                IsAnswered = r.IsAnswered,
                IsForwarded = r.IsForwarded,
                Stream = r.Stream,
                Folder = (FolderType) r.Folder,
                FolderRestore = (FolderType) r.FolderRestore,
                Spam = r.Spam,
                IsRemoved = r.IsRemoved,
                TimeModified = r.TimeModified,
                MimeMessageId = r.MimeMessageId,
                MimeInReplyTo = r.MimeInReplyTo,
                ChainId = r.ChainId,
                ChainDate = r.ChainDate,
                IsTextBodyOnly = r.IsTextBodyOnly,
                HasParseError = r.HasParseError,
                CalendarUid = r.CalendarUid
            };

            return mail;
        }
    }
}