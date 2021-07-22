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
using System.IO;
using System.Linq;
using ASC.Core;
using ASC.Core.Users;
using ASC.Mail.Aggregator.Tests.Common.Utils;
using ASC.Mail.Models;
using ASC.Mail.Enums;
using ASC.Mail.Utils;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ASC.Common;
using ASC.Common.Logging;
using ASC.Api.Core.Auth;
using ASC.Api.Core.Middleware;
using ASC.Mail.Core.Engine;
using Autofac;
using ASC.ElasticSearch;
using ASC.Api.Core;
using ASC.Web.Files.Api;
using ASC.Files.Core.Security;
using ASC.Web.Files.Utils;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Entities;

namespace ASC.Mail.Aggregator.Tests.Common.Filters
{
    [TestFixture]
    internal class FullTextSearchUpdateIndexTests
    {
        private const int CURRENT_TENANT = 0;
        public const string PASSWORD = "123456";
        public const string DOMAIN = "gmail.com";
        public const string EMAIL_NAME = "Test User";

        public UserInfo TestUser { get; set; }
        IServiceProvider ServiceProvider { get; set; }
        IHost TestHost { get; set; }

        [OneTimeSetUp]
        public void Prepare()
        {
            var args = new string[] { };

            TestHost = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    var buided = config.Build();
                    var path = buided["pathToConf"];
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.GetFullPath(Path.Combine(hostContext.HostingEnvironment.ContentRootPath, path));
                    }

                    config.SetBasePath(path);

