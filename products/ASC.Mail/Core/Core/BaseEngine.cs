using ASC.Common;
using ASC.Mail.Configuration;

namespace ASC.Mail.Core
{
    [Scope]
    public class BaseEngine
    {
        internal MailSettings MailSettings;
        public BaseEngine(MailSettings mailSettings)
        {
            MailSettings = mailSettings;
        }
    }
}
