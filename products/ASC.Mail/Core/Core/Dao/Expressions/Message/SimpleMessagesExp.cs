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


using ASC.Mail.Core.Dao.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ASC.Mail.Core.Dao.Expressions.Message
{
    public class SimpleMessagesExp : IMessagesExp
    {
        public int Tenant { get; private set; }

        public string User { get; set; }
        public bool? IsRemoved { get; set; }
        public int? Folder { get; set; }
        public int? MailboxId { get; set; }
        public string ChainId { get; set; }
        public string Md5 { get; set; }
        public string MimeMessageId { get; set; }
        public int? MessageId { get; set; }
        public bool? Unread { get; set; }

        public List<int> MessageIds { get; set; }
        public List<int> FoldersIds { get; set; }
        public List<string> ChainIds { get; set; }
        public List<int> TagIds { get; set; }
        public int? UserFolderId { get; set; }

        public string OrderBy { get; set; }
        public bool? OrderAsc { get; set; }
        public int? StartIndex { get; set; }
        public int? Limit { get; set; }

        public string Subject { get; set; }

        public DateTime? DateSent { get; set; }

        public Expression<Func<MailMail, bool>> Exp { get; set; }

        public SimpleMessagesExp(int tenant)
        {
            Tenant = tenant;
        }

        public SimpleMessagesExp(int tenant, string user, bool? isRemoved = false)
        {
            Tenant = tenant;
            User = user;
            IsRemoved = isRemoved;
        }

        public static MessagesExpBuilder CreateBuilder(int tenant, string user, bool? isRemoved = false)
        {
            return new MessagesExpBuilder(tenant, user, isRemoved);
        }

        public static MessagesExpBuilder CreateBuilder(int tenant)
        {
            return new MessagesExpBuilder(tenant);
        }

        private const string MM_ALIAS = "mm";

        public Expression<Func<MailMail, bool>> GetExpression()
        {
            Expression<Func<MailMail, bool>> exp = m => m.TenantId == Tenant;

            if (!string.IsNullOrEmpty(User))
            {
                exp = exp.And(m => m.IdUser == User);
            }

            if (MessageId.HasValue)
            {
                exp = exp.And(m => m.Id == MessageId.Value);
            }

            if (MessageIds != null)
            {
                exp = exp.And(m => MessageIds.Contains(m.Id));
            }

            if (ChainIds != null)
            {
                exp = exp.And(m => ChainIds.Contains(m.ChainId));
            }

            if (Folder.HasValue)
            {
                exp = exp.And(m => m.Folder == Folder.Value);
            }

            if (IsRemoved.HasValue)
            {
                exp = exp.And(m => m.IsRemoved == IsRemoved.Value);
            }

            if (MailboxId.HasValue)
            {
                exp = exp.And(m => m.IdMailbox == MailboxId.Value);
            }

            if (!string.IsNullOrEmpty(Md5))
            {
                exp = exp.And(m => m.Md5 == Md5);
            }

            if (!string.IsNullOrEmpty(MimeMessageId))
            {
                exp = exp.And(m => m.MimeMessageId == MimeMessageId);
            }

            if (!string.IsNullOrEmpty(ChainId))
            {
                exp = exp.And(m => m.ChainId == ChainId);
            }

            if (FoldersIds != null && FoldersIds.Any())
            {
                exp = exp.And(m => FoldersIds.Contains(m.Folder));
            }

            if (Unread.HasValue)
            {
                exp = exp.And(m => m.Unread == Unread.Value);
            }

            if (!string.IsNullOrEmpty(Subject))
            {
                exp = exp.And(m => m.Subject.Equals(Subject));
            }

            if (DateSent.HasValue)
            {
                exp = exp.And(m => m.DateSent.Equals(DateSent.Value));
            }

            if (Exp != null)
            {
                exp = exp.And(Exp);
            }

            return exp;
        }
    }
}