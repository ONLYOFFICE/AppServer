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
using ASC.Mail.Exceptions;
using ASC.Mail.Core.Dao.Entities;

namespace ASC.Mail.Aggregator.Tests.Common.UserFolders
{
    [TestFixture]
    internal class UserFoldersTests
    {
        private const int CURRENT_TENANT = 0;
        public const string PASSWORD = "123456";
        public const string DOMAIN = "gmail.com";

        private static readonly string TestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
           @"..\..\..\Data\");
        private const string EML1_FILE_NAME = @"bad_encoding.eml";
        private const string EML2_FILE_NAME = @"embed_image.eml";
        private static readonly string Eml1Path = TestFolderPath + EML1_FILE_NAME;
        private static readonly string Eml2Path = TestFolderPath + EML2_FILE_NAME;

        public UserInfo TestUser { get; private set; }
        public MailBoxData TestMailbox { get; set; }
        public int MailId { get; set; }
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
                        .AddUserFolderEngineService()
                        .AddCoreSettingsService()
                        .AddApiDateTimeHelper();

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

            securityContext.AuthenticateMe(TestUser.ID);

            var mailboxSettings = mailBoxSettingEngine.GetMailBoxSettings(DOMAIN);

            var testMailboxes = mailboxSettings.ToMailboxList(TestUser.Email, PASSWORD, CURRENT_TENANT, TestUser.ID.ToString());

            TestMailbox = testMailboxes.FirstOrDefault();

            if (TestMailbox == null || !mailboxEngine.SaveMailBox(TestMailbox))
            {
                throw new Exception(string.Format("Can't create mailbox with email: {0}", TestUser.Email));
            }
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

            // Clear TestUser mail index
            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();

            var t = scope.ServiceProvider.GetService<MailMail>();
            if (factoryIndexer.Support(t))
                factoryIndexer.DeleteAsync(s => s.Where(m => m.IdUser, TestUser.ID.ToString())).Wait();

            // Clear TestUser mail data
            var mailGarbageEngine = scope.ServiceProvider.GetService<MailGarbageEngine>();
            mailGarbageEngine.ClearUserMail(TestUser.ID, tenantManager.GetCurrentTenant());
        }

        [Test]
        public void CreateFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var folder = userFolderEngine.Create("Test folder");

