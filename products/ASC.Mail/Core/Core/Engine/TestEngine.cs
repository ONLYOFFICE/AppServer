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
using ASC.Common.Logging;
using ASC.Common.Web;
using ASC.Mail.Clients;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Models;
using ASC.Mail.Enums;
using ASC.Mail.Exceptions;
using ASC.Mail.Extensions;
using ASC.Mail.Utils;
using MailMessage = ASC.Mail.Models.MailMessageData;
using ASC.Core;
using Microsoft.Extensions.Options;
using ASC.Data.Storage;
using ASC.Mail.Storage;
using ASC.Common;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class TestEngine
    {
        private int Tenant => TenantManager.GetCurrentTenant().TenantId;
        private string User => SecurityContext.CurrentAccount.ID.ToString();

        private ILog Log { get; }
        private SecurityContext SecurityContext { get; }
        private TenantManager TenantManager { get; }
        private CoreSettings CoreSettings { get; }
        private AccountEngine AccountEngine { get; }
        private MailboxEngine MailboxEngine { get; }
        private MessageEngine MessageEngine { get; }
        private IndexEngine IndexEngine { get; }
        private StorageFactory StorageFactory { get; }

        private const string SAMPLE_UIDL = "api sample";
        private const string SAMPLE_REPLY_UIDL = "api sample reply";
        private const string LOREM_IPSUM_SUBJECT = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
        private const string LOREM_IPSUM_INTRO = "Lorem ipsum introduction";
        private const string LOREM_IPSUM_BODY =
            "<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce vestibulum luctus mauris, " +
            "eget blandit libero auctor quis. Vestibulum quam ex, euismod sit amet luctus eget, condimentum " +
            "vel nulla. Etiam pretium justo tortor, gravida scelerisque augue porttitor sed. Sed in purus neque. " +
            "Sed eget efficitur erat. Ut lobortis eros vitae urna lacinia, mattis efficitur felis accumsan. " +
            "Nullam at dapibus tortor, ut vulputate libero. Fusce ac auctor eros. Aenean justo quam, sodales nec " +
            "risus eget, cursus semper lacus. Nullam mattis neque ac felis euismod aliquet. Donec id eros " +
            "condimentum, egestas sapien vitae, tempor tortor. Nam vehicula ligula eget congue egestas. " +
            "Nulla facilisi. Aenean sodales gravida arcu, a volutpat nulla accumsan ac. Duis leo enim, condimentum " +
            "in malesuada at, rhoncus sed ex. Quisque fringilla scelerisque lacus.</p>";

        public TestEngine(
            SecurityContext securityContext,
            TenantManager tenantManager,
            CoreSettings coreSettings,
            AccountEngine accountEngine,
            MailboxEngine mailboxEngine,
            MessageEngine messageEngine,
            IndexEngine indexEngine,
            StorageFactory storageFactory,
            IOptionsMonitor<ILog> option)
        {
            SecurityContext = securityContext;
            TenantManager = tenantManager;
            CoreSettings = coreSettings;
            AccountEngine = accountEngine;
            MailboxEngine = mailboxEngine;
            MessageEngine = messageEngine;
            IndexEngine = indexEngine;
            StorageFactory = storageFactory;

            Log = option.Get("ASC.Mail.TestEngine");
        }

        public int CreateSampleMessage(TestMessageModel model, bool add2Index = false)
        {
            var folder = model.FolderId.HasValue ? (FolderType)model.FolderId.Value : FolderType.Inbox;

            if (!MailFolder.IsIdOk(folder))
                throw new ArgumentException(@"Invalid folder id", "folderId");

            if (!model.MailboxId.HasValue)
                throw new ArgumentException(@"Invalid mailbox id", "mailboxId");

            var accounts = AccountEngine.GetAccountInfoList().ToAccountData().ToList();

            var account = model.MailboxId.HasValue
                ? accounts.FirstOrDefault(a => a.MailboxId == model.MailboxId)
                : accounts.FirstOrDefault(a => a.IsDefault) ?? accounts.FirstOrDefault();

            if (account == null)
                throw new ArgumentException("Mailbox not found");

            var mbox = MailboxEngine.GetMailboxData(
                new СoncreteUserMailboxExp(account.MailboxId, Tenant, User));

            if (mbox == null)
                throw new ArgumentException("no such mailbox");

            var internalId = string.IsNullOrEmpty(model.MimeMessageId)
                ? MailUtil.CreateMessageId(TenantManager, CoreSettings, Log)
                : model.MimeMessageId;

            var restoreFolder = folder == FolderType.Spam || folder == FolderType.Trash
                ? FolderType.Inbox
                : folder;

            string sampleBody;
            string sampleIntro;

            if (!model.To.Any())
            {
                model.To = new List<string> { mbox.EMail.Address };
            }

            if (!string.IsNullOrEmpty(model.Body))
            {
                sampleBody = model.Body;
                sampleIntro = MailUtil.GetIntroduction(model.Body);
            }
            else
            {
                sampleBody = LOREM_IPSUM_BODY;
                sampleIntro = LOREM_IPSUM_INTRO;
            }

            var sampleMessage = new MailMessage
            {
                Date = model.Date ?? DateTime.UtcNow,
                MimeMessageId = internalId,
                MimeReplyToId = null,
                ChainId = internalId,
                ReplyTo = "",
                To = string.Join(", ", model.To.ToArray()),
                Cc = model.Cc.Any() ? string.Join(", ", model.Cc.ToArray()) : "",
                Bcc = model.Bcc.Any() ? string.Join(", ", model.Bcc.ToArray()) : "",
                Subject = string.IsNullOrEmpty(model.Subject) ? LOREM_IPSUM_SUBJECT : model.Subject,
                Important = model.Importance,
                TextBodyOnly = false,
                Attachments = new List<MailAttachmentData>(),
                Size = sampleBody.Length,
                MailboxId = mbox.MailBoxId,
                HtmlBody = sampleBody,
                Introduction = sampleIntro,
                Folder = folder,
                RestoreFolderId = restoreFolder,
                IsNew = model.Unread,
                StreamId = MailUtil.CreateStreamId(),
                CalendarUid = model.CalendarUid
            };

            if (!string.IsNullOrEmpty(model.FromAddress))
            {
                var from = Parser.ParseAddress(model.FromAddress);

                sampleMessage.From = from.ToString();
                sampleMessage.FromEmail = from.Email;
            }
            else
            {
                sampleMessage.From = MailUtil.CreateFullEmail(mbox.Name, mbox.EMail.Address);
                sampleMessage.FromEmail = mbox.EMail.Address;
            }

            if (model.TagIds != null && model.TagIds.Any())
            {
                sampleMessage.TagIds = model.TagIds;
            }

            MessageEngine.StoreMailBody(mbox, sampleMessage, Log);

            var id = MessageEngine.MailSave(mbox, sampleMessage, 0, folder, restoreFolder, model.UserFolderId,
                SAMPLE_UIDL, "", false);

            if (!add2Index)
                return id;

            var message = MessageEngine.GetMessage(id, new MailMessageData.Options());

            message.IsNew = model.Unread;

            var wrapper = message.ToMailWrapper(mbox.TenantId, new Guid(mbox.UserId));

            IndexEngine.Add(wrapper);

            return id;
        }

        public int CreateReplyToSampleMessage(int id, string body, bool add2Index = false)
        {
            var message = MessageEngine.GetMessage(id, new MailMessage.Options());

            if (message == null)
                throw new ArgumentException("Message with id not found");

            var mbox = MailboxEngine.GetMailboxData(
                new СoncreteUserMailboxExp(message.MailboxId, Tenant, User));

            if (mbox == null)
                throw new ArgumentException("Mailbox not found");

            var mimeMessageId = MailUtil.CreateMessageId(TenantManager, CoreSettings, Log);

            var sampleMessage = new MailMessage
            {
                Date = DateTime.UtcNow,
                MimeMessageId = mimeMessageId,
                MimeReplyToId = message.MimeMessageId,
                ChainId = message.MimeMessageId,
                ReplyTo = message.FromEmail,
                To = message.FromEmail,
                Cc = "",
                Bcc = "",
                Subject = "Re: " + message.Subject,
                Important = message.Important,
                TextBodyOnly = false,
                Attachments = new List<MailAttachmentData>(),
                Size = body.Length,
                MailboxId = mbox.MailBoxId,
                HtmlBody = body,
                Introduction = body,
                Folder = FolderType.Sent,
                RestoreFolderId = FolderType.Sent,
                IsNew = false,
                StreamId = MailUtil.CreateStreamId(),
                From = MailUtil.CreateFullEmail(mbox.Name, mbox.EMail.Address),
                FromEmail = mbox.EMail.Address
            };

            MessageEngine.StoreMailBody(mbox, sampleMessage, Log);

            var replyId = MessageEngine.MailSave(mbox, sampleMessage, 0, FolderType.Sent, FolderType.Sent, null,
                SAMPLE_REPLY_UIDL, "", false);

            if (!add2Index)
                return replyId;

            var replyMessage = MessageEngine.GetMessage(replyId, new MailMessageData.Options());

            var wrapper = replyMessage.ToMailWrapper(mbox.TenantId, new Guid(mbox.UserId));

            IndexEngine.Add(wrapper);

            return replyId;
        }

        public MailAttachmentData AppendAttachmentsToSampleMessage(int? messageId, TestAttachmentModel model)
        {
            if (!messageId.HasValue || messageId.Value <= 0)
                throw new ArgumentException(@"Invalid message id", "messageId");

            var message = MessageEngine.GetMessage(messageId.Value, new MailMessage.Options());

            if (message == null)
                throw new AttachmentsException(AttachmentsException.Types.MessageNotFound, "Message not found.");

            if (!message.Uidl.Equals(SAMPLE_UIDL))
                throw new Exception("Message is not api sample.");

            if (string.IsNullOrEmpty(model.Filename))
                throw new Exception("File name is empty.");

            if (model.Stream == null)
                throw new Exception("File stream is empty.");

            model.ContentType = string.IsNullOrEmpty(model.ContentType) ? MimeMapping.GetMimeMapping(model.Filename) : model.ContentType;

            return MessageEngine.AttachFile(Tenant, User, message, model.Filename, model.Stream, model.Stream.Length, model.ContentType);
        }

        public int LoadSampleMessage(TestMessageModel model, bool add2Index = false)
        {
            var folder = model.FolderId.HasValue ? (FolderType)model.FolderId.Value : FolderType.Inbox;

            if (!MailFolder.IsIdOk(folder))
                throw new ArgumentException(@"Invalid folder id", "folderId");

            if (!model.MailboxId.HasValue)
                throw new ArgumentException(@"Invalid mailbox id", "mailboxId");

            if(model.EmlStream == null)
                throw new ArgumentException(@"Invalid eml stream", "emlStream");

            var accounts = AccountEngine.GetAccountInfoList().ToAccountData().ToList();

            var account = model.MailboxId.HasValue
                ? accounts.FirstOrDefault(a => a.MailboxId == model.MailboxId)
                : accounts.FirstOrDefault(a => a.IsDefault) ?? accounts.FirstOrDefault();

            if (account == null)
                throw new ArgumentException("Mailbox not found");

            var mbox = MailboxEngine.GetMailboxData(
                new СoncreteUserMailboxExp(account.MailboxId, Tenant, User));

            if (mbox == null)
                throw new ArgumentException("no such mailbox");

            var mimeMessage = MailClient.ParseMimeMessage(model.EmlStream);

            var storage = StorageFactory.GetMailStorage(mbox.TenantId);

            var message = MessageEngine.Save(mbox, mimeMessage, SAMPLE_UIDL,
                new MailFolder(folder, ""), model.UserFolderId, model.Unread, Log);

            if (message == null)
                return -1;

            if (!add2Index)
                return message.Id;

            IndexEngine.Add(message.ToMailWrapper(mbox.TenantId, new Guid(mbox.UserId)));

            return message.Id;
        }
    }
}
