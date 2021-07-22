using ASC.Mail.Models;
using System.Net.Mail;

namespace ASC.Mail.Aggregator.CollectionService.Queue.Data
{
    public static class Extensions
    {
        public static MailBoxData ToMailbox(this MailboxData mailboxData)
        {
            return new MailBoxData
            {
                TenantId = mailboxData.TenantId,
                UserId = mailboxData.UserId,
                MailBoxId = mailboxData.MailboxId,
                EMail = new MailAddress(mailboxData.EMail),
                Imap = mailboxData.Imap,
                IsTeamlab = mailboxData.IsTeamlab,
                Size = mailboxData.Size,
                MessagesCount = mailboxData.MessagesCount
            };
        }

        public static MailboxData ToMailboxData(this MailBoxData mailbox)
        {
            return new MailboxData
            {
                MailboxId = mailbox.MailBoxId,
                TenantId = mailbox.TenantId,
                UserId = mailbox.UserId,
                EMail = mailbox.EMail.Address,
                Imap = mailbox.Imap,
                IsTeamlab = mailbox.IsTeamlab,
                MessagesCount = mailbox.MessagesCount,
                Size = mailbox.Size
            };
        }
    }
}