            Assert.Greater(folder.Id, 0);
        }

        [Test]
        public void CreateFolderWithAlreadyExistingNameTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            const string name = "Test folder";

            var folder = userFolderEngine.Create(name);

            Assert.Greater(folder.Id, 0);

            Assert.Throws<AlreadyExistsFolderException>(() =>
            {
                userFolderEngine.Create(name);
            });
        }

        [Test]
        public void CreateFolderWithoutParentTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            Assert.Throws<ArgumentException>(() =>
            {
                userFolderEngine.Create("Test folder", 777);
            });
        }

        [Test]
        public void CreateFolderWithoutNameTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            Assert.Throws<EmptyFolderException>(() =>
            {
                userFolderEngine.Create("");
            });
        }

        [Test]
        public void CreateSubFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var folder = userFolderEngine.Create("Test folder");

            Assert.Greater(folder.Id, 0);

            var subFolder = userFolderEngine.Create("Test sub folder", folder.Id);

            Assert.Greater(subFolder.Id, 0);

            var rootFolder = userFolderEngine.Get(folder.Id);

            Assert.IsNotNull(rootFolder);

            Assert.AreEqual(1, rootFolder.FolderCount);
        }

        //TODO: fix userFolderEngine.Delete
        /*[Test]
        public void RemoveSubFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var folder = userFolderEngine.Create("Test folder");

            Assert.Greater(folder.Id, 0);

            var subFolder = userFolderEngine.Create("Test sub folder", folder.Id);

            Assert.Greater(subFolder.Id, 0);

            var rootFolder = userFolderEngine.Get(folder.Id);

            Assert.IsNotNull(rootFolder);

            Assert.AreEqual(1, rootFolder.FolderCount);

            userFolderEngine.Delete(subFolder.Id);

            rootFolder = userFolderEngine.Get(rootFolder.Id);

            Assert.IsNotNull(rootFolder);

            Assert.AreEqual(0, rootFolder.FolderCount);
        }*/

        [Test]
        public void ChangeNameTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            const string name = "Folder Name";

            var folder = userFolderEngine.Create(name);

            Assert.Greater(folder.Id, 0);

            Assert.AreEqual(name, folder.Name);

            const string new_name = "New Folder Name";

            var resultFolder = userFolderEngine.Update(folder.Id, new_name);

            Assert.IsNotNull(resultFolder);

            Assert.Greater(resultFolder.Id, 0);

            Assert.AreEqual(0, resultFolder.FolderCount);

            Assert.AreEqual(new_name, resultFolder.Name);

            Assert.AreNotEqual(folder.Name, resultFolder.Name);
        }

        [Test]
        public void ChangeNameToExistingTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            const string name1 = "Folder Name 1";

            var folder1 = userFolderEngine.Create(name1);

            Assert.Greater(folder1.Id, 0);

            Assert.AreEqual(name1, folder1.Name);

            const string name2 = "New Folder Name";

            var folder2 = userFolderEngine.Create(name2);

            Assert.Greater(folder2.Id, 0);

            Assert.AreEqual(name2, folder2.Name);

            Assert.Throws<AlreadyExistsFolderException>(() =>
            {
                userFolderEngine.Update(folder2.Id, name1);
            });
        }

        [Test]
        public void MoveToBaseFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var baseFolder = userFolderEngine.Create("Folder 1");

            Assert.Greater(baseFolder.Id, 0);

            var folder = userFolderEngine.Create("Folder 1.1");

            Assert.Greater(folder.Id, 0);

            userFolderEngine.Update(folder.Id, folder.Name, baseFolder.Id);

            var resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);

            Assert.AreEqual(0, resultBaseFolder.ParentId);

            Assert.AreEqual(1, resultBaseFolder.FolderCount);

            var resultFolder = userFolderEngine.Get(folder.Id);

            Assert.Greater(resultFolder.Id, 0);

            Assert.AreEqual(baseFolder.Id, resultFolder.ParentId);
        }

        [Test]
        public void MoveFromBaseFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var baseFolder = userFolderEngine.Create("Folder 1");

            Assert.Greater(baseFolder.Id, 0);

            var folder = userFolderEngine.Create("Folder 1.1", baseFolder.Id);

            Assert.Greater(folder.Id, 0);

            Assert.AreEqual(baseFolder.Id, folder.ParentId);

            baseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.AreEqual(1, baseFolder.FolderCount);

            userFolderEngine.Update(folder.Id, folder.Name, 0);

            var resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);

            Assert.AreEqual(0, resultBaseFolder.FolderCount);

            var resultFolder = userFolderEngine.Get(folder.Id);

            Assert.Greater(resultFolder.Id, 0);
        }

        [Test]
        public void WrongMoveFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var baseFolder = userFolderEngine.Create("Folder 1");

            Assert.Greater(baseFolder.Id, 0);

            var folder = userFolderEngine.Create("Folder 1.1", baseFolder.Id);

            Assert.Greater(folder.Id, 0);

            Assert.AreEqual(baseFolder.Id, folder.ParentId);

            Assert.Throws<MoveFolderException>(() =>
            {
                userFolderEngine.Update(baseFolder.Id, baseFolder.Name, folder.Id);
            });
        }

        [Test]
        public void WrongChangeParentToCurrentTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var baseFolder = userFolderEngine.Create("Folder 1");

            Assert.Greater(baseFolder.Id, 0);

            var folder = userFolderEngine.Create("Folder 1.1", baseFolder.Id);

            Assert.Greater(folder.Id, 0);

            Assert.AreEqual(baseFolder.Id, folder.ParentId);

            Assert.Throws<ArgumentException>(() =>
            {
                userFolderEngine.Update(folder.Id, folder.Name, folder.Id);
            });
        }

        [Test]
        public void WrongChangeParentToChildTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var root = userFolderEngine.Create("Root");

            Assert.Greater(root.Id, 0);

            var f1 = userFolderEngine.Create("1", root.Id);

            Assert.Greater(f1.Id, 0);

            Assert.AreEqual(root.Id, f1.ParentId);

            root = userFolderEngine.Get(root.Id);

            Assert.AreEqual(1, root.FolderCount);

            var f11 = userFolderEngine.Create("1.1", f1.Id);

            Assert.Greater(f11.Id, 0);

            Assert.AreEqual(f1.Id, f11.ParentId);

            f1 = userFolderEngine.Get(f1.Id);

            Assert.AreEqual(1, f1.FolderCount);

            var f111 = userFolderEngine.Create("1.1.1", f11.Id);

            Assert.Greater(f111.Id, 0);

            Assert.AreEqual(f11.Id, f111.ParentId);

            Assert.Throws<MoveFolderException>(() =>
            {
                userFolderEngine.Update(f11.Id, f11.Name, f111.Id);
            });
        }

        //TODO: fix userFolderEngine.Delete
        /*[Test]
        public void DeleteFolderInTheMiddleTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();

            var root = userFolderEngine.Create("Root");

            Assert.Greater(root.Id, 0);

            var f1 = userFolderEngine.Create("1", root.Id);

            Assert.Greater(f1.Id, 0);

            Assert.AreEqual(root.Id, f1.ParentId);

            root = userFolderEngine.Get(root.Id);

            Assert.AreEqual(1, root.FolderCount);

            var f11 = userFolderEngine.Create("1.1", f1.Id);

            Assert.Greater(f11.Id, 0);

            Assert.AreEqual(f1.Id, f11.ParentId);

            f1 = userFolderEngine.Get(f1.Id);

            Assert.AreEqual(1, f1.FolderCount);

            var f111 = userFolderEngine.Create("1.1.1", f11.Id);

            Assert.Greater(f111.Id, 0);

            Assert.AreEqual(f11.Id, f111.ParentId);

            f11 = userFolderEngine.Get(f11.Id);

            Assert.AreEqual(1, f11.FolderCount);

            var f2 = userFolderEngine.Create("2", root.Id);

            Assert.Greater(f2.Id, 0);

            Assert.AreEqual(root.Id, f2.ParentId);

            f1 = userFolderEngine.Get(root.Id);

            Assert.AreEqual(4, f1.FolderCount);

            userFolderEngine.Delete(f11.Id);

            f1 = userFolderEngine.Get(root.Id);

            Assert.AreEqual(2, f1.FolderCount);

            userFolderEngine.Delete(root.Id);

            var list = userFolderEngine.GetList();

            Assert.IsEmpty(list);
        }*/

        [Test]
        public void LoadMessagesToUserFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();
            var testEngine = scope.ServiceProvider.GetService<TestEngine>();

            var baseFolder = userFolderEngine.Create("Folder 1");

            Assert.Greater(baseFolder.Id, 0);

            var resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadCount);
            Assert.AreEqual(0, resultBaseFolder.TotalCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(0, resultBaseFolder.TotalChainCount);

            int mailId1;

            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.UserFolder,
                    UserFolderId = baseFolder.Id,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = true,
                    EmlStream = fs
                };

                mailId1 = testEngine.LoadSampleMessage(model);
            }

            Assert.Greater(mailId1, 0);

            resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadCount);
            Assert.AreEqual(1, resultBaseFolder.TotalCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(1, resultBaseFolder.TotalChainCount);

            int mailId2;

            using (var fs = new FileStream(Eml2Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.UserFolder,
                    UserFolderId = baseFolder.Id,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = false,
                    EmlStream = fs
                };

                mailId2 = testEngine.LoadSampleMessage(model);
            }

            Assert.Greater(mailId2, 0);

            resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadCount);
            Assert.AreEqual(2, resultBaseFolder.TotalCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(2, resultBaseFolder.TotalChainCount);
        }

        [Test]
        public void MoveMessagesFromDefaulFolderToUserFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();
            var testEngine = scope.ServiceProvider.GetService<TestEngine>();
            var messageEngine = scope.ServiceProvider.GetService<MessageEngine>();

            var baseFolder = userFolderEngine.Create("Folder 1");

            Assert.Greater(baseFolder.Id, 0);

            var resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadCount);
            Assert.AreEqual(0, resultBaseFolder.TotalCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(0, resultBaseFolder.TotalChainCount);

            int mailId1;
            int mailId2;

            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.Inbox,
                    UserFolderId = null,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = true,
                    EmlStream = fs
                };

                mailId1 = testEngine.LoadSampleMessage(model);
            }

            using (var fs = new FileStream(Eml2Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.Inbox,
                    UserFolderId = null,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = false,
                    EmlStream = fs
                };

                mailId2 = testEngine.LoadSampleMessage(model);
            }

            Assert.Greater(mailId1, 0);
            Assert.Greater(mailId2, 0);

            messageEngine.SetFolder(new List<int> { mailId1, mailId2 }, FolderType.UserFolder,
                baseFolder.Id);

            resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadCount);
            Assert.AreEqual(2, resultBaseFolder.TotalCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(2, resultBaseFolder.TotalChainCount);
        }

        [Test]
        public void MoveMessagesFromUserFolderToDefaulFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();
            var testEngine = scope.ServiceProvider.GetService<TestEngine>();
            var messageEngine = scope.ServiceProvider.GetService<MessageEngine>();

            var baseFolder = userFolderEngine.Create("Folder 1");

            Assert.Greater(baseFolder.Id, 0);

            var resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadCount);
            Assert.AreEqual(0, resultBaseFolder.TotalCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(0, resultBaseFolder.TotalChainCount);

            int mailId1;
            int mailId2;

            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.UserFolder,
                    UserFolderId = baseFolder.Id,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = true,
                    EmlStream = fs
                };

                mailId1 = testEngine.LoadSampleMessage(model);
            }

            using (var fs = new FileStream(Eml2Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.UserFolder,
                    UserFolderId = baseFolder.Id,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = false,
                    EmlStream = fs
                };

                mailId2 = testEngine.LoadSampleMessage(model);
            }

            Assert.Greater(mailId1, 0);
            Assert.Greater(mailId2, 0);

            resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadCount);
            Assert.AreEqual(2, resultBaseFolder.TotalCount);
            Assert.AreEqual(1, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(2, resultBaseFolder.TotalChainCount);

            messageEngine.SetFolder(new List<int> { mailId1, mailId2 }, FolderType.Inbox);

            resultBaseFolder = userFolderEngine.Get(baseFolder.Id);

            Assert.IsNotNull(resultBaseFolder);
            Assert.AreEqual(0, resultBaseFolder.ParentId);
            Assert.AreEqual(0, resultBaseFolder.FolderCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadCount);
            Assert.AreEqual(0, resultBaseFolder.TotalCount);
            Assert.AreEqual(0, resultBaseFolder.UnreadChainCount);
            Assert.AreEqual(0, resultBaseFolder.TotalChainCount);
        }

        [Test]
        public void MoveMessagesFromUserFolderToAnotherUserFolderTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var userFolderEngine = scope.ServiceProvider.GetService<UserFolderEngine>();
            var testEngine = scope.ServiceProvider.GetService<TestEngine>();
            var messageEngine = scope.ServiceProvider.GetService<MessageEngine>();

            var folder1 = userFolderEngine.Create("Folder 1");

            Assert.Greater(folder1.Id, 0);

            var resultFolder1 = userFolderEngine.Get(folder1.Id);

            Assert.IsNotNull(resultFolder1);
            Assert.AreEqual(0, resultFolder1.ParentId);
            Assert.AreEqual(0, resultFolder1.FolderCount);
            Assert.AreEqual(0, resultFolder1.UnreadCount);
            Assert.AreEqual(0, resultFolder1.TotalCount);
            Assert.AreEqual(0, resultFolder1.UnreadChainCount);
            Assert.AreEqual(0, resultFolder1.TotalChainCount);

            var folder2 = userFolderEngine.Create("Folder 2");

            Assert.Greater(folder2.Id, 0);

            var resultFolder2 = userFolderEngine.Get(folder2.Id);

            Assert.IsNotNull(resultFolder2);
            Assert.AreEqual(0, resultFolder2.ParentId);
            Assert.AreEqual(0, resultFolder2.FolderCount);
            Assert.AreEqual(0, resultFolder2.UnreadCount);
            Assert.AreEqual(0, resultFolder2.TotalCount);
            Assert.AreEqual(0, resultFolder2.UnreadChainCount);
            Assert.AreEqual(0, resultFolder2.TotalChainCount);

            int mailId1;
            int mailId2;

            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.UserFolder,
                    UserFolderId = folder1.Id,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = true,
                    EmlStream = fs
                };

                mailId1 = testEngine.LoadSampleMessage(model);
            }

            using (var fs = new FileStream(Eml2Path, FileMode.Open, FileAccess.Read))
            {
                var model = new TestMessageModel
                {
                    FolderId = (int)FolderType.UserFolder,
                    UserFolderId = folder1.Id,
                    MailboxId = TestMailbox.MailBoxId,
                    Unread = false,
                    EmlStream = fs
                };

                mailId2 = testEngine.LoadSampleMessage(model);
            }

            Assert.Greater(mailId1, 0);
            Assert.Greater(mailId2, 0);

            resultFolder1 = userFolderEngine.Get(folder1.Id);

            Assert.IsNotNull(resultFolder1);
            Assert.AreEqual(0, resultFolder1.ParentId);
            Assert.AreEqual(0, resultFolder1.FolderCount);
            Assert.AreEqual(1, resultFolder1.UnreadCount);
            Assert.AreEqual(2, resultFolder1.TotalCount);
            Assert.AreEqual(1, resultFolder1.UnreadChainCount);
            Assert.AreEqual(2, resultFolder1.TotalChainCount);

            messageEngine.SetFolder(new List<int> { mailId1, mailId2 }, FolderType.UserFolder, folder2.Id);

            resultFolder1 = userFolderEngine.Get(folder1.Id);

            Assert.IsNotNull(resultFolder1);
            Assert.AreEqual(0, resultFolder1.ParentId);
            Assert.AreEqual(0, resultFolder1.FolderCount);
            Assert.AreEqual(0, resultFolder1.UnreadCount);
            Assert.AreEqual(0, resultFolder1.TotalCount);
            Assert.AreEqual(0, resultFolder1.UnreadChainCount);
            Assert.AreEqual(0, resultFolder1.TotalChainCount);

            resultFolder2 = userFolderEngine.Get(folder2.Id);

            Assert.IsNotNull(resultFolder2);
            Assert.AreEqual(0, resultFolder2.ParentId);
            Assert.AreEqual(0, resultFolder2.FolderCount);
            Assert.AreEqual(1, resultFolder2.UnreadCount);
            Assert.AreEqual(2, resultFolder2.TotalCount);
            Assert.AreEqual(1, resultFolder2.UnreadChainCount);
            Assert.AreEqual(2, resultFolder2.TotalChainCount);
        }
    }
}
