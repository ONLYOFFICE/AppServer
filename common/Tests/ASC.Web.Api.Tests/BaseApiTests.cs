using System;
using System.IO;
using System.Reflection;
using ASC.Api.Settings;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Core.Common.EF.Context;
using ASC.Core.Common.EF.Model;
using ASC.Core.Common.Settings;
using ASC.Core.Tenants;
using ASC.Web.Api.Controllers;
using ASC.Web.Core.Utility.Settings;
using ASC.Web.Studio.UserControls.FirstTime;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NUnit.Framework;

namespace ASC.Web.Api.Tests
{
    [SetUpFixture]
    public class MySetUpClass
    {
        protected IServiceScope Scope { get; set; }

        [OneTimeSetUp]
        public void CreateDb()
        {
            var host = Program.CreateHostBuilder(new string[] {
                "--pathToConf", Path.Combine("..", "..", "..", "..","..", "..", "config"),
                "--ConnectionStrings:default:connectionString", BaseApiTests.TestConnection,
                "--migration:enabled", "true",
                "--core:products:folder", Path.Combine("..", "..", "..", "..","..", "..", "products")}).Build();

            Migrate(host.Services);
            Migrate(host.Services, Assembly.GetExecutingAssembly().GetName().Name);

            Scope = host.Services.CreateScope();
        }

        [OneTimeTearDown]
        public void DropDb()
        {
            var context = Scope.ServiceProvider.GetService<DbContextManager<TenantDbContext>>();
            context.Value.Database.EnsureDeleted();
        }


        private void Migrate(IServiceProvider serviceProvider, string testAssembly = null)
        {
            using var scope = serviceProvider.CreateScope();

            if (!string.IsNullOrEmpty(testAssembly))
            {
                var configuration = scope.ServiceProvider.GetService<IConfiguration>();
                configuration["testAssembly"] = testAssembly;
            }

            using var db = scope.ServiceProvider.GetService<DbContextManager<TenantDbContext>>();
            db.Value.Migrate();
        }
    }
    class BaseApiTests
    {
        protected ILog Log { get; set; }
        protected UserManager UserManager { get; set; }
        protected Tenant CurrentTenant { get; set; }
        protected SecurityContext SecurityContext { get; set; }
        protected UserOptions UserOptions { get; set; }
        protected IServiceScope scope { get; set; }
        protected SettingsManager settingsManager { get; set; }
        protected DbWebstudioSettings dbWebStudioSettings { get; set; }

        protected FirstTimeTenantSettings firstTimeTenantSettings { get; set; }

        public const string TestConnection = "Server=localhost;Database=onlyoffice_test;User ID=root;Password=root;Pooling=true;Character Set=utf8;AutoEnlist=false;SSL Mode=none;AllowPublicKeyRetrieval=True";
        public virtual void SetUp()
        {
            var host = Program.CreateHostBuilder(new string[] {
                "--pathToConf" , Path.Combine("..", "..", "..", "..","..", "..", "config"),
                "--ConnectionStrings:default:connectionString", TestConnection,
                 "--migration:enabled", "true" }).Build();

            scope = host.Services.CreateScope();

            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var tenant = tenantManager.GetTenant(1);
            tenantManager.SetCurrentTenant(tenant);
            CurrentTenant = tenant;

            firstTimeTenantSettings = scope.ServiceProvider.GetService<FirstTimeTenantSettings>();
            settingsManager = scope.ServiceProvider.GetService<SettingsManager>();
            dbWebStudioSettings = scope.ServiceProvider.GetService<DbWebstudioSettings>();
            UserManager = scope.ServiceProvider.GetService<UserManager>();
            SecurityContext = scope.ServiceProvider.GetService<SecurityContext>();
            UserOptions = scope.ServiceProvider.GetService<IOptions<UserOptions>>().Value;
            Log = scope.ServiceProvider.GetService<IOptionsMonitor<ILog>>().CurrentValue;
            SecurityContext.AuthenticateMe(CurrentTenant.OwnerId);
        }


    }
}
