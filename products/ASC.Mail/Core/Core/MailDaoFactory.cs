
using System;

using ASC.Common;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao;
using ASC.Mail.Core.Dao.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ASC.Mail.Core
{
    [Scope]
    public class MailDaoFactory : IMailDaoFactory
    {
        private IServiceProvider ServiceProvider { get; }
        private MailDbContext MailDbContext { get; }

        public MailDaoFactory(
            IServiceProvider serviceProvider,
            DbContextManager<MailDbContext> dbContextManager)
        {
            ServiceProvider = serviceProvider;
            MailDbContext = dbContextManager.Get("mail");
        }

        public MailDbContext GetContext()
        {
            return MailDbContext;
        }

        public IAccountDao GetAccountDao()
        {
            return ServiceProvider.GetService<IAccountDao>();
        }

        public IAlertDao GetAlertDao()
        {
            return ServiceProvider.GetService<IAlertDao>();
        }

        public IAttachmentDao GetAttachmentDao()
        {
            return ServiceProvider.GetService<IAttachmentDao>();
        }

        public IChainDao GetChainDao()
        {
            return ServiceProvider.GetService<IChainDao>();
        }

        public IContactCardDao GetContactCardDao()
        {
            return ServiceProvider.GetService<IContactCardDao>();
        }

        public IContactDao GetContactDao()
        {
            return ServiceProvider.GetService<IContactDao>();
        }

        public IContactInfoDao GetContactInfoDao()
        {
            return ServiceProvider.GetService<IContactInfoDao>();
        }

        public ICrmContactDao GetCrmContactDao()
        {
            return ServiceProvider.GetService<ICrmContactDao>();
        }

        public ICrmLinkDao GetCrmLinkDao()
        {
            return ServiceProvider.GetService<ICrmLinkDao>();
        }

        public IDisplayImagesAddressDao GetDisplayImagesAddressDao()
        {
            return ServiceProvider.GetService<IDisplayImagesAddressDao>();
        }

        public IFilterDao GetFilterDao()
        {
            return ServiceProvider.GetService<IFilterDao>();
        }

        public IFolderDao GetFolderDao()
        {
            return ServiceProvider.GetService<IFolderDao>();
        }

        public IImapFlagsDao GetImapFlagsDao()
        {
            return ServiceProvider.GetService<IImapFlagsDao>();
        }

        public IImapSpecialMailboxDao GetImapSpecialMailboxDao()
        {
            return ServiceProvider.GetService<IImapSpecialMailboxDao>();
        }

        public IMailboxAutoreplyDao GetMailboxAutoreplyDao()
        {
            return ServiceProvider.GetService<IMailboxAutoreplyDao>();
        }

        public IMailboxAutoreplyHistoryDao GetMailboxAutoreplyHistoryDao()
        {
            return ServiceProvider.GetService<IMailboxAutoreplyHistoryDao>();
        }

        public IMailboxDao GetMailboxDao()
        {
            return ServiceProvider.GetService<IMailboxDao>();
        }

        public IMailboxDomainDao GetMailboxDomainDao()
        {
            return ServiceProvider.GetService<IMailboxDomainDao>();
        }

        public IMailboxProviderDao GetMailboxProviderDao()
        {
            return ServiceProvider.GetService<IMailboxProviderDao>();
        }

        public IMailboxServerDao GetMailboxServerDao()
        {
            return ServiceProvider.GetService<IMailboxServerDao>();
        }

        public IMailboxSignatureDao GetMailboxSignatureDao()
        {
            return ServiceProvider.GetService<IMailboxSignatureDao>();
        }

        public IMailDao GetMailDao()
        {
            return ServiceProvider.GetService<IMailDao>();
        }

        public IMailGarbageDao GetMailGarbageDao()
        {
            return ServiceProvider.GetService<IMailGarbageDao>();
        }

        public IMailInfoDao GetMailInfoDao()
        {
            return ServiceProvider.GetService<IMailInfoDao>();
        }

        public IServerAddressDao GetServerAddressDao()
        {
            return ServiceProvider.GetService<IServerAddressDao>();
        }

        public IServerDao GetServerDao()
        {
            return ServiceProvider.GetService<IServerDao>();
        }

        public IServerDnsDao GetServerDnsDao()
        {
            return ServiceProvider.GetService<IServerDnsDao>();
        }

        public IServerDomainDao GetServerDomainDao()
        {
            return ServiceProvider.GetService<IServerDomainDao>();
        }

        public IServerGroupDao GetServerGroupDao()
        {
            return ServiceProvider.GetService<IServerGroupDao>();
        }

        public ITagAddressDao GetTagAddressDao()
        {
            return ServiceProvider.GetService<ITagAddressDao>();
        }

        public ITagDao GetTagDao()
        {
            return ServiceProvider.GetService<ITagDao>();
        }

        public ITagMailDao GetTagMailDao()
        {
            return ServiceProvider.GetService<ITagMailDao>();
        }

        public IUserFolderDao GetUserFolderDao()
        {
            return ServiceProvider.GetService<IUserFolderDao>();
        }

        public IUserFolderTreeDao GetUserFolderTreeDao()
        {
            return ServiceProvider.GetService<IUserFolderTreeDao>();
        }

        public IUserFolderXMailDao GetUserFolderXMailDao()
        {
            return ServiceProvider.GetService<IUserFolderXMailDao>();
        }

        public IDbContextTransaction BeginTransaction(System.Data.IsolationLevel? level = null)
        {
            return level.HasValue ? MailDbContext.Database.BeginTransaction(level.Value) : MailDbContext.Database.BeginTransaction();
        }
    }
    public class MailDaoFactoryExtension
    {
        public static void Register(DIHelper services)
        {
            services.TryAdd<IAccountDao, AccountDao>();
            services.TryAdd<IAlertDao, AlertDao>();
            services.TryAdd<IAttachmentDao, AttachmentDao>();
            services.TryAdd<IChainDao, ChainDao>();
            services.TryAdd<IContactCardDao, ContactCardDao>();
            services.TryAdd<IContactDao, ContactDao>();
            services.TryAdd<IContactInfoDao, ContactInfoDao>();
            services.TryAdd<ICrmContactDao, CrmContactDao>();
            services.TryAdd<ICrmLinkDao, CrmLinkDao>();
            services.TryAdd<IDisplayImagesAddressDao, DisplayImagesAddressDao>();
            services.TryAdd<IFilterDao, FilterDao>();
            services.TryAdd<IFolderDao, FolderDao>();
            services.TryAdd<IImapFlagsDao, ImapFlagsDao>();
            services.TryAdd<IImapSpecialMailboxDao, ImapSpecialMailboxDao>();
            services.TryAdd<IMailboxAutoreplyDao, MailboxAutoreplyDao>();
            services.TryAdd<IMailboxAutoreplyHistoryDao, MailboxAutoreplyHistoryDao>();
            services.TryAdd<IMailboxDao, MailboxDao>();
            services.TryAdd<IMailboxDomainDao, MailboxDomainDao>();
            services.TryAdd<IMailboxProviderDao, MailboxProviderDao>();
            services.TryAdd<IMailboxServerDao, MailboxServerDao>();
            services.TryAdd<IMailboxSignatureDao, MailboxSignatureDao>();
            services.TryAdd<IMailDao, MailDao>();
            services.TryAdd<IMailGarbageDao, MailGarbageDao>();
            services.TryAdd<IMailInfoDao, MailInfoDao>();
            services.TryAdd<IServerAddressDao, ServerAddressDao>();
            services.TryAdd<IServerDao, ServerDao>();
            services.TryAdd<IServerDnsDao, ServerDnsDao>();
            services.TryAdd<IServerDomainDao, ServerDomainDao>();
            services.TryAdd<IServerGroupDao, ServerGroupDao>();
            services.TryAdd<ITagAddressDao, TagAddressDao>();
            services.TryAdd<ITagDao, TagDao>();
            services.TryAdd<ITagMailDao, TagMailDao>();
            services.TryAdd<IUserFolderDao, UserFolderDao>();
            services.TryAdd<IUserFolderTreeDao, UserFolderTreeDao>();
            services.TryAdd<IUserFolderXMailDao, UserFolderXMailDao>();
        }
    }
}
