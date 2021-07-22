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
using ASC.Core;
using ASC.Mail.Models;
using ASC.Mail.Enums;
using NUnit.Framework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ASC.Common;
using ASC.Api.Core.Auth;
using ASC.Api.Core.Middleware;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Autofac;
using ASC.Mail.Core.Engine;
using ASC.Common.Logging;
using ASC.Api.Core;
using ASC.Mail.Clients;
using ASC.Mail.Enums.Filter;
using ASC.Mail.Extensions;

namespace ASC.Mail.Aggregator.Tests.Common.Filters
{
    [TestFixture]
    internal class FilterConditionsTests
    {
        private const int CURRENT_TENANT = 0;

        public MailMessageData MessageData { get; private set; }

        private static readonly string TestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"..\..\..\Data\");

        private const string EML1_FILE_NAME = @"Test subject.eml";

        private static readonly string Eml1Path = TestFolderPath + EML1_FILE_NAME;

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
                        .AddSecurityContextService()
                        .AddApiContextService()
                        .AddFilterEngineService()
                        .AddCoreSettingsService();

                    var builder = new ContainerBuilder();
                    var container = builder.Build();

                    services.TryAddSingleton(container);

                    //services.AddAutofac(hostContext.Configuration, hostContext.HostingEnvironment.ContentRootPath);
                })
                .UseConsoleLifetime()
                .Build();

            TestHost.Start();

            ServiceProvider = TestHost.Services;

            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var coreSettings = scope.ServiceProvider.GetService<CoreSettings>();

            using var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read);

            var mimeMessage = MailClient.ParseMimeMessage(fs);

            MessageData = mimeMessage.CreateMailMessage(tenantManager, coreSettings,
                -1, FolderType.Inbox, true, "--chain-id--", DateTime.UtcNow, "--stream-id--");
        }

        [Test]
        public void CheckFromEmailConditionMatchSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.From,
                Operation = ConditionOperationType.Matches,
                Value = "mono.mail.4test@gmail.com"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }

        [Test]
        public void CheckFromEmailConditionContainsSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.From,
                Operation = ConditionOperationType.Contains,
                Value = "Alexey Safronov"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }

        [Test]
        public void CheckFromEmailConditionNotMatchSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.From,
                Operation = ConditionOperationType.NotMatches,
                Value = "to@to.com"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }

        [Test]
        public void CheckFromEmailConditionNotContainsSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.From,
                Operation = ConditionOperationType.NotContains,
                Value = "@to.com"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }

        [Test]
        public void CheckToEmailsConditionMatchSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.To,
                Operation = ConditionOperationType.Matches,
                Value = "alexey.safronov@onlyoffice.com"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);

            condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.To,
                Operation = ConditionOperationType.Matches,
                Value = "doctor@onlyomail.com"
            };

            success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }

        [Test]
        public void CheckCcEmailsConditionMatchSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.Cc,
                Operation = ConditionOperationType.Matches,
                Value = "mono.mail.4test@mail.ru"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);

            condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.Cc,
                Operation = ConditionOperationType.Matches,
                Value = "mono.mail.4test@yandex.ru"
            };

            success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }

        [Test]
        public void CheckToOrCcEmailsConditionMatchSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.ToOrCc,
                Operation = ConditionOperationType.Matches,
                Value = "alexey.safronov@onlyoffice.com"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);

            condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.ToOrCc,
                Operation = ConditionOperationType.Matches,
                Value = "mono.mail.4test@yandex.ru"
            };

            success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }

        [Test]
        public void CheckSubjectConditionMatchSuccess()
        {
            using var scope = ServiceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();

            tenantManager.SetCurrentTenant(CURRENT_TENANT);
            securityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

            var filterEngine = scope.ServiceProvider.GetService<FilterEngine>();

            var condition = new MailSieveFilterConditionData
            {
                Key = ConditionKeyType.Subject,
                Operation = ConditionOperationType.Matches,
                Value = "Test subject"
            };

            var success = filterEngine.IsConditionSucceed(condition, MessageData);

            Assert.AreEqual(true, success);
        }
    }
}
