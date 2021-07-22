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


using System.Collections.Generic;
using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class FolderDao : BaseMailDao, IFolderDao
    {
        public FolderDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public Folder GetFolder(FolderType folderType)
        {
            var folder = MailDbContext.MailFolderCounters
                .Where(f => f.Tenant == Tenant && f.IdUser == UserId && f.Folder == folderType)
                .Select(ToFolder)
                .SingleOrDefault();

            return folder;
        }

        public List<Folder> GetFolders()
        {
            var folders = MailDbContext.MailFolderCounters
                .Where(f => f.Tenant == Tenant && f.IdUser == UserId)
                .Select(ToFolder)
                .ToList();

            return folders;
        }

        public int Save(Folder folder)
        {
            var mailFolder = new MailFolderCounters
            {
                Tenant = folder.Tenant,
                IdUser = folder.UserId,
                Folder = folder.FolderType,
                UnreadMessagesCount = (uint)folder.UnreadCount,
                UnreadConversationsCount = (uint)folder.UnreadChainCount,
                TotalMessagesCount = (uint)folder.TotalCount,
                TotalConversationsCount = (uint)folder.TotalChainCount
            };

            MailDbContext.AddOrUpdate(t => t.MailFolderCounters, mailFolder);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int ChangeFolderCounters(
            FolderType folder,
            int? unreadMessDiff = null,
            int? totalMessDiff = null,
            int? unreadConvDiff = null,
            int? totalConvDiff = null)
        {
            if (!unreadMessDiff.HasValue
                && !totalMessDiff.HasValue
                && !unreadConvDiff.HasValue
                && !totalConvDiff.HasValue)
            {
                return -1;
            }

            var mailFolder = MailDbContext.MailFolderCounters
                .Where(f => f.Tenant == Tenant && f.IdUser == UserId && f.Folder == folder)
                .SingleOrDefault();

            if (mailFolder == null)
                return 0;

            if (unreadMessDiff.HasValue)
            {
                if (unreadMessDiff.Value == 0)
                    mailFolder.UnreadMessagesCount = 0;
                else
                    mailFolder.UnreadMessagesCount += (uint)unreadMessDiff.Value;
            }

            if (totalMessDiff.HasValue)
            {
                if (totalMessDiff.Value == 0)
                    mailFolder.TotalMessagesCount = 0;
                else
                    mailFolder.TotalMessagesCount += (uint)totalMessDiff.Value;
            }

            if (unreadConvDiff.HasValue)
            {
                if (unreadConvDiff.Value == 0)
                    mailFolder.UnreadConversationsCount = 0;
                else
                    mailFolder.UnreadConversationsCount += (uint)unreadConvDiff.Value;
            }

            if (totalConvDiff.HasValue)
            {
                if (totalConvDiff.Value == 0)
                    mailFolder.TotalConversationsCount = 0;
                else
                    mailFolder.TotalConversationsCount += (uint)totalConvDiff.Value;
            }

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int Delete()
        {
            var queryDelete = MailDbContext.MailFolderCounters
                .Where(f => f.Tenant == Tenant && f.IdUser == UserId);

            MailDbContext.MailFolderCounters.RemoveRange(queryDelete);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected Folder ToFolder(MailFolderCounters r)
        {
            var f = new Folder
            {
                Tenant = r.Tenant,
                UserId = r.IdUser,
                FolderType = (FolderType) r.Folder,
                TimeModified = r.TimeModified,
                UnreadCount = (int)r.UnreadMessagesCount,
                TotalCount = (int)r.TotalMessagesCount,
                UnreadChainCount = (int)r.UnreadConversationsCount,
                TotalChainCount = (int)r.TotalConversationsCount
            };

            return f;
        }
    }
}
