
using ASC.Common;
using ASC.Mail.Core.Dao;
using ASC.Mail.Core.Dao.Interfaces;

using Microsoft.EntityFrameworkCore.Storage;

namespace ASC.Mail.Core
{
    [Scope(typeof(MailDaoFactory), Additional = typeof(MailDaoFactoryExtension))]
    public interface IMailDaoFactory
    {
        MailDbContext GetContext();

        IAccountDao GetAccountDao();

        IAlertDao GetAlertDao();

        IAttachmentDao GetAttachmentDao();

        IChainDao GetChainDao();

        IContactCardDao GetContactCardDao();

        IContactDao GetContactDao();

        IContactInfoDao GetContactInfoDao();

        ICrmContactDao GetCrmContactDao();

        ICrmLinkDao GetCrmLinkDao();

        IDisplayImagesAddressDao GetDisplayImagesAddressDao();

        IFilterDao GetFilterDao();

        IFolderDao GetFolderDao();

        IImapFlagsDao GetImapFlagsDao();

        IImapSpecialMailboxDao GetImapSpecialMailboxDao();

        IMailboxAutoreplyDao GetMailboxAutoreplyDao();

        IMailboxAutoreplyHistoryDao GetMailboxAutoreplyHistoryDao();

        IMailboxDao GetMailboxDao();

        IMailboxDomainDao GetMailboxDomainDao();

        IMailboxProviderDao GetMailboxProviderDao();

        IMailboxServerDao GetMailboxServerDao();

        IMailboxSignatureDao GetMailboxSignatureDao();

        IMailDao GetMailDao();

        IMailGarbageDao GetMailGarbageDao();

        IMailInfoDao GetMailInfoDao();

        IServerAddressDao GetServerAddressDao();

        IServerDao GetServerDao();

        IServerDnsDao GetServerDnsDao();

        IServerDomainDao GetServerDomainDao();

        IServerGroupDao GetServerGroupDao();

        ITagAddressDao GetTagAddressDao();

        ITagDao GetTagDao();

        ITagMailDao GetTagMailDao();

        IUserFolderDao GetUserFolderDao();

        IUserFolderTreeDao GetUserFolderTreeDao();

        IUserFolderXMailDao GetUserFolderXMailDao();

        public IDbContextTransaction BeginTransaction(System.Data.IsolationLevel? level = null);
    }
}
