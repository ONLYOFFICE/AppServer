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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class FilterDao : BaseMailDao, IFilterDao
    {
        public FilterDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public List<Filter> GetList()
        {
            var filters = MailDbContext.MailFilter
                .Where(f => f.Tenant == Tenant && f.IdUser == UserId)
                .Select(ToFilter)
                .ToList();

            return filters;
        }

        public Filter Get(int id)
        {
            var filter = MailDbContext.MailFilter
                .Where(f => f.Tenant == Tenant && f.IdUser == UserId && f.Id == id)
                .Select(ToFilter)
                .SingleOrDefault();

            return filter;
        }

        public int Save(Filter filter)
        {
            var now = DateTime.UtcNow;

            var mailFilter = new MailFilter { 
                Id = filter.Id,
                Tenant = filter.Tenant,
                IdUser = filter.User,
                Enabled = filter.Enabled,
                Filter = filter.FilterData,
                Position = filter.Position,
                DateModified = now
            };

            if (filter.Id == 0)
            {
                mailFilter.DateCreated = now;
            }

            var entry = MailDbContext.AddOrUpdate(t => t.MailFilter, mailFilter);

            MailDbContext.SaveChanges();

            return entry.Id;
        }

        public int Delete(int id)
        {
            var filter = MailDbContext.MailFilter
               .Where(f => f.Tenant == Tenant && f.IdUser == UserId && f.Id == id)
               .SingleOrDefault();

            MailDbContext.MailFilter.Remove(filter);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected Filter ToFilter(MailFilter r)
        {
            var f = new Filter
            {
                Id = r.Id,
                Tenant = r.Tenant,
                User = r.IdUser,
                Enabled = r.Enabled.GetValueOrDefault(false),
                FilterData = r.Filter,
                Position = r.Position
            };

            return f;
        }
    }
}