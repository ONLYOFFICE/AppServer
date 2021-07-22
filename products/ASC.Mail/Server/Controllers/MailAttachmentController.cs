using ASC.Mail.Core.Engine;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Models;
using ASC.Mail.Utils;
using ASC.Web.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace ASC.Mail.Controllers
{
    public partial class MailController : ControllerBase
    {
        /// <summary>
        /// Export all message's attachments to MyDocuments
        /// </summary>
        /// <param name="id_message">Id of any message</param>
        /// <param name="id_folder" optional="true">Id of Documents folder (if empty then @My)</param>
        /// <returns>Count of exported attachments</returns>
        /// <category>Messages</category>
        [Update(@"messages/attachments/export")]
        public int ExportAttachmentsToDocuments(int id_message, string id_folder = null)
        {
            if (id_message < 1)
                throw new ArgumentException(@"Invalid message id", "id_message");

            if (string.IsNullOrEmpty(id_folder))
                id_folder = DocumentsEngine.MY_DOCS_FOLDER_ID;

            var savedAttachmentsList = DocumentsEngine.StoreAttachmentsToDocuments(id_message, id_folder);

            return savedAttachmentsList.Count;
        }

        /// <summary>
        /// Export attachment to MyDocuments
        /// </summary>
        /// <param name="id_attachment">Id of any attachment from the message</param>
        /// <param name="id_folder" optional="true">Id of Documents folder (if empty then @My)</param>
        /// <returns>Id document in My Documents</returns>
        /// <category>Messages</category>
        [Update(@"messages/attachment/export")]
        public object ExportAttachmentToDocuments(int id_attachment, string id_folder = null)
        {
            if (id_attachment < 1)
                throw new ArgumentException(@"Invalid attachment id", "id_attachment");

            if (string.IsNullOrEmpty(id_folder))
                id_folder = DocumentsEngine.MY_DOCS_FOLDER_ID;

            var documentId = DocumentsEngine.StoreAttachmentToDocuments(id_attachment, id_folder);

            return documentId;
        }

        /// <summary>
        /// Add attachment to draft
        /// </summary>
        /// <param name="id_message">Id of any message</param>
        /// <param name="name">File name</param>
        /// <param name="file">File stream</param>
        /// <param name="content_type">File content type</param>
        /// <returns>MailAttachment</returns>
        /// <category>Messages</category>
        [Create(@"messages/attachment/add")]
        public MailAttachmentData AddAttachment(int id_message, string name, Stream file, string content_type)
        {
            var attachment = MessageEngine
                .AttachFileToDraft(TenantId, UserId, id_message, name, file, file.Length, content_type);

            return attachment;
        }

        /// <summary>
        /// Add attachment to draft
        /// </summary>
        /// <param name="id_message">Id of any message</param>
        /// <param name="ical_body">File name</param>
        /// <returns>MailAttachment</returns>
        /// <category>Messages</category>
        [Create(@"messages/calendarbody/add")]
        public MailAttachmentData AddCalendarBody(int id_message, string ical_body)
        {
            if (string.IsNullOrEmpty(ical_body))
                throw new ArgumentException(@"Empty calendar body", "ical_body");

            var calendar = MailUtil.ParseValidCalendar(ical_body, Log);

            if (calendar == null)
                throw new ArgumentException(@"Invalid calendar body", "ical_body");

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);

            writer.Write(ical_body);
            writer.Flush();
            ms.Position = 0;

            var attachment = MessageEngine
                .AttachFileToDraft(TenantId, UserId, id_message, calendar.Method.ToLowerInvariant() + ".ics",
                    ms, ms.Length, "text/calendar");

            return attachment;
        }

        /// <summary>
        /// Download all attachments from message
        /// </summary>
        /// <short>
        /// Download all attachments from message
        /// </short>
        /// <param name="messageId">Id of message</param>
        /// <returns>Attachment Archive</returns>
        [Update(@"messages/attachment/downloadall/{messageId}")]
        public MailOperationStatus DownloadAllAttachments(int messageId)
        {
            //Thread.CurrentThread.CurrentCulture = CurrentCulture;
            //Thread.CurrentThread.CurrentUICulture = CurrentCulture;

            return OperationEngine.DownloadAllAttachments(messageId, TranslateMailOperationStatus);
        }
    }
}
