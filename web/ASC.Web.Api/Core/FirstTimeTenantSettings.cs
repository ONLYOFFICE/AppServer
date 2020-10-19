/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Common.Utils;
using ASC.Core;
using ASC.Core.Billing;
using ASC.Core.Common.Settings;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.MessagingSystem;
using ASC.Web.Api.Models;
using ASC.Web.Core.PublicResources;
using ASC.Web.Core.Users;
using ASC.Web.Core.Utility.Settings;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.Core.Notify;
using ASC.Web.Studio.UserControls.Management;
using ASC.Web.Studio.Utility;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ASC.Web.Studio.UserControls.FirstTime
{
    public class FirstTimeTenantSettings
    {
        private ILog Log { get; }
        private IConfiguration Configuration { get; }
        private TenantManager TenantManager { get; }
        private CoreSettings CoreSettings { get; }
        private TenantExtra TenantExtra { get; }
        private SettingsManager SettingsManager { get; }
        private UserManager UserManager { get; }
        private SetupInfo SetupInfo { get; }
        private SecurityContext SecurityContext { get; }
        private PaymentManager PaymentManager { get; }
        private MessageService MessageService { get; }
        private LicenseReader LicenseReader { get; }
        private StudioNotifyService StudioNotifyService { get; }
        private TimeZoneConverter TimeZoneConverter { get; }
        public CoreBaseSettings CoreBaseSettings { get; }

        public FirstTimeTenantSettings(
            IOptionsMonitor<ILog> options,
            IConfiguration configuration,
            TenantManager tenantManager,
            CoreSettings coreSettings,
            TenantExtra tenantExtra,
            SettingsManager settingsManager,
            UserManager userManager,
            SetupInfo setupInfo,
            SecurityContext securityContext,
            PaymentManager paymentManager,
            MessageService messageService,
            LicenseReader licenseReader,
            StudioNotifyService studioNotifyService,
            TimeZoneConverter timeZoneConverter,
            CoreBaseSettings coreBaseSettings)
        {
            Log = options.CurrentValue;
            Configuration = configuration;
            TenantManager = tenantManager;
            CoreSettings = coreSettings;
            TenantExtra = tenantExtra;
            SettingsManager = settingsManager;
            UserManager = userManager;
            SetupInfo = setupInfo;
            SecurityContext = securityContext;
            PaymentManager = paymentManager;
            MessageService = messageService;
            LicenseReader = licenseReader;
            StudioNotifyService = studioNotifyService;
            TimeZoneConverter = timeZoneConverter;
            CoreBaseSettings = coreBaseSettings;
        }

        public WizardSettings SaveData(WizardModel wizardModel)
        {
            try
            {
                var (email, passwordHash, lng, timeZone, promocode, amiid, analytics, subscribeFromSite) = wizardModel;

                var tenant = TenantManager.GetCurrentTenant();
                var settings = SettingsManager.Load<WizardSettings>();
                if (settings.Completed)
                {
                    throw new Exception("Wizard passed.");
                }

                if (!string.IsNullOrEmpty(SetupInfo.AmiMetaUrl) && IncorrectAmiId(amiid))
                {
                    //throw new Exception(Resource.EmailAndPasswordIncorrectAmiId); TODO
                }

                if (tenant.OwnerId == Guid.Empty)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(6)); // wait cache interval
                    tenant = TenantManager.GetTenant(tenant.TenantId);
                    if (tenant.OwnerId == Guid.Empty)
                    {
                        Log.Error(tenant.TenantId + ": owner id is empty.");
                    }
                }

                var currentUser = UserManager.GetUsers(TenantManager.GetCurrentTenant().OwnerId);

                if (!UserManagerWrapper.ValidateEmail(email))
                {
                    throw new Exception(Resource.EmailAndPasswordIncorrectEmail);
                }

                if (string.IsNullOrEmpty(passwordHash))
                    throw new Exception(Resource.ErrorPasswordEmpty);

                SecurityContext.SetUserPasswordHash(currentUser.ID, passwordHash);

                email = email.Trim();
                if (currentUser.Email != email)
                {
                    currentUser.Email = email;
                    currentUser.ActivationStatus = EmployeeActivationStatus.NotActivated;
                }
                UserManager.SaveUserInfo(currentUser);

                if (!string.IsNullOrWhiteSpace(promocode))
                {
                    try
                    {
                        PaymentManager.ActivateKey(promocode);
                    }
                    catch (Exception err)
                    {
                        Log.Error("Incorrect Promo: " + promocode, err);
                        throw new Exception(Resource.EmailAndPasswordIncorrectPromocode);
                    }
                }

                if (RequestLicense)
                {
                    TariffSettings.SetLicenseAccept(SettingsManager);
                    MessageService.Send(MessageAction.LicenseKeyUploaded);

                    LicenseReader.RefreshLicense();
                }

                if (TenantExtra.Opensource)
                {
                    settings.Analytics = analytics;
                }
                settings.Completed = true;
                SettingsManager.Save(settings);

                TrySetLanguage(tenant, lng);

                tenant.TimeZone = TimeZoneConverter.GetTimeZone(timeZone).Id;

                TenantManager.SaveTenant(tenant);

                StudioNotifyService.SendCongratulations(currentUser);
                StudioNotifyService.SendRegData(currentUser);

                SendInstallInfo(currentUser);

                if (subscribeFromSite && TenantExtra.Opensource && !CoreBaseSettings.CustomMode)
                {
                    SubscribeFromSite(currentUser);
                }

                return settings;
            }
            catch (BillingNotFoundException)
            {
                throw new Exception(UserControlsCommonResource.LicenseKeyNotFound);
            }
            catch (BillingNotConfiguredException)
            {
                throw new Exception(UserControlsCommonResource.LicenseKeyNotCorrect);
            }
            catch (BillingException)
            {
                throw new Exception(UserControlsCommonResource.LicenseException);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public bool RequestLicense
        {
            get
            {
                return TenantExtra.EnableTarrifSettings && TenantExtra.Enterprise && !TenantExtra.EnterprisePaid;
            }
        }

        private void TrySetLanguage(Tenant tenant, string lng)
        {
            if (string.IsNullOrEmpty(lng)) return;

            try
            {
                var culture = CultureInfo.GetCultureInfo(lng);
                tenant.Language = culture.Name;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        private static string _amiId;

        private bool IncorrectAmiId(string customAmiId)
        {
            customAmiId = (customAmiId ?? "").Trim();
            if (string.IsNullOrEmpty(customAmiId)) return true;

            if (string.IsNullOrEmpty(_amiId))
            {
                var getAmiIdUrl = SetupInfo.AmiMetaUrl + "instance-id";
                var request = (HttpWebRequest)WebRequest.Create(getAmiIdUrl);
                try
                {
                    using (var response = request.GetResponse())
                    using (var responseStream = response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        _amiId = reader.ReadToEnd();
                    }

                    Log.Debug("Instance id: " + _amiId);
                }
                catch (Exception e)
                {
                    Log.Error("Request AMI id", e);
                }
            }

            return string.IsNullOrEmpty(_amiId) || _amiId != customAmiId;
        }

        public void SendInstallInfo(UserInfo user)
        {
            try
            {
                StudioNotifyService.SendRegData(user);

                var url = Configuration["web:install-url"];
                if (string.IsNullOrEmpty(url)) return;

                var tenant = TenantManager.GetCurrentTenant();
                var q = new MailQuery
                {
                    Email = user.Email,
                    Id = CoreSettings.GetKey(tenant.TenantId),
                    Alias = tenant.GetTenantDomain(CoreSettings),
                };

                var index = url.IndexOf("?v=", StringComparison.InvariantCultureIgnoreCase);
                if (0 < index)
                {
                    q.Version = url.Substring(index + 3) + Environment.OSVersion;
                    url = url.Substring(0, index);
                }

                using var webClient = new WebClient();
                var values = new NameValueCollection
                        {
                            {"query", Signature.Create(q, "4be71393-0c90-41bf-b641-a8d9523fba5c")}
                        };
                webClient.UploadValues(url, values);
            }
            catch (Exception error)
            {
                Log.Error(error);
            }
        }

        private void SubscribeFromSite(UserInfo user)
        {
            try
            {
                var url = (SetupInfo.TeamlabSiteRedirect ?? "").Trim().TrimEnd('/');
                if (string.IsNullOrEmpty(url)) return;

                url += "/post.ashx";

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 10000;

                var bodyString = string.Format("type=sendsubscription&email={0}", HttpUtility.UrlEncode(user.Email));
                var bytes = Encoding.UTF8.GetBytes(bodyString);
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null) throw new Exception("Response is null");

                    using var reader = new StreamReader(stream);
                    Log.Debug("Subscribe response: " + reader.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Log.Error("Subscribe request", e);
            }
        }

        private class MailQuery
        {
            public string Email { get; set; }
            public string Version { get; set; }
            public string Id { get; set; }
            public string Alias { get; set; }
        }
    }

    public static class FirstTimeTenantSettingsExtension
    {
        public static DIHelper AddFirstTimeTenantSettings(this DIHelper services)
        {
            services.TryAddTransient<FirstTimeTenantSettings>();

            return services
                .AddTenantManagerService()
                .AddCoreConfigurationService()
                .AddCoreSettingsService()
                .AddTenantExtraService()
                .AddSettingsManagerService()
                .AddSetupInfo()
                .AddSecurityContextService()
                .AddPaymentManagerService()
                .AddMessageServiceService()
                .AddLicenseReaderService()
                .AddStudioNotifyServiceService();
        }
    }
}