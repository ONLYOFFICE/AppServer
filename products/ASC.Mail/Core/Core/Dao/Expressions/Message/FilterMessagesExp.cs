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
using System.Linq.Expressions;
using ASC.ElasticSearch;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Enums;
using ASC.Mail.Extensions;
using ASC.Mail.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ASC.Mail.Core.Dao.Expressions.Message
{
    public class FilterMessagesExp : IMessagesExp
    {
        public List<int> Ids { get; private set; }
        public MailSearchFilterData Filter { get; private set; }
        public int Tenant { get; private set; }
        public string User { get; private set; }

        public string OrderBy
        {
            get { return Filter.Sort; }
        }

        public bool? OrderAsc { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }

        public List<int> TagIds
        {
            get { return Filter.CustomLabels; }
        }

        public int? UserFolderId
        {
            get { return Filter.UserFolderId; }
        }

        public FilterMessagesExp(List<int> ids, int tenant, string user, MailSearchFilterData filter)
        {
            Filter = filter;
            Tenant = tenant;
            User = user;
            Ids = ids;

            StartIndex = filter.Page.HasValue ? 0 : (int?) null;

            if (filter.Page.HasValue
                && filter.Page.Value > 0
                && filter.PageSize.HasValue
                && filter.PageSize.Value > 0)
            {
                StartIndex = filter.Page.Value*filter.PageSize;
            }

            Limit = Filter.PageSize;

            OrderAsc = string.IsNullOrEmpty(Filter.SortOrder)
                    ? (bool?)null
                    : Filter.SortOrder == DefineConstants.ASCENDING;
        }

        private const string MM_ALIAS = "mm";

        public virtual Expression<Func<MailMail, bool>> GetExpression()
        {
            Expression<Func<MailMail, bool>> filterExp = m => m.Folder == (int)Filter.PrimaryFolder;

            if (Filter.Unread.HasValue)
            {
                filterExp = filterExp.And(m => m.Unread == Filter.Unread);
            }

            if (Filter.Attachments.HasValue)
                filterExp = filterExp.And(m => m.AttachmentsCount == 0);

            if (Filter.PeriodFrom.HasValue && Filter.PeriodTo.HasValue)
            {
                var fromTs = TimeSpan.FromMilliseconds(Filter.PeriodFrom.Value);
                var from = DefineConstants.BaseJsDateTime.Add(fromTs);

                var toTs = TimeSpan.FromMilliseconds(Filter.PeriodTo.Value);
                var to = DefineConstants.BaseJsDateTime.Add(toTs);

                filterExp = filterExp.And(m => m.DateSent > from && m.DateSent < to);
            }

            if (Filter.Important.HasValue)
            {
                filterExp = filterExp.And(m => m.Importance == true);
            }

            if (Filter.WithCalendar.HasValue)
            {
                filterExp = filterExp.And(m => m.CalendarUid != null);
            }

            if (!string.IsNullOrEmpty(Filter.FromAddress)) //TODO: fix && !FactoryIndexer<MailWrapper>.Support)
            {
                if (Filter.PrimaryFolder == FolderType.Sent || Filter.PrimaryFolder == FolderType.Draft)
                    filterExp = filterExp.And(m => m.ToText.Contains(Filter.FromAddress, StringComparison.InvariantCultureIgnoreCase));
                else
                    filterExp = filterExp.And(m => m.FromText.Contains(Filter.FromAddress, StringComparison.InvariantCultureIgnoreCase));
            }

            if (!string.IsNullOrEmpty(Filter.ToAddress)) //TODO: fix  && !FactoryIndexer<MailWrapper>.Support)
            {
                if (Filter.PrimaryFolder == FolderType.Sent || Filter.PrimaryFolder == FolderType.Draft)
                    filterExp = filterExp.And(m => m.FromText.Contains(Filter.ToAddress, StringComparison.InvariantCultureIgnoreCase));
                else
                    filterExp = filterExp.And(m => m.ToText.Contains(Filter.ToAddress, StringComparison.InvariantCultureIgnoreCase));
            }

            if (Filter.MailboxId.HasValue)
            {
                filterExp = filterExp.And(m => m.IdMailbox == Filter.MailboxId.Value);
            }

            if (!string.IsNullOrEmpty(Filter.SearchText)) //TODO: fix  && !FactoryIndexer<MailWrapper>.Support)
            {
                filterExp = filterExp.And(m => 
                        m.FromText.Contains(Filter.SearchText, StringComparison.InvariantCultureIgnoreCase)
                     || m.ToText.Contains(Filter.SearchText, StringComparison.InvariantCultureIgnoreCase)
                     || m.Cc.Contains(Filter.SearchText, StringComparison.InvariantCultureIgnoreCase)
                     || m.Bcc.Contains(Filter.SearchText, StringComparison.InvariantCultureIgnoreCase)
                     || m.Subject.Contains(Filter.SearchText, StringComparison.InvariantCultureIgnoreCase)
                     || m.Introduction.Contains(Filter.SearchText, StringComparison.InvariantCultureIgnoreCase));
            }

            if (Ids != null && Ids.Any())
            {
                filterExp = filterExp.And(m => Ids.Contains(m.Id));
            }

            filterExp = filterExp.And(m => m.TenantId == Tenant && m.IdUser == User && m.IsRemoved == false);

            return filterExp;
        }

        public static bool TryGetFullTextSearchIds(
            FactoryIndexer<MailMail> factoryIndexer,
            IServiceProvider serviceProvider, 
            MailSearchFilterData filter, 
            string user, out List<int> ids, out long total, DateTime? dateSend = null)
        {
            ids = new List<int>();

            var t = serviceProvider.GetService<MailMail>();
            if (!factoryIndexer.Support(t))
            {
                total = 0;
                return false;
            }

            if (filter.Page.HasValue && filter.Page.Value < 0) {
                total = 0;
                return true;
            }

            var userId = new Guid(user);

            Selector<MailMail> selector = null;

            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                selector = new Selector<MailMail>(serviceProvider).MatchAll(filter.SearchText);
            }

            if (!string.IsNullOrEmpty(filter.FromAddress))
            {
                Selector<MailMail> tempSelector;

                if (filter.PrimaryFolder == FolderType.Sent || filter.PrimaryFolder == FolderType.Draft)
                {
                    tempSelector = new Selector<MailMail>(serviceProvider).Match(s => s.ToText, filter.FromAddress);
                }
                else
                {
                    tempSelector = new Selector<MailMail>(serviceProvider).Match(s => s.FromText, filter.FromAddress);
                }

                if (selector != null)
                    selector &= tempSelector;
                else
                    selector = tempSelector;
            }

            if (!string.IsNullOrEmpty(filter.ToAddress))
            {
                Selector<MailMail> tempSelector;

                if (filter.PrimaryFolder == FolderType.Sent || filter.PrimaryFolder == FolderType.Draft)
                {
                    tempSelector = new Selector<MailMail>(serviceProvider).Match(s => s.FromText, filter.ToAddress);
                }
                else
                {
                    tempSelector = new Selector<MailMail>(serviceProvider).Match(s => s.ToText, filter.ToAddress);
                }

                if (selector != null)
                    selector &= tempSelector;
                else
                    selector = tempSelector;
            }

            if (selector == null)
                selector = new Selector<MailMail>(serviceProvider);

            selector.Where(r => r.Folder, (int) filter.PrimaryFolder);

            if (filter.MailboxId.HasValue)
            {
                selector.Where(s => s.IdMailbox, filter.MailboxId.Value);
            }

            if (filter.Unread.HasValue)
            {
                selector.Where(s => s.Unread, filter.Unread.Value);
            }

            if (filter.Important.HasValue)
            {
                selector.Where(s => s.Importance, filter.Important.Value);
            }

            if (filter.Attachments.HasValue)
            {
                selector.Where(s => s.HasAttachments, filter.Attachments.Value);
            }

            if (filter.PrimaryFolder == FolderType.UserFolder && filter.UserFolderId.HasValue)
            {
                selector.InAll(s => s.UserFolders.Select(f => f.Id), new[] { filter.UserFolderId.Value });
            }

            if (filter.WithCalendar.HasValue)
            {
                selector.Where(m => m.WithCalendar, filter.WithCalendar.Value);
            }

            if (dateSend.HasValue)
            {
                if (filter.SortOrder == DefineConstants.ASCENDING)
                {
                    selector.Ge(r => r.DateSent, dateSend.Value);
                }
                else
                {
                    selector.Le(r => r.DateSent, dateSend.Value);
                }
            }

            if (filter.CustomLabels != null && filter.CustomLabels.Any())
            {
                selector.InAll(r => r.Tags.Select(t => t.Id), filter.CustomLabels.ToArray());
            }

            if (filter.PeriodFrom.HasValue && filter.PeriodTo.HasValue)
            {
                var fromTs = TimeSpan.FromMilliseconds(filter.PeriodFrom.Value);
                var from = DefineConstants.BaseJsDateTime.Add(fromTs);

                var toTs = TimeSpan.FromMilliseconds(filter.PeriodTo.Value);
                var to = DefineConstants.BaseJsDateTime.Add(toTs);

                selector.Ge(s => s.DateSent, from);
                selector.Le(s => s.DateSent, to);
            }

            var pageSize = filter.PageSize.GetValueOrDefault(25);

            if (filter.Page.HasValue && filter.Page.Value > 0)
            {
                selector.Limit(filter.Page.Value * pageSize, pageSize);
            }
            else if (filter.PageSize.HasValue)
            {
                selector.Limit(0, pageSize);
            }

            selector.Where(r => r.IdUser, userId.ToString())
                .Sort(r => r.DateSent, filter.SortOrder == DefineConstants.ASCENDING);

            return factoryIndexer.TrySelectIds(s => selector, out ids, out total);
        }
    }
}