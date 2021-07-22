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
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Notify.Signalr;
using ASC.Data.Storage;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.Attachment;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Core.Dao.Expressions.Message;
using ASC.Mail.Enums;
using ASC.Mail.Extensions;
using ASC.Mail.Models;
using ASC.Mail.Models.Base;
using ASC.Mail.Storage;
using ASC.Mail.Utils;

using Microsoft.Extensions.Options;

using MailMessage = ASC.Mail.Models.MailMessageData;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class ComposeEngineBase
    {
        public ILog Log { get; set; }
        public static SignalrServiceClient _signalrServiceClient;
        public readonly bool _isAutoreply;
        public readonly bool _sslCertificatePermit;
        public const string EMPTY_HTML_BODY = "<div dir=\"ltr\"><br></div>"; // GMail style

        public int Tenant => TenantManager.GetCurrentTenant().TenantId;

        public string User => SecurityContext.CurrentAccount.ID.ToString();

        private protected AccountEngine AccountEngine { get; }
        private protected MailboxEngine MailboxEngine { get; }
        private protected MessageEngine MessageEngine { get; }
        private QuotaEngine QuotaEngine { get; }
        private protected IndexEngine IndexEngine { get; }
        private FolderEngine FolderEngine { get; }
        private protected IMailDaoFactory MailDaoFactory { get; }
        private protected StorageManager StorageManager { get; }
        private SecurityContext SecurityContext { get; }
        private protected TenantManager TenantManager { get; }
        private protected CoreSettings CoreSettings { get; }
        private StorageFactory StorageFactory { get; }
        internal MailSettings MailSettings { get; }
        public class DeliveryFailureMessageTranslates
        {
            public string DaemonEmail { get; set; }
            public string SubjectLabel { get; set; }
            public string AutomaticMessageLabel { get; set; }
            public string MessageIdentificator { get; set; }
            public string RecipientsLabel { get; set; }
            public string RecommendationsLabel { get; set; }
            public string TryAgainButtonLabel { get; set; }
            public string FaqInformationLabel { get; set; }
            public string ReasonLabel { get; set; }

            public DeliveryFailureMessageTranslates(string daemonEmail,
                string subjectLabel,
                string automaticMessageLabel,
                string messageIdentificator,
                string recipientsLabel,
                string recommendationsLabel,
                string tryAgainButtonLabel,
                string faqInformationLabel,
                string reasonLabel
                )
            {
                DaemonEmail = daemonEmail;
                SubjectLabel = subjectLabel;
                AutomaticMessageLabel = automaticMessageLabel;
                MessageIdentificator = messageIdentificator;
                RecipientsLabel = recipientsLabel;
                RecommendationsLabel = recommendationsLabel;
                TryAgainButtonLabel = tryAgainButtonLabel;
                FaqInformationLabel = faqInformationLabel;
                ReasonLabel = reasonLabel;
            }

            public static DeliveryFailureMessageTranslates Defauilt
            {
                get
                {
                    return new DeliveryFailureMessageTranslates("mail-daemon@onlyoffice.com",
                        "Message Delivery Failure",
                        "This message was created automatically by mail delivery software.",
                        "Delivery failed for message with subject \"{subject}\" from {date}.",
                        "Message could not be delivered to recipient(s)",
                        "Please, check your message recipients addresses and message format. " +
                        "If you are sure your message is correct, check all the {account_name} account settings, " +
                        "and, if everything is correct, sign in to the mail service you use and confirm any " +
                        "verification questions, in case there are some. After then try again.",
                        "Change your message",
                        "In case the error persists, please read the {url_begin}FAQ section{url_end} " +
                        "to learn more about the problem.",
                        "Reason");
                }
            }
        }

        public DeliveryFailureMessageTranslates DaemonLabels { get; internal set; }

        public ComposeEngineBase(
            AccountEngine accountEngine,
            MailboxEngine mailboxEngine,
            MessageEngine messageEngine,
            QuotaEngine quotaEngine,
            IndexEngine indexEngine,
            FolderEngine folderEngine,
            IMailDaoFactory mailDaoFactory,
            StorageManager storageManager,
            SecurityContext securityContext,
            TenantManager tenantManager,
            CoreSettings coreSettings,
            StorageFactory storageFactory,
            IOptionsSnapshot<SignalrServiceClient> optionsSnapshot,
            IOptionsMonitor<ILog> option,
            MailSettings mailSettings,
            DeliveryFailureMessageTranslates daemonLabels = null)
        {
            AccountEngine = accountEngine;
            MailboxEngine = mailboxEngine;
            MessageEngine = messageEngine;
            QuotaEngine = quotaEngine;
            IndexEngine = indexEngine;
            FolderEngine = folderEngine;
            MailDaoFactory = mailDaoFactory;
            StorageManager = storageManager;
            SecurityContext = securityContext;
            TenantManager = tenantManager;
            CoreSettings = coreSettings;
            StorageFactory = storageFactory;

            MailSettings = mailSettings;

            Log = option.Get("ASC.Mail.ComposeEngineBase");

            DaemonLabels = daemonLabels ?? DeliveryFailureMessageTranslates.Defauilt;

            _sslCertificatePermit = MailSettings.SslCertificatesErrorsPermit;

            if (_signalrServiceClient != null) return;
            _signalrServiceClient = optionsSnapshot.Get("mail");
        }

        #region .Public

        public virtual MailMessage Save(MessageModel model, DeliveryFailureMessageTranslates translates = null)
        {
            var mailAddress = new MailAddress(model.From);

            var accounts = AccountEngine.GetAccountInfoList().ToAccountData();

            var account = accounts.FirstOrDefault(a => a.Email.ToLower().Equals(mailAddress.Address));

            if (account == null)
                throw new ArgumentException("Mailbox not found");

            if (account.IsGroup)
                throw new InvalidOperationException("Saving emails from a group address is forbidden");

            var mbox = MailboxEngine.GetMailboxData(
                new СoncreteUserMailboxExp(account.MailboxId, Tenant, User));

            if (mbox == null)
                throw new ArgumentException("No such mailbox");

            string mimeMessageId, streamId;

            var previousMailboxId = mbox.MailBoxId;

            if (model.Id > 0)
            {
                var message = MessageEngine.GetMessage(model.Id, new MailMessageData.Options
                {
                    LoadImages = false,
                    LoadBody = true,
                    NeedProxyHttp = MailSettings.NeedProxyHttp,
                    NeedSanitizer = false
                });

                if (message.Folder != FolderType.Draft)
                {
                    throw new InvalidOperationException("Saving emails is permitted only in the Drafts folder");
                }

                if (message.HtmlBody.Length > MailSettings.MaximumMessageBodySize)
                {
                    throw new InvalidOperationException("Message body exceeded limit (" + MailSettings.MaximumMessageBodySize / 1024 + " KB)");
                }

                mimeMessageId = message.MimeMessageId;

                streamId = message.StreamId;

                previousMailboxId = message.MailboxId;
            }
            else
            {
                mimeMessageId = MailUtil.CreateMessageId(TenantManager, CoreSettings);
                streamId = MailUtil.CreateStreamId();
            }

            var fromAddress = MailUtil.CreateFullEmail(mbox.Name, mbox.EMail.Address);

            var compose = new MailDraftData(model.Id, mbox, fromAddress, model.To, model.Cc, model.Bcc, model.Subject, mimeMessageId,
                    model.MimeReplyToId, model.Importance, model.Tags, model.Body, streamId, model.Attachments, model.CalendarIcs)
            {
                PreviousMailboxId = previousMailboxId
            };

            DaemonLabels = translates ?? DeliveryFailureMessageTranslates.Defauilt;

            return Save(compose);
        }

        public MailMessage Save(MailComposeBase compose)
        {
            var embededAttachmentsForSaving = FixHtmlBodyWithEmbeddedAttachments(compose);

            var message = compose.ToMailMessage();

            //var engine = new EngineFactory(compose.Mailbox.TenantId, compose.Mailbox.UserId);

            var addIndex = compose.Id == 0;

            var attachmentsToRestore = message.Attachments.Where(att => att.streamId != message.StreamId || att.isTemp).ToList();

            var needRestoreAttachments = attachmentsToRestore.Any();

            if (needRestoreAttachments)
            {
                message.Attachments.ForEach(
                    attachment =>
                        MessageEngine.StoreAttachmentCopy(compose.Mailbox.TenantId, compose.Mailbox.UserId,
                            attachment, compose.StreamId));
            }

            MessageEngine.StoreMailBody(compose.Mailbox, message, Log);

            long usedQuota;

            using (var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                compose.Id = MessageEngine.MailSave(compose.Mailbox, message, compose.Id, message.Folder, message.Folder, null,
                    string.Empty, string.Empty, false, out usedQuota);

                message.Id = compose.Id;

                if (compose.AccountChanged)
                {
                    MessageEngine.UpdateChain(message.ChainId, message.Folder, null, compose.PreviousMailboxId,
                        compose.Mailbox.TenantId, compose.Mailbox.UserId);
                }

                if (compose.Id > 0 && needRestoreAttachments)
                {
                    var existingAttachments = MailDaoFactory.GetAttachmentDao().GetAttachments(
                        new ConcreteMessageAttachmentsExp(compose.Id, compose.Mailbox.TenantId, compose.Mailbox.UserId));

                    foreach (var attachment in message.Attachments)
                    {
                        if (existingAttachments.Any(x => x.Id == attachment.fileId))
                        {
                            continue;
                        }

                        var attach = attachment.ToAttachmnet(compose.Id);
                        attach.Id = 0;

                        var newId = MailDaoFactory.GetAttachmentDao().SaveAttachment(attach);
                        attachment.fileId = newId;
                    }

                    if (message.Attachments.Any())
                    {
                        var count = MailDaoFactory.GetAttachmentDao().GetAttachmentsCount(
                            new ConcreteMessageAttachmentsExp(compose.Id, compose.Mailbox.TenantId, compose.Mailbox.UserId));

                        MailDaoFactory.GetMailInfoDao().SetFieldValue(
                            SimpleMessagesExp.CreateBuilder(compose.Mailbox.TenantId, compose.Mailbox.UserId)
                                .SetMessageId(compose.Id)
                                .Build(),
                            "AttachmentsCount",
                            count);
                    }
                }

                if (compose.Id > 0 && embededAttachmentsForSaving.Any())
                {
                    foreach (var attachment in embededAttachmentsForSaving)
                    {
                        var newId = MailDaoFactory.GetAttachmentDao().SaveAttachment(attachment.ToAttachmnet(compose.Id));
                        attachment.fileId = newId;
                    }

                    if (message.Attachments.Any())
                    {
                        var count = MailDaoFactory.GetAttachmentDao().GetAttachmentsCount(
                            new ConcreteMessageAttachmentsExp(compose.Id, compose.Mailbox.TenantId, compose.Mailbox.UserId));

                        MailDaoFactory.GetMailInfoDao().SetFieldValue(
                            SimpleMessagesExp.CreateBuilder(compose.Mailbox.TenantId, compose.Mailbox.UserId)
                                .SetMessageId(compose.Id)
                                .Build(),
                            "AttachmentsCount",
                            count);
                    }
                }

                MessageEngine
                    .UpdateChain(message.ChainId, message.Folder, null,
                    compose.Mailbox.MailBoxId, compose.Mailbox.TenantId, compose.Mailbox.UserId);

                if (compose.AccountChanged)
                {
                    MailDaoFactory.GetCrmLinkDao().UpdateCrmLinkedMailboxId(message.ChainId, compose.PreviousMailboxId,
                        compose.Mailbox.MailBoxId);
                }

                tx.Commit();

            }

            if (usedQuota > 0)
            {
                QuotaEngine.QuotaUsedDelete(usedQuota);
            }

            if (addIndex)
            {
                IndexEngine.Add(message.ToMailWrapper(compose.Mailbox.TenantId, new Guid(compose.Mailbox.UserId)));
            }
            else
            {
                IndexEngine.Update(new List<MailMail>
                {
                    message.ToMailWrapper(compose.Mailbox.TenantId,
                        new Guid(compose.Mailbox.UserId))
                });
            }

            try
            {
                var tempStorage = StorageFactory.GetMailStorage(compose.Mailbox.TenantId);

                tempStorage.DeleteDirectory("attachments_temp", compose.Mailbox.UserId + "/" + compose.StreamId + "/");
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Clearing temp storage failed with exception: {0}", ex.ToString());
            }

            return message;
        }

        public MailMessage GetTemplate()
        {
            var template = new MailMessage
            {
                Attachments = new List<MailAttachmentData>(),
                Bcc = "",
                Cc = "",
                Subject = "",
                From = "",
                HtmlBody = "",
                Important = false,
                ReplyTo = "",
                MimeMessageId = "",
                MimeReplyToId = "",
                To = "",
                StreamId = MailUtil.CreateStreamId()
            };

            return template;
        }

        #endregion

        #region .Private

        private List<MailAttachmentData> FixHtmlBodyWithEmbeddedAttachments(MailComposeBase compose)
        {
            var embededAttachmentsForSaving = new List<MailAttachmentData>();

            var embeddedLinks = compose.GetEmbeddedAttachmentLinks(StorageManager);
            if (!embeddedLinks.Any())
                return embededAttachmentsForSaving;

            var fckStorage = StorageManager.GetDataStoreForCkImages(compose.Mailbox.TenantId);
            var attachmentStorage = StorageManager.GetDataStoreForAttachments(compose.Mailbox.TenantId);
            var currentMailFckeditorUrl = fckStorage.GetUri(StorageManager.CKEDITOR_IMAGES_DOMAIN, "").ToString();
            var currentUserStorageUrl = MailStoragePathCombiner.GetUserMailsDirectory(compose.Mailbox.UserId);

            foreach (var embeddedLink in embeddedLinks)
            {
                try
                {
                    var isFckImage = embeddedLink.StartsWith(currentMailFckeditorUrl);
                    var prefixLength = isFckImage
                        ? currentMailFckeditorUrl.Length
                        : embeddedLink.IndexOf(currentUserStorageUrl, StringComparison.Ordinal) +
                          currentUserStorageUrl.Length + 1;
                    var fileLink = HttpUtility.UrlDecode(embeddedLink.Substring(prefixLength));
                    var fileName = Path.GetFileName(fileLink);
                    var attach = new MailAttachmentData
                    {
                        fileName = fileName,
                        storedName = fileName,
                        contentId = embeddedLink.GetMd5(),
                        storedFileUrl = fileLink,
                        streamId = compose.StreamId,
                        user = compose.Mailbox.UserId,
                        tenant = compose.Mailbox.TenantId,
                        mailboxId = compose.Mailbox.MailBoxId
                    };

                    var savedAttachment =
                        MessageEngine.GetAttachment(
                            new ConcreteContentAttachmentExp(compose.Id, attach.contentId));

                    var savedAttachmentId = savedAttachment == null ? 0 : savedAttachment.fileId;

                    var attachmentWasSaved = savedAttachmentId != 0;
                    var currentImgStorage = isFckImage ? fckStorage : attachmentStorage;
                    var domain = isFckImage ? StorageManager.CKEDITOR_IMAGES_DOMAIN : compose.Mailbox.UserId;

                    if (compose.Id == 0 || !attachmentWasSaved)
                    {
                        attach.data = StorageManager.LoadDataStoreItemData(domain, fileLink, currentImgStorage);

                        StorageManager.StoreAttachmentWithoutQuota(attach);

                        embededAttachmentsForSaving.Add(attach);
                    }

                    if (attachmentWasSaved)
                    {
                        attach = MessageEngine.GetAttachment(
                            new ConcreteUserAttachmentExp(savedAttachmentId, compose.Mailbox.TenantId, compose.Mailbox.UserId));

                        var path = MailStoragePathCombiner.GerStoredFilePath(attach);
                        currentImgStorage = attachmentStorage;
                        attach.storedFileUrl =
                            MailStoragePathCombiner.GetStoredUrl(currentImgStorage.GetUri(path));
                    }

                    compose.HtmlBody = compose.HtmlBody.Replace(embeddedLink, attach.storedFileUrl);

                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("ChangeEmbededAttachmentLinksForStoring() failed with exception: {0}", ex.ToString());
                }
            }

            return embededAttachmentsForSaving;
        }

        #endregion
    }
}
