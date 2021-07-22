using ASC.Files.Core.Security;
using System;
using System.Collections.Generic;
using System.IO;

namespace ASC.Mail.Models
{
    public class TestMessageModel
    {
        public int? FolderId { get; set; }
        public int? MailboxId { get; set; }
        public List<string> To { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }
        public bool Importance { get; set; }
        public bool Unread { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string CalendarUid { get; set; } // = null,
        public DateTime? Date { get; set; } // = null,
        public List<int> TagIds { get; set; } // = null,
        public string FromAddress { get; set; } // = null,
        public string MimeMessageId { get; set; } // = null,
        public int? UserFolderId { get; set; } // = null
        public Stream EmlStream { get; set; } // = null
    }
}
