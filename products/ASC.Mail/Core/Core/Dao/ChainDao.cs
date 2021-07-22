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
using System.Reflection;
using ASC.Api.Core;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.Conversation;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class ChainDao : BaseMailDao, IChainDao
    {
        public ChainDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public List<Chain> GetChains(IConversationsExp exp)
        {
            var chains = MailDbContext.MailChain
                .Where(exp.GetExpression())
                .Select(ToChain)
                .ToList();

            return chains;
        }

        public Dictionary<int, int> GetChainCount(IConversationsExp exp)
        {
            var dictionary = MailDbContext.MailChain
                    .Where(exp.GetExpression())
                    .GroupBy(c => c.Folder, (folderId, c) =>
                    new
                    {
                        folder = (int)folderId,
                        count = c.Count()
                    })
                    .ToDictionary(o => o.folder, o => o.count);

            return dictionary;
        }

        public Dictionary<int, int> GetChainUserFolderCount(bool? unread = null)
        {
            var query = MailDbContext.MailUserFolderXMail
                .Join(MailDbContext.MailMail, x => (int)x.IdMail, m => m.Id,
                (x, m) => new
                {
                    UFxMail = x,
                    Mail = m
                })
                .Join(MailDbContext.MailChain, x => x.Mail.ChainId, c => c.Id,
                (x, c) => new
                {
                    x.UFxMail,
                    x.Mail,
                    Chain = c
                })
                .Where(t => t.UFxMail.Tenant == Tenant)
                .Where(t => t.UFxMail.IdUser == UserId);

            if (unread.HasValue)
            {
                query = query.Where(t => t.Mail.Unread == unread.Value);
            }

            var dictionary = query
                .GroupBy(t => new { t.UFxMail.IdFolder, t.Chain.Id })
                .Select(g => new { g.Key.IdFolder, g.Key.Id })
                .ToList()
                .GroupBy(g => g.IdFolder)
                .ToDictionary(g => g.Key, g => g.Count());

            return dictionary;
        }

        public Dictionary<int, int> GetChainUserFolderCount(List<int> userFolderIds, bool? unread = null)
        {
            var query = MailDbContext.MailUserFolderXMail
                .Join(MailDbContext.MailMail, x => (int)x.IdMail, m => m.Id,
                (x, m) => new
                {
                    UFxMail = x,
                    Mail = m
                })
                .Join(MailDbContext.MailChain, x => x.Mail.ChainId, c => c.Id, 
                (x, c) => new 
                {
                    x.UFxMail,
                    x.Mail,
                    Chain = c
                })
                .Where(t => t.UFxMail.Tenant == Tenant)
                .Where(t => t.UFxMail.IdUser == UserId)
                .Where(t => userFolderIds.Contains((int)t.UFxMail.IdFolder));

            if (unread.HasValue)
            {
                query = query.Where(t => t.Mail.Unread == unread.Value);
            }

            var dictionary = query
                 .GroupBy(t => new { t.UFxMail.IdFolder, t.Chain.Id })
                 .Select(g => new { g.Key.IdFolder, g.Key.Id })
                 .ToList()
                 .GroupBy(g => g.IdFolder)
                 .ToDictionary(g => g.Key, g => g.Count());

            return dictionary;
        }

        public int SaveChain(Chain chain)
        {
            var mailChain = new MailChain { 
                Id = chain.Id,
                IdMailbox = (uint)chain.MailboxId,
                Tenant = (uint)chain.Tenant,
                IdUser = chain.User,
                Folder = (uint)chain.Folder,
                Length = (uint)chain.Length,
                Unread = chain.Unread,
                HasAttachments = chain.HasAttachments,
                Importance = chain.Importance,
                Tags = chain.Tags
            };

            var entry = MailDbContext.AddOrUpdate(c => c.MailChain, mailChain);

            var count = MailDbContext.SaveChanges();

            return count;
        }

        public int Delete(IConversationsExp exp)
        {
            var query = MailDbContext.MailChain.Where(exp.GetExpression());

            MailDbContext.MailChain.RemoveRange(query);

            var count = MailDbContext.SaveChanges();

            return count;
        }

        public int SetFieldValue<T>(IConversationsExp exp, string field, T value)
        {
            Type type = typeof(MailChain);
            PropertyInfo pi = type.GetProperty(field);

            if (pi == null)
                throw new ArgumentException("Field not found");

            var chains = MailDbContext.MailChain
                .Where(exp.GetExpression())
                .ToList();

            foreach (var chain in chains)
            {
                pi.SetValue(chain, Convert.ChangeType(value, pi.PropertyType), null);
            }

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected Chain ToChain(MailChain r)
        {
            var chain = new Chain
            {
                Id = r.Id,
                MailboxId = (int)r.IdMailbox,
                Tenant = (int)r.Tenant,
                User = r.IdUser,
                Folder = (FolderType) r.Folder,
                Length = (int)r.Length,
                Unread = r.Unread,
                HasAttachments = r.HasAttachments,
                Importance = r.Importance,
                Tags = r.Tags
            };

            return chain;
        }
    }
}