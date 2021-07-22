using ASC.Files.Core.Security;
using System.Collections.Generic;

namespace ASC.Mail.Models
{
    public class MessageModel
    {
        public int Id { get; set; }
        public string From { get; set; }
        public List<string> To { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }
        public string MimeReplyToId { get; set; }
        public bool Importance { get; set; }
        public string Subject { get; set; }
        public List<int> Tags { get; set; }
        public string Body { get; set; }
        public List<MailAttachmentData> Attachments { get; set; }
        public FileShare FileLinksShareMode { get; set; }
        public string CalendarIcs { get; set; }
        public bool IsAutoreply { get; set; }
        public bool RequestReceipt { get; set; }
        public bool RequestRead { get; set; }
    }
}
