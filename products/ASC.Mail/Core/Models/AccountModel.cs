using ASC.Mail.Enums;

namespace ASC.Mail.Models
{
    public class AccountModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string Server { get; set; }
        public string SmtpLogin { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public bool SmtpAuth { get; set; }
        public bool Imap { get; set; }
        public bool Restrict { get; set; }
        public EncryptionType IncomingEncryptionType { get; set; }
        public EncryptionType OutcomingEncryptionType { get; set; }
        public SaslMechanism IncomingAuthenticationType { get; set; }
        public SaslMechanism OutcomingAuthenticationType { get; set; }
    }
}