                    config
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                        {"pathToConf", path}
                        })
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", true)
                        .AddJsonFile("storage.json")
                        .AddJsonFile("kafka.json")
                        .AddJsonFile($"kafka.{hostContext.HostingEnvironment.EnvironmentName}.json", true)
                        .AddEnvironmentVariables();

                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpContextAccessor();

                    var diHelper = new DIHelper(services);

                    diHelper
                        .AddCookieAuthHandler()
                        .AddCultureMiddleware()
                        .AddIpSecurityFilter()
                        .AddPaymentFilter()
                        .AddProductSecurityFilter()
                        .AddTenantStatusFilter();

                    diHelper.AddNLogManager("ASC.Api", "ASC.Web");

                    diHelper
                        .AddTenantManagerService()
                        .AddUserManagerService()
                        .AddSecurityContextService()
                        .AddMailBoxSettingEngineService()
                        .AddMailboxEngineService()
                        .AddApiHelperService()
                        .AddFolderEngineService()
                        .AddUserFolderEngineService()
                        .AddFactoryIndexerService()
                        .AddFactoryIndexerService<MailMail>()
                        .AddMailGarbageEngineService()
                        .AddTestEngineService()
                        .AddMessageEngineService()
                        .AddTagEngineService()
                        .AddCoreSettingsService()
                        .AddApiDateTimeHelper()
                        .AddFilesIntegrationService()
                        .AddFileSecurityService()
                        .AddFileConverterService();

                    var builder = new ContainerBuilder();
                    var container = builder.Build();

                    services.TryAddSingleton(container);

                    //services.AddAutofac(hostContext.Configuration, hostContext.HostingEnvironment.ContentRootPath);
                })
                .UseConsoleLifetime()
                .Build();

            TestHost.Start();

            ServiceProvider = TestHost.Services;
        }

        [SetUp]
        public void SetUp()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();
            var mailBoxSettingEngine = scope.ServiceProvider.GetService<MailBoxSettingEngine>();
            var mailboxEngine = scope.ServiceProvider.GetService<MailboxEngine>();
            var apiHelper = scope.ServiceProvider.GetService<ApiHelper>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var testEngine = scope.ServiceProvider.GetService<TestEngine>();

            TestUser = TestHelper.CreateNewRandomEmployee(userManager, securityContext, tenantManager, apiHelper);
        }

        [TearDown]
        public void CleanUp()
        {
            if (TestUser == null || TestUser.ID == Guid.Empty)
                return;

            using var scope = ServiceProvider.CreateScope();

            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            // Remove TestUser profile
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            userManager.DeleteUser(TestUser.ID);
        }

        [Test]
        public void UpdateIndexFolder()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            if (!TestHelper.IgnoreIfFullTextSearch<MailMail>(false, scope.ServiceProvider))
                return;

            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var MailMail = CreateMailMail();

            factoryIndexer.Index(MailMail);

            var success = factoryIndexer.TrySelectIds(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out List<int> mailIds);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, mailIds.Count);
            Assert.AreEqual(MailMail.Id, mailIds[0]);

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    Folder = (byte)FolderType.Sent
                },
                true,
                w => w.Folder);


            success = factoryIndexer.TrySelect(i => i
                .Where(s => s.IdUser, TestUser.ID.ToString()), out IReadOnlyCollection<MailMail> wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            var wrapper = wrappers.First();

            // No Changes
            Assert.AreEqual(MailMail.Id, wrapper.Id);
            Assert.AreEqual(MailMail.TenantId, wrapper.TenantId);
            Assert.AreEqual(MailMail.IdUser, wrapper.IdUser);
            Assert.AreEqual(MailMail.FromText, wrapper.FromText);
            Assert.AreEqual(MailMail.ToText, wrapper.ToText);
            Assert.AreEqual(MailMail.Cc, wrapper.Cc);
            Assert.AreEqual(MailMail.Bcc, wrapper.Bcc);
            Assert.AreEqual(MailMail.Subject, wrapper.Subject);
            Assert.AreEqual(MailMail.DateSent, wrapper.DateSent);
            Assert.AreEqual(MailMail.Unread, wrapper.Unread);
            Assert.AreEqual(MailMail.HasAttachments, wrapper.HasAttachments);
            Assert.AreEqual(MailMail.Importance, wrapper.Importance);
            Assert.AreEqual(MailMail.IsRemoved, wrapper.IsRemoved);
            Assert.AreEqual(MailMail.ChainId, wrapper.ChainId);
            Assert.AreEqual(MailMail.ChainDate, wrapper.ChainDate);
            Assert.IsEmpty(wrapper.UserFolders);

            // Has Changes
            Assert.AreEqual(FolderType.Sent, (FolderType)wrapper.Folder);
        }

        [Test]
        public void UpdateIndexMoveFromUserFolder()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            if (!TestHelper.IgnoreIfFullTextSearch<MailMail>(false, scope.ServiceProvider))
                return;

            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var MailMail = CreateMailMail(true);

            factoryIndexer.Index(MailMail);

            var success = factoryIndexer.TrySelectIds(i => i
                .Where(s => s.Folder, (byte)FolderType.UserFolder)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out List<int> mailIds);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, mailIds.Count);
            Assert.AreEqual(MailMail.Id, mailIds[0]);

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    Folder = (byte)FolderType.Inbox
                },
                true,
                w => w.Folder);

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    UserFolders = new List<MailUserFolder>()
                },
                UpdateAction.Replace,
                w => w.UserFolders.ToList());


            success = factoryIndexer.TrySelect(i => i
                .Where(s => s.IdUser, TestUser.ID.ToString()), out IReadOnlyCollection<MailMail> wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            var wrapper = wrappers.First();

            // No Changes
            Assert.AreEqual(MailMail.Id, wrapper.Id);
            Assert.AreEqual(MailMail.TenantId, wrapper.TenantId);
            Assert.AreEqual(MailMail.IdUser, wrapper.IdUser);
            Assert.AreEqual(MailMail.FromText, wrapper.FromText);
            Assert.AreEqual(MailMail.ToText, wrapper.ToText);
            Assert.AreEqual(MailMail.Cc, wrapper.Cc);
            Assert.AreEqual(MailMail.Bcc, wrapper.Bcc);
            Assert.AreEqual(MailMail.Subject, wrapper.Subject);
            Assert.AreEqual(MailMail.DateSent, wrapper.DateSent);
            Assert.AreEqual(MailMail.Unread, wrapper.Unread);
            Assert.AreEqual(MailMail.HasAttachments, wrapper.HasAttachments);
            Assert.AreEqual(MailMail.Importance, wrapper.Importance);
            Assert.AreEqual(MailMail.IsRemoved, wrapper.IsRemoved);
            Assert.AreEqual(MailMail.ChainId, wrapper.ChainId);
            Assert.AreEqual(MailMail.ChainDate, wrapper.ChainDate);

            // Has Changes
            Assert.AreEqual(FolderType.Inbox, (FolderType)wrapper.Folder);
            Assert.IsEmpty(wrapper.UserFolders);
        }

        [Test]
        public void UpdateIndexMoveToUserFolder()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            if (!TestHelper.IgnoreIfFullTextSearch<MailMail>(false, scope.ServiceProvider))
                return;

            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var MailMail = CreateMailMail();

            factoryIndexer.Index(MailMail);

            var success = factoryIndexer.TrySelectIds(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out List<int> mailIds);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, mailIds.Count);
            Assert.AreEqual(MailMail.Id, mailIds[0]);

            var newUserFolder = new MailUserFolder
            {
                Id = 1
            };

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    Folder = (byte)FolderType.UserFolder
                },
                true,
                w => w.Folder);

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    UserFolders = new List<MailUserFolder> { newUserFolder }
                },
                UpdateAction.Replace,
                w => w.UserFolders.ToList());


            success = factoryIndexer.TrySelect(i => i
                .Where(s => s.IdUser, TestUser.ID.ToString()), out IReadOnlyCollection<MailMail> wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            var wrapper = wrappers.First();

            // No Changes
            Assert.AreEqual(MailMail.Id, wrapper.Id);
            Assert.AreEqual(MailMail.TenantId, wrapper.TenantId);
            Assert.AreEqual(MailMail.IdUser, wrapper.IdUser);
            Assert.AreEqual(MailMail.FromText, wrapper.FromText);
            Assert.AreEqual(MailMail.ToText, wrapper.ToText);
            Assert.AreEqual(MailMail.Cc, wrapper.Cc);
            Assert.AreEqual(MailMail.Bcc, wrapper.Bcc);
            Assert.AreEqual(MailMail.Subject, wrapper.Subject);
            Assert.AreEqual(MailMail.DateSent, wrapper.DateSent);
            Assert.AreEqual(MailMail.Unread, wrapper.Unread);
            Assert.AreEqual(MailMail.HasAttachments, wrapper.HasAttachments);
            Assert.AreEqual(MailMail.Importance, wrapper.Importance);
            Assert.AreEqual(MailMail.IsRemoved, wrapper.IsRemoved);
            Assert.AreEqual(MailMail.ChainId, wrapper.ChainId);
            Assert.AreEqual(MailMail.ChainDate, wrapper.ChainDate);

            // Has Changes
            Assert.AreEqual(FolderType.UserFolder, (FolderType)wrapper.Folder);

            var userFolder = wrapper.UserFolders.First();
            Assert.AreEqual(newUserFolder.Id, userFolder.Id);
        }

        [Test]
        public void CreateIndexWithTags()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            if (!TestHelper.IgnoreIfFullTextSearch<MailMail>(false, scope.ServiceProvider))
                return;

            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var tagIds = new List<int>
            {
                777,
                888,
                111
            };

            var MailMail = CreateMailMail(tagIds: tagIds);

            factoryIndexer.Index(MailMail);

            var success = factoryIndexer.TrySelect(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out IReadOnlyCollection<MailMail> wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            var wrapper = wrappers.First();
            Assert.AreEqual(tagIds.Count, wrapper.Tags.Count);
        }

        [Test]
        public void UpdateIndexAddNewTag()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            if (!TestHelper.IgnoreIfFullTextSearch<MailMail>(false, scope.ServiceProvider))
                return;

            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var MailMail = CreateMailMail();

            factoryIndexer.Index(MailMail);

            var success = factoryIndexer.TrySelect(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out IReadOnlyCollection<MailMail> wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            var wrapper = wrappers.First();
            Assert.AreEqual(0, wrapper.Tags.Count);

            const int tag_id = 777;

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    Tags = new List<MailTag>
                    {
                        new MailTag
                        {
                            Id = tag_id
                        }
                    }
                },
                UpdateAction.Add,
                w => w.Tags.ToList());

            success = factoryIndexer.TrySelect(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            wrapper = wrappers.First();
            Assert.AreEqual(1, wrapper.Tags.Count);

            var tag = wrapper.Tags.First();
            Assert.AreEqual(tag_id, tag.Id);
        }

        [Test]
        public void UpdateIndexRemoveExistingTag()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            if (!TestHelper.IgnoreIfFullTextSearch<MailMail>(false, scope.ServiceProvider))
                return;

            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var tagIds = new List<int>
            {
                777,
                888,
                111
            };

            var MailMail = CreateMailMail(tagIds: tagIds);

            factoryIndexer.Index(MailMail);

            var success = factoryIndexer.TrySelect(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out IReadOnlyCollection<MailMail> wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            var wrapper = wrappers.First();
            Assert.AreEqual(3, wrapper.Tags.Count);

            const int tag_id = 888;

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    Tags = new List<MailTag>
                    {
                        new MailTag
                        {
                            Id = tag_id
                        }
                    }
                },
                UpdateAction.Remove,
                w => w.Tags.ToList());

            success = factoryIndexer.TrySelect(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            wrapper = wrappers.First();
            Assert.AreEqual(2, wrapper.Tags.Count);
            Assert.AreEqual(false, wrapper.Tags.Any(t => t.Id == tag_id));
        }

        [Test]
        public void UpdateIndexRemoveAllTags()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            if (!TestHelper.IgnoreIfFullTextSearch<MailMail>(false, scope.ServiceProvider))
                return;

            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var tagIds = new List<int>
            {
                777,
                888,
                111
            };

            var MailMail = CreateMailMail(tagIds: tagIds);

            factoryIndexer.Index(MailMail);

            var success = factoryIndexer.TrySelect(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out IReadOnlyCollection<MailMail> wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            var wrapper = wrappers.First();
            Assert.AreEqual(3, wrapper.Tags.Count);

            factoryIndexer.Update(
                new MailMail
                {
                    Id = MailMail.Id,
                    Tags = new List<MailTag>()
                },
                UpdateAction.Replace,
                w => w.Tags.ToList());

            success = factoryIndexer.TrySelect(i => i
                .Where(s => s.Folder, (byte)FolderType.Inbox)
                .Where(s => s.IdUser, TestUser.ID.ToString()), out wrappers);

            Assert.AreEqual(true, success);
            Assert.AreEqual(1, wrappers.Count);

            wrapper = wrappers.First();
            Assert.AreEqual(0, wrapper.Tags.Count);

        }

        public MailMail CreateMailMail(bool inUserFolder = false, List<int> tagIds = null)
        {
            const string from = "from@from.com";
            const string to = "to@to.com";
            const string cc = "cc@cc.com";
            const string bcc = "bcc@bcc.com";
            const string subject = "Test subject";
            const string chain_id = "--some-chain-id--";

            var now = DateTime.UtcNow;

            var MailMail = new MailMail
            {
                Id = 1,
                TenantId = CURRENT_TENANT,
                IdUser = TestUser.ID.ToString(),
                FromText = from,
                ToText = to,
                Cc = cc,
                Bcc = bcc,
                Subject = subject,
                Folder = inUserFolder ? (byte)FolderType.UserFolder : (byte)FolderType.Inbox,
                DateSent = now,
                Unread = true,
                IdMailbox = 1,
                HasAttachments = true,
                Importance = true,
                IsRemoved = false,
                ChainId = chain_id,
                ChainDate = now,
                Tags = tagIds == null || !tagIds.Any()
                    ? new List<MailTag>()
                    : tagIds.ConvertAll(tId => new MailTag
                    {
                        Id = tId,
                        TenantId = CURRENT_TENANT
                    }),
                UserFolders = inUserFolder
                    ? new List<MailUserFolder>
                    {
                        new MailUserFolder
                        {
                            Id = 1
                        }
                    }
                    : new List<MailUserFolder>(),
                TimeModified = now
            };

            return MailMail;
        }
    }
}
