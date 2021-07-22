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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.Message;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class MailInfoDao : BaseMailDao, IMailInfoDao
    {
        public MailInfoDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        { 
        }

        public List<MailInfo> GetMailInfoList(IMessagesExp exp, bool skipSelectTags = false)
        {
            var query = MailDbContext.MailMail
                .Where(exp.GetExpression());

            if (exp.TagIds != null && exp.TagIds.Any())
            {
                query = query.Where(m => 
                    MailDbContext.MailTagMail
                        .Where(t => t.IdMail == m.Id && t.Tenant == Tenant && t.IdUser == UserId)
                        .Count() == exp.TagIds.Count);
            }

            if (exp.UserFolderId.HasValue)
            {
                query = query.Where(m =>
                    MailDbContext.MailUserFolderXMail
                        .Where(t => t.IdMail == m.Id && t.Tenant == Tenant && t.IdUser == UserId && t.IdFolder == exp.UserFolderId.Value)
                        .FirstOrDefault() != null);
            }

            if (exp.StartIndex.HasValue)
            {
                query = query.Skip(exp.StartIndex.Value);
            }

            if (exp.Limit.HasValue)
            {
                query = query.Take(exp.Limit.Value);
            }

            if (!string.IsNullOrEmpty(exp.OrderBy))
            {
                var sortField = "DateSent";

                if (exp.OrderBy == DefineConstants.ORDER_BY_SUBJECT)
                {
                    sortField = "Subject";
                }
                else if (exp.OrderBy == DefineConstants.ORDER_BY_SENDER)
                {
                    sortField = "FromText";
                }
                else if (exp.OrderBy == DefineConstants.ORDER_BY_DATE_CHAIN)
                {
                    sortField = "ChainDate";
                }

                query = query.OrderBy(sortField, exp.OrderAsc.GetValueOrDefault());
            }

            var list = query
                .Select(m => new { 
                    Mail = m,
                    LabelsString = skipSelectTags ? "" : string.Join(",", MailDbContext.MailTagMail.Where(t => t.IdMail == m.Id).Select(t => t.IdTag))
                })
                .Select(x => ToMailInfo(x.Mail, x.LabelsString))
                .ToList();

            return list;
        }

        public long GetMailInfoTotal(IMessagesExp exp)
        {
            var query = MailDbContext.MailMail
                .Where(exp.GetExpression());

            if (exp.TagIds != null && exp.TagIds.Any())
            {
                query = query.Where(m =>
                    MailDbContext.MailTagMail
                        .Where(t => t.IdMail == m.Id && t.Tenant == Tenant && t.IdUser == UserId)
                        .Count() == exp.TagIds.Count);
            }

            if (exp.UserFolderId.HasValue)
            {
                query = query.Where(m =>
                    MailDbContext.MailUserFolderXMail
                        .Where(t => t.IdMail == m.Id && t.Tenant == Tenant && t.IdUser == UserId && t.IdFolder == exp.UserFolderId.Value)
                        .FirstOrDefault() != null);
            }

            var total = query.Count();

            return total;
        }

        public Dictionary<int, int> GetMailCount(IMessagesExp exp)
        {
            var dictionary = MailDbContext.MailMail
                .Where(exp.GetExpression())
                .GroupBy(m => m.Folder)
                .Select(g => new
                {
                    FolderId = g.Key,
                    Count = g.Count()
                })
                .ToDictionary(o => o.FolderId, o => o.Count);

            return dictionary;
        }

        public Dictionary<int, int> GetMailUserFolderCount(List<int> userFolderIds, bool? unread = null)
        {
            var query = MailDbContext.MailUserFolderXMail
                .Join(MailDbContext.MailMail, x => (int)x.IdMail, m => m.Id,
                (x, m) => new
                {
                    UFxMail = x,
                    Mail = m
                })
                .Where(t => t.UFxMail.Tenant == Tenant)
                .Where(t => t.UFxMail.IdUser == UserId)
                .Where(t => userFolderIds.Contains(t.UFxMail.IdFolder));

            if (unread.HasValue) {
                query = query.Where(t => t.Mail.Unread == unread.Value);
            }

            var dictionary = query
                .ToList()
                .GroupBy(t => t.UFxMail.IdFolder)
                .ToDictionary(g => g.Key, g => g.Count());

            return dictionary;
        }

        public Dictionary<int, int> GetMailUserFolderCount(bool? unread = null)
        {
            var query = MailDbContext.MailUserFolderXMail
                .Join(MailDbContext.MailMail, x => (int)x.IdMail, m => m.Id,
                (x, m) => new
                {
                    UFxMail = x,
                    Mail = m
                })
                .Where(t => t.UFxMail.Tenant == Tenant)
                .Where(t => t.UFxMail.IdUser == UserId);

            if (unread.HasValue)
            {
                query = query.Where(t => t.Mail.Unread == unread.Value);
            }

            var dictionary = query
                .ToList()
                .GroupBy(t => t.UFxMail.IdFolder)
                .ToDictionary(g => g.Key, g => g.Count());

            return dictionary;
        }

        public Tuple<int, int> GetRangeMails(IMessagesExp exp)
        {
            //TODO: fix: make one query

            var max = MailDbContext.MailMail
                .Where(exp.GetExpression())
                .Max(m => m.Id);

            var min = MailDbContext.MailMail
                .Where(exp.GetExpression())
                .Min(m => m.Id);

            return new Tuple<int, int>(min, max);
        }

        public T GetFieldMaxValue<T>(IMessagesExp exp, string field)
        {
            Type type = typeof(MailMail);
            PropertyInfo pi = type.GetProperty(field);

            if (pi == null)
                throw new ArgumentException("Field not found");

            var x = Expression.Parameter(typeof(MailMail), "x");
            var body = Expression.PropertyOrField(x, field);
            var lambda = Expression.Lambda<Func<MailMail, T>>(body, x);

            var max = MailDbContext.MailMail
                .Where(exp.GetExpression())
                .Select(lambda.Compile())
                .DefaultIfEmpty<T>()
                .Max();

            return (T)max;
        }

        public int SetFieldValue<T>(IMessagesExp exp, string field, T value)
        {
            Type type = typeof(MailMail);
            PropertyInfo pi = type.GetProperty(field);

            if (pi == null)
                throw new ArgumentException("Field not found");

            var mails = MailDbContext.MailMail
                .Where(exp.GetExpression())
                .ToList();

            foreach (var mail in mails)
            {
                pi.SetValue(mail, Convert.ChangeType(value, pi.PropertyType), null);
            }

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int SetFieldsEqual(IMessagesExp exp, string fieldFrom, string fieldTo)
        {
            Type type = typeof(MailMail);
            PropertyInfo piFrom = type.GetProperty(fieldFrom);
            PropertyInfo piTo = type.GetProperty(fieldTo);

            if (piFrom == null)
                throw new ArgumentException("FieldFrom not found");

            if (piTo == null)
                throw new ArgumentException("FieldTo not found");

            var mails = MailDbContext.MailMail
                .Where(exp.GetExpression())
                .ToList();

            foreach (var mail in mails)
            {
                var value = piFrom.GetValue(mail);

                piTo.SetValue(mail, Convert.ChangeType(value, piFrom.PropertyType), null);
            }

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public List<MailInfo> GetChainedMessagesInfo(List<int> ids)
        {
            var chainsInfo = GetMailInfoList(
                SimpleMessagesExp.CreateBuilder(Tenant, UserId)
                    .SetMessageIds(ids)
                    .Build());

            var chainArray = chainsInfo.Select(r => r.ChainId).Distinct().ToArray();

            const int max_query_count = 25;
            var i = 0;
            var unsortedMessages = new List<MailInfo>();

            do
            {
                var partChains = chainArray.Skip(i).Take(max_query_count).ToList();

                if (!partChains.Any())
                    break;

                var exp = SimpleMessagesExp.CreateBuilder(Tenant, UserId)
                        .SetChainIds(partChains)
                        .Build();

                var selectedMessages = GetMailInfoList(exp);

                unsortedMessages.AddRange(selectedMessages);

                i += max_query_count;

            } while (true);

            var result = unsortedMessages
                .Where(r => chainsInfo.FirstOrDefault(c =>
                    c.ChainId == r.ChainId &&
                    c.MailboxId == r.MailboxId &&
                    ((r.Folder == FolderType.Inbox || r.Folder == FolderType.Sent)
                        ? c.Folder == FolderType.Inbox || c.Folder == FolderType.Sent
                        : c.Folder == r.Folder)) != null)
                .ToList();

            return result;
        }

        protected static MailInfo ToMailInfo(MailMail r, string labelsString)
        {
            var mailInfo = new MailInfo
            {
                Id = r.Id,
                From = r.FromText,
                To = r.ToText,
                Cc = r.Cc,
                ReplyTo = r.ReplyTo,
                Subject = r.Subject,
                Importance = r.Importance,
                DateSent = r.DateSent,
                Size = r.Size,
                HasAttachments = r.AttachmentsCount > 0,
                IsNew = r.Unread,
                IsAnswered = r.IsAnswered,
                IsForwarded = r.IsForwarded,
                LabelsString = labelsString,
                FolderRestore = (FolderType) r.FolderRestore,
                Folder = (FolderType) r.Folder,
                ChainId = r.ChainId,
                ChainDate = r.ChainDate,
                MailboxId = r.IdMailbox,
                CalendarUid = string.IsNullOrEmpty(r.CalendarUid) ? null : r.CalendarUid,
                Stream = r.Stream,
                Uidl = r.Uidl,
                IsRemoved = r.IsRemoved,
                Intoduction = r.Introduction
            };

            return mailInfo;
        }
    }
}