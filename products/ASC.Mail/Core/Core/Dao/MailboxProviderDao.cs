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


using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class MailboxProviderDao : BaseMailDao, IMailboxProviderDao
    {
        public MailboxProviderDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public MailboxProvider GetProvider(int id)
        {
            var provider = MailDbContext.MailMailboxProvider
                .Where(d => d.Id == id)
                .Select(ToMailboxProvider)
                .FirstOrDefault();

            return provider;
        }

        public MailboxProvider GetProvider(string providerName)
        {
            var provider = MailDbContext.MailMailboxProvider
                .Where(d => d.Name == providerName)
                .Select(ToMailboxProvider)
                .FirstOrDefault();

            return provider;
        }

        public int SaveProvider(MailboxProvider provider)
        {
            var mailboxProvider = new MailMailboxProvider
            {
                Id = provider.Id,
                Name = provider.Name,
                DisplayName = provider.DisplayName,
                DisplayShortName = provider.DisplayShortName,
                Documentation = provider.Url
            };

            var result = MailDbContext.MailMailboxProvider.Add(mailboxProvider).Entity;

            MailDbContext.SaveChanges();

            return result.Id;
        }

        protected MailboxProvider ToMailboxProvider(MailMailboxProvider r)
        {
            var p = new MailboxProvider
            {
                Id = r.Id,
                Name = r.Name,
                DisplayName = r.DisplayName,
                DisplayShortName = r.DisplayShortName,
                Url = r.Documentation
            };

            return p;
        }
    }
}