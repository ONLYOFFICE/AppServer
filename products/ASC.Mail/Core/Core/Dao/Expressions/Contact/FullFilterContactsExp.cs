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
using ASC.ElasticSearch;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Enums;
using ASC.Mail.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ASC.Mail.Core.Dao.Expressions.Contact
{
    [Scope]
    public class FullFilterContactsExp : SimpleFilterContactsExp
    {
        public ContactInfoType? InfoType { get; private set; }
        public bool? IsPrimary { get; private set; }
        public MailDbContext MailDb { get; }
        public FactoryIndexer<MailContact> FactoryIndexer { get; }
        public FactoryIndexer FactoryIndexerCommon { get; }
        public IServiceProvider ServiceProvider { get; }
        public string SearchTerm { get; private set; }
        public int? Type { get; set; }

        public FullFilterContactsExp(int tenant, string user, 
            MailDbContext mailDbContext,
            FactoryIndexer<MailContact> factoryIndexer,
            FactoryIndexer factoryIndexerCommon,
            IServiceProvider serviceProvider,
            string searchTerm = null, int? type = null, ContactInfoType? infoType = null, 
            bool? isPrimary = null, bool? orderAsc = true, int? startIndex = null,
            int? limit = null)
            : base(tenant, user, orderAsc, startIndex, limit)
        {
            InfoType = infoType;
            IsPrimary = isPrimary;
            MailDb = mailDbContext;
            FactoryIndexer = factoryIndexer;
            FactoryIndexerCommon = factoryIndexerCommon;
            ServiceProvider = serviceProvider;
            SearchTerm = searchTerm;
            Type = type;
        }

        public override Expression<Func<MailContact, bool>> GetExpression()
        {
            var exp = base.GetExpression();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var foundIndex = false;

                var t = ServiceProvider.GetService<MailContact>();
                if (FactoryIndexer.Support(t) && FactoryIndexerCommon.CheckState(false))
                {
                    var selector = new Selector<MailContact>(ServiceProvider)
                        .MatchAll(SearchTerm)
                        .Where(s => s.IdUser, User);

                    if (InfoType.HasValue)
                    {
                        selector.InAll(s => s.InfoList.Select(i => i.Type), new[] { (int)InfoType.Value });
                    }

                    if (IsPrimary.HasValue)
                    {
                        selector.InAll(s => s.InfoList.Select(i => i.IsPrimary), new[] { IsPrimary.Value });
                    }

                    if (FactoryIndexer.TrySelectIds(s => selector, out List<int> ids))
                    {
                        foundIndex = true;
                        exp = exp.And(r => ids.Contains((int)r.Id)); // if ids.length == 0 then IN (1=0) - equals to no results
                    }
                }

                if (!foundIndex)
                {
                    var contactInfoQuery = MailDb.MailContactInfo
                        .Where(o => o.TenantId == Tenant 
                            && o.IdUser == User 
                            && o.Data.Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase));

                    if (IsPrimary.HasValue)
                    {
                        contactInfoQuery.Where(o => o.IsPrimary == IsPrimary.Value);
                    }

                    if (InfoType.HasValue)
                    {
                        contactInfoQuery.Where(o => o.Type == (int)InfoType.Value);
                    }

                    var ids = contactInfoQuery
                        .Select(o => o.IdContact)
                        .Distinct()
                        .ToList();

                    exp = exp.And(r => r.Description.Contains(SearchTerm) || r.Name.Contains(SearchTerm) || ids.Contains(r.Id));
                }
            }

            if (Type.HasValue)
            {
                exp = exp.And(c => c.Type == Type.Value);
            }

            return exp;
        }
    }
}