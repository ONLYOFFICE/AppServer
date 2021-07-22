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


using System.Collections.Generic;
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
    public class UserFolderXMailDao : BaseMailDao, IUserFolderXMailDao
    {
        public UserFolderXMailDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public UserFolderXMail Get(int mailId)
        {
            var result = MailDbContext.MailUserFolderXMail
                .Where(r => r.Tenant == Tenant && r.IdUser == UserId && r.IdMail == mailId)
                .Select(ToUserFolderXMail)
                .SingleOrDefault();

            return result;
        }

        public List<UserFolderXMail> GetList(int? folderId = null, List<int> mailIds = null)
        {
            var query = MailDbContext.MailUserFolderXMail
                .Where(r => r.Tenant == Tenant && r.IdUser == UserId);

            if (folderId.HasValue)
            {
                query.Where(r => r.IdFolder == folderId.Value);
            }

            if (mailIds != null && mailIds.Any())
            {
                query.Where(r => mailIds.Contains((int)r.IdMail));
            }

            var list = query.Select(ToUserFolderXMail).ToList();

            return list;
        }

        public List<int> GetMailIds(int folderId)
        {
            var list = MailDbContext.MailUserFolderXMail
                .Where(r => r.Tenant == Tenant && r.IdUser == UserId && r.IdFolder == folderId)
                .Select(r => (int)r.IdMail)
                .ToList();

            return list;
        }

        public void SetMessagesFolder(IEnumerable<int> messageIds, int folderId)
        {
            var idMessages = messageIds as IList<int> ?? messageIds.ToList();
            if (!idMessages.Any())
                return;

            var items = new List<MailUserFolderXMail>();
            int i, messagessLen;
            for (i = 0, messagessLen = idMessages.Count; i < messagessLen; i++)
            {
                var messageId = (uint)idMessages[i];

                items.Add(new MailUserFolderXMail { 
                    Tenant = Tenant,
                    IdUser = UserId,
                    IdMail = messageId,
                    IdFolder = folderId
                });

                if ((i % 100 != 0 || i == 0) && i + 1 != messagessLen)
                    continue;

                MailDbContext.MailUserFolderXMail.AddRange(items);

                MailDbContext.SaveChanges();

                items = new List<MailUserFolderXMail>();
            }
        }

        public int Save(UserFolderXMail item)
        {
            var newItem = new MailUserFolderXMail
            {
                Tenant = item.Tenant,
                IdUser = item.User,
                IdMail = (uint)item.MailId,
                IdFolder = item.FolderId
            };

            MailDbContext.AddOrUpdate(t => t.MailUserFolderXMail, newItem);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int Remove(int? mailId = null, int? folderId = null)
        {
            var query = MailDbContext.MailUserFolderXMail
                .Where(r => r.Tenant == Tenant && r.IdUser == UserId);

            if (mailId.HasValue)
            {
                query.Where(r => r.IdMail == mailId.Value);
            }

            if (folderId.HasValue)
            {
                query.Where(r => r.IdFolder == folderId.Value);
            }

            MailDbContext.MailUserFolderXMail.RemoveRange(query);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int Remove(List<int> mailIds)
        {
            var query = MailDbContext.MailUserFolderXMail
                .Where(r => r.Tenant == Tenant && r.IdUser == UserId)
                .Where(r => mailIds.Contains((int)r.IdMail));

            MailDbContext.MailUserFolderXMail.RemoveRange(query);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int RemoveByMailbox(int mailboxId)
        {
            var queryDelete = MailDbContext.MailUserFolderXMail
                .Join(MailDbContext.MailMail, r => (int)r.IdMail, r => r.Id, (ufxm, m) => new
                {
                    UserFoldertXMail = ufxm,
                    MailMail = m
                })
                .Where(o => o.MailMail.IdMailbox == mailboxId && o.MailMail.TenantId == Tenant && o.MailMail.IdUser == UserId)
                .Select(o => o.UserFoldertXMail);

            MailDbContext.MailUserFolderXMail.RemoveRange(queryDelete);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected UserFolderXMail ToUserFolderXMail(MailUserFolderXMail r)
        {
            var folderXMail = new UserFolderXMail
            {
                Tenant = r.Tenant,
                User = r.IdUser,
                MailId = (int)r.IdMail,
                FolderId = r.IdFolder,
                TimeModified = r.TimeCreated
            };

            return folderXMail;
        }
    }
}