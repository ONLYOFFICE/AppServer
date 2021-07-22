using System;
using Newtonsoft.Json;
using ASC.Core;
using ASC.Core.Common.EF.Context;
using ASC.Core.Common.EF.Model;
using ASC.Core.Data;
using ASC.Core.Users;
using ASC.Web.Api.Models;
using ASC.Web.Api.Tests.Infrastructure;
using ASC.Web.Core.Utility.Settings;
using NUnit.Framework;

namespace ASC.Web.Api.Tests
{
    [TestFixture]
    class WizardTest : BaseApiTests
    {
        public UserInfo NewUser { get; set; }
        public WebstudioDbContext webstudioDbContext { get; set; }
        public DbSettingsManager dbSettingsManager { get; set; }

        public IServiceProvider ServiceProvider { get; set; }
        public WizardSettings wizardSettings { get; set; }

        [OneTimeSetUp]
        public override void SetUp()
        {
            base.SetUp();
            NewUser = UserManager.GetUsers(CurrentTenant.OwnerId);
        }

        [TestCaseSource(typeof(ApiTestsData), nameof(ApiTestsData.WebStudioSettingsData))]
        [Category("Wizard")]
        [Order(1)]
        public void WizardDataTest(int tenant, Guid userId, string data)
        {
            var user = UserManager.GetUsers(Guid.Parse("99223c7b-e3c9-11eb-9063-982cbc0ea1e5"));
            SecurityContext.AuthenticateMe(user.ID);
            var WebStudioData = new DbWebstudioSettings
            {
                TenantId = tenant,
                Id = user.ID,
                UserId = userId,
                Data = data
            };

            var wizard = settingsManager.Load<WizardSettings>();
            var json = JsonConvert.SerializeObject(wizard.Completed);
            var jsonString = "{\"" + $"Completed\":{json}" + "}";
            Assert.AreEqual(WebStudioData.Data, jsonString);
        }


        [TestCaseSource(typeof(ApiTestsData), nameof(ApiTestsData.UserForWizard))]
        [Category("Wizard")]
        [Order(2)]
        public void WizardSaveDataTest(string email, string passwordHash, string lng, string timeZone, string promocode, string amiid, bool subscribeFromSite)
        {
            var wizardModel = new WizardModel
            {
                Email = email,
                PasswordHash = passwordHash,
                Lng = lng,
                TimeZone = timeZone,
                Promocode = promocode,
                AmiId = amiid,
                SubscribeFromSite = subscribeFromSite
            };
            var wizard = firstTimeTenantSettings.SaveData(wizardModel);
            SecurityContext.AuthenticateMe(CurrentTenant.OwnerId);
            Assert.IsTrue(wizard.Completed);
            Assert.Throws<Exception>(() => firstTimeTenantSettings.SaveData(wizardModel));
        }

    }
}