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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.ElasticSearch;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.Conversation;
using ASC.Mail.Core.Dao.Expressions.Message;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;
using ASC.Web.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class TagEngine
    {
        private int Tenant => TenantManager.GetCurrentTenant().TenantId;
        private string UserId => SecurityContext.CurrentAccount.ID.ToString();

        private TenantManager TenantManager { get; }
        private SecurityContext SecurityContext { get; }
        private ILog Log { get; }
        private IMailDaoFactory MailDaoFactory { get; }
        private IndexEngine IndexEngine { get; }
        private FactoryIndexer<MailMail> FactoryIndexer { get; }
        private FactoryIndexer FactoryIndexerCommon { get; }
        private IServiceProvider ServiceProvider { get; }
        private WebItemSecurity WebItemSecurity { get; }
        public TagEngine(
            TenantManager tenantManager,
            SecurityContext securityContext,
            IMailDaoFactory mailDaoFactory,
            IndexEngine indexEngine,
            WebItemSecurity webItemSecurity,
            FactoryIndexer<MailMail> factoryIndexer,
            FactoryIndexer factoryIndexerCommon,
            IServiceProvider serviceProvider,
            IOptionsMonitor<ILog> option)
        {
            TenantManager = tenantManager;
            SecurityContext = securityContext;

            MailDaoFactory = mailDaoFactory;

            IndexEngine = indexEngine;

            FactoryIndexer = factoryIndexer;
            FactoryIndexerCommon = factoryIndexerCommon;
            ServiceProvider = serviceProvider;
            WebItemSecurity = webItemSecurity;

            Log = option.Get("ASC.Mail.TagEngine");
        }

        public Tag GetTag(int id)
        {
            return MailDaoFactory.GetTagDao().GetTag(id);
        }

        public Tag GetTag(string name)
        {
            return MailDaoFactory.GetTagDao().GetTag(name);
        }

        public List<Tag> GetTags()
        {
            var tagList = MailDaoFactory.GetTagDao().GetTags();

            if (!WebItemSecurity.IsAvailableForMe(WebItemManager.CRMProductID))
            {
                return tagList
                    .Where(p => p.TagName != "")
                    .OrderByDescending(p => p.Id)
                    .ToList();
            }

            var actualCrmTags = MailDaoFactory.GetTagDao().GetCrmTags();

            var removedCrmTags =
                tagList.Where(t => t.Id < 0 && !actualCrmTags.Exists(ct => ct.Id == t.Id))
                    .ToList();

            if (removedCrmTags.Any())
            {
                MailDaoFactory.GetTagDao().DeleteTags(removedCrmTags.Select(t => t.Id).ToList());
                removedCrmTags.ForEach(t => tagList.Remove(t));
            }

            foreach (var crmTag in actualCrmTags)
            {
                var tag = tagList.FirstOrDefault(t => t.Id == crmTag.Id);
                if (tag != null)
                    tag.TagName = crmTag.TagName;
                else
                    tagList.Add(crmTag);
            }

            return tagList
                .Where(p => !string.IsNullOrEmpty(p.TagName))
                .OrderByDescending(p => p.Id)
                .ToList();
        }

        public List<Tag> GetCrmTags(string email)
        {
            var tags = new List<Tag>();

            var allowedContactIds = MailDaoFactory.GetCrmContactDao().GetCrmContactIds(email);

            if (!allowedContactIds.Any())
                return tags;

            tags = MailDaoFactory.GetTagDao().GetCrmTags(allowedContactIds);

            return tags
                .Where(p => !string.IsNullOrEmpty(p.TagName))
                .OrderByDescending(p => p.Id)
                .ToList();
        }

        public bool IsTagExists(string name)
        {
            var tag = MailDaoFactory.GetTagDao().GetTag(name);

            return tag != null;

        }

        public Tag CreateTag(string name, string style, IEnumerable<string> addresses)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            //TODO: Need transaction?

            var tag = MailDaoFactory.GetTagDao().GetTag(name);

            if (tag != null)
                throw new ArgumentException("Tag name already exists");

            var emails = addresses as IList<string> ?? addresses.ToList();

            tag = new Tag
            {
                Id = 0,
                TagName = name,
                Tenant = Tenant,
                User = UserId,
                Addresses = string.Join(";", emails),
                Style = style,
                Count = 0,
                CrmId = 0
            };

            var id = MailDaoFactory.GetTagDao().SaveTag(tag);

            if (id < 0)
                throw new Exception("Save failed");

            foreach (var email in emails)
            {
                MailDaoFactory.GetTagAddressDao().Save(id, email);
            }

            tag.Id = id;

            //Commit transaction

            return tag;
        }

        public Tag UpdateTag(int id, string name, string style, IEnumerable<string> addresses)
        {
            var tag = MailDaoFactory.GetTagDao().GetTag(id);

            if (tag == null)
                throw new ArgumentException(@"Tag not found");

            if (!tag.TagName.Equals(name))
            {
                var tagByName = MailDaoFactory.GetTagDao().GetTag(name);

                if (tagByName != null && tagByName.Id != id)
                    throw new ArgumentException(@"Tag name already exists");

                tag.TagName = name;
                tag.Style = style;
            }

            //Start transaction
            var oldAddresses = MailDaoFactory.GetTagAddressDao().GetTagAddresses(tag.Id);

            var newAddresses = addresses as IList<string> ?? addresses.ToList();
            tag.Addresses = string.Join(";", newAddresses);

            MailDaoFactory.GetTagDao().SaveTag(tag);

            if (!newAddresses.Any())
            {
                if (oldAddresses.Any())
                    MailDaoFactory.GetTagAddressDao().Delete(tag.Id);
            }
            else
            {
                foreach (var oldAddress in oldAddresses)
                {
                    if (!newAddresses.Contains(oldAddress))
                    {
                        MailDaoFactory.GetTagAddressDao().Delete(tag.Id, oldAddress);
                    }
                }

                foreach (var newAddress in newAddresses)
                {
                    if (!oldAddresses.Contains(newAddress))
                    {
                        MailDaoFactory.GetTagAddressDao().Save(tag.Id, newAddress);
                    }
                }
            }

            //Commit transaction

            return tag;
        }

        public bool DeleteTag(int id)
        {
            //Begin transaction

            MailDaoFactory.GetTagDao().DeleteTag(id);

            MailDaoFactory.GetTagAddressDao().Delete(id);

            MailDaoFactory.GetTagMailDao().DeleteByTagId(id);

            //Commit transaction

            return true;
        }

        public List<int> GetOrCreateTags(int tenant, string user, string[] names)
        {
            var tagIds = new List<int>();

            if (!names.Any())
                return tagIds;


            var tags = MailDaoFactory.GetTagDao().GetTags();

            foreach (var name in names)
            {
                var tag =
                    tags.FirstOrDefault(t => t.TagName.Equals(name, StringComparison.InvariantCultureIgnoreCase));

                if (tag != null)
                {
                    tagIds.Add(tag.Id);
                    continue;
                }

                tag = new Tag
                {
                    Id = 0,
                    TagName = name,
                    Addresses = "",
                    Count = 0,
                    CrmId = 0,
                    Style = (Math.Abs(name.GetHashCode() % 16) + 1).ToString(CultureInfo.InvariantCulture),
                    Tenant = tenant,
                    User = user
                };

                var id = MailDaoFactory.GetTagDao().SaveTag(tag);

                if (id > 0)
                {
                    Log.InfoFormat("TagEngine->GetOrCreateTags(): new tag '{0}' with id = {1} has bee created",
                        name, id);

                    tagIds.Add(id);
                }
            }

            return tagIds;
        }

        public void SetMessagesTag(List<int> messageIds, int tagId)
        {
            using (var tx = MailDaoFactory.BeginTransaction())
            {
                if (!SetMessagesTag(MailDaoFactory, messageIds, tagId))
                {
                    tx.Rollback();
                    return;
                }

                tx.Commit();
            }

            UpdateIndexerTags(messageIds, UpdateAction.Add, tagId);

            Log.InfoFormat("TagEngine->SetMessagesTag(): tag with id = {0} has bee added to messages [{1}]", tagId,
                string.Join(",", messageIds));
        }

        public bool SetMessagesTag(IMailDaoFactory daoFactory, List<int> messageIds, int tagId)
        {
            var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

            if (tag == null)
            {
                return false;
            }

            GetValidForUserMessages(messageIds, out List<int> validIds, out List<ChainInfo> chains);

            MailDaoFactory.GetTagMailDao().SetMessagesTag(validIds, tag.Id);

            UpdateTagsCount(tag);

            foreach (var chain in chains)
            {
                UpdateChainTags(chain.Id, chain.Folder, chain.MailboxId);
            }

            // Change time_modified for index
            MailDaoFactory.GetMailDao().SetMessagesChanged(validIds);

            return true;
        }

        public void UpdateChainTags(string chainId, FolderType folder, int mailboxId)
        {
            var tags = MailDaoFactory.GetTagMailDao().GetChainTags(chainId, folder, mailboxId);

            var updateQuery = SimpleConversationsExp.CreateBuilder(Tenant, UserId)
                    .SetChainId(chainId)
                    .SetMailboxId(mailboxId)
                    .SetFolder((int)folder)
                    .Build();

            MailDaoFactory.GetChainDao().SetFieldValue(
                updateQuery,
                "Tags",
                tags);
        }

        public void UnsetMessagesTag(List<int> messageIds, int tagId)
        {
            List<int> validIds;

            using (var tx = MailDaoFactory.BeginTransaction())
            {
                GetValidForUserMessages(messageIds, out validIds, out List<ChainInfo> chains);

                MailDaoFactory.GetTagMailDao().Delete(tagId, validIds);

                var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

                if (tag != null)
                    UpdateTagsCount(tag);

                foreach (var chain in chains)
                {
                    UpdateChainTags(chain.Id, chain.Folder, chain.MailboxId);
                }

                // Change time_modified for index
                MailDaoFactory.GetMailDao().SetMessagesChanged(validIds);

                tx.Commit();
            }

            UpdateIndexerTags(validIds, UpdateAction.Remove, tagId);
        }

        public void SetConversationsTag(IEnumerable<int> messagesIds, int tagId)
        {
            var ids = messagesIds as IList<int> ?? messagesIds.ToList();

            if (!ids.Any()) return;

            List<int> validIds;

            using (var tx = MailDaoFactory.BeginTransaction())
            {
                var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

                if (tag == null)
                {
                    tx.Rollback();
                    return;
                }

                var foundedChains = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(messagesIds.ToList());

                if (!foundedChains.Any())
                {
                    tx.Rollback();
                    return;
                }

                validIds = foundedChains.Select(r => r.Id).ToList();
                var chains =
                    foundedChains.GroupBy(r => new { r.ChainId, r.Folder, r.MailboxId })
                        .Select(
                            r =>
                                new ChainInfo
                                {
                                    Id = r.Key.ChainId,
                                    Folder = r.Key.Folder,
                                    MailboxId = r.Key.MailboxId
                                });

                MailDaoFactory.GetTagMailDao().SetMessagesTag(validIds, tag.Id);

                UpdateTagsCount(tag);

                foreach (var chain in chains)
                {
                    UpdateChainTags(chain.Id, chain.Folder, chain.MailboxId);
                }

                // Change time_modified for index
                MailDaoFactory.GetMailDao().SetMessagesChanged(validIds);

                tx.Commit();
            }

            UpdateIndexerTags(validIds, UpdateAction.Add, tagId);
        }

        public void UnsetConversationsTag(IEnumerable<int> messagesIds, int tagId)
        {
            var ids = messagesIds as IList<int> ?? messagesIds.ToList();

            if (!ids.Any()) return;

            List<int> validIds;

            using (var tx = MailDaoFactory.BeginTransaction())
            {
                var foundedChains = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(messagesIds.ToList());

                if (!foundedChains.Any())
                {
                    tx.Rollback();
                    return;
                }

                validIds = foundedChains.Select(r => r.Id).ToList();

                var chains =
                    foundedChains.GroupBy(r => new { r.ChainId, r.Folder, r.MailboxId })
                        .Select(
                            r =>
                                new ChainInfo
                                {
                                    Id = r.Key.ChainId,
                                    Folder = r.Key.Folder,
                                    MailboxId = r.Key.MailboxId
                                });

                MailDaoFactory.GetTagMailDao().Delete(tagId, validIds);

                var tag = MailDaoFactory.GetTagDao().GetTag(tagId);

                if (tag != null)
                    UpdateTagsCount(tag);

                foreach (var chain in chains)
                {
                    UpdateChainTags(chain.Id, chain.Folder, chain.MailboxId);
                }

                // Change time_modified for index
                MailDaoFactory.GetMailDao().SetMessagesChanged(validIds);

                tx.Commit();
            }

            UpdateIndexerTags(validIds, UpdateAction.Remove, tagId);
        }

        private void UpdateIndexerTags(List<int> ids, UpdateAction action, int tagId)
        {
            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t) || !FactoryIndexerCommon.CheckState(false))
                return;

            if (ids == null || !ids.Any())
                return;

            var data = new MailMail
            {
                Tags = new List<MailTag>
                    {
                        new MailTag
                        {
                            Id = tagId
                        }
                    }
            };

            Expression<Func<Selector<MailMail>, Selector<MailMail>>> exp =
                s => s.In(m => m.Id, ids.ToArray());

            IndexEngine.Update(data, exp, action, s => s.Tags.ToList());
        }

        private void UpdateTagsCount(Tag tag)
        {
            var count = MailDaoFactory.GetTagMailDao().CalculateTagCount(tag.Id);

            tag.Count = count;

            MailDaoFactory.GetTagDao().SaveTag(tag);
        }

        private void GetValidForUserMessages(List<int> messagesIds, out List<int> validIds,
            out List<ChainInfo> chains)
        {
            var mailInfoList = MailDaoFactory.GetMailInfoDao().GetMailInfoList(
                SimpleMessagesExp.CreateBuilder(Tenant, UserId)
                    .SetMessageIds(messagesIds)
                    .Build());

            validIds = new List<int>();
            chains = new List<ChainInfo>();

            foreach (var mailInfo in mailInfoList)
            {
                validIds.Add(mailInfo.Id);
                chains.Add(new ChainInfo
                {
                    Id = mailInfo.ChainId,
                    Folder = mailInfo.Folder,
                    MailboxId = mailInfo.MailboxId
                });
            }
        }
    }
}
