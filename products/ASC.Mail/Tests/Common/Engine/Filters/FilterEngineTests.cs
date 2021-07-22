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
using ASC.Mail.Enums.Filter;
using ASC.Web.Files.Api;
using ASC.Files.Core.Security;
using ASC.Web.Files.Utils;
using ASC.Mail.Core.Dao.Entities;

namespace ASC.Mail.Aggregator.Tests.Common.Filters
{
    [TestFixture]
    internal class FilterEngineTests
    {
        private const int CURRENT_TENANT = 0;
        public const string PASSWORD = "123456";
        public const string DOMAIN = "gmail.com";

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
                        .AddFilterEngineService()
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

            // Clear TestUser mail index
            var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer<MailMail>>();
            var factoryIndexerHelper = scope.ServiceProvider.GetService<FactoryIndexerHelper>();

            /*var t = scope.ServiceProvider.GetService<MailWrapper>();
            if (factoryIndexerHelper.Support(t))
                factoryIndexer.DeleteAsync(s => s.Where(m => m.UserId, TestUser.ID)).Wait();*/

            // Clear TestUser mail data
            var mailGarbageEngine = scope.ServiceProvider.GetService<MailGarbageEngine>();
            mailGarbageEngine.ClearUserMail(TestUser.ID, tenantManager.GetCurrentTenant());
        }

        [Test]
        public void CreateBaseFilterTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetService<UserManager>();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            CreateBaseFilter(filterEngine);
        }

        [Test]
        public void CreateFullFilterTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.ToOrCc,
                        Operation = ConditionOperationType.NotContains,
                        Value = "toOrcc@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.To,
                        Operation = ConditionOperationType.Matches,
                        Value = "to@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.Cc,
                        Operation = ConditionOperationType.NotMatches,
                        Value = "cc@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.Subject,
                        Operation = ConditionOperationType.Contains,
                        Value = "[TEST]"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.DeleteForever
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "5" // Spam default folder id
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkTag,
                        Data = "111" // Fake tag Id
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            var id = filterEngine.Create(filter);

            Assert.Greater(id, 0);
        }

        [Test]
        public void GetFilterTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var id = CreateBaseFilter(filterEngine);

            var rFilter = filterEngine.Get(id);

            Assert.IsNotNull(rFilter);

            Assert.AreEqual(id, rFilter.Id);
        }

        [Test]
        public void GetFiltersTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            CreateBaseFilter(filterEngine);
            CreateBaseFilter(filterEngine);

            var rFilters = filterEngine.GetList();

            Assert.AreEqual(rFilters.Count, 2);
        }

        [Test]
        public void UpdateFilterTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var id = CreateBaseFilter(filterEngine);

            var rFilter = filterEngine.Get(id);

            Assert.IsNotNull(rFilter);

            Assert.AreEqual(id, rFilter.Id);

            Assert.AreEqual(rFilter.Conditions.First().Key, ConditionKeyType.From);
            Assert.AreEqual(rFilter.Actions.First().Action, ActionType.MarkAsRead);

            rFilter.Conditions.First().Key = ConditionKeyType.To;
            rFilter.Actions.First().Action = ActionType.DeleteForever;

            filterEngine.Update(rFilter);

            rFilter = filterEngine.Get(id);

            Assert.AreEqual(rFilter.Conditions.First().Key, ConditionKeyType.To);
            Assert.AreEqual(rFilter.Actions.First().Action, ActionType.DeleteForever);
        }

        [Test]
        public void DeleteFilterTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var id = CreateBaseFilter(filterEngine);

            var success = filterEngine.Delete(id);

            Assert.AreEqual(true, success);

            var rFilter = filterEngine.Get(id);

            Assert.AreEqual(null, rFilter);
        }

        [Test]
        public void CreateDisabledFilterTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var id = CreateBaseFilter(filterEngine, false);

            var rFilter = filterEngine.Get(id);

            Assert.AreEqual(false, rFilter.Enabled);
        }

        [Test]
        public void EnabledFilterTest()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(TestUser.ID);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var id = CreateBaseFilter(filterEngine, false);

            var rFilter = filterEngine.Get(id);

            rFilter.Enabled = true;

            var success = filterEngine.Update(rFilter);

            Assert.AreEqual(true, success);

            rFilter = filterEngine.Get(rFilter.Id);

            Assert.AreEqual(true, rFilter.Enabled);
        }

        private static int CreateBaseFilter(FilterEngine engine, bool enabled = true)
        {
            var filter = new MailSieveFilterData
            {
                Enabled = enabled,

                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                }
            };

            var id = engine.Create(filter);

            Assert.Greater(id, 0);

            return id;
        }
    }
}
