using ASC.Files.Core.Security;
using System;
using System.Collections.Generic;
using System.IO;

namespace ASC.Mail.Models
{
    public class TestAttachmentModel
    {
        public string Filename { get; set; }
        public Stream Stream { get; set; }
        public string ContentType { get; set; }
    }
}
