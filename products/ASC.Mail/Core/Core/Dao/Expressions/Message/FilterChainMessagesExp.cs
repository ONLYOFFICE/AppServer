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
using ASC.Mail.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ASC.Mail.Core.Dao.Expressions.Message
{
    public class FilterChainMessagesExp : FilterMessagesExp
    {
        public FilterChainMessagesExp(MailSearchFilterData filter, int tenant, string user, List<int> ids = null)
            : base(ids ?? new List<int>() , tenant, user, filter)
        {
        }

        public override Expression<Func<MailMail, bool>> GetExpression()
        {
            var exp = base.GetExpression();

            if (Filter.FromDate.HasValue)
            {
                var prevFlag = Filter.PrevFlag.GetValueOrDefault(false);

                if (Filter.SortOrder == DefineConstants.DESCENDING)
                {
                    exp = prevFlag
                        ? exp.And(m => m.ChainDate >= Filter.FromDate.Value)
                        : exp.And(m => m.ChainDate <= Filter.FromDate.Value);
                }
                else
                {
                    exp = prevFlag
                        ? exp.And(m => m.ChainDate <= Filter.FromDate.Value)
                        : exp.And(m => m.ChainDate >= Filter.FromDate.Value);
                }

                if (prevFlag)
                {
                    OrderAsc = string.IsNullOrEmpty(Filter.SortOrder)
                    ? (bool?)null
                    : !OrderAsc.Value;
                }
            }

            return exp;
        }

        public static bool TryGetFullTextSearchChains(
            FactoryIndexer<MailMail> factoryIndexer,
            IServiceProvider serviceProvider,
            MailSearchFilterData filter, string user, out List<MailMail> mailWrappers)
        {
            mailWrappers = new List<MailMail>();

            var t = serviceProvider.GetService<MailMail>();
            if (!factoryIndexer.Support(t))
            {
                return false;
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

            selector.Where(r => r.Folder, (int)filter.PrimaryFolder);

            if (filter.MailboxId.HasValue)
            {
                selector.Where(r => r.IdMailbox, filter.MailboxId.Value);
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
                if(filter.Attachments.Value)
                    selector.Gt(s => s.AttachmentsCount, 0);
                else
                    selector.Where(s => s.AttachmentsCount, 0);
            }

            if (filter.PrimaryFolder == FolderType.UserFolder && filter.UserFolderId.HasValue)
            {
                selector.InAll(s => s.UserFolders.Select(f => f.Id), new[] { filter.UserFolderId.Value });
            }

            if (filter.WithCalendar.HasValue)
            {
                //TODO: Fix selector.Where(m => m.WithCalendar, filter.WithCalendar.Value);
            }

            if (filter.CustomLabels != null && filter.CustomLabels.Any())
            {
                selector.InAll(r => r.Tags.Select(t => t.Id), filter.CustomLabels.ToArray());
            }

            if (filter.FromDate.HasValue)
            {
                var prevFlag = filter.PrevFlag.GetValueOrDefault(false);

                if (filter.SortOrder == DefineConstants.DESCENDING)
                {
                    if (prevFlag)
                    {
                        selector.Ge(r => r.ChainDate, filter.FromDate.Value);
                    }
                    else
                    {
                        selector.Le(r => r.ChainDate, filter.FromDate.Value);
                    }
                }
                else
                {
                    if (prevFlag)
                    {
                        selector.Le(r => r.ChainDate, filter.FromDate.Value);
                    }
                    else
                    {
                        selector.Ge(r => r.ChainDate, filter.FromDate.Value);
                    }
                }
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

            if (filter.Page.HasValue)
            {
                selector.Limit(filter.Page.Value, filter.PageSize.GetValueOrDefault(25));
            }
            else if (filter.PageSize.HasValue)
            {
                selector.Limit(0, filter.PageSize.Value);
            }

            selector.Where(r => r.IdUser, userId.ToString())
                .Sort(r => r.ChainDate, filter.SortOrder == DefineConstants.ASCENDING);

            if (!factoryIndexer.TrySelect(s => selector, out IReadOnlyCollection<MailMail> result))
                return false;

            mailWrappers = result.ToList();

            return true;
        }
    }
}