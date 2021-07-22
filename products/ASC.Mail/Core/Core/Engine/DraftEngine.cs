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
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Notify.Signalr;
using ASC.Data.Storage;
using ASC.ElasticSearch;
using ASC.Mail.Clients;
using ASC.Mail.Configuration;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.Contact;
using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Core.Dao.Expressions.Message;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;
using ASC.Mail.Exceptions;
using ASC.Mail.Extensions;
using ASC.Mail.Models;
using ASC.Mail.Storage;
using ASC.Mail.Utils;
using ASC.Web.Files.Services.WCFService;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using MimeKit;

using HttpContext = Microsoft.AspNetCore.Http.HttpContext;
using MailMessage = ASC.Mail.Models.MailMessageData;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class DraftEngine : ComposeEngineBase
    {
        private HttpContext HttpContext { get; set; }
        private List<ServerFolderAccessInfo> ServerFolderAccessInfos { get; set; }

        private CrmLinkEngine CrmLinkEngine { get; }
        private EmailInEngine EmailInEngine { get; }
        private FilterEngine FilterEngine { get; }
        private AutoreplyEngine AutoreplyEngine { get; }
        private AlertEngine AlertEngine { get; }
        private ContactEngine ContactEngine { get; }
        private SecurityContext SecurityContext { get; }
        private FileStorageService<string> FileStorageService { get; }
        private FactoryIndexer<MailContact> FactoryIndexer { get; }
        FactoryIndexer FactoryIndexerCommon { get; set; }
        private IServiceProvider ServiceProvider { get; }

        public DraftEngine(
            SecurityContext securityContext,
            TenantManager tenantManager,
            IMailDaoFactory mailDaoFactory,
            AccountEngine accountEngine,
            MailboxEngine mailboxEngine,
            MessageEngine messageEngine,
            QuotaEngine quotaEngine,
            IndexEngine indexEngine,
            FolderEngine folderEngine,
            CrmLinkEngine crmLinkEngine,
            EmailInEngine emailInEngine,
            FilterEngine filterEngine,
            AutoreplyEngine autoreplyEngine,
            AlertEngine alertEngine,
            ContactEngine contactEngine,
            StorageManager storageManager,
            CoreSettings coreSettings,
            StorageFactory storageFactory,
            FileStorageService<string> fileStorageService,
            FactoryIndexer<MailContact> factoryIndexer,
            FactoryIndexer factoryIndexerCommon,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IOptionsSnapshot<SignalrServiceClient> optionsSnapshot,
            IOptionsMonitor<ILog> option,
            MailSettings mailSettings,
            DeliveryFailureMessageTranslates daemonLabels = null)
            : base(
            accountEngine,
            mailboxEngine,
            messageEngine,
            quotaEngine,
            indexEngine,
            folderEngine,
            mailDaoFactory,
            storageManager,
            securityContext,
            tenantManager,
            coreSettings,
            storageFactory,
            optionsSnapshot,
            option,
            mailSettings,
            daemonLabels)
        {
            CrmLinkEngine = crmLinkEngine;
            EmailInEngine = emailInEngine;
            FilterEngine = filterEngine;
            AutoreplyEngine = autoreplyEngine;
            AlertEngine = alertEngine;
            ContactEngine = contactEngine;
            SecurityContext = securityContext;
            FileStorageService = fileStorageService;
            FactoryIndexer = factoryIndexer;
            FactoryIndexerCommon = factoryIndexerCommon;
            ServiceProvider = serviceProvider;
            HttpContext = httpContextAccessor?.HttpContext;

            ServerFolderAccessInfos = MailDaoFactory.GetImapSpecialMailboxDao().GetServerFolderAccessInfoList();

            Log = option.Get("ASC.Mail.DraftEngine");
        }

        #region .Public

        public long Send(MessageModel model, DeliveryFailureMessageTranslates translates = null)
        {
            if (model.Id < 1)
                model.Id = 0;

            if (string.IsNullOrEmpty(model.From))
                throw new ArgumentNullException("from");

            if (!model.To.Any())
                throw new ArgumentNullException("to");

            var mailAddress = new MailAddress(model.From);

            var accounts = AccountEngine.GetAccountInfoList().ToAccountData();

            var account = accounts.FirstOrDefault(a => a.Email.ToLower().Equals(mailAddress.Address));

            if (account == null)
                throw new ArgumentException("Mailbox not found");

            if (account.IsGroup)
                throw new InvalidOperationException("Sending emails from a group address is forbidden");

            var mbox = MailboxEngine.GetMailboxData(
                new СoncreteUserMailboxExp(account.MailboxId, Tenant, User));

            if (mbox == null)
                throw new ArgumentException("No such mailbox");

            if (!mbox.Enabled)
                throw new InvalidOperationException("Sending emails from a disabled account is forbidden");

            string mimeMessageId, streamId;

            var previousMailboxId = mbox.MailBoxId;

            if (model.Id > 0)
            {
                var message = MessageEngine.GetMessage(model.Id, new MailMessage.Options
                {
                    LoadImages = false,
                    LoadBody = true,
                    NeedProxyHttp = MailSettings.NeedProxyHttp,
                    NeedSanitizer = false
                });

                if (message.Folder != FolderType.Draft && message.Folder != FolderType.Templates)
                {
                    throw new InvalidOperationException("Sending emails is permitted only in the Drafts folder");
                }

                if (message.HtmlBody.Length > MailSettings.MaximumMessageBodySize)
                {
                    throw new InvalidOperationException("Message body exceeded limit (" + MailSettings.MaximumMessageBodySize / 1024 + " KB)");
                }

                mimeMessageId = message.MimeMessageId;

                streamId = message.StreamId;

                foreach (var attachment in model.Attachments)
                {
                    attachment.streamId = streamId;
                }

                previousMailboxId = message.MailboxId;
            }
            else
            {
                mimeMessageId = MailUtil.CreateMessageId(TenantManager, CoreSettings);
                streamId = MailUtil.CreateStreamId();
            }

            var fromAddress = MailUtil.CreateFullEmail(mbox.Name, mailAddress.Address);

            var draft = new MailDraftData(model.Id, mbox, fromAddress, model.To, model.Cc, model.Bcc, model.Subject, mimeMessageId, model.MimeReplyToId,
                model.Importance, model.Tags, model.Body, streamId, model.Attachments, model.CalendarIcs)
            {
                FileLinksShareMode = model.FileLinksShareMode,
                PreviousMailboxId = previousMailboxId,
                RequestReceipt = model.RequestReceipt,
                RequestRead = model.RequestRead,
                IsAutogenerated = !string.IsNullOrEmpty(model.CalendarIcs),
                IsAutoreplied = model.IsAutoreply
            };

            DaemonLabels = translates ?? DeliveryFailureMessageTranslates.Defauilt;

            return Send(draft);
        }

        public long Send(MailDraftData draft)
        {
            if (string.IsNullOrEmpty(draft.HtmlBody))
                draft.HtmlBody = EMPTY_HTML_BODY;

            var message = Save(draft);

            if (message.Id <= 0)
                throw new ArgumentException(string.Format("DraftManager-Send: Invalid message.Id = {0}", message.Id));

            ValidateAddresses(DraftFieldTypes.From, new List<string> { draft.From }, true);

            message.ToList = ValidateAddresses(DraftFieldTypes.To, draft.To, true);
            message.CcList = ValidateAddresses(DraftFieldTypes.Cc, draft.Cc, false);
            message.BccList = ValidateAddresses(DraftFieldTypes.Bcc, draft.Bcc, false);

            var scheme = HttpContext == null
                ? Uri.UriSchemeHttp
                : HttpContext.Request.GetUrlRewriter().Scheme;

            SetDraftSending(draft);

            Task.Run(() =>
            {
                try
                {
                    TenantManager.SetCurrentTenant(draft.Mailbox.TenantId);

                    SecurityContext.AuthenticateMe(new Guid(draft.Mailbox.UserId));

                    draft.ChangeEmbeddedAttachmentLinks(Log);

                    draft.ChangeSmileLinks(Log);

                    draft.ChangeAttachedFileLinksAddresses(FileStorageService, Log);

                    draft.ChangeAttachedFileLinksImages(Log);

                    if (!string.IsNullOrEmpty(draft.CalendarIcs))
                    {
                        draft.ChangeAllImagesLinksToEmbedded(Log);
                    }

                    draft.ChangeUrlProxyLinks(Log);

                    var mimeMessage = draft.ToMimeMessage(StorageManager);

                    using (var mc = new MailClient(draft.Mailbox, CancellationToken.None, ServerFolderAccessInfos,
                        certificatePermit: draft.Mailbox.IsTeamlab || _sslCertificatePermit, log: Log,
                        enableDsn: draft.RequestReceipt))
                    {
                        mc.Send(mimeMessage,
                            draft.Mailbox.Imap && !DisableImapSendSyncServers.Contains(draft.Mailbox.Server));
                    }

                    try
                    {
                        SaveIcsAttachment(draft, mimeMessage);

                        SendMailNotification(draft);

                        ReleaseSendingDraftOnSuccess(draft, message);

                        CrmLinkEngine.AddRelationshipEventForLinkedAccounts(draft.Mailbox, message, scheme);

                        EmailInEngine.SaveEmailInData(draft.Mailbox, message, scheme);

                        SaveFrequentlyContactedAddress(draft.Mailbox.TenantId, draft.Mailbox.UserId, mimeMessage);

                        var filters = FilterEngine.GetList();

                        if (filters.Any())
                        {
                            FilterEngine.ApplyFilters(message, draft.Mailbox, new Models.MailFolder(FolderType.Sent, ""), filters);
                        }

                        IndexEngine.Update(new List<MailMail>
                        {
                            message.ToMailWrapper(draft.Mailbox.TenantId,
                                new Guid(draft.Mailbox.UserId))
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("Unexpected Error in Send() Id = {0}\r\nException: {1}",
                            message.Id, ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Mail->Send failed: Exception: {0}", ex.ToString());

                    AddNotificationAlertToMailbox(draft, ex);

                    ReleaseSendingDraftOnFailure(draft);

                    SendMailErrorNotification(draft);
                }
                finally
                {
                    if (draft.IsAutoreplied)
                    {
                        AutoreplyEngine
                            .SaveAutoreplyHistory(draft.Mailbox, message);
                    }
                }
            });

            return message.Id;
        }

        #endregion

        #region .Private

        private void SetDraftSending(MailDraftData draft)
        {
            MessageEngine.SetConversationsFolder(new List<int> { draft.Id }, FolderType.Sending);
        }

        private void ReleaseSendingDraftOnSuccess(MailDraftData draft, MailMessage message)
        {
            using var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted);

            // message was correctly send - lets update its chains id
            var draftChainId = message.ChainId;
            // before moving message from draft to sent folder - lets recalculate its correct chain id
            var chainInfo = MessageEngine.DetectChain(draft.Mailbox,
                message.MimeMessageId, message.MimeReplyToId, message.Subject);

            message.ChainId = chainInfo.Id;

            if (message.ChainId.Equals(message.MimeMessageId))
                message.MimeReplyToId = null;

            if (!draftChainId.Equals(message.ChainId))
            {
                MailDaoFactory.GetMailInfoDao().SetFieldValue(
                    SimpleMessagesExp.CreateBuilder(Tenant, User)
                        .SetMessageId(message.Id)
                        .Build(),
                    "ChainId",
                    message.ChainId);

                MessageEngine.UpdateChain(draftChainId, FolderType.Sending, null, draft.Mailbox.MailBoxId,
                    draft.Mailbox.TenantId, draft.Mailbox.UserId);

                MailDaoFactory.GetCrmLinkDao().UpdateCrmLinkedChainId(draftChainId, draft.Mailbox.MailBoxId, message.ChainId);
            }

            MessageEngine.UpdateChain(message.ChainId, FolderType.Sending, null, draft.Mailbox.MailBoxId,
                draft.Mailbox.TenantId, draft.Mailbox.UserId);

            var listObjects = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(new List<int> { draft.Id });

            if (!listObjects.Any())
                return;

            MessageEngine.SetFolder(MailDaoFactory, listObjects, FolderType.Sent);

            MailDaoFactory.GetMailInfoDao().SetFieldValue(
                SimpleMessagesExp.CreateBuilder(Tenant, User)
                    .SetMessageId(draft.Id)
                    .Build(),
                "FolderRestore",
                FolderType.Sent);

            tx.Commit();
        }

        private void ReleaseSendingDraftOnFailure(MailDraftData draft)
        {
            using var tx = MailDaoFactory.BeginTransaction(IsolationLevel.ReadUncommitted);

            var listObjects = MailDaoFactory.GetMailInfoDao().GetChainedMessagesInfo(new List<int> { draft.Id });

            if (!listObjects.Any())
                return;

            MessageEngine.SetFolder(MailDaoFactory, listObjects, FolderType.Draft);

            tx.Commit();
        }

        private void SaveIcsAttachment(MailDraftData draft, MimeMessage mimeMessage)
        {
            if (string.IsNullOrEmpty(draft.CalendarIcs)) return;

            try
            {
                var icsAttachment =
                    mimeMessage.Attachments.FirstOrDefault(
                        a => a.ContentType.IsMimeType("application", "ics"));

                if (icsAttachment == null)
                    return;

                using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(draft.CalendarIcs)))
                {
                    MessageEngine
                        .AttachFileToDraft(draft.Mailbox.TenantId, draft.Mailbox.UserId, draft.Id,
                            icsAttachment.ContentType.Name, memStream, memStream.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Warn(string.Format("Problem with attach ICAL to message. mailId={0} Exception:\r\n{1}\r\n", draft.Id, ex));
            }
        }

        private static List<MailAddress> ValidateAddresses(DraftFieldTypes fieldType, List<string> addresses,
            bool strongValidation)
        {
            if (addresses == null || !addresses.Any())
            {
                if (strongValidation)
                {
                    throw new DraftException(DraftException.ErrorTypes.EmptyField, "Empty email address in {0} field",
                        fieldType);
                }

                return null;
            }

            try
            {
                return addresses.ToMailAddresses();
            }
            catch (Exception ex)
            {
                throw new DraftException(DraftException.ErrorTypes.IncorrectField, ex.Message, fieldType);
            }
        }

        private void SendMailErrorNotification(MailDraftData draft)
        {
            try
            {
                // send success notification
                _signalrServiceClient.SendMailNotification(draft.Mailbox.TenantId, draft.Mailbox.UserId, -1);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error with wcf signalrServiceClient: {0}, {1}", ex.Message, ex.StackTrace);
            }
        }

        private void SendMailNotification(MailDraftData draft)
        {
            try
            {
                var state = 0;
                if (!string.IsNullOrEmpty(draft.CalendarIcs))
                {
                    switch (draft.CalendarMethod)
                    {
                        case DefineConstants.ICAL_REQUEST:
                            state = 1;
                            break;
                        case DefineConstants.ICAL_REPLY:
                            state = 2;
                            break;
                        case DefineConstants.ICAL_CANCEL:
                            state = 3;
                            break;
                    }
                }

                // send success notification
                _signalrServiceClient.SendMailNotification(draft.Mailbox.TenantId, draft.Mailbox.UserId, state);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error with wcf signalrServiceClient: {0}, {1}", ex.Message, ex.StackTrace);
            }
        }

        private void SaveFrequentlyContactedAddress(int tenant, string user, MimeMessage mimeMessage)
        {
            var recipients = new List<MailboxAddress>();
            recipients.AddRange(mimeMessage.To.Mailboxes);
            recipients.AddRange(mimeMessage.Cc.Mailboxes);
            recipients.AddRange(mimeMessage.Bcc.Mailboxes);

            var treatedAddresses = new List<string>();
            foreach (var recipient in recipients)
            {
                var email = recipient.Address;
                if (treatedAddresses.Contains(email))
                    continue;

                var exp = new FullFilterContactsExp(tenant, user, MailDaoFactory.GetContext(), FactoryIndexer, FactoryIndexerCommon, ServiceProvider,
                    searchTerm: email, infoType: ContactInfoType.Email);

                var contacts = ContactEngine.GetContactCards(exp);

                if (!contacts.Any())
                {
                    var emails = ContactEngine.SearchEmails(tenant, user, email, 1);
                    if (!emails.Any())
                    {
                        var contactCard = new ContactCard(0, tenant, user, recipient.Name, "",
                            ContactType.FrequentlyContacted, new[] { email });

                        ContactEngine.SaveContactCard(contactCard);
                    }
                }

                treatedAddresses.Add(email);
            }
        }

        private static List<string> DisableImapSendSyncServers
        {
            get
            {
                var config = ConfigurationManager.AppSettings["mail.disable-imap-send-sync-servers"] ?? "imap.googlemail.com|imap.gmail.com|imap-mail.outlook.com";
                return string.IsNullOrEmpty(config) ? new List<string>() : config.Split('|').ToList();
            }
        }

        private void AddNotificationAlertToMailbox(MailDraftData draft, Exception exOnSanding)
        {
            try
            {
                var sbMessage = new StringBuilder(1024);

                sbMessage
                    .AppendFormat("<div style=\"max-width:500px;font: normal 12px Arial, Tahoma,sans-serif;\"><p style=\"color:gray;font: normal 12px Arial, Tahoma,sans-serif;\">{0}</p>",
                        DaemonLabels.AutomaticMessageLabel)
                    .AppendFormat("<p style=\"font: normal 12px Arial, Tahoma,sans-serif;\">{0}</p>", DaemonLabels.MessageIdentificator
                        .Replace("{subject}", draft.Subject)
                        .Replace("{date}", DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                    .AppendFormat("<div><p style=\"font: normal 12px Arial, Tahoma,sans-serif;\">{0}:</p><ul style=\"color:#333;font: normal 12px Arial, Tahoma,sans-serif;\">",
                        DaemonLabels.RecipientsLabel);

                draft.To.ForEach(rcpt => sbMessage.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(rcpt)));
                draft.Cc.ForEach(rcpt => sbMessage.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(rcpt)));
                draft.Bcc.ForEach(rcpt => sbMessage.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(rcpt)));

                sbMessage
                    .AppendFormat("</ul>")
                    .AppendFormat("<p style=\"font: normal 12px Arial, Tahoma,sans-serif;\">{0}</p>",
                        DaemonLabels.RecommendationsLabel
                            .Replace("{account_name}", "<b>" + draft.From + "</b>"))
                    .AppendFormat(
                        "<a id=\"delivery_failure_button\" mailid={0} class=\"button blue\" style=\"margin-right:8px;\">{1}</a></div>",
                        draft.Id, DaemonLabels.TryAgainButtonLabel)
                    .AppendFormat("<p style=\"font: normal 12px Arial, Tahoma,sans-serif;\">{0}</p>",
                        DaemonLabels.FaqInformationLabel
                            .Replace("{url_begin}",
                                "<a id=\"delivery_failure_faq_link\" target=\"blank\" href=\"#\" class=\"link underline\">")
                            .Replace("{url_end}", "</a>"));

                const int max_length = 300;

                var smtpResponse = string.IsNullOrEmpty(exOnSanding.Message)
                    ? "no response"
                    : exOnSanding.Message.Length > max_length
                        ? exOnSanding.Message.Substring(0, max_length)
                        : exOnSanding.Message;

                sbMessage.AppendFormat("<p style=\"color:gray;font: normal 12px Arial, Tahoma,sans-serif;\">{0}: \"{1}\"</p></div>", DaemonLabels.ReasonLabel,
                    smtpResponse);

                draft.Mailbox.Name = "";

                var messageDelivery = new MailDraftData(0, draft.Mailbox, DaemonLabels.DaemonEmail,
                    new List<string>() { draft.From }, new List<string>(), new List<string>(),
                    DaemonLabels.SubjectLabel,
                    MailUtil.CreateStreamId(), "", true, new List<int>(), sbMessage.ToString(), MailUtil.CreateStreamId(),
                    new List<MailAttachmentData>());

                // SaveToDraft To Inbox
                var notifyMessageItem = messageDelivery.ToMailMessage();
                notifyMessageItem.ChainId = notifyMessageItem.MimeMessageId;
                notifyMessageItem.IsNew = true;

                MessageEngine.StoreMailBody(draft.Mailbox, notifyMessageItem, Log);

                var mailDaemonMessageid = MessageEngine.MailSave(draft.Mailbox, notifyMessageItem, 0,
                    FolderType.Inbox, FolderType.Inbox, null,
                    string.Empty, string.Empty, false);

                AlertEngine.CreateDeliveryFailureAlert(
                    draft.Mailbox.TenantId,
                    draft.Mailbox.UserId,
                    draft.Mailbox.MailBoxId,
                    draft.Subject,
                    draft.From,
                    draft.Id,
                    mailDaemonMessageid);
            }
            catch (Exception exError)
            {
                Log.ErrorFormat("AddNotificationAlertToMailbox() in MailboxId={0} failed with exception:\r\n{1}",
                    draft.Mailbox.MailBoxId, exError.ToString());
            }
        }

        #endregion
    }
}
