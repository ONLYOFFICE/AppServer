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
using System.Data;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Common.Web;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Core.Tenants;
using ASC.Data.Storage;
using ASC.ElasticSearch;
using ASC.Files.Core.Security;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions;
using ASC.Mail.Core.Dao.Expressions.Attachment;
using ASC.Mail.Core.Dao.Expressions.Conversation;
using ASC.Mail.Core.Dao.Expressions.Message;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;
using ASC.Mail.Exceptions;
using ASC.Mail.Extensions;
using ASC.Mail.Models;
using ASC.Mail.Storage;
using ASC.Mail.Utils;
using ASC.Web.Core.Files;
using ASC.Web.Files.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MimeKit;

using IFilesDaoFactory = ASC.Files.Core.IDaoFactory;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class MessageEngine : BaseEngine
    {
        private IMailDaoFactory MailDaoFactory { get; }
        private TenantManager TenantManager { get; }
        private SecurityContext SecurityContext { get; }
        private UserFolderEngine UserFolderEngine { get; }
        private FolderEngine FolderEngine { get; }
        private IndexEngine IndexEngine { get; }
        private QuotaEngine QuotaEngine { get; }
        private TagEngine TagEngine { get; }
        private TenantUtil TenantUtil { get; }
        private CoreSettings CoreSettings { get; }
        private FactoryIndexer<MailMail> FactoryIndexer { get; }
        private FactoryIndexer FactoryIndexerCommon { get; }
        private IServiceProvider ServiceProvider { get; }
        private StorageFactory StorageFactory { get; }
        private StorageManager StorageManager { get; }
        private IFilesDaoFactory FilesDaoFactory { get; }
        private FileSecurity FilesSeurity { get; }
        private FileConverter FileConverter { get; }
        private ILog Log { get; }

        public int Tenant => TenantManager.GetCurrentTenant().TenantId;
        public string User => SecurityContext.CurrentAccount.ID.ToString();

        public IDataStore Storage => StorageFactory.GetMailStorage(Tenant);

        private const int CHUNK_SIZE = 3;

        public MessageEngine(
            TenantManager tenantManager,
            SecurityContext securityContext,
            UserFolderEngine userFolderEngine,
            FolderEngine folderEngine,
            IndexEngine indexEngine,
            QuotaEngine quotaEngine,
            TagEngine tagEngine,
            TenantUtil tenantUtil,
            CoreSettings coreSettings,
            StorageFactory storageFactory,
            StorageManager storageManager,
            FileSecurity filesSeurity,
            FileConverter fileConverter,
            FactoryIndexer<MailMail> factoryIndexer,
            FactoryIndexer factoryIndexerCommon,
            IFilesDaoFactory filesDaoFactory,
            IMailDaoFactory mailDaoFactory,
            IServiceProvider serviceProvider,
            IOptionsMonitor<ILog> option,
            MailSettings mailSettings) : base(mailSettings)
        {
            MailDaoFactory = mailDaoFactory;
            TenantManager = tenantManager;
            SecurityContext = securityContext;
            UserFolderEngine = userFolderEngine;
            FolderEngine = folderEngine;
            IndexEngine = indexEngine;
            QuotaEngine = quotaEngine;
            TagEngine = tagEngine;
            TenantUtil = tenantUtil;
            CoreSettings = coreSettings;
            FactoryIndexer = factoryIndexer;
            FactoryIndexerCommon = factoryIndexerCommon;
            ServiceProvider = serviceProvider;
            StorageFactory = storageFactory;
            StorageManager = storageManager;
            FilesDaoFactory = filesDaoFactory;
            FilesSeurity = filesSeurity;
            FileConverter = fileConverter;
            Log = option.Get("ASC.Mail.MessageEngine");
        }

        public MailMessageData GetMessage(int messageId, MailMessageData.Options options)
        {
            var mail = MailDaoFactory.GetMailDao().GetMail(new ConcreteUserMessageExp(messageId, Tenant, User, !options.OnlyUnremoved));

            return GetMessage(mail, options);
        }

        public MailMessageData GetNextMessage(int messageId, MailMessageData.Options options)
        {
            var mail = MailDaoFactory.GetMailDao().GetNextMail(new ConcreteNextUserMessageExp(messageId, Tenant, User, !options.OnlyUnremoved));

            return GetMessage(mail, options);
        }

        public Stream GetMessageStream(int id)
        {
            var mail = MailDaoFactory.GetMailDao().GetMail(new ConcreteUserMessageExp(id, Tenant, User, false));

            if (mail == null)
                throw new ArgumentException("Message not found with id=" + id);

            var dataStore = StorageFactory.GetMailStorage(Tenant);

            var key = MailStoragePathCombiner.GetBodyKey(User, mail.Stream);

            return dataStore.GetReadStream(string.Empty, key);
        }

        public Tuple<int, int> GetRangeMessages(IMessagesExp exp)
        {
            return MailDaoFactory.GetMailInfoDao().GetRangeMails(exp);
        }

        private MailMessageData GetMessage(Entities.Mail mail, MailMessageData.Options options)
        {
            if (mail == null)
                return null;

            var tagIds = MailDaoFactory.GetTagMailDao().GetTagIds(new List<int> { mail.Id });

            var attachments = MailDaoFactory.GetAttachmentDao().GetAttachments(
                new ConcreteMessageAttachmentsExp(mail.Id, Tenant, User,
                    onlyEmbedded: options.LoadEmebbedAttachements));

            return ToMailMessage(mail, tagIds, attachments, options);
        }

        public List<MailMessageData> GetFilteredMessages(MailSearchFilterData filter, out long totalMessagesCount)
        {
            var res = new List<MailMessageData>();

            var ids = new List<int>();

            long total = 0;

            if (filter.UserFolderId.HasValue && UserFolderEngine.Get(filter.UserFolderId.Value) == null)
                throw new ArgumentException("Folder not found");

            var t = ServiceProvider.GetService<MailMail>();
            if (!filter.IsDefault() && FactoryIndexer.Support(t) && FactoryIndexerCommon.CheckState(false))
            {
                if (FilterMessagesExp.TryGetFullTextSearchIds(FactoryIndexer, ServiceProvider,
                    filter, User, out ids, out total))
                {
                    if (!ids.Any())
                    {
                        totalMessagesCount = 0;
                        return res;
                    }
                }
            }

            IMessagesExp exp;

            var tenantInfo = TenantManager.GetTenant(Tenant);
            var utcNow = DateTime.UtcNow;

            if (ids.Any())
            {
                var pageSize = filter.PageSize.GetValueOrDefault(25);
                var page = filter.Page.GetValueOrDefault(1);

                exp = SimpleMessagesExp.CreateBuilder(Tenant, User)
                        .SetMessageIds(ids)
                        .SetOrderBy(filter.Sort)
                        .SetOrderAsc(filter.SortOrder == DefineConstants.ASCENDING)
                        .SetLimit(pageSize)
                        .Build();

                var list = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp)
                    .ConvertAll(m => ToMailMessage(m, tenantInfo, utcNow));

                var pagedCount = (list.Count + page * pageSize);

                totalMessagesCount = page == 0 ? total : total - pagedCount;

                return list;
            }
            else
            {
                exp = new FilterMessagesExp(ids, Tenant, User, filter);

                if (filter.IsDefault())
                {
                    var folders = MailDaoFactory.GetFolderDao().GetFolders();

                    var currentFolder =
                        folders.FirstOrDefault(f => f.FolderType == filter.PrimaryFolder);

                    if (currentFolder != null && currentFolder.FolderType == FolderType.UserFolder)
                    {
                        totalMessagesCount = MailDaoFactory.GetMailInfoDao().GetMailInfoTotal(exp);
                    }
                    else
                    {
                        totalMessagesCount = currentFolder == null
                            ? 0
                            : filter.Unread.HasValue
                                ? filter.Unread.Value
                                    ? currentFolder.UnreadCount
                                    : currentFolder.TotalCount - currentFolder.UnreadCount
                                : currentFolder.TotalCount;
                    }
                }
                else
                {
                    totalMessagesCount = MailDaoFactory.GetMailInfoDao().GetMailInfoTotal(exp);
                }

                if (totalMessagesCount == 0)
                    return res;

                var list = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp)
                    .ConvertAll(m => ToMailMessage(m, tenantInfo, utcNow));

                return list;
            }
        }

        public List<MailMessageData> GetFilteredMessages(MailSieveFilterData filter, int page, int pageSize, out long totalMessagesCount)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            var res = new List<MailMessageData>();

            if (FilterSieveMessagesExp.TryGetFullTextSearchIds(FactoryIndexer, ServiceProvider,
                filter, User, out List<int> ids, out long total))
            {
                if (!ids.Any())
                {
                    totalMessagesCount = 0;
                    return res;
                }
            }

            var exp = new FilterSieveMessagesExp(ids, Tenant, User, filter, page, pageSize, FactoryIndexer, ServiceProvider);

            totalMessagesCount = ids.Any() ? total : MailDaoFactory.GetMailInfoDao().GetMailInfoTotal(exp);

            if (totalMessagesCount == 0)
            {
                return res;
            }

            var tenantInfo = TenantManager.GetTenant(Tenant);
            var utcNow = DateTime.UtcNow;

            var list = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp)
                .ConvertAll(m => ToMailMessage(m, tenantInfo, utcNow));

            return list;
        }

        public int GetNextFilteredMessageId(int messageId, MailSearchFilterData filter)
        {
            var mail = MailDaoFactory.GetMailDao().GetMail(new ConcreteUserMessageExp(messageId, Tenant, User, false));

            if (mail == null)
                return -1;

            var t = ServiceProvider.GetService<MailMail>();
            if (FactoryIndexer.Support(t) && FactoryIndexerCommon.CheckState(false))
            {
                if (FilterMessagesExp.TryGetFullTextSearchIds(FactoryIndexer, ServiceProvider,
                    filter, User, out List<int> ids, out long total, mail.DateSent))
                {
                    if (!ids.Any())
                        return -1;

                    return ids.Where(id => id != messageId)
                        .DefaultIfEmpty(-1)
                        .First();
                }
            }

            var exp = new FilterNextMessageExp(mail.DateSent, Tenant, User, filter);

            var list = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp);

            return list.Where(m => m.Id != messageId)
                .Select(m => m.Id)
                .DefaultIfEmpty(-1)
                .First();
        }

        //TODO: Simplify
        public bool SetUnread(List<int> ids, bool unread, bool allChain = false)
        {
            var ids2Update = new List<int>();
            List<MailInfo> chainedMessages;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                chainedMessages = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(ids);

                if (!chainedMessages.Any())
                    return true;

                var listIds = allChain
                    ? chainedMessages.Where(x => x.IsNew == !unread).Select(x => x.Id).ToList()
                    : ids;

                if (!listIds.Any())
                    return true;

                var exp = SimpleMessagesExp.CreateBuilder(Tenant, User)
                            .SetMessageIds(listIds)
                            .Build();

                MailDaoFactory.GetMailInfoDao().SetFieldValue(exp, "Unread", unread);

                var sign = unread ? 1 : -1;

                var folderConvMessCounters = new List<Tuple<FolderType, int, int>>();

                var fGroupedChains = chainedMessages.GroupBy(m => new { m.ChainId, m.Folder, m.MailboxId });

                int? userFolder = null;

                if (chainedMessages.Any(m => m.Folder == FolderType.UserFolder))
                {
                    var item = MailDaoFactory.GetUserFolderXMailDao().Get(ids.First());
                    userFolder = item == null ? null : item.FolderId;
                }

                foreach (var fChainMessages in fGroupedChains)
                {
                    var chainUnreadBefore = fChainMessages.Any(m => m.IsNew);

                    var firstFlag = true;

                    var unreadMessDiff = 0;

                    foreach (var m in fChainMessages.Where(m => listIds.Contains(m.Id) && m.IsNew != unread))
                    {
                        m.IsNew = unread;

                        unreadMessDiff++;

                        if (!firstFlag)
                            continue;

                        ids2Update.Add(m.Id);

                        firstFlag = false;
                    }

                    var chainUnreadAfter = fChainMessages.Any(m => m.IsNew);

                    var unreadConvDiff = chainUnreadBefore == chainUnreadAfter ? 0 : 1;

                    var tplFolderIndex =
                        folderConvMessCounters.FindIndex(tpl => tpl.Item1 == fChainMessages.Key.Folder);

                    if (tplFolderIndex == -1)
                    {
                        folderConvMessCounters.Add(
                            Tuple.Create(fChainMessages.Key.Folder,
                                unreadMessDiff,
                                unreadConvDiff));
                    }
                    else
                    {
                        var tplFolder = folderConvMessCounters[tplFolderIndex];

                        folderConvMessCounters[tplFolderIndex] = Tuple.Create(fChainMessages.Key.Folder,
                            tplFolder.Item2 + unreadMessDiff,
                            tplFolder.Item3 + unreadConvDiff);
                    }
                }

                foreach (var f in folderConvMessCounters)
                {
                    var folder = f.Item1;

                    var unreadMessDiff = f.Item2 != 0 ? sign * f.Item2 : (int?)null;
                    var unreadConvDiff = f.Item3 != 0 ? sign * f.Item3 : (int?)null;

                    FolderEngine.ChangeFolderCounters(folder, userFolder,
                        unreadMessDiff, unreadConvDiff: unreadConvDiff);
                }

                foreach (var id in ids2Update)
                    UpdateMessageChainUnreadFlag(Tenant, User, id);

                if (userFolder.HasValue)
                {
                    var userFoldersIds = MailDaoFactory.GetUserFolderXMailDao().GetList(mailIds: chainedMessages.Select(m => m.Id).ToList())
                        .Select(ufxm => ufxm.FolderId)
                        .Distinct()
                        .ToList();

                    UserFolderEngine.RecalculateCounters(MailDaoFactory, userFoldersIds);
                }

                tx.Commit();
            }

            var data = new MailMail
            {
                Unread = unread
            };

            ids2Update = allChain ? chainedMessages.Select(m => m.Id).ToList() : ids;

            IndexEngine.Update(data, s => s.In(m => m.Id, ids2Update.ToArray()), wrapper => wrapper.Unread);

            return true;
        }

        public bool SetImportant(List<int> ids, bool importance)
        {
            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var exp = SimpleMessagesExp.CreateBuilder(Tenant, User)
                        .SetMessageIds(ids)
                        .Build();

                MailDaoFactory.GetMailInfoDao().SetFieldValue(exp, "Importance", importance);

                foreach (var messageId in ids)
                    UpdateMessageChainImportanceFlag(Tenant, User, messageId);

                tx.Commit();
            }

            var data = new MailMail
            {
                Importance = importance
            };

            IndexEngine.Update(data, s => s.In(m => m.Id, ids.ToArray()), wrapper => wrapper.Importance);

            return true;
        }

        public void Restore(List<int> ids)
        {
            List<MailInfo> mailInfoList;

            mailInfoList = MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                SimpleMessagesExp.CreateBuilder(Tenant, User)
                        .SetMessageIds(ids)
                        .Build());

            if (!mailInfoList.Any())
                return;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                Restore(MailDaoFactory, mailInfoList);
                tx.Commit();
            }

            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t))
                return;

            var mails = mailInfoList.ConvertAll(m => new MailMail
            {
                Id = m.Id,
                Folder = (byte)m.FolderRestore
            });

            IndexEngine.Update(mails, wrapper => wrapper.Folder);
        }

        //TODO: Simplify
        public void Restore(IMailDaoFactory MailDaoFactory, List<MailInfo> mailsInfo)
        {
            if (!mailsInfo.Any())
                return;

            var uniqueChainInfo = mailsInfo
                .ConvertAll(x => new
                {
                    folder = x.Folder,
                    chain_id = x.ChainId,
                    id_mailbox = x.MailboxId
                })
                .Distinct();

            var prevInfo = mailsInfo.ConvertAll(x => new
            {
                id = x.Id,
                unread = x.IsNew,
                folder = x.Folder,
                folder_restore = x.FolderRestore,
                chain_id = x.ChainId,
                id_mailbox = x.MailboxId
            });

            var ids = mailsInfo.ConvertAll(x => x.Id);

            var exp = SimpleMessagesExp.CreateBuilder(Tenant, User)
                    .SetMessageIds(ids)
                    .Build();

            MailDaoFactory.GetMailInfoDao().SetFieldsEqual(exp, "FolderRestore", "Folder");

            // Update chains in old folder
            foreach (var info in uniqueChainInfo)
                UpdateChain(info.chain_id, info.folder, null, info.id_mailbox, Tenant, User);

            var unreadMessagesCountCollection = new Dictionary<FolderType, int>();
            var totalMessagesCountCollection = new Dictionary<FolderType, int>();

            foreach (var info in prevInfo)
            {
                if (totalMessagesCountCollection.ContainsKey(info.folder_restore))
                    totalMessagesCountCollection[info.folder_restore] += 1;
                else
                    totalMessagesCountCollection.Add(info.folder_restore, 1);

                if (!info.unread) continue;
                if (unreadMessagesCountCollection.ContainsKey(info.folder_restore))
                    unreadMessagesCountCollection[info.folder_restore] += 1;
                else
                    unreadMessagesCountCollection.Add(info.folder_restore, 1);
            }

            // Update chains in new restored folder
            UpdateChainFields(Tenant, User, ids);

            var prevTotalUnreadCount = 0;
            var prevTotalCount = 0;

            int? totalMessDiff;
            int? unreadMessDiff;
            foreach (var keyPair in totalMessagesCountCollection)
            {
                var folderRestore = keyPair.Key;
                var totalRestore = keyPair.Value;

                totalMessDiff = totalRestore != 0 ? totalRestore : null;

                int unreadRestore;
                unreadMessagesCountCollection.TryGetValue(folderRestore, out unreadRestore);

                unreadMessDiff = unreadRestore != 0 ? unreadRestore : null;

                FolderEngine.ChangeFolderCounters(folderRestore, null,
                    unreadMessDiff, totalMessDiff);

                prevTotalUnreadCount -= unreadRestore;
                prevTotalCount -= totalRestore;
            }

            // Subtract the restored number of messages in the previous folder

            unreadMessDiff = prevTotalUnreadCount != 0 ? prevTotalUnreadCount : null;
            totalMessDiff = prevTotalCount != 0 ? prevTotalCount : null;

            FolderEngine.ChangeFolderCounters(prevInfo[0].folder, null,
                unreadMessDiff, totalMessDiff);
        }

        public void SetFolder(List<int> ids, FolderType folder, int? userFolderId = null)
        {
            if (!ids.Any())
                throw new ArgumentNullException("ids");

            if (userFolderId.HasValue && folder != FolderType.UserFolder)
            {
                folder = FolderType.UserFolder;
            }

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                SetFolder(MailDaoFactory, ids, folder, userFolderId);
                tx.Commit();
            }

            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t))
                return;

            var data = new MailMail
            {
                Folder = (byte)folder,
                UserFolders = userFolderId.HasValue
                    ? new List<MailUserFolder>
                    {
                        new MailUserFolder
                        {
                            Id = userFolderId.Value
                        }
                    }
                    : new List<MailUserFolder>()
            };

            Expression<Func<Selector<MailMail>, Selector<MailMail>>> exp =
                s => s.In(m => m.Id, ids.ToArray());

            IndexEngine.Update(data, exp, w => w.Folder);

            IndexEngine.Update(data, exp, UpdateAction.Replace, w => w.UserFolders.ToList());
        }

        public void SetFolder(IMailDaoFactory MailDaoFactory, List<int> ids, FolderType toFolder,
            int? toUserFolderId = null)
        {
            var query = SimpleMessagesExp.CreateBuilder(Tenant, User)
                        .SetMessageIds(ids)
                        .Build();

            var mailInfoList = MailDaoFactory.GetMailInfoDao().GetMailInfoList(query);

            if (!mailInfoList.Any()) return;

            SetFolder(MailDaoFactory, mailInfoList, toFolder, toUserFolderId);
        }

        public void SetFolder(IMailDaoFactory MailDaoFactory, List<MailInfo> mailsInfo, FolderType toFolder,
            int? toUserFolderId = null)
        {
            if (!mailsInfo.Any())
                return;

            if (toUserFolderId.HasValue && UserFolderEngine.Get(toUserFolderId.Value) == null)
                throw new ArgumentException("Folder not found");

            var messages = mailsInfo.ConvertAll(x =>
            {
                var srcUserFolderId = (int?)null;

                if (x.Folder == FolderType.UserFolder)
                {
                    var item = MailDaoFactory.GetUserFolderXMailDao().Get(x.Id);
                    srcUserFolderId = item == null ? null : item.FolderId;
                }

                return new
                {
                    id = x.Id,
                    unread = x.IsNew,
                    folder = x.Folder,
                    userFolderId = srcUserFolderId,
                    chain_id = x.ChainId,
                    id_mailbox = x.MailboxId
                };
            })
            .Where(m => m.folder != toFolder || m.userFolderId != toUserFolderId)
            .ToList();

            if (!messages.Any())
                return;

            var uniqueChainInfo = messages
                .ConvertAll(x => new
                {
                    x.folder,
                    x.userFolderId,
                    x.chain_id,
                    x.id_mailbox
                })
                .Distinct();

            var prevInfo = messages.ConvertAll(x => new
            {
                x.id,
                x.unread,
                x.folder,
                x.userFolderId,
                x.chain_id,
                x.id_mailbox
            });

            var ids = messages.Select(x => x.id).ToList();

            var updateQuery = SimpleMessagesExp.CreateBuilder(Tenant, User)
                    .SetMessageIds(ids)
                    .Build();

            MailDaoFactory.GetMailInfoDao().SetFieldValue(updateQuery,
                "Folder",
                toFolder);

            if (toUserFolderId.HasValue)
            {
                UserFolderEngine.SetFolderMessages(toUserFolderId.Value, ids);
            }
            else if (prevInfo.Any(x => x.userFolderId.HasValue))
            {
                var prevIds = prevInfo.Where(x => x.userFolderId.HasValue).Select(x => x.id).ToList();

                UserFolderEngine.DeleteFolderMessages(MailDaoFactory, prevIds);
            }

            foreach (var info in uniqueChainInfo)
            {
                UpdateChain(
                    info.chain_id,
                    info.folder,
                    info.userFolderId,
                    info.id_mailbox,
                    Tenant, User);
            }

            var totalMessages = prevInfo.GroupBy(x => new { x.folder, x.userFolderId })
                .Select(group => new { group.Key, Count = group.Count() });

            var unreadMessages = prevInfo.Where(x => x.unread)
                .GroupBy(x => new { x.folder, x.userFolderId })
                .Select(group => new { group.Key, Count = group.Count() })
                .ToList();

            UpdateChainFields(Tenant, User, ids);

            var movedTotalUnreadCount = 0;
            var movedTotalCount = 0;
            int? totalMessDiff;
            int? unreadMessDiff;

            foreach (var keyPair in totalMessages)
            {
                var srcFolder = keyPair.Key.folder;
                var srcUserFolder = keyPair.Key.userFolderId;
                var totalMove = keyPair.Count;

                var unreadItem = unreadMessages.FirstOrDefault(
                        x => x.Key.folder == srcFolder && x.Key.userFolderId == srcUserFolder);

                var unreadMove = unreadItem != null ? unreadItem.Count : 0;

                unreadMessDiff = unreadMove != 0 ? unreadMove * (-1) : null;
                totalMessDiff = totalMove != 0 ? totalMove * (-1) : null;

                FolderEngine.ChangeFolderCounters(srcFolder, srcUserFolder,
                    unreadMessDiff, totalMessDiff);

                movedTotalUnreadCount += unreadMove;
                movedTotalCount += totalMove;
            }

            unreadMessDiff = movedTotalUnreadCount != 0 ? movedTotalUnreadCount : null;
            totalMessDiff = movedTotalCount != 0 ? movedTotalCount : null;

            FolderEngine.ChangeFolderCounters(toFolder, toUserFolderId,
                unreadMessDiff, totalMessDiff);

            // Correction of UserFolders counters

            var userFolderIds = prevInfo.Where(x => x.folder == FolderType.UserFolder)
                .Select(x => x.userFolderId.Value)
                .Distinct()
                .ToList();

            if (userFolderIds.Count() == 0 && !toUserFolderId.HasValue) // Only for movement from/to UserFolders
                return;

            if (toUserFolderId.HasValue)
                userFolderIds.Add(toUserFolderId.Value);

            UserFolderEngine.RecalculateCounters(MailDaoFactory, userFolderIds);
        }

        public void SetRemoved(List<int> ids)
        {
            if (!ids.Any())
                throw new ArgumentNullException("ids");

            long usedQuota;

            var mailInfoList =
                MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                    SimpleMessagesExp.CreateBuilder(Tenant, User)
                        .SetMessageIds(ids)
                        .Build());

            if (!mailInfoList.Any()) return;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                usedQuota = SetRemoved(MailDaoFactory, mailInfoList);
                tx.Commit();
            }

            QuotaEngine.QuotaUsedDelete(usedQuota);

            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t))
                return;

            IndexEngine.Remove(ids, Tenant, new Guid(User));
        }

        public long SetRemoved(IMailDaoFactory MailDaoFactory, List<MailInfo> deleteInfo)
        {
            if (!deleteInfo.Any())
                return 0;

            var messageFieldsInfo = deleteInfo
                .ConvertAll(r =>
                    new
                    {
                        id = r.Id,
                        folder = r.Folder,
                        unread = r.IsNew
                    });

            var ids = messageFieldsInfo.Select(m => m.id).ToList();

            MailDaoFactory.GetMailInfoDao().SetFieldValue(
                SimpleMessagesExp.CreateBuilder(Tenant, User)
                    .SetMessageIds(ids)
                    .Build(),
                "IsRemoved",
                true);

            var exp = new ConcreteMessagesAttachmentsExp(ids, Tenant, User, onlyEmbedded: null);

            var usedQuota = MailDaoFactory.GetAttachmentDao().GetAttachmentsSize(exp);

            MailDaoFactory.GetAttachmentDao().SetAttachmnetsRemoved(exp);

            var tagIds = MailDaoFactory.GetTagMailDao().GetTagIds(ids.ToList());

            MailDaoFactory.GetTagMailDao().DeleteByMailIds(tagIds);

            foreach (var tagId in tagIds)
            {
                var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

                if (tag == null)
                    continue;

                var count = MailDaoFactory.GetTagMailDao().CalculateTagCount(tag.Id);

                tag.Count = count;

                MailDaoFactory.GetTagDao().SaveTag(tag);
            }

            var totalCollection = (from row in messageFieldsInfo
                                   group row by row.folder
                into g
                                   select new { id = g.Key, diff = -g.Count() })
                .ToList();

            var unreadCollection = (from row in messageFieldsInfo.Where(m => m.unread)
                                    group row by row.folder
                into g
                                    select new { id = g.Key, diff = -g.Count() })
                .ToList();

            foreach (var folder in totalCollection)
            {
                var unreadInFolder = unreadCollection
                    .FirstOrDefault(f => f.id == folder.id);

                var unreadMessDiff = unreadInFolder != null ? unreadInFolder.diff : (int?)null;
                var totalMessDiff = folder.diff != 0 ? folder.diff : (int?)null;

                FolderEngine.ChangeFolderCounters(folder.id, null,
                    unreadMessDiff, totalMessDiff);
            }

            UpdateChainFields(Tenant, User,
                messageFieldsInfo.Select(m => Convert.ToInt32(m.id)).ToList());

            return usedQuota;
        }

        public void SetRemoved(FolderType folder)
        {
            long usedQuota;

            var mailInfoList = MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                SimpleMessagesExp.CreateBuilder(Tenant, User)
                    .SetFolder((int)folder)
                    .Build());

            if (!mailInfoList.Any()) return;

            var ids = mailInfoList.Select(m => m.Id).ToList();

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                MailDaoFactory.GetMailInfoDao().SetFieldValue(
                    SimpleMessagesExp.CreateBuilder(Tenant, User)
                        .SetFolder((int)folder)
                        .Build(),
                    "IsRemoved",
                    true);

                var exp = new ConcreteMessagesAttachmentsExp(ids, Tenant, User, onlyEmbedded: null);

                usedQuota = MailDaoFactory.GetAttachmentDao().GetAttachmentsSize(exp);

                MailDaoFactory.GetAttachmentDao().SetAttachmnetsRemoved(exp);


                var tagIds = MailDaoFactory.GetTagMailDao().GetTagIds(ids.ToList());

                MailDaoFactory.GetTagMailDao().DeleteByMailIds(tagIds);

                foreach (var tagId in tagIds)
                {
                    var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

                    if (tag == null)
                        continue;

                    var count = MailDaoFactory.GetTagMailDao().CalculateTagCount(tag.Id);

                    tag.Count = count;

                    MailDaoFactory.GetTagDao().SaveTag(tag);
                }

                MailDaoFactory.GetChainDao().Delete(SimpleConversationsExp.CreateBuilder(Tenant, User)
                        .SetFolder((int)folder)
                        .Build());

                MailDaoFactory.GetFolderDao().ChangeFolderCounters(folder, 0, 0, 0, 0);

                tx.Commit();
            }

            if (usedQuota <= 0)
                return;

            QuotaEngine.QuotaUsedDelete(usedQuota);
        }

        public int MailSave(MailBoxData mailbox, MailMessageData message,
            int messageId, FolderType folder, FolderType folderRestore, int? userFolderId,
            string uidl, string md5, bool saveAttachments)
        {
            int id;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                id = MailSave(mailbox, message, messageId,
                    folder, folderRestore, userFolderId,
                    uidl, md5, saveAttachments, out long usedQuota);

                tx.Commit();
            }

            return id;
        }

        public int MailSave(MailBoxData mailbox, MailMessageData message,
            int messageId, FolderType folder, FolderType folderRestore, int? userFolderId,
            string uidl, string md5, bool saveAttachments, out long usedQuota)
        {
            var countAttachments = 0;
            usedQuota = 0;

            if (messageId != 0)
            {
                countAttachments = MailDaoFactory.GetAttachmentDao().GetAttachmentsCount(
                    new ConcreteMessageAttachmentsExp(messageId, mailbox.TenantId, mailbox.UserId));
            }

            var address = mailbox.EMail.Address.ToLowerInvariant();

            var mail = new Entities.Mail
            {
                Id = messageId,
                Tenant = Tenant,
                User = User,
                MailboxId = mailbox.MailBoxId,
                Address = address,
                From = message.From,
                To = message.To,
                Reply = message.ReplyTo,
                Subject = message.Subject,
                Cc = message.Cc,
                Bcc = message.Bcc,
                Importance = message.Important,
                DateReceived = DateTime.UtcNow,
                DateSent = message.Date.ToUniversalTime(),
                Size = message.Size,
                AttachCount = !saveAttachments
                    ? countAttachments
                    : (message.Attachments != null ? message.Attachments.Count : 0),
                Unread = message.IsNew,
                IsAnswered = message.IsAnswered,
                IsForwarded = message.IsForwarded,
                Stream = message.StreamId,
                Folder = folder,
                FolderRestore = folderRestore,
                Spam = false,
                MimeMessageId = message.MimeMessageId,
                MimeInReplyTo = message.MimeReplyToId,
                ChainId = message.ChainId,
                Introduction = message.Introduction,
                HasParseError = message.HasParseError,
                CalendarUid = message.CalendarUid,
                Uidl = uidl,
                Md5 = md5
            };

            var mailId = MailDaoFactory.GetMailDao().Save(mail);

            if (messageId == 0)
            {
                var unreadMessDiff = message.IsNew ? 1 : (int?)null;
                FolderEngine.ChangeFolderCounters(folder, userFolderId, unreadMessDiff, 1);

                if (userFolderId.HasValue)
                {
                    UserFolderEngine.SetFolderMessages(userFolderId.Value, new List<int> { mailId });
                }
            }

            if (saveAttachments &&
                message.Attachments != null &&
                message.Attachments.Count > 0)
            {
                var exp = new ConcreteMessageAttachmentsExp(mailId, mailbox.TenantId, mailbox.UserId, onlyEmbedded: null);

                usedQuota = MailDaoFactory.GetAttachmentDao().GetAttachmentsSize(exp);

                MailDaoFactory.GetAttachmentDao().SetAttachmnetsRemoved(exp);

                foreach (var attachment in message.Attachments)
                {
                    var newId = MailDaoFactory.GetAttachmentDao().SaveAttachment(attachment.ToAttachmnet(mailId));
                    attachment.fileId = newId;
                }

                var count = MailDaoFactory.GetAttachmentDao().GetAttachmentsCount(
                                new ConcreteMessageAttachmentsExp(mailId, mailbox.TenantId, mailbox.UserId));

                MailDaoFactory.GetMailInfoDao().SetFieldValue(
                    SimpleMessagesExp.CreateBuilder(mailbox.TenantId, mailbox.UserId)
                        .SetMessageId(mailId)
                        .Build(),
                    "AttachmentsCount",
                    count);
            }

            if (!string.IsNullOrEmpty(message.FromEmail) && message.FromEmail.Length > 0)
            {
                if (messageId > 0)
                    MailDaoFactory.GetTagMailDao().DeleteByMailIds(new List<int> { mailId });

                if (message.TagIds == null)
                    message.TagIds = new List<int>();

                var tagAddressesTagIds = MailDaoFactory.GetTagAddressDao().GetTagIds(message.FromEmail);

                tagAddressesTagIds.ForEach(tagId =>
                {
                    if (!message.TagIds.Contains(tagId))
                        message.TagIds.Add(tagId);
                });

                if (message.TagIds.Any())
                {
                    foreach (var tagId in message.TagIds)
                    {
                        var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

                        if (tag == null)
                            continue;

                        MailDaoFactory.GetTagMailDao().SetMessagesTag(new[] { mailId }, tag.Id);

                        var count = MailDaoFactory.GetTagMailDao().CalculateTagCount(tag.Id);

                        tag.Count = count;

                        MailDaoFactory.GetTagDao().SaveTag(tag);
                    }
                }
            }

            UpdateMessagesChains(MailDaoFactory, mailbox, message.MimeMessageId, message.ChainId, folder, userFolderId);

            Log.DebugFormat("MailSave() tenant='{0}', user_id='{1}', email='{2}', from='{3}', id_mail='{4}'",
                mailbox.TenantId, mailbox.UserId, mailbox.EMail, message.From, mailId);

            return mailId;
        }

        public ChainInfo DetectChain(MailBoxData mailbox, string mimeMessageId,
            string mimeReplyToId, string subject)
        {
            var chainId = mimeMessageId; //Chain id is equal to root conversataions message - MimeMessageId
            var chainDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(mimeMessageId) && !string.IsNullOrEmpty(mimeReplyToId))
            {
                chainId = mimeReplyToId;

                try
                {
                    var chainAndSubject =
                        MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                            SimpleMessagesExp.CreateBuilder(Tenant, User)
                                .SetMailboxId(mailbox.MailBoxId)
                                .SetMimeMessageId(mimeReplyToId)
                                .Build())
                            .ConvertAll(x => new
                            {
                                chain_id = x.ChainId,
                                subject = x.Subject,
                                chainDate = x.ChainDate
                            })
                            .Distinct()
                            .FirstOrDefault()
                        ?? MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                            SimpleMessagesExp.CreateBuilder(Tenant, User)
                                .SetMailboxId(mailbox.MailBoxId)
                                .SetChainId(mimeReplyToId)
                                .Build())
                            .ConvertAll(x => new
                            {
                                chain_id = x.ChainId,
                                subject = x.Subject,
                                chainDate = x.ChainDate
                            })
                            .Distinct()
                            .FirstOrDefault();

                    if (chainAndSubject != null)
                    {
                        var chainSubject = MailUtil.NormalizeSubject(chainAndSubject.subject);
                        var messageSubject = MailUtil.NormalizeSubject(subject);

                        if (chainSubject.Equals(messageSubject))
                        {
                            chainId = chainAndSubject.chain_id;
                            chainDate = chainAndSubject.chainDate;
                        }
                        else
                        {
                            chainId = mimeMessageId;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.WarnFormat(
                        "DetectChainId() params tenant={0}, user_id='{1}', mailbox_id={2}, mime_message_id='{3}' Exception:\r\n{4}",
                        mailbox.TenantId, mailbox.UserId, mailbox.MailBoxId, mimeMessageId, ex.ToString());
                }
            }

            Log.DebugFormat(
                "DetectChainId() tenant='{0}', user_id='{1}', mailbox_id='{2}', mime_message_id='{3}' Result: {4}",
                mailbox.TenantId, mailbox.UserId, mailbox.MailBoxId, mimeMessageId, chainId);

            return new ChainInfo
            {
                Id = chainId,
                MailboxId = mailbox.MailBoxId,
                ChainDate = chainDate
            };
        }

        //TODO: Need refactoring
        public MailMessageData Save(
            MailBoxData mailbox, MimeMessage mimeMessage, string uidl, Models.MailFolder folder,
            int? userFolderId, bool unread = true, ILog log = null)
        {
            if (mailbox == null)
                throw new ArgumentException(@"mailbox is null", "mailbox");

            if (mimeMessage == null)
                throw new ArgumentException(@"message is null", "mimeMessage");

            if (uidl == null)
                throw new ArgumentException(@"uidl is null", "uidl");

            if (log == null)
                log = new NullLog();

            var fromEmail = mimeMessage.From.Mailboxes.FirstOrDefault();

            var md5 =
                    string.Format("{0}|{1}|{2}|{3}",
                        mimeMessage.From.Mailboxes.Any() ? mimeMessage.From.Mailboxes.First().Address : "",
                        mimeMessage.Subject, mimeMessage.Date.UtcDateTime, mimeMessage.MessageId).GetMd5();

            var fromThisMailBox = fromEmail != null &&
                                  fromEmail.Address.ToLowerInvariant()
                                      .Equals(mailbox.EMail.Address.ToLowerInvariant());

            var toThisMailBox =
                mimeMessage.To.Mailboxes.Select(addr => addr.Address.ToLowerInvariant())
                    .Contains(mailbox.EMail.Address.ToLowerInvariant());

            List<int> tagsIds = null;

            if (folder.Tags.Any())
            {
                log.Debug("GetOrCreateTags()");
                tagsIds = TagEngine.GetOrCreateTags(mailbox.TenantId, mailbox.UserId, folder.Tags);
            }

            log.Debug("UpdateExistingMessages()");

            var found = UpdateExistingMessages(mailbox, folder.Folder, uidl, md5,
                mimeMessage.MessageId, MailUtil.NormalizeStringForMySql(mimeMessage.Subject), mimeMessage.Date.UtcDateTime, fromThisMailBox, toThisMailBox, tagsIds, log);

            var needSave = !found;
            if (!needSave)
                return null;

            log.Debug("DetectChainId()");

            var chainInfo = DetectChain(mailbox, mimeMessage.MessageId, mimeMessage.InReplyTo,
                mimeMessage.Subject);

            var streamId = MailUtil.CreateStreamId();

            log.Debug("Convert MimeMessage->MailMessage");

            var message = mimeMessage.ConvertToMailMessage(
                TenantManager, CoreSettings,
                folder, unread, chainInfo.Id,
                chainInfo.ChainDate, streamId,
                mailbox.MailBoxId, true, log);

            log.Debug("TryStoreMailData()");

            if (!TryStoreMailData(message, mailbox, log))
            {
                throw new Exception("Failed to save message");
            }

            log.Debug("MailSave()");

            if (TrySaveMail(mailbox, message, folder, userFolderId, uidl, md5, log))
            {
                return message;
            }

            if (TryRemoveMailDirectory(mailbox, message.StreamId, log))
            {
                log.InfoFormat("Problem with mail proccessing(Account:{0}). Body and attachment have been deleted", mailbox.EMail);
            }
            else
            {
                throw new Exception("Can't delete mail folder with data");
            }

            return null;
        }
        //TODO: Need refactoring
        public string StoreMailBody(MailBoxData mailBoxData, MailMessageData messageItem, ILog log)
        {
            if (string.IsNullOrEmpty(messageItem.HtmlBody) && (messageItem.HtmlBodyStream == null || messageItem.HtmlBodyStream.Length == 0))
                return string.Empty;

            // Using id_user as domain in S3 Storage - allows not to add quota to tenant.
            var savePath = MailStoragePathCombiner.GetBodyKey(mailBoxData.UserId, messageItem.StreamId);

            Storage.QuotaController = null;

            try
            {
                string response;

                if (messageItem.HtmlBodyStream != null && messageItem.HtmlBodyStream.Length > 0)
                {
                    messageItem.HtmlBodyStream.Seek(0, SeekOrigin.Begin);

                    response = Storage
                            .Save(savePath, messageItem.HtmlBodyStream, MailStoragePathCombiner.BODY_FILE_NAME)
                            .ToString();
                }
                else
                {
                    using (var reader = new MemoryStream(Encoding.UTF8.GetBytes(messageItem.HtmlBody)))
                    {
                        response = Storage
                            .Save(savePath, reader, MailStoragePathCombiner.BODY_FILE_NAME)
                            .ToString();
                    }
                }

                log.DebugFormat("StoreMailBody() tenant='{0}', user_id='{1}', save_body_path='{2}' Result: {3}",
                            mailBoxData.TenantId, mailBoxData.UserId, savePath, response);

                return response;
            }
            catch (Exception ex)
            {
                log.ErrorFormat(
                    "StoreMailBody() Problems with message saving in messageId={0}. \r\n Exception: \r\n {1}\r\n",
                    messageItem.MimeMessageId, ex.ToString());

                Storage.Delete(string.Empty, savePath);
                throw;
            }
        }
        //TODO: Need refactoring
        public static Dictionary<int, string> GetPop3NewMessagesIDs(IMailDaoFactory MailDaoFactory, MailBoxData mailBox, Dictionary<int, string> uidls,
            int chunk)
        {
            var newMessages = new Dictionary<int, string>();

            if (!uidls.Any() || uidls.Count == mailBox.MessagesCount)
                return newMessages;

            var i = 0;

            var chunkUidls = uidls.Skip(i).Take(chunk).ToList();

            do
            {
                var checkList = chunkUidls.Select(u => u.Value).Distinct().ToList();

                var existingUidls = MailDaoFactory.GetMailDao().GetExistingUidls(mailBox.MailBoxId, checkList);

                if (!existingUidls.Any())
                {
                    var messages = newMessages;
                    foreach (var item in
                        chunkUidls.Select(uidl => new KeyValuePair<int, string>(uidl.Key, uidl.Value))
                            .Where(item => !messages.Contains(item)))
                    {
                        newMessages.Add(item.Key, item.Value);
                    }
                }
                else if (existingUidls.Count != chunkUidls.Count)
                {
                    var messages = newMessages;
                    foreach (var item in (from uidl in chunkUidls
                                          where !existingUidls.Contains(uidl.Value)
                                          select new KeyValuePair<int, string>(uidl.Key, uidl.Value)).Where(
                            item => !messages.Contains(item)))
                    {
                        newMessages.Add(item.Key, item.Value);
                    }
                }

                i += chunk;

                chunkUidls = uidls.Skip(i).Take(chunk).ToList();

            } while (chunkUidls.Any());

            return newMessages;
        }

        private void UpdateMessagesChains(IMailDaoFactory MailDaoFactory, MailBoxData mailbox, string mimeMessageId,
            string chainId, FolderType folder, int? userFolderId)
        {
            var chainsForUpdate = new[] { new { id = chainId, folder } };

            // if mime_message_id == chain_id - message is first in chain, because it isn't reply
            if (!string.IsNullOrEmpty(mimeMessageId) && mimeMessageId != chainId)
            {
                var query = SimpleConversationsExp.CreateBuilder(mailbox.TenantId, mailbox.UserId)
                    .SetMailboxId(mailbox.MailBoxId)
                    .SetChainId(mimeMessageId)
                    .Build();

                var chains = MailDaoFactory.GetChainDao().GetChains(query)
                    .Select(x => new { id = x.Id, folder = x.Folder })
                    .ToArray();

                if (chains.Any())
                {
                    var updateQuery = SimpleMessagesExp.CreateBuilder(mailbox.TenantId, mailbox.UserId)
                            .SetChainId(mimeMessageId)
                            .Build();

                    MailDaoFactory.GetMailInfoDao().SetFieldValue(
                        updateQuery,
                        "ChainId",
                        chainId);

                    chainsForUpdate = chains.Concat(chainsForUpdate).ToArray();

                    var getQuery = SimpleMessagesExp.CreateBuilder(Tenant, User)
                                .SetMailboxId(mailbox.MailBoxId)
                                .SetChainId(chainId)
                                .Build();

                    var newChainsForUpdate =
                        MailDaoFactory.GetMailInfoDao()
                            .GetMailInfoList(getQuery)
                            .ConvertAll(x => new
                            {
                                id = chainId,
                                folder = x.Folder
                            })
                            .Distinct();

                    chainsForUpdate = chainsForUpdate.Concat(newChainsForUpdate).ToArray();
                }
            }

            foreach (var c in chainsForUpdate.Distinct())
            {
                UpdateChain(c.id, c.folder, userFolderId, mailbox.MailBoxId,
                    mailbox.TenantId, mailbox.UserId);
            }
        }

        //TODO: Need refactoring
        private bool TrySaveMail(MailBoxData mailbox, MailMessageData message,
            Models.MailFolder folder, int? userFolderId, string uidl, string md5, ILog log)
        {
            try
            {
                var folderRestoreId = folder.Folder == FolderType.Spam ? FolderType.Inbox : folder.Folder;

                var attempt = 1;

                while (attempt < 3)
                {
                    try
                    {
                        message.Id = MailSave(mailbox, message, 0,
                            folder.Folder, folderRestoreId, userFolderId, uidl, md5, true);

                        break;
                    }
                    catch (Exception exSql)
                    {
                        if (!exSql.Message.StartsWith("Deadlock found"))
                            throw;

                        if (attempt > 2)
                            throw;

                        log.WarnFormat("[DEADLOCK] MailSave() try again (attempt {0}/2)", attempt);

                        attempt++;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("TrySaveMail Exception:\r\n{0}\r\n", ex.ToString());
            }

            return false;
        }

        //TODO: Need refactoring
        public bool TryStoreMailData(MailMessageData message, MailBoxData mailbox, ILog log)
        {
            try
            {
                if (message.Attachments.Any())
                {
                    log.Debug("StoreAttachments()");
                    var index = 0;
                    message.Attachments.ForEach(att =>
                    {
                        att.fileNumber = ++index;
                        att.mailboxId = mailbox.MailBoxId;
                    });

                    StoreAttachments(mailbox, message.Attachments, message.StreamId);

                    log.Debug("MailMessage.ReplaceEmbeddedImages()");
                    message.ReplaceEmbeddedImages(log);
                }

                log.Debug("StoreMailBody()");
                StoreMailBody(mailbox, message, log);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("TryStoreMailData(Account:{0}): Exception:\r\n{1}\r\n", mailbox.EMail, ex.ToString());

                //Trying to delete all attachments and mailbody
                if (TryRemoveMailDirectory(mailbox, message.StreamId, log))
                {
                    log.InfoFormat("Problem with mail proccessing(Account:{0}). Body and attachment have been deleted", mailbox.EMail);
                }

                return false;
            }

            return true;
        }
        //TODO: Need refactoring
        private bool TryRemoveMailDirectory(MailBoxData mailbox, string streamId, ILog log)
        {
            //Trying to delete all attachments and mailbody
            try
            {
                Storage.DeleteDirectory(string.Empty,
                    MailStoragePathCombiner.GetMessageDirectory(mailbox.UserId, streamId));
                return true;
            }
            catch (Exception ex)
            {
                log.DebugFormat(
                    "Problems with mail_directory deleting. Account: {0}. Folder: {1}/{2}/{3}. Exception: {4}",
                    mailbox.EMail, mailbox.TenantId, mailbox.UserId, streamId, ex.ToString());

                return false;
            }
        }
        //TODO: Need refactoring
        private bool UpdateExistingMessages(MailBoxData mailbox, FolderType folder, string uidl, string md5,
            string mimeMessageId, string subject, DateTime dateSent, bool fromThisMailBox, bool toThisMailBox, List<int> tagsIds, ILog log)
        {
            if ((string.IsNullOrEmpty(md5) || md5.Equals(DefineConstants.MD5_EMPTY)) && string.IsNullOrEmpty(mimeMessageId))
            {
                return false;
            }

            var builder = SimpleMessagesExp.CreateBuilder(mailbox.TenantId, mailbox.UserId, null)
                .SetMailboxId(mailbox.MailBoxId);

            var exp = (string.IsNullOrEmpty(mimeMessageId)
                ? builder.SetMd5(md5)
                : builder.SetMimeMessageId(mimeMessageId))
                .Build();

            var messagesInfo = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp);

            if (!messagesInfo.Any() && folder == FolderType.Sent)
            {
                exp = SimpleMessagesExp.CreateBuilder(mailbox.TenantId, mailbox.UserId, null)
                    .SetMailboxId(mailbox.MailBoxId)
                    .SetFolder((int)FolderType.Sent)
                    .SetSubject(subject)
                    .SetDateSent(dateSent)
                    .Build();

                messagesInfo = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp);
            }

            if (!messagesInfo.Any())
                return false;

            var idList = messagesInfo.Where(m => !m.IsRemoved).Select(m => m.Id).ToList();
            if (!idList.Any())
            {
                log.Info("Message already exists and it was removed from portal.");
                return true;
            }

            if (mailbox.Imap)
            {
                if (tagsIds != null) // Add new tags to existing messages
                {
                    using (var tx = MailDaoFactory.BeginTransaction())
                    {
                        if (tagsIds.Any(tagId => !TagEngine.SetMessagesTag(MailDaoFactory, idList, tagId)))
                        {
                            tx.Rollback();
                            return false;
                        }

                        tx.Commit();
                    }
                }

                if ((!fromThisMailBox || !toThisMailBox) && messagesInfo.Exists(m => m.FolderRestore == folder))
                {
                    var clone = messagesInfo.FirstOrDefault(m => m.FolderRestore == folder && m.Uidl == uidl);
                    if (clone != null)
                        log.InfoFormat("Message already exists: mailId={0}. Clone", clone.Id);
                    else
                    {
                        var existMessage = messagesInfo.First();

                        if (!existMessage.IsRemoved)
                        {
                            if (string.IsNullOrEmpty(existMessage.Uidl))
                            {
                                MailDaoFactory.GetMailInfoDao().SetFieldValue(
                                SimpleMessagesExp.CreateBuilder(mailbox.TenantId, mailbox.UserId)
                                    .SetMessageId(existMessage.Id)
                                    .Build(),
                                "Uidl",
                                uidl);
                            }
                        }

                        log.Info("Message already exists by MD5|MimeMessageId|Subject|DateSent");
                    }


                    return true;
                }
            }
            else
            {
                if (!fromThisMailBox && toThisMailBox && messagesInfo.Count == 1)
                {
                    log.InfoFormat("Message already exists: mailId={0}. Outbox clone", messagesInfo.First().Id);
                    return true;
                }
            }

            if (folder == FolderType.Sent)
            {
                var sentCloneForUpdate =
                    messagesInfo.FirstOrDefault(
                        m => m.FolderRestore == FolderType.Sent && string.IsNullOrEmpty(m.Uidl));

                if (sentCloneForUpdate != null)
                {
                    if (!sentCloneForUpdate.IsRemoved)
                    {
                        MailDaoFactory.GetMailInfoDao().SetFieldValue(
                            SimpleMessagesExp.CreateBuilder(mailbox.TenantId, mailbox.UserId)
                                .SetMessageId(sentCloneForUpdate.Id)
                                .Build(),
                            "Uidl",
                            uidl);
                    }

                    log.InfoFormat("Message already exists: mailId={0}. Outbox clone", sentCloneForUpdate.Id);

                    return true;
                }
            }

            if (folder == FolderType.Spam)
            {
                var first = messagesInfo.First();

                log.InfoFormat("Message already exists: mailId={0}. It was moved to spam on server", first.Id);

                return true;
            }

            var fullClone = messagesInfo.FirstOrDefault(m => m.FolderRestore == folder && m.Uidl == uidl);
            if (fullClone == null)
                return false;

            log.InfoFormat("Message already exists: mailId={0}. Full clone", fullClone.Id);
            return true;

        }

        public List<Chain> GetChainsById(string id)
        {
            var exp = SimpleConversationsExp.CreateBuilder(Tenant, User)
                .SetChainIds(new List<string> { id })
                .Build();

            return MailDaoFactory.GetChainDao().GetChains(exp);
        }

        public long GetNextConversationId(int id, MailSearchFilterData filter)
        {
            var mail = MailDaoFactory.GetMailDao().GetMail(new ConcreteUserMessageExp(id, Tenant, User));

            if (mail == null)
                return 0;

            filter.FromDate = mail.ChainDate;
            filter.FromMessage = id;
            filter.PageSize = 1;

            var messages = GetFilteredConversations(filter, out bool hasMore);
            return messages.Any() ? messages.First().Id : 0;
        }

        public List<MailMessageData> GetConversations(MailSearchFilterData filterData, out bool hasMore)
        {
            if (filterData == null)
                throw new ArgumentNullException("filterData");

            var filter = (MailSearchFilterData)filterData.Clone();

            if (filter.UserFolderId.HasValue && UserFolderEngine.Get(filter.UserFolderId.Value) == null)
                throw new ArgumentException("Folder not found");

            var filteredConversations = GetFilteredConversations(filter, out hasMore);

            if (!filteredConversations.Any())
                return filteredConversations;

            var chainIds = new List<string>();
            filteredConversations.ForEach(x => chainIds.Add(x.ChainId));

            var exp = SimpleConversationsExp.CreateBuilder(Tenant, User)
                .SetChainIds(chainIds)
                .SetFoldersIds(
                    filter.PrimaryFolder == FolderType.Inbox ||
                    filter.PrimaryFolder == FolderType.Sent
                        ? new List<int> { (int)FolderType.Inbox, (int)FolderType.Sent }
                        : new List<int> { (int)filter.PrimaryFolder })
                .Build();

            var extendedInfo = MailDaoFactory.GetChainDao().GetChains(exp);

            foreach (var chain in filteredConversations)
            {
                var chainMessages = extendedInfo.FindAll(x => x.MailboxId == chain.MailboxId && x.Id == chain.ChainId);
                if (!chainMessages.Any()) continue;
                chain.IsNew = chainMessages.Any(x => x.Unread);
                chain.HasAttachments = chainMessages.Any(x => x.HasAttachments);
                chain.Important = chainMessages.Any(x => x.Importance);
                chain.ChainLength = chainMessages.Sum(x => x.Length);
                var firstOrDefault = chainMessages.FirstOrDefault(x => !string.IsNullOrEmpty(x.Tags));
                chain.LabelsString = firstOrDefault != null ? firstOrDefault.Tags : "";
            }

            return filteredConversations;
        }

        public List<MailMessageData> GetConversationMessages(int tenant, string user, int messageId,
            bool loadAllContent, bool needProxyHttp, bool needMailSanitazer, bool markRead = false)
        {
            var messageInfo = MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                SimpleMessagesExp.CreateBuilder(tenant, user)
                    .SetMessageId(messageId)
                    .Build())
                .SingleOrDefault();

            if (messageInfo == null)
                throw new ArgumentException("Message Id not found");

            var searchFolders = new List<int>();

            if (messageInfo.Folder == FolderType.Inbox || messageInfo.Folder == FolderType.Sent)
                searchFolders.AddRange(new[] { (int)FolderType.Inbox, (int)FolderType.Sent });
            else
                searchFolders.Add((int)messageInfo.Folder);

            var exp = SimpleMessagesExp.CreateBuilder(tenant, user)
                .SetMailboxId(messageInfo.MailboxId)
                .SetChainId(messageInfo.ChainId)
                .SetFoldersIds(searchFolders)
                .Build();

            var mailInfoList = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp);

            var ids = mailInfoList.Select(m => m.Id).ToList();

            var messages =
                ids.ConvertAll<MailMessageData>(id =>
                {
                    return GetMessage(id,
                        new MailMessageData.Options
                        {
                            LoadImages = false,
                            LoadBody = loadAllContent || (id == messageId),
                            NeedProxyHttp = needProxyHttp,
                            NeedSanitizer = needMailSanitazer
                        });
                })
                    .Where(mailInfo => mailInfo != null)
                    .OrderBy(m => m.Date)
                    .ToList();

            if (!markRead)
                return messages;

            var unreadMessages = messages.Where(message => message.WasNew).ToList();
            if (!unreadMessages.Any())
                return messages;

            var unreadMessagesCountByFolder = new Dictionary<FolderType, int>();

            foreach (var message in unreadMessages)
            {
                if (unreadMessagesCountByFolder.ContainsKey(message.Folder))
                    unreadMessagesCountByFolder[message.Folder] += 1;
                else
                    unreadMessagesCountByFolder.Add(message.Folder, 1);
            }

            int? userFolder = null;

            if (unreadMessagesCountByFolder.Keys.Any(k => k == FolderType.UserFolder))
            {
                var item = MailDaoFactory.GetUserFolderXMailDao().Get(ids.First());
                userFolder = item == null ? null : item.FolderId;
            }

            List<int> ids2Update;

            using (var tx = MailDaoFactory.BeginTransaction())
            {
                ids2Update = unreadMessages.Select(x => x.Id).ToList();

                MailDaoFactory.GetMailInfoDao().SetFieldValue(
                    SimpleMessagesExp.CreateBuilder(tenant, user)
                        .SetMessageIds(ids2Update)
                        .Build(),
                    "Unread",
                    false);

                foreach (var keyPair in unreadMessagesCountByFolder)
                {
                    var folderType = keyPair.Key;

                    var unreadMessDiff = keyPair.Value != 0 ? keyPair.Value * (-1) : (int?)null;

                    FolderEngine.ChangeFolderCounters(folderType, userFolder,
                            unreadMessDiff, unreadConvDiff: -1);

                    MailDaoFactory.GetChainDao().SetFieldValue(
                        SimpleConversationsExp.CreateBuilder(tenant, user)
                            .SetChainId(messageInfo.ChainId)
                            .SetMailboxId(messageInfo.MailboxId)
                            .SetFolder((int)keyPair.Key)
                            .Build(),
                        "Unread",
                        false);
                }

                if (userFolder.HasValue)
                {
                    var userFoldersIds = MailDaoFactory.GetUserFolderXMailDao().GetList(mailIds: ids)
                        .Select(ufxm => ufxm.FolderId)
                        .Distinct()
                        .ToList();

                    UserFolderEngine.RecalculateCounters(MailDaoFactory, userFoldersIds);
                }

                tx.Commit();
            }

            var data = new MailMail
            {
                Unread = false
            };

            IndexEngine.Update(data, s => s.In(m => m.Id, ids2Update.ToArray()), wrapper => wrapper.Unread);

            return messages;
        }

        public void SetConversationsFolder(List<int> ids, FolderType folder, int? userFolderId = null)
        {
            if (!ids.Any())
                throw new ArgumentNullException("ids");

            List<MailInfo> listObjects;

            listObjects = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(ids);

            if (!listObjects.Any())
                return;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                SetFolder(MailDaoFactory, listObjects, folder, userFolderId);
                tx.Commit();
            }


            if (folder == FolderType.Inbox || folder == FolderType.Sent || folder == FolderType.Spam)
            {
                //TODO: fix OperationEngine.ApplyFilters(listObjects.Select(o => o.Id).ToList());
            }

            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t))
                return;

            var data = new MailMail
            {
                Folder = (byte)folder,
                UserFolders = userFolderId.HasValue
                    ? new List<MailUserFolder>
                    {
                        new MailUserFolder
                        {
                            Id = userFolderId.Value
                        }
                    }
                    : new List<MailUserFolder>()
            };

            Expression<Func<Selector<MailMail>, Selector<MailMail>>> exp =
                s => s.In(m => m.Id, listObjects.Select(o => o.Id).ToArray());

            IndexEngine.Update(data, exp, w => w.Folder);

            IndexEngine.Update(data, exp, UpdateAction.Replace, w => w.UserFolders.ToList());
        }

        public void RestoreConversations(int tenant, string user, List<int> ids)
        {
            if (!ids.Any())
                throw new ArgumentNullException("ids");

            List<MailInfo> listObjects;

            listObjects = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(ids);

            if (!listObjects.Any())
                return;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                Restore(MailDaoFactory, listObjects);
                tx.Commit();
            }

            var filterApplyIds =
                listObjects.Where(
                    m =>
                        m.FolderRestore == FolderType.Inbox || m.FolderRestore == FolderType.Sent ||
                        m.FolderRestore == FolderType.Spam).Select(m => m.Id).ToList();

            if (filterApplyIds.Any())
            {
                //TODO: fix OperationEngine.ApplyFilters(filterApplyIds);
            }

            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t))
                return;

            var mails = listObjects.ConvertAll(m => new MailMail
            {
                Id = m.Id,
                Folder = (byte)m.FolderRestore
            });

            IndexEngine.Update(mails, wrapper => wrapper.Folder);
        }

        public void DeleteConversations(int tenant, string user, List<int> ids)
        {
            if (!ids.Any())
                throw new ArgumentNullException("ids");

            long usedQuota;

            List<MailInfo> listObjects;

            listObjects = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(ids);

            if (!listObjects.Any())
                return;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                usedQuota = SetRemoved(MailDaoFactory, listObjects);
                tx.Commit();
            }

            QuotaEngine.QuotaUsedDelete(usedQuota);

            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t))
                return;

            IndexEngine.Remove(listObjects.Select(info => info.Id).ToList(), Tenant, new Guid(User));
        }

        public void SetConversationsImportanceFlags(int tenant, string user, bool important, List<int> ids)
        {
            List<MailInfo> mailInfos;

            mailInfos = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(ids);

            var chainsInfo = mailInfos
                .Select(m => new
                {
                    m.ChainId,
                    m.MailboxId,
                    m.Folder
                })
                .Distinct().ToList();

            if (!chainsInfo.Any())
                throw new Exception("no chain messages belong to current user");

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                Expression<Func<Dao.Entities.MailMail, bool>> exp = t => true;

                var сhains = new List<Tuple<int, string>>();
                foreach (var chain in chainsInfo)
                {
                    var key = new Tuple<int, string>(chain.MailboxId, chain.ChainId);

                    if (сhains.Any() &&
                            сhains.Contains(key) &&
                            (chain.Folder == FolderType.Inbox || chain.Folder == FolderType.Sent))
                    {
                        continue;
                    }

                    Expression<Func<Dao.Entities.MailMail, bool>> innerWhere = m => m.ChainId == chain.ChainId && m.IdMailbox == chain.MailboxId;

                    if (chain.Folder == FolderType.Inbox || chain.Folder == FolderType.Sent)
                    {
                        innerWhere = innerWhere.And(m => m.Folder == (int)FolderType.Inbox || m.Folder == (int)FolderType.Sent);

                        сhains.Add(key);
                    }
                    else
                    {
                        innerWhere = innerWhere.And(m => m.Folder == (int)chain.Folder);
                    }

                    exp = exp.Or(innerWhere);
                }

                MailDaoFactory.GetMailInfoDao().SetFieldValue(
                    SimpleMessagesExp.CreateBuilder(tenant, user)
                        .SetExp(exp)
                        .Build(),
                    "Importance",
                    important);

                foreach (var chain in chainsInfo)
                {
                    MailDaoFactory.GetChainDao().SetFieldValue(
                        SimpleConversationsExp.CreateBuilder(tenant, user)
                            .SetChainId(chain.ChainId)
                            .SetMailboxId(chain.MailboxId)
                            .SetFolder((int)chain.Folder)
                            .Build(),
                        "Importance",
                        important);
                }

                tx.Commit();
            }

            var data = new MailMail
            {
                Importance = important
            };

            IndexEngine.Update(data, s => s.In(m => m.Id, mailInfos.Select(o => o.Id).ToArray()),
                    wrapper => wrapper.Importance);
        }

        public void UpdateMessageChainAttachmentsFlag(int tenant, string user, int messageId)
        {
            var mail = MailDaoFactory.GetMailDao().GetMail(new ConcreteUserMessageExp(messageId, tenant, user));

            if (mail == null)
                return;

            var maxQuery = SimpleMessagesExp.CreateBuilder(tenant, user)
                    .SetChainId(mail.ChainId)
                    .SetMailboxId(mail.MailboxId)
                    .SetFolder((int)mail.Folder)
                    .Build();

            var maxValue = MailDaoFactory.GetMailInfoDao().GetFieldMaxValue<int>(
                maxQuery,
                "AttachmentsCount");

            var updateQuery = SimpleConversationsExp.CreateBuilder(tenant, user)
                    .SetChainId(mail.ChainId)
                    .SetMailboxId(mail.MailboxId)
                    .SetFolder((int)mail.Folder)
                    .Build();

            MailDaoFactory.GetChainDao().SetFieldValue(
                updateQuery,
                "HasAttachments",
                (maxValue > 0));
        }

        public void UpdateMessageChainUnreadFlag(int tenant, string user, int messageId)
        {
            UpdateMessageChainFlag(tenant, user, messageId, "Unread", "Unread");
        }

        public void UpdateMessageChainImportanceFlag(int tenant, string user, int messageId)
        {
            UpdateMessageChainFlag(tenant, user, messageId, "Importance", "Importance");
        }

        private void UpdateMessageChainFlag(int tenant, string user, int messageId, string fieldFrom, string fieldTo)
        {
            var mail = MailDaoFactory.GetMailDao().GetMail(new ConcreteUserMessageExp(messageId, tenant, user));

            if (mail == null)
                return;

            var maxQuery = SimpleMessagesExp.CreateBuilder(tenant, user)
                    .SetChainId(mail.ChainId)
                    .SetMailboxId(mail.MailboxId)
                    .SetFolder((int)mail.Folder)
                    .Build();

            var maxValue = MailDaoFactory.GetMailInfoDao().GetFieldMaxValue<bool>(
                maxQuery,
                fieldFrom);

            MailDaoFactory.GetChainDao().SetFieldValue(
                SimpleConversationsExp.CreateBuilder(tenant, user)
                    .SetChainId(mail.ChainId)
                    .SetMailboxId(mail.MailboxId)
                    .SetFolder((int)mail.Folder)
                    .Build(),
                fieldTo,
                maxValue);
        }

        public void UpdateChainFields(int tenant, string user, List<int> ids)
        {
            var mailInfoList = MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                SimpleMessagesExp.CreateBuilder(tenant, user, null)
                    .SetMessageIds(ids)
                    .Build())
                .ConvertAll(x => new
                {
                    id_mailbox = x.MailboxId,
                    chain_id = x.ChainId,
                    folder = x.Folder
                });

            if (!mailInfoList.Any()) return;

            foreach (var info in mailInfoList.GroupBy(t => new { t.id_mailbox, t.chain_id, t.folder }))
            {
                int? userFolder = null;

                if (info.Key.folder == FolderType.UserFolder)
                {
                    var item = MailDaoFactory.GetUserFolderXMailDao().Get(ids.First());
                    userFolder = item == null ? null : item.FolderId;
                }

                UpdateChain(info.Key.chain_id, info.Key.folder, userFolder, info.Key.id_mailbox, tenant, user);
            }
        }

        public class TempData
        {
            public long Length { get; set; }
            public DateTime Date { get; set; }
            public int Unread { get; set; }
            public int AttachCount { get; set; }
            public int Importance { get; set; }
        }

        // Method for updating chain flags, date and length.
        public void UpdateChain(string chainId, FolderType folder, int? userFolderId, int mailboxId,
            int tenant, string user)
        {
            if (string.IsNullOrEmpty(chainId)) return;

            var folderId = (int)folder;

            //var p1 = new SqlParameter("@p1", Tenant);
            //var p2 = new SqlParameter("@p2", User);
            //var p3 = new SqlParameter("@p3", false);
            //var p4 = new SqlParameter("@p4", chainId);
            //var p5 = new SqlParameter("@p5", mailboxId);
            //var p6 = new SqlParameter("@p6", folderId);

            var chainInfo = MailDaoFactory.GetContext().ExecuteQuery<TempData>(
                $"SELECT COUNT(*) as Length, " +
                $"MAX(m.date_sent) as Date, " +
                $"MAX(m.unread) as Unread, " +
                $"MAX(m.attachments_count) as AttachCount, " +
                $"MAX(m.importance) as Importance " +
                $"FROM mail_mail m " +
                $"WHERE m.tenant = {Tenant} " +
                $"AND m.id_user = '{User}' " +
                $"AND m.is_removed = 0 " +
                $"AND m.chain_id = '{chainId}' " +
                $"AND m.id_mailbox = {mailboxId} " +
                $"AND m.folder = {(int)folder}")
                .FirstOrDefault();

            /*var chainQuery = MailDaoFactory.MailDb.Mail
                .Where(m => m.Tenant == Tenant)
                .Where(m => m.IdUser == User)
                .Where(m => m.IsRemoved == false)
                .Where(m => m.ChainId == chainId)
                .Where(m => m.IdMailbox == mailboxId)
                .Where(m => m.Folder == folderId)
                //.Select(m => new { m.Id, m.DateSent, m.Unread, m.AttachmentsCount, m.Importance });
                .GroupBy(m => m.Id)
                .Select(g => new
                {
                    length = g.Count(),
                    date = g.Max(m => m.DateSent),
                    unread = g.Max(m => m.Unread),
                    attach_count = g.Max(m => m.AttachmentsCount),
                    importance = g.Max(m => m.Importance)
                });*/

            //var str = chainQuery.ToSql();//.ToString();

            //var chainInfoList = chainQuery.ToList(); //.FirstOrDefault();

            /*var chainInfo = chainInfoList
                    .GroupBy(m => m.Id)
                    .Select(g => new
                    {
                        length = g.Count(),
                        date = g.Max(m => m.DateSent),
                        unread = g.Max(m => m.Unread),
                        attach_count = g.Max(m => m.AttachmentsCount),
                        importance = g.Max(m => m.Importance)
                    }).FirstOrDefault();*/

            //var chainInfo = chainQuery.FirstOrDefault();

            if (chainInfo == null)
            {
                throw new InvalidDataException("Conversation is absent in MAIL_MAIL");
            }

            var query = SimpleConversationsExp.CreateBuilder(tenant, user)
                .SetMailboxId(mailboxId)
                .SetChainId(chainId)
                .SetFolder((int)folder)
                .Build();

            var storedChainInfo = MailDaoFactory.GetChainDao().GetChains(query);

            var chainUnreadFlag = storedChainInfo.Any(c => c.Unread);

            if (0 == chainInfo.Length)
            {
                var deletQuery = SimpleConversationsExp.CreateBuilder(tenant, user)
                    .SetFolder((int)folder)
                    .SetMailboxId(mailboxId)
                    .SetChainId(chainId)
                    .Build();

                var result = MailDaoFactory.GetChainDao().Delete(deletQuery);

                Log.DebugFormat(
                    "UpdateChain() row deleted from chain table tenant='{0}', " +
                    "user_id='{1}', id_mailbox='{2}', folder='{3}', " +
                    "chain_id='{4}' result={5}",
                    tenant, user, mailboxId, folder, chainId, result);

                var unreadConvDiff = chainUnreadFlag ? -1 : (int?)null;

                FolderEngine.ChangeFolderCounters(folder, userFolderId,
                    unreadConvDiff: unreadConvDiff, totalConvDiff: -1);
            }
            else
            {
                var updateQuery = SimpleMessagesExp.CreateBuilder(tenant, user)
                        .SetChainId(chainId)
                        .SetMailboxId(mailboxId)
                        .SetFolder((int)folder)
                        .Build();

                MailDaoFactory.GetMailInfoDao().SetFieldValue(updateQuery,
                    "ChainDate",
                    chainInfo.Date);

                var tags = MailDaoFactory.GetTagMailDao().GetChainTags(chainId, folder, mailboxId);

                var chain = new Chain
                {
                    Id = chainId,
                    Tenant = tenant,
                    User = user,
                    MailboxId = mailboxId,
                    Folder = folder,
                    Length = (int)chainInfo.Length,
                    Unread = chainInfo.Unread == 1,
                    HasAttachments = chainInfo.AttachCount > 0,
                    Importance = chainInfo.Importance == 1,
                    Tags = tags
                };

                MailDaoFactory.GetChainDao().SaveChain(chain);

                Log.DebugFormat(
                    "UpdateChain() row inserted to chain table tenant='{0}', user_id='{1}', id_mailbox='{2}', folder='{3}', chain_id='{4}'",
                    tenant, user, mailboxId, folder, chainId);

                var unreadConvDiff = (int?)null;
                var totalConvDiff = (int?)null;

                if (!storedChainInfo.Any())
                {
                    totalConvDiff = 1;
                    unreadConvDiff = chainInfo.Unread == 1 ? 1 : null;
                }
                else
                {
                    if (chainUnreadFlag != (chainInfo.Unread == 1))
                    {
                        unreadConvDiff = chainInfo.Unread == 1 ? 1 : -1;
                    }
                }

                FolderEngine.ChangeFolderCounters(folder, userFolderId,
                    unreadConvDiff: unreadConvDiff, totalConvDiff: totalConvDiff);
            }
        }

        public List<MailMessageData> GetConversation(int id, bool? loadAll, bool? markRead, bool? needSanitize)
        {
            if (id <= 0)
                throw new ArgumentException(@"id must be positive integer", "id");
#if DEBUG
            var watch = new Stopwatch();
            watch.Start();
#endif
            var list = GetConversationMessages(Tenant, User, id,
                loadAll.GetValueOrDefault(false),
                MailSettings.NeedProxyHttp,
                needSanitize.GetValueOrDefault(false),
                markRead.GetValueOrDefault(false));
#if DEBUG
            watch.Stop();
            Log.DebugFormat("Mail->GetConversation(id={0})->Elapsed {1}ms (NeedProxyHttp={2}, NeedSanitizer={3})", id,
                watch.Elapsed.TotalMilliseconds, MailSettings.NeedProxyHttp, needSanitize.GetValueOrDefault(false));
#endif
            var item = list.FirstOrDefault(m => m.Id == id);

            if (item == null || item.Folder != FolderType.UserFolder)
                return list;

            var userFolder = UserFolderEngine.GetByMail((uint)item.Id);

            if (userFolder != null)
            {
                list.ForEach(m => m.UserFolderId = userFolder.Id);
            }

            return list;
        }

        private List<MailMessageData> GetFilteredConversations(MailSearchFilterData filter, out bool hasMore)
        {
            var conversations = new List<MailMessageData>();
            var skipFlag = false;
            var chunkIndex = 0;

            if (filter.FromDate.HasValue && filter.FromMessage.HasValue && filter.FromMessage.Value > 0)
            {
                skipFlag = true;
            }

            var prevFlag = filter.PrevFlag.GetValueOrDefault(false);

            var tenantInfo = TenantManager.GetTenant(Tenant);
            var utcNow = DateTime.UtcNow;
            var pageSize = filter.PageSize.GetValueOrDefault(25);

            while (conversations.Count < pageSize + 1)
            {
                filter.PageSize = CHUNK_SIZE * pageSize;

                IMessagesExp exp = null;

                var t = ServiceProvider.GetService<MailMail>();
                if (!filter.IsDefault() && FactoryIndexer.Support(t) && FactoryIndexerCommon.CheckState(false))
                {
                    filter.Page = chunkIndex * CHUNK_SIZE * pageSize; // Elastic Limit from {index of last message} to {count of messages}

                    if (FilterChainMessagesExp.TryGetFullTextSearchChains(FactoryIndexer, ServiceProvider,
                        filter, User, out List<MailMail> Mails))
                    {
                        if (!Mails.Any())
                            break;

                        var ids = Mails.Select(c => c.Id).ToList();

                        var query = SimpleMessagesExp.CreateBuilder(Tenant, User)
                            .SetMessageIds(ids)
                            .SetOrderBy(filter.Sort);

                        if (prevFlag)
                        {
                            query.SetOrderAsc(!(filter.SortOrder == DefineConstants.ASCENDING));
                        }
                        else
                        {
                            query.SetOrderAsc(filter.SortOrder == DefineConstants.ASCENDING);
                        }

                        exp = query
                            .Build();
                    }
                }
                else
                {
                    filter.Page = chunkIndex; // MySQL Limit from {page by size} to {size}

                    exp = new FilterChainMessagesExp(filter, Tenant, User);
                }

                chunkIndex++;

                var listMessages = MailDaoFactory.GetMailInfoDao().GetMailInfoList(exp, true)
                    .ConvertAll(m => ToMailMessage(m, tenantInfo, utcNow));

                if (0 == listMessages.Count)
                    break;

                if (skipFlag && filter.FromMessage.HasValue)
                {
                    var messageData = listMessages.FirstOrDefault(m => m.Id == filter.FromMessage.Value);

                    if (messageData != null)
                    {
                        // Skip chain messages by FromMessage.
                        listMessages =
                            listMessages.Where(
                                m => !(m.ChainId.Equals(messageData.ChainId) && m.MailboxId == messageData.MailboxId))
                                .ToList();
                    }

                    skipFlag = false;
                }

                foreach (var messageData in listMessages)
                {
                    var existingChainIndex =
                        conversations.FindIndex(
                            c => c.ChainId == messageData.ChainId && c.MailboxId == messageData.MailboxId);

                    if (existingChainIndex > -1)
                    {
                        if (conversations[existingChainIndex].Date < messageData.Date)
                            conversations[existingChainIndex] = messageData;
                    }
                    else
                    {
                        conversations.Add(messageData);
                    }
                }

                if (conversations.Count > pageSize)
                    break;
            }

            hasMore = conversations.Count > pageSize;

            if (hasMore)
            {
                conversations = conversations.Take(pageSize).ToList();
            }

            if (prevFlag)
            {
                conversations.Reverse();
            }

            return conversations;
        }

        public MailAttachmentData GetAttachment(IAttachmentExp exp)
        {
            var attachment = MailDaoFactory.GetAttachmentDao().GetAttachment(exp);

            return ToAttachmentData(attachment);
        }

        public List<MailAttachmentData> GetAttachments(IAttachmentsExp exp)
        {
            var attachments = MailDaoFactory.GetAttachmentDao().GetAttachments(exp);

            return attachments.ConvertAll(ToAttachmentData);
        }

        public long GetAttachmentsSize(IAttachmentsExp exp)
        {
            var size = MailDaoFactory.GetAttachmentDao().GetAttachmentsSize(exp);

            return size;
        }

        public int GetAttachmentNextFileNumber(IAttachmentsExp exp)
        {
            var number = MailDaoFactory.GetAttachmentDao().GetAttachmentsMaxFileNumber(exp);

            number++;

            return number;
        }

        public MailAttachmentData AttachFileFromDocuments(int tenant, string user, int messageId, string fileId, string version, bool needSaveToTemp = false)
        {
            MailAttachmentData result;

            var fileDao = FilesDaoFactory.GetFileDao<string>();

            var file = string.IsNullOrEmpty(version)
                           ? fileDao.GetFile(fileId)
                           : fileDao.GetFile(fileId, Convert.ToInt32(version));

            if (file == null)
                throw new AttachmentsException(AttachmentsException.Types.DocumentNotFound, "File not found.");

            if (!FilesSeurity.CanRead(file))
                throw new AttachmentsException(AttachmentsException.Types.DocumentAccessDenied,
                                               "Access denied.");

            if (!fileDao.IsExistOnStorage(file))
            {
                throw new AttachmentsException(AttachmentsException.Types.DocumentNotFound,
                                               "File not exists on storage.");
            }

            Log.InfoFormat("Original file id: {0}", file.ID);
            Log.InfoFormat("Original file name: {0}", file.Title);
            var fileExt = FileUtility.GetFileExtension(file.Title);
            var curFileType = FileUtility.GetFileTypeByFileName(file.Title);
            Log.InfoFormat("File converted type: {0}", file.ConvertedType);

            if (file.ConvertedType != null)
            {
                switch (curFileType)
                {
                    case FileType.Image:
                        fileExt = file.ConvertedType == ".zip" ? ".pptt" : file.ConvertedType;
                        break;
                    case FileType.Spreadsheet:
                        fileExt = file.ConvertedType != ".xlsx" ? ".xlst" : file.ConvertedType;
                        break;
                    default:
                        if (file.ConvertedType == ".doct" || file.ConvertedType == ".xlst" || file.ConvertedType == ".pptt")
                            fileExt = file.ConvertedType;
                        break;
                }
            }

            var convertToExt = string.Empty;
            switch (curFileType)
            {
                case FileType.Document:
                    if (fileExt == ".doct")
                        convertToExt = ".docx";
                    break;
                case FileType.Spreadsheet:
                    if (fileExt == ".xlst")
                        convertToExt = ".xlsx";
                    break;
                case FileType.Presentation:
                    if (fileExt == ".pptt")
                        convertToExt = ".pptx";
                    break;
            }

            if (!string.IsNullOrEmpty(convertToExt) && fileExt != convertToExt)
            {
                var fileName = Path.ChangeExtension(file.Title, convertToExt);
                Log.InfoFormat("Changed file name - {0} for file {1}:", fileName, file.ID);

                using var readStream = FileConverter.Exec(file, convertToExt);

                if (readStream == null)
                    throw new AttachmentsException(AttachmentsException.Types.DocumentAccessDenied, "Access denied.");

                using var memStream = new MemoryStream();

                readStream.CopyTo(memStream);
                result = AttachFileToDraft(tenant, user, messageId, fileName, memStream, memStream.Length, null, needSaveToTemp);
                Log.InfoFormat("Attached attachment: ID - {0}, Name - {1}, StoredUrl - {2}", result.fileName, result.fileName, result.storedFileUrl);
            }
            else
            {
                using var readStream = fileDao.GetFileStream(file);

                if (readStream == null)
                    throw new AttachmentsException(AttachmentsException.Types.DocumentAccessDenied, "Access denied.");

                result = AttachFileToDraft(tenant, user, messageId, file.Title, readStream, readStream.CanSeek ? readStream.Length : file.ContentLength, null, needSaveToTemp);
                Log.InfoFormat("Attached attachment: ID - {0}, Name - {1}, StoredUrl - {2}", result.fileName, result.fileName, result.storedFileUrl);
            }

            return result;
        }

        public MailAttachmentData AttachFile(int tenant, string user, MailMessageData message,
            string name, Stream inputStream, long contentLength, string contentType = null, bool needSaveToTemp = false)
        {
            if (message == null)
                throw new AttachmentsException(AttachmentsException.Types.MessageNotFound, "Message not found.");

            if (string.IsNullOrEmpty(message.StreamId))
                throw new AttachmentsException(AttachmentsException.Types.MessageNotFound, "StreamId is empty.");

            var messageId = message.Id;

            var totalSize = GetAttachmentsSize(new ConcreteMessageAttachmentsExp(messageId, tenant, user));

            totalSize += contentLength;

            if (totalSize > DefineConstants.ATTACHMENTS_TOTAL_SIZE_LIMIT)
                throw new AttachmentsException(AttachmentsException.Types.TotalSizeExceeded,
                    "Total size of all files exceeds limit!");

            var fileNumber =
                GetAttachmentNextFileNumber(new ConcreteMessageAttachmentsExp(messageId, tenant,
                    user));

            var attachment = new MailAttachmentData
            {
                fileName = name,
                contentType = string.IsNullOrEmpty(contentType) ? MimeMapping.GetMimeMapping(name) : contentType,
                needSaveToTemp = needSaveToTemp,
                fileNumber = fileNumber,
                size = contentLength,
                data = inputStream.ReadToEnd(),
                streamId = message.StreamId,
                tenant = tenant,
                user = user,
                mailboxId = message.MailboxId
            };

            QuotaEngine.QuotaUsedAdd(contentLength);

            try
            {
                StorageManager.StoreAttachmentWithoutQuota(attachment);
            }
            catch
            {
                QuotaEngine.QuotaUsedDelete(contentLength);
                throw;
            }

            if (!needSaveToTemp)
            {
                int attachCount;

                using (var tx = MailDaoFactory.BeginTransaction())
                {
                    attachment.fileId = MailDaoFactory.GetAttachmentDao().SaveAttachment(attachment.ToAttachmnet(messageId));

                    attachCount = MailDaoFactory.GetAttachmentDao().GetAttachmentsCount(
                        new ConcreteMessageAttachmentsExp(messageId, tenant, user));

                    MailDaoFactory.GetMailInfoDao().SetFieldValue(
                        SimpleMessagesExp.CreateBuilder(tenant, user)
                            .SetMessageId(messageId)
                            .Build(),
                        "AttachmentsCount",
                        attachCount);

                    UpdateMessageChainAttachmentsFlag(tenant, user, messageId);

                    tx.Commit();
                }

                if (attachCount == 1)
                {
                    var data = new MailMail
                    {
                        HasAttachments = true
                    };

                    IndexEngine.Update(data, s => s.Where(m => m.Id, messageId), wrapper => wrapper.HasAttachments);
                }
            }

            return attachment;
        }

        public MailAttachmentData AttachFileToDraft(int tenant, string user, int messageId,
            string name, Stream inputStream, long contentLength, string contentType = null, bool needSaveToTemp = false)
        {
            if (messageId < 1)
                throw new AttachmentsException(AttachmentsException.Types.BadParams, "Field 'id_message' must have non-negative value.");

            if (tenant < 0)
                throw new AttachmentsException(AttachmentsException.Types.BadParams, "Field 'id_tenant' must have non-negative value.");

            if (String.IsNullOrEmpty(user))
                throw new AttachmentsException(AttachmentsException.Types.BadParams, "Field 'id_user' is empty.");

            if (contentLength == 0)
                throw new AttachmentsException(AttachmentsException.Types.EmptyFile, "Empty files not supported.");

            var message = GetMessage(messageId, new MailMessageData.Options());

            if (message.Folder != FolderType.Draft && message.Folder != FolderType.Templates && message.Folder != FolderType.Sending)
                throw new AttachmentsException(AttachmentsException.Types.BadParams, "Message is not a draft or templates.");

            return AttachFile(tenant, user, message, name, inputStream, contentLength, contentType, needSaveToTemp);
        }

        public void StoreAttachmentCopy(int tenant, string user, MailAttachmentData attachment, string streamId)
        {
            try
            {
                if (attachment.streamId.Equals(streamId) && !attachment.isTemp) return;

                string s3Key;

                var dataClient = StorageFactory.GetMailStorage(tenant);

                if (attachment.needSaveToTemp || attachment.isTemp)
                {
                    s3Key = MailStoragePathCombiner.GetTempStoredFilePath(attachment);
                }
                else
                {
                    s3Key = MailStoragePathCombiner.GerStoredFilePath(attachment);
                }

                if (!dataClient.IsFile(s3Key)) return;

                attachment.fileNumber =
                    !string.IsNullOrEmpty(attachment.contentId) //Upload hack: embedded attachment have to be saved in 0 folder
                        ? 0
                        : attachment.fileNumber;

                var newS3Key = MailStoragePathCombiner.GetFileKey(user, streamId, attachment.fileNumber,
                                                                    attachment.storedName);

                var copyS3Url = dataClient.Copy(s3Key, string.Empty, newS3Key);

                attachment.storedFileUrl = MailStoragePathCombiner.GetStoredUrl(copyS3Url);

                attachment.streamId = streamId;

                attachment.tempStoredUrl = null;

                Log.DebugFormat("StoreAttachmentCopy() tenant='{0}', user_id='{1}', stream_id='{2}', new_s3_key='{3}', copy_s3_url='{4}', storedFileUrl='{5}',  filename='{6}'",
                    tenant, user, streamId, newS3Key, copyS3Url, attachment.storedFileUrl, attachment.fileName);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("CopyAttachment(). filename='{0}', ctype='{1}' Exception:\r\n{2}\r\n",
                           attachment.fileName,
                           attachment.contentType,
                    ex.ToString());

                throw;
            }
        }

        public void DeleteMessageAttachments(int tenant, string user, int messageId, List<int> attachmentIds)
        {
            long usedQuota;
            int attachCount;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var exp = new ConcreteMessageAttachmentsExp(messageId, tenant, user, attachmentIds,
                    onlyEmbedded: null);

                usedQuota = MailDaoFactory.GetAttachmentDao().GetAttachmentsSize(exp);

                MailDaoFactory.GetAttachmentDao().SetAttachmnetsRemoved(exp);

                attachCount = MailDaoFactory.GetAttachmentDao().GetAttachmentsCount(
                    new ConcreteMessageAttachmentsExp(messageId, tenant, user));

                MailDaoFactory.GetMailInfoDao().SetFieldValue(
                    SimpleMessagesExp.CreateBuilder(tenant, user)
                        .SetMessageId(messageId)
                        .Build(),
                    "AttachmentsCount",
                    attachCount);

                UpdateMessageChainAttachmentsFlag(tenant, user, messageId);

                tx.Commit();
            }

            if (attachCount == 0)
            {
                var data = new MailMail
                {
                    HasAttachments = false
                };

                IndexEngine.Update(data, s => s.Where(m => m.Id, messageId), wrapper => wrapper.HasAttachments);
            }

            if (usedQuota <= 0)
                return;

            QuotaEngine.QuotaUsedDelete(usedQuota);
        }

        public void StoreAttachments(MailBoxData mailBoxData, List<MailAttachmentData> attachments, string streamId)
        {
            if (!attachments.Any() || string.IsNullOrEmpty(streamId)) return;

            try
            {
                var quotaAddSize = attachments.Sum(a => a.data != null ? a.data.LongLength : a.dataStream.Length);

                foreach (var attachment in attachments)
                {
                    var isAttachmentNameHasBadName = string.IsNullOrEmpty(attachment.fileName)
                                                         || attachment.fileName.IndexOfAny(Path.GetInvalidPathChars()) != -1
                                                         || attachment.fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1;
                    if (isAttachmentNameHasBadName)
                    {
                        attachment.fileName = string.Format("attacment{0}{1}", attachment.fileNumber,
                                                                               MimeMapping.GetExtention(attachment.contentType));
                    }

                    attachment.streamId = streamId;
                    attachment.tenant = mailBoxData.TenantId;
                    attachment.user = mailBoxData.UserId;

                    //TODO: Check TenantId and UserId in StorageManager
                    StorageManager.StoreAttachmentWithoutQuota(attachment);
                }

                QuotaEngine.QuotaUsedAdd(quotaAddSize);
            }
            catch
            {
                var storedAttachmentsKeys = attachments
                                            .Where(a => !string.IsNullOrEmpty(a.storedFileUrl))
                                            .Select(MailStoragePathCombiner.GerStoredFilePath)
                                            .ToList();

                if (storedAttachmentsKeys.Any())
                {
                    var storage = StorageFactory.GetMailStorage(mailBoxData.TenantId);

                    storedAttachmentsKeys.ForEach(key => storage.Delete(string.Empty, key));
                }

                Log.InfoFormat("[Failed] StoreAttachments(mailboxId={0}). All message attachments were deleted.", mailBoxData.MailBoxId);

                throw;
            }
        }

        public MailMessageData ToMailMessage(MailInfo mailInfo, Tenant tenantInfo, DateTime utcNow)
        {
            var now = TenantUtil.DateTimeFromUtc(tenantInfo.TimeZone, utcNow);
            var date = TenantUtil.DateTimeFromUtc(tenantInfo.TimeZone, mailInfo.DateSent);
            var chainDate = TenantUtil.DateTimeFromUtc(tenantInfo.TimeZone, mailInfo.ChainDate);

            var isToday = (now.Year == date.Year && now.Date == date.Date);
            var isYesterday = (now.Year == date.Year && now.Date == date.Date.AddDays(1));

            return new MailMessageData
            {
                Id = mailInfo.Id,
                From = mailInfo.From,
                To = mailInfo.To,
                Cc = mailInfo.Cc,
                ReplyTo = mailInfo.ReplyTo,
                Subject = mailInfo.Subject,
                Important = mailInfo.Importance,
                Date = date,
                Size = mailInfo.Size,
                HasAttachments = mailInfo.HasAttachments,
                IsNew = mailInfo.IsNew,
                IsAnswered = mailInfo.IsAnswered,
                IsForwarded = mailInfo.IsForwarded,
                LabelsString = mailInfo.LabelsString,
                RestoreFolderId = mailInfo.FolderRestore,
                Folder = mailInfo.Folder,
                ChainId = mailInfo.ChainId ?? "",
                ChainLength = 1,
                ChainDate = chainDate,
                IsToday = isToday,
                IsYesterday = isYesterday,
                MailboxId = mailInfo.MailboxId,
                CalendarUid = mailInfo.CalendarUid,
                Introduction = mailInfo.Intoduction
            };
        }

        protected MailMessageData ToMailMessage(Entities.Mail mail, List<int> tags, List<Attachment> attachments,
            MailMessageData.Options options)
        {
            var now = TenantUtil.DateTimeFromUtc(TenantManager.GetTenant(Tenant).TimeZone, DateTime.UtcNow);
            var date = TenantUtil.DateTimeFromUtc(TenantManager.GetTenant(Tenant).TimeZone, mail.DateSent);
            var isToday = (now.Year == date.Year && now.Date == date.Date);
            var isYesterday = (now.Year == date.Year && now.Date == date.Date.AddDays(1));

            var item = new MailMessageData
            {
                Id = mail.Id,
                ChainId = mail.ChainId,
                ChainDate = mail.ChainDate,
                Attachments = null,
                Address = mail.Address,
                Bcc = mail.Bcc,
                Cc = mail.Cc,
                Date = date,
                From = mail.From,
                HasAttachments = mail.AttachCount > 0,
                Important = mail.Importance,
                IsAnswered = mail.IsAnswered,
                IsForwarded = mail.IsForwarded,
                IsNew = false,
                TagIds = tags,
                ReplyTo = mail.Reply,
                Size = mail.Size,
                Subject = mail.Subject,
                To = mail.To,
                StreamId = mail.Stream,
                Folder = mail.Folder,
                WasNew = mail.Unread,
                IsToday = isToday,
                IsYesterday = isYesterday,
                Introduction = !string.IsNullOrEmpty(mail.Introduction) ? mail.Introduction.Trim() : "",
                TextBodyOnly = mail.IsTextBodyOnly,
                MailboxId = mail.MailboxId,
                RestoreFolderId = mail.FolderRestore,
                HasParseError = mail.HasParseError,
                MimeMessageId = mail.MimeMessageId,
                MimeReplyToId = mail.MimeInReplyTo,
                CalendarUid = mail.CalendarUid,
                Uidl = mail.Uidl
            };

            //Reassemble paths
            if (options.LoadBody)
            {
                var htmlBody = "";

                if (!item.HasParseError)
                {
#if DEBUG
                    var watch = new Stopwatch();
                    double swtGetBodyMilliseconds;
                    double swtSanitazeilliseconds = 0;
#endif

                    var dataStore = StorageFactory.GetMailStorage(Tenant);
                    var key = MailStoragePathCombiner.GetBodyKey(User, item.StreamId);

                    try
                    {
#if DEBUG
                        Log.DebugFormat(
                            "Mail->GetMailInfo(id={0})->Start Body Load tenant: {1}, user: '{2}', key='{3}'",
                            mail.Id, Tenant, User, key);

                        watch.Start();
#endif
                        using (var s = dataStore.GetReadStream(string.Empty, key))
                        {
                            htmlBody = Encoding.UTF8.GetString(s.ReadToEnd());
                        }
#if DEBUG
                        watch.Stop();
                        swtGetBodyMilliseconds = watch.Elapsed.TotalMilliseconds;
                        watch.Reset();
#endif
                        if (options.NeedSanitizer && item.Folder != FolderType.Draft &&
                            !item.From.Equals(MailSettings.MailDaemonEmail))
                        {
#if DEBUG
                            watch.Start();
#endif
                            bool imagesAreBlocked;

                            Log.DebugFormat(
                                "Mail->GetMailInfo(id={0})->Start Sanitize Body tenant: {1}, user: '{2}', BodyLength: {3} bytes",
                                mail.Id, Tenant, User, htmlBody.Length);

                            htmlBody = HtmlSanitizer.Sanitize(htmlBody, out imagesAreBlocked,
                                new HtmlSanitizer.Options(options.LoadImages, options.NeedProxyHttp));

#if DEBUG
                            watch.Stop();
                            swtSanitazeilliseconds = watch.Elapsed.TotalMilliseconds;
#endif
                            item.ContentIsBlocked = imagesAreBlocked;
                        }
#if DEBUG
                        Log.DebugFormat(
                            "Mail->GetMailInfo(id={0})->Elapsed: BodyLoad={1}ms, Sanitaze={2}ms (NeedSanitizer={3}, NeedProxyHttp={4})",
                            mail.Id, swtGetBodyMilliseconds, swtSanitazeilliseconds, options.NeedSanitizer,
                            options.NeedProxyHttp);
#endif
                    }
                    catch (Exception ex)
                    {
                        item.IsBodyCorrupted = true;
                        htmlBody = "";
                        Log.Error(
                            string.Format("Mail->GetMailInfo(tenant={0} user=\"{1}\" messageId={2} key=\"{3}\")",
                                Tenant, User, mail.Id, key), ex);
#if DEBUG
                        watch.Stop();
                        swtGetBodyMilliseconds = watch.Elapsed.TotalMilliseconds;
                        Log.DebugFormat(
                            "Mail->GetMailInfo(id={0})->Elapsed [BodyLoadFailed]: BodyLoad={1}ms, Sanitaze={2}ms (NeedSanitizer={3}, NeedProxyHttp={4})",
                            mail.Id, swtGetBodyMilliseconds, swtSanitazeilliseconds, options.NeedSanitizer,
                            options.NeedProxyHttp);
#endif
                    }
                }

                item.HtmlBody = htmlBody;
            }

            item.Attachments = attachments.ConvertAll(ToAttachmentData);

            return item;
        }

        public static MailAttachmentData ToAttachmentData(Attachment attachment)
        {
            if (attachment == null) return null;

            var a = new MailAttachmentData
            {
                fileId = attachment.Id,
                fileName = attachment.Name,
                storedName = attachment.StoredName,
                contentType = attachment.Type,
                size = attachment.Size,
                fileNumber = attachment.FileNumber,
                streamId = attachment.Stream,
                tenant = attachment.Tenant,
                user = attachment.User,
                contentId = attachment.ContentId,
                mailboxId = attachment.MailboxId
            };

            return a;
        }
    }
}
