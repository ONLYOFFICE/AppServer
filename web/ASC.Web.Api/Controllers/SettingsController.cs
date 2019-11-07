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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.ServiceModel.Security;
using System.Web;

using ASC.Api.Collections;
using ASC.Api.Core;
using ASC.Api.Utils;
using ASC.Common.Logging;
using ASC.Common.Threading;
using ASC.Common.Utils;
using ASC.Core;
using ASC.Core.Billing;
using ASC.Core.Common.Configuration;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.Data.Storage.Configuration;
using ASC.Data.Storage.Migration;
using ASC.IPSecurity;
using ASC.MessagingSystem;
using ASC.Web.Api.Models;
using ASC.Web.Api.Routing;
using ASC.Web.Core;
using ASC.Web.Core.PublicResources;
using ASC.Web.Core.Sms;
using ASC.Web.Core.Utility;
using ASC.Web.Core.Utility.Settings;
using ASC.Web.Core.WebZones;
using ASC.Web.Core.WhiteLabel;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.Core.Notify;
using ASC.Web.Studio.Core.Quota;
using ASC.Web.Studio.Core.SMS;
using ASC.Web.Studio.Core.Statistic;
using ASC.Web.Studio.Core.TFA;
using ASC.Web.Studio.UserControls.CustomNavigation;
using ASC.Web.Studio.Utility;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Settings
{
    [DefaultRoute]
    [ApiController]
    public partial class SettingsController : ControllerBase
    {
        private const int ONE_THREAD = 1;
        private static readonly DistributedTaskQueue quotaTasks = new DistributedTaskQueue("quotaOperations", ONE_THREAD);

        private static DistributedTaskQueue LDAPTasks { get; } = new DistributedTaskQueue("ldapOperations");

        private static DistributedTaskQueue SMTPTasks { get; } = new DistributedTaskQueue("smtpOperations");

        public Tenant Tenant { get { return ApiContext.Tenant; } }

        public ApiContext ApiContext { get; }

        public LogManager LogManager { get; }

        public MessageService MessageService { get; }

        public StudioNotifyService StudioNotifyService { get; }

        public IWebHostEnvironment WebHostEnvironment { get; }


        public SettingsController(LogManager logManager,
            MessageService messageService,
            StudioNotifyService studioNotifyService,
            ApiContext apiContext)
        {
            LogManager = logManager;
            MessageService = messageService;
            StudioNotifyService = studioNotifyService;
            ApiContext = apiContext;
        }

        [Read("")]
        [AllowAnonymous]
        public SettingsWrapper GetSettings()
        {
            var settings = new SettingsWrapper
            {
                Culture = Tenant.GetCulture().ToString(),
                GreetingSettings = Tenant.Name
            };

            if (SecurityContext.IsAuthenticated)
            {
                settings.TrustedDomains = Tenant.TrustedDomains;
                settings.TrustedDomainsType = Tenant.TrustedDomainsType;
                var timeZone = Tenant.TimeZone;
                settings.Timezone = timeZone.Id;
                settings.UtcOffset = timeZone.GetUtcOffset(DateTime.UtcNow);
                settings.UtcHoursOffset = settings.UtcOffset.TotalHours;
                settings.OwnerId = Tenant.OwnerId;
            }

            return settings;
        }

        [Read("quota")]
        public QuotaWrapper GetQuotaUsed()
        {
            return QuotaWrapper.GetCurrent(Tenant);
        }

        [AllowAnonymous]
        [Read("cultures")]
        public List<CultureInfo> GetSupportedCultures()
        {
            return SetupInfo.EnabledCultures;
        }

        [Read("timezones")]
        public List<object> GetTimeZones()
        {
            var timeZones =  TimeZoneInfo.GetSystemTimeZones().ToList();

            if (timeZones.All(tz => tz.Id != "UTC"))
            {
                timeZones.Add(TimeZoneInfo.Utc);
            }

            List<object> listOfTimezones = new List<object>();

            foreach (var tz in timeZones.OrderBy(z => z.BaseUtcOffset))
            {
                var displayName = tz.DisplayName;
                if (tz.StandardName.StartsWith("GMT") && !tz.StandardName.StartsWith("GMT "))
                {
                    displayName = string.Format("(UTC{0}{1}) ", tz.BaseUtcOffset < TimeSpan.Zero ? "-" : "+", tz.BaseUtcOffset.ToString(@"hh\:mm")) + tz.Id;
                   
                }

                listOfTimezones.Add(new TimezonesModel { Id = tz.Id, DisplayName = displayName });

            }

            return listOfTimezones;
        }

/*        [Read("greetingsettings")]
        public string GetGreetingSettings()
        {
            return Tenant.Name;
        }*/

        [Create("greetingsettings")]
        public object SaveGreetingSettings(GreetingSettingsModel model)
        {
            try
            {
                SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

                Tenant.Name = model.Title;
                CoreContext.TenantManager.SaveTenant(Tenant);

                MessageService.Send(MessageAction.GreetingSettingsUpdated);

                return new { Status = 1, Message = Resource.SuccessfullySaveGreetingSettingsMessage };
            }
            catch (Exception e)
            {
                return new { Status = 0, Message = e.Message.HtmlEncode() };
            }
        }

        [Create("greetingsettings/restore")]
        public object RestoreGreetingSettings()
        {
            try
            {
                SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

               TenantInfoSettings.Load().RestoreDefaultTenantName();

                return new
                {
                    Status = 1,
                    Message = Resource.SuccessfullySaveGreetingSettingsMessage,
                    CompanyName = CoreContext.TenantManager.GetCurrentTenant().Name
                };
            }
            catch (Exception e)
            {
                return new { Status = 0, Message = e.Message.HtmlEncode() };
            }
        }

        [Read("recalculatequota")]
        public void RecalculateQuota()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var operations = quotaTasks.GetTasks()
                .Where(t => t.GetProperty<int>(QuotaSync.TenantIdKey) == Tenant.TenantId);

            if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
            {
                throw new InvalidOperationException(Resource.LdapSettingsTooManyOperations);
            }

            var op = new QuotaSync(Tenant.TenantId);

            quotaTasks.QueueTask(op.RunJob, op.GetDistributedTask());
        }

        [Read("checkrecalculatequota")]
        public bool CheckRecalculateQuota()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var task = quotaTasks.GetTasks().FirstOrDefault(t => t.GetProperty<int>(QuotaSync.TenantIdKey) == Tenant.TenantId);

            if (task != null && task.Status == DistributedTaskStatus.Completed)
            {
                quotaTasks.RemoveTask(task.Id);
                return false;
            }

            return task != null;
        }

        [AllowAnonymous]
        [Read("version/build", false)]
        public BuildVersion GetBuildVersions()
        {
            return BuildVersion.GetCurrentBuildVersion();
        }

        [Read("version")]
        public TenantVersionWrapper GetVersions()
        {
            return new TenantVersionWrapper(Tenant.Version, CoreContext.TenantManager.GetTenantVersions());
        }

        [Update("version")]
        public TenantVersionWrapper SetVersion(SettingsModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            CoreContext.TenantManager.GetTenantVersions().FirstOrDefault(r => r.Id == model.VersionId).NotFoundIfNull();
            CoreContext.TenantManager.SetTenantVersion(Tenant, model.VersionId);

            return GetVersions();
        }

        [Read("security")]
        public IEnumerable<SecurityWrapper> GetWebItemSecurityInfo(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                ids = WebItemManager.Instance.GetItemsAll().Select(i => i.ID.ToString());
            }

            var subItemList = WebItemManager.Instance.GetItemsAll().Where(item => item.IsSubItem()).Select(i => i.ID.ToString());

            return ids.Select(r => WebItemSecurity.GetSecurityInfo(Tenant.TenantId, r))
                      .Select(i => new SecurityWrapper
                      {
                          WebItemId = i.WebItemId,
                          Enabled = i.Enabled,
                          Users = i.Users.Select(r => EmployeeWraper.Get(r, ApiContext)),
                          Groups = i.Groups.Select(g => new GroupWrapperSummary(g, ApiContext)),
                          IsSubItem = subItemList.Contains(i.WebItemId),
                      }).ToList();
        }

        [Read("security/{id}")]
        public bool GetWebItemSecurityInfo(Guid id)
        {
            var module = WebItemManager.Instance[id];

            return module != null && !module.IsDisabled(Tenant);
        }

        [Read("security/modules")]
        public object GetEnabledModules()
        {
            var EnabledModules = WebItemManager.Instance.GetItems(Tenant, WebZoneType.All, ItemAvailableState.Normal)
                                        .Where(item => !item.IsSubItem() && item.Visible)
                                        .ToList()
                                        .Select(item => new
                                        {
                                            id = item.ProductClassName.HtmlEncode(),
                                            title = item.Name.HtmlEncode()
                                        });

            return EnabledModules;
        }

        [Read("security/password")]
        [Authorize(AuthenticationSchemes = "confirm", Roles = "Everyone")]
        public object GetPasswordSettings()
        {
            var UserPasswordSettings = PasswordSettings.Load();

            return UserPasswordSettings;
        }

        [Update("security")]
        public IEnumerable<SecurityWrapper> SetWebItemSecurity(WebItemSecurityModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            WebItemSecurity.SetSecurity(model.Id, model.Enabled, model.Subjects?.ToArray());
            var securityInfo = GetWebItemSecurityInfo(new List<string> { model.Id });

            if (model.Subjects == null) return securityInfo;

            var productName = GetProductName(new Guid(model.Id));

            if (!model.Subjects.Any())
            {
                MessageService.Send(MessageAction.ProductAccessOpened, productName);
            }
            else
            {
                foreach (var info in securityInfo)
                {
                    if (info.Groups.Any())
                    {
                        MessageService.Send(MessageAction.GroupsOpenedProductAccess, productName, info.Groups.Select(x => x.Name));
                    }
                    if (info.Users.Any())
                    {
                        MessageService.Send(MessageAction.UsersOpenedProductAccess, productName, info.Users.Select(x => HttpUtility.HtmlDecode(x.DisplayName)));
                    }
                }
            }

            return securityInfo;
        }

        [Update("security/access")]
        public IEnumerable<SecurityWrapper> SetAccessToWebItems(WebItemSecurityModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var itemList = new ItemDictionary<string, bool>();

            foreach (var item in model.Items)
            {
                if (!itemList.ContainsKey(item.Key))
                    itemList.Add(item.Key, item.Value);
            }

            var defaultPageSettings = StudioDefaultPageSettings.Load();

            foreach (var item in itemList)
            {
                Guid[] subjects = null;
                var productId = new Guid(item.Key);

                if (item.Value)
                {
                    if (WebItemManager.Instance[productId] is IProduct webItem)
                    {
                        var productInfo = WebItemSecurity.GetSecurityInfo(Tenant.TenantId, item.Key);
                        var selectedGroups = productInfo.Groups.Select(group => group.ID).ToList();
                        var selectedUsers = productInfo.Users.Select(user => user.ID).ToList();
                        selectedUsers.AddRange(selectedGroups);
                        if (selectedUsers.Count > 0)
                        {
                            subjects = selectedUsers.ToArray();
                        }
                    }
                }
                else if (productId == defaultPageSettings.DefaultProductID)
                {
                    (defaultPageSettings.GetDefault() as StudioDefaultPageSettings).Save();
                }

                WebItemSecurity.SetSecurity(item.Key, item.Value, subjects);
            }

            MessageService.Send(MessageAction.ProductsListUpdated);

            return GetWebItemSecurityInfo(itemList.Keys.ToList());
        }

        [Read("security/administrator/{productid}")]
        public IEnumerable<EmployeeWraper> GetProductAdministrators(Guid productid)
        {
            return WebItemSecurity.GetProductAdministrators(Tenant, productid)
                                  .Select(r => EmployeeWraper.Get(r, ApiContext))
                                  .ToList();
        }

        [Read("security/administrator")]
        public object IsProductAdministrator(Guid productid, Guid userid)
        {
            var result = WebItemSecurity.IsProductAdministrator(Tenant, productid, userid);
            return new { ProductId = productid, UserId = userid, Administrator = result, };
        }

        [Update("security/administrator")]
        public object SetProductAdministrator(SecurityModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            WebItemSecurity.SetProductAdministrator(Tenant, model.ProductId, model.UserId, model.Administrator);

            var admin = CoreContext.UserManager.GetUsers(model.UserId);

            if (model.ProductId == Guid.Empty)
            {
                var messageAction = model.Administrator ? MessageAction.AdministratorOpenedFullAccess : MessageAction.AdministratorDeleted;
                MessageService.Send(messageAction, MessageTarget.Create(admin.ID), admin.DisplayUserName(false));
            }
            else
            {
                var messageAction = model.Administrator ? MessageAction.ProductAddedAdministrator : MessageAction.ProductDeletedAdministrator;
                MessageService.Send(messageAction, MessageTarget.Create(admin.ID), GetProductName(model.ProductId), admin.DisplayUserName(false));
            }

            return new { model.ProductId, model.UserId, model.Administrator };
        }

        [Read("logo")]
        public string GetLogo()
        {
            return TenantInfoSettings.Load().GetAbsoluteCompanyLogoPath();
        }


        ///<visible>false</visible>
        [Create("whitelabel/save")]
        public void SaveWhiteLabelSettings(WhiteLabelModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            if (!TenantLogoManager.WhiteLabelEnabled || !TenantLogoManager.WhiteLabelPaid)
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "WhiteLabel");
            }

            var _tenantWhiteLabelSettings = TenantWhiteLabelSettings.Load();

            if (model.Logo != null)
            {
                var logoDict = new Dictionary<int, string>();
                model.Logo.ToList().ForEach(n => logoDict.Add(n.Key, n.Value));

                _tenantWhiteLabelSettings.SetLogo(Tenant.TenantId, logoDict);
            }

            _tenantWhiteLabelSettings.LogoText = model.LogoText;
            _tenantWhiteLabelSettings.Save(Tenant.TenantId);

        }


        ///<visible>false</visible>
        [Create("whitelabel/savefromfiles")]
        public void SaveWhiteLabelSettingsFromFiles(WhiteLabelModel model)
        {
            if (model.Attachments != null && model.Attachments.Any())
            {
                var _tenantWhiteLabelSettings = TenantWhiteLabelSettings.Load();

                foreach (var f in model.Attachments)
                {
                    var parts = f.FileName.Split('.');

                    var logoType = (WhiteLabelLogoTypeEnum)(Convert.ToInt32(parts[0]));
                    var fileExt = parts[1];
                    using var inputStream = f.OpenReadStream();
                    _tenantWhiteLabelSettings.SetLogoFromStream(logoType, fileExt, inputStream);
                }
                _tenantWhiteLabelSettings.Save(Tenant.TenantId);
            }
            else
            {
                throw new InvalidOperationException("No input files");
            }
        }


        ///<visible>false</visible>
        [Read("whitelabel/sizes")]
        public object GetWhiteLabelSizes()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            if (!TenantLogoManager.WhiteLabelEnabled)
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "WhiteLabel");
            }

            return
            new[]
            {
                new {type = (int)WhiteLabelLogoTypeEnum.LightSmall, name = WhiteLabelLogoTypeEnum.LightSmall.ToString(), height = TenantWhiteLabelSettings.logoLightSmallSize.Height, width = TenantWhiteLabelSettings.logoLightSmallSize.Width},
                new {type = (int)WhiteLabelLogoTypeEnum.Dark, name = WhiteLabelLogoTypeEnum.Dark.ToString(), height = TenantWhiteLabelSettings.logoDarkSize.Height, width = TenantWhiteLabelSettings.logoDarkSize.Width},
                new {type = (int)WhiteLabelLogoTypeEnum.Favicon, name = WhiteLabelLogoTypeEnum.Favicon.ToString(), height = TenantWhiteLabelSettings.logoFaviconSize.Height, width = TenantWhiteLabelSettings.logoFaviconSize.Width},
                new {type = (int)WhiteLabelLogoTypeEnum.DocsEditor, name = WhiteLabelLogoTypeEnum.DocsEditor.ToString(), height = TenantWhiteLabelSettings.logoDocsEditorSize.Height, width = TenantWhiteLabelSettings.logoDocsEditorSize.Width}
            };
        }



        ///<visible>false</visible>
        [Read("whitelabel/logos")]
        public Dictionary<int, string> GetWhiteLabelLogos(bool retina)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            if (!TenantLogoManager.WhiteLabelEnabled)
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "WhiteLabel");
            }

            var _tenantWhiteLabelSettings = TenantWhiteLabelSettings.Load();


            var result = new Dictionary<int, string>
            {
                { (int)WhiteLabelLogoTypeEnum.LightSmall, CommonLinkUtility.GetFullAbsolutePath(HttpContext, _tenantWhiteLabelSettings.GetAbsoluteLogoPath(WhiteLabelLogoTypeEnum.LightSmall, !retina)) },
                { (int)WhiteLabelLogoTypeEnum.Dark, CommonLinkUtility.GetFullAbsolutePath(HttpContext, _tenantWhiteLabelSettings.GetAbsoluteLogoPath(WhiteLabelLogoTypeEnum.Dark, !retina)) },
                { (int)WhiteLabelLogoTypeEnum.Favicon, CommonLinkUtility.GetFullAbsolutePath(HttpContext, _tenantWhiteLabelSettings.GetAbsoluteLogoPath(WhiteLabelLogoTypeEnum.Favicon, !retina)) },
                { (int)WhiteLabelLogoTypeEnum.DocsEditor, CommonLinkUtility.GetFullAbsolutePath(HttpContext, _tenantWhiteLabelSettings.GetAbsoluteLogoPath(WhiteLabelLogoTypeEnum.DocsEditor, !retina)) }
            };

            return result;
        }

        ///<visible>false</visible>
        [Read("whitelabel/logotext")]
        public string GetWhiteLabelLogoText()
        {
            if (!TenantLogoManager.WhiteLabelEnabled)
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "WhiteLabel");
            }

            var whiteLabelSettings = TenantWhiteLabelSettings.Load();

            return whiteLabelSettings.LogoText ?? TenantWhiteLabelSettings.DefaultLogoText;
        }


        ///<visible>false</visible>
        [Update("whitelabel/restore")]
        public void RestoreWhiteLabelOptions()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            if (!TenantLogoManager.WhiteLabelEnabled || !TenantLogoManager.WhiteLabelPaid)
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "WhiteLabel");
            }

            var _tenantWhiteLabelSettings = TenantWhiteLabelSettings.Load();
            _tenantWhiteLabelSettings.RestoreDefault();

            var _tenantInfoSettings = TenantInfoSettings.Load();
            _tenantInfoSettings.RestoreDefaultLogo();
            _tenantInfoSettings.Save();
        }

        [Read("iprestrictions")]
        public IEnumerable<IPRestriction> GetIpRestrictions()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            return IPRestrictionsService.Get(Tenant.TenantId);
        }

        [Update("iprestrictions")]
        public IEnumerable<string> SaveIpRestrictions(IpRestrictionsModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            return IPRestrictionsService.Save(model.Ips, Tenant.TenantId);
        }

        [Update("iprestrictions/settings")]
        public IPRestrictionsSettings UpdateIpRestrictionsSettings(IpRestrictionsModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var settings = new IPRestrictionsSettings { Enable = model.Enable };
            settings.Save();

            return settings;
        }

        [Update("tips")]
        public TipsSettings UpdateTipsSettings(SettingsModel model)
        {
            var settings = new TipsSettings { Show = model.Show };
            settings.SaveForCurrentUser();

            if (!model.Show && !string.IsNullOrEmpty(SetupInfo.TipsAddress))
            {
                try
                {
                    using var client = new WebClient();
                    var data = new NameValueCollection
                    {
                        ["userId"] = SecurityContext.CurrentAccount.ID.ToString(),
                        ["tenantId"] = Tenant.TenantId.ToString(CultureInfo.InvariantCulture)
                    };

                    client.UploadValues(string.Format("{0}/tips/deletereaded", SetupInfo.TipsAddress), data);
                }
                catch (Exception e)
                {
                    LogManager.GetLogger("ASC").Error(e.Message, e);
                }
            }

            return settings;
        }

        [Update("tips/change/subscription")]
        public bool UpdateTipsSubscription()
        {
            return StudioPeriodicNotify.ChangeSubscription(Tenant, SecurityContext.CurrentAccount.ID);
        }

        [Update("wizard/complete")]
        public WizardSettings CompleteWizard()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var settings = WizardSettings.Load();

            if (settings.Completed)
                return settings;

            settings.Completed = true;
            settings.Save();

            return settings;
        }

        [Update("tfaapp")]
        public bool TfaSettings(TfaModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var result = false;

            MessageAction action;
            switch (model.Type)
            {
                case "sms":
                    if (!StudioSmsNotificationSettings.IsVisibleSettings)
                        throw new Exception(Resource.SmsNotAvailable);

                    if (!SmsProviderManager.Enabled())
                        throw new MethodAccessException();

                    StudioSmsNotificationSettings.Enable = true;
                    action = MessageAction.TwoFactorAuthenticationEnabledBySms;

                    if (TfaAppAuthSettings.Enable)
                    {
                        TfaAppAuthSettings.Enable = false;
                    }

                    result = true;

                    break;

                case "app":
                    if (!TfaAppAuthSettings.IsVisibleSettings)
                    {
                        throw new Exception(Resource.TfaAppNotAvailable);
                    }

                    TfaAppAuthSettings.Enable = true;
                    action = MessageAction.TwoFactorAuthenticationEnabledByTfaApp;

                    if (StudioSmsNotificationSettings.IsVisibleSettings && StudioSmsNotificationSettings.Enable)
                    {
                        StudioSmsNotificationSettings.Enable = false;
                    }

                    result = true;

                    break;

                default:
                    if (TfaAppAuthSettings.Enable)
                    {
                        TfaAppAuthSettings.Enable = false;
                    }

                    if (StudioSmsNotificationSettings.IsVisibleSettings && StudioSmsNotificationSettings.Enable)
                    {
                        StudioSmsNotificationSettings.Enable = false;
                    }

                    action = MessageAction.TwoFactorAuthenticationDisabled;

                    break;
            }

            if (result)
            {
                CookiesManager.ResetTenantCookie(HttpContext);
            }

            MessageService.Send(action);
            return result;
        }

        ///<visible>false</visible>
        [Read("tfaappcodes")]
        public IEnumerable<object> TfaAppGetCodes()
        {
            var currentUser = CoreContext.UserManager.GetUsers(SecurityContext.CurrentAccount.ID);

            if (!TfaAppAuthSettings.IsVisibleSettings || !TfaAppUserSettings.EnableForUser(currentUser.ID))
                throw new Exception(Resource.TfaAppNotAvailable);

            if (currentUser.IsVisitor(ApiContext.Tenant) || currentUser.IsOutsider(ApiContext.Tenant))
                throw new NotSupportedException("Not available.");

            return TfaAppUserSettings.LoadForCurrentUser().CodesSetting.Select(r => new { r.IsUsed, r.Code }).ToList();
        }

        [Update("tfaappnewcodes")]
        public IEnumerable<object> TfaAppRequestNewCodes()
        {
            var currentUser = CoreContext.UserManager.GetUsers(Tenant.TenantId, SecurityContext.CurrentAccount.ID);

            if (!TfaAppAuthSettings.IsVisibleSettings || !TfaAppUserSettings.EnableForUser(currentUser.ID))
                throw new Exception(Resource.TfaAppNotAvailable);

            if (currentUser.IsVisitor(Tenant) || currentUser.IsOutsider(Tenant))
                throw new NotSupportedException("Not available.");

            var codes = currentUser.GenerateBackupCodes().Select(r => new { r.IsUsed, r.Code }).ToList();
            MessageService.Send(MessageAction.UserConnectedTfaApp, MessageTarget.Create(currentUser.ID), currentUser.DisplayUserName(false));
            return codes;
        }

        [Update("tfaappnewapp")]
        public string TfaAppNewApp(TfaModel model)
        {
            var isMe = model.Id.Equals(Guid.Empty);
            var user = CoreContext.UserManager.GetUsers(isMe ? SecurityContext.CurrentAccount.ID : model.Id);

            if (!isMe && !SecurityContext.CheckPermissions(Tenant, new UserSecurityProvider(user.ID), Constants.Action_EditUser))
                throw new SecurityAccessDeniedException(Resource.ErrorAccessDenied);

            if (!TfaAppAuthSettings.IsVisibleSettings || !TfaAppUserSettings.EnableForUser(user.ID))
                throw new Exception(Resource.TfaAppNotAvailable);

            if (user.IsVisitor(Tenant) || user.IsOutsider(Tenant))
                throw new NotSupportedException("Not available.");

            TfaAppUserSettings.DisableForUser(user.ID);
            MessageService.Send(MessageAction.UserDisconnectedTfaApp, MessageTarget.Create(user.ID), user.DisplayUserName(false));

            if (isMe)
            {
                return CommonLinkUtility.GetConfirmationUrl(Tenant.TenantId, user.Email, ConfirmType.TfaActivation);
            }

            StudioNotifyService.SendMsgTfaReset(Tenant.TenantId, user);
            return string.Empty;
        }

        ///<visible>false</visible>
        [Update("welcome/close")]
        public void CloseWelcomePopup()
        {
            var currentUser = CoreContext.UserManager.GetUsers(SecurityContext.CurrentAccount.ID);

            var collaboratorPopupSettings = CollaboratorSettings.LoadForCurrentUser();

            if (!(currentUser.IsVisitor(Tenant) && collaboratorPopupSettings.FirstVisit && !currentUser.IsOutsider(Tenant)))
                throw new NotSupportedException("Not available.");

            collaboratorPopupSettings.FirstVisit = false;
            collaboratorPopupSettings.SaveForCurrentUser();
        }

        ///<visible>false</visible>
        [Update("colortheme")]
        public void SaveColorTheme(SettingsModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            ColorThemesSettings.SaveColorTheme(model.Theme);
            MessageService.Send(MessageAction.ColorThemeChanged);
        }

        ///<visible>false</visible>
        [Update("timeandlanguage")]
        public string TimaAndLanguage(SettingsModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var culture = CultureInfo.GetCultureInfo(model.Lng);

            var changelng = false;
            if (SetupInfo.EnabledCultures.Find(c => string.Equals(c.Name, culture.Name, StringComparison.InvariantCultureIgnoreCase)) != null)
            {
                if (!string.Equals(Tenant.Language, culture.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    Tenant.Language = culture.Name;
                    changelng = true;
                }
            }

            var oldTimeZone = Tenant.TimeZone;
            var timeZones = TimeZoneInfo.GetSystemTimeZones().ToList();
            if (timeZones.All(tz => tz.Id != "UTC"))
            {
                timeZones.Add(TimeZoneInfo.Utc);
            }
            Tenant.TimeZone = timeZones.FirstOrDefault(tz => tz.Id == model.TimeZoneID) ?? TimeZoneInfo.Utc;

            CoreContext.TenantManager.SaveTenant(Tenant);

            if (!Tenant.TimeZone.Id.Equals(oldTimeZone.Id) || changelng)
            {
                if (!Tenant.TimeZone.Id.Equals(oldTimeZone.Id))
                {
                    MessageService.Send(MessageAction.TimeZoneSettingsUpdated);
                }
                if (changelng)
                {
                    MessageService.Send(MessageAction.LanguageSettingsUpdated);
                }
            }

            return Resource.SuccessfullySaveSettingsMessage;
        }

        ///<visible>false</visible>
        [Update("defaultpage")]
        public string SaveDefaultPageSettings(SettingsModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            new StudioDefaultPageSettings { DefaultProductID = model.DefaultProductID }.Save();

            MessageService.Send(MessageAction.DefaultStartPageSettingsUpdated);

            return Resource.SuccessfullySaveSettingsMessage;
        }


        private static string GetProductName(Guid productId)
        {
            var product = WebItemManager.Instance[productId];
            return productId == Guid.Empty ? "All" : product != null ? product.Name : productId.ToString();
        }

        [Read("license/refresh")]
        public bool RefreshLicense()
        {
            if (!CoreContext.Configuration.Standalone) return false;
            LicenseReader.RefreshLicense();
            return true;
        }


        [Read("customnavigation/getall")]
        public List<CustomNavigationItem> GetCustomNavigationItems()
        {
            return CustomNavigationSettings.Load().Items;
        }

        [Read("customnavigation/getsample")]
        public CustomNavigationItem GetCustomNavigationItemSample()
        {
            return CustomNavigationItem.GetSample();
        }

        [Read("customnavigation/get/{id}")]
        public CustomNavigationItem GetCustomNavigationItem(Guid id)
        {
            return CustomNavigationSettings.Load().Items.FirstOrDefault(item => item.Id == id);
        }

        [Create("customnavigation/create")]
        public CustomNavigationItem CreateCustomNavigationItem(CustomNavigationItem item)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var settings = CustomNavigationSettings.Load();

            var exist = false;

            foreach (var existItem in settings.Items)
            {
                if (existItem.Id != item.Id) continue;

                existItem.Label = item.Label;
                existItem.Url = item.Url;
                existItem.ShowInMenu = item.ShowInMenu;
                existItem.ShowOnHomePage = item.ShowOnHomePage;

                if (existItem.SmallImg != item.SmallImg)
                {
                    StorageHelper.DeleteLogo(existItem.SmallImg);
                    existItem.SmallImg = StorageHelper.SaveTmpLogo(Tenant.TenantId, item.SmallImg);
                }

                if (existItem.BigImg != item.BigImg)
                {
                    StorageHelper.DeleteLogo(existItem.BigImg);
                    existItem.BigImg = StorageHelper.SaveTmpLogo(Tenant.TenantId, item.BigImg);
                }

                exist = true;
                break;
            }

            if (!exist)
            {
                item.Id = Guid.NewGuid();
                item.SmallImg = StorageHelper.SaveTmpLogo(Tenant.TenantId, item.SmallImg);
                item.BigImg = StorageHelper.SaveTmpLogo(Tenant.TenantId, item.BigImg);

                settings.Items.Add(item);
            }

            settings.Save();

            MessageService.Send(MessageAction.CustomNavigationSettingsUpdated);

            return item;
        }

        [Delete("customnavigation/delete/{id}")]
        public void DeleteCustomNavigationItem(Guid id)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var settings = CustomNavigationSettings.Load();

            var terget = settings.Items.FirstOrDefault(item => item.Id == id);

            if (terget == null) return;

            StorageHelper.DeleteLogo(terget.SmallImg);
            StorageHelper.DeleteLogo(terget.BigImg);

            settings.Items.Remove(terget);
            settings.Save();

            MessageService.Send(MessageAction.CustomNavigationSettingsUpdated);
        }

        [Update("emailactivation")]
        public EmailActivationSettings UpdateEmailActivationSettings(bool show)
        {
            var settings = new EmailActivationSettings { Show = show };

            settings.SaveForCurrentUser();

            return settings;
        }

        ///<visible>false</visible>
        [Read("companywhitelabel")]
        public List<CompanyWhiteLabelSettings> GetCompanyWhiteLabelSettings()
        {
            var result = new List<CompanyWhiteLabelSettings>();

            var instance = CompanyWhiteLabelSettings.Instance;

            result.Add(instance);

            if (!instance.IsDefault && !instance.IsLicensor)
            {
                result.Add(instance.GetDefault() as CompanyWhiteLabelSettings);
            }

            return result;
        }

        [Read("statistics/spaceusage/{id}")]
        public List<UsageSpaceStatItemWrapper> GetSpaceUsageStatistics(Guid id)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var webtem = WebItemManager.Instance.GetItems(Tenant, WebZoneType.All, ItemAvailableState.All)
                                       .FirstOrDefault(item =>
                                                       item != null &&
                                                       item.ID == id &&
                                                       item.Context != null &&
                                                       item.Context.SpaceUsageStatManager != null);

            if (webtem == null) return new List<UsageSpaceStatItemWrapper>();

            return webtem.Context.SpaceUsageStatManager.GetStatData()
                         .ConvertAll(it => new UsageSpaceStatItemWrapper
                         {
                             Name = it.Name.HtmlEncode(),
                             Icon = it.ImgUrl,
                             Disabled = it.Disabled,
                             Size = FileSizeComment.FilesSizeToString(it.SpaceUsage),
                             Url = it.Url
                         });
        }

        [Read("statistics/visit")]
        public List<ChartPointWrapper> GetVisitStatistics(ApiDateTime fromDate, ApiDateTime toDate)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var from = TenantUtil.DateTimeFromUtc(fromDate);
            var to = TenantUtil.DateTimeFromUtc(toDate);

            var points = new List<ChartPointWrapper>();

            if (from.CompareTo(to) >= 0) return points;

            for (var d = new DateTime(from.Ticks); d.Date.CompareTo(to.Date) <= 0; d = d.AddDays(1))
            {
                points.Add(new ChartPointWrapper
                {
                    DisplayDate = d.Date.ToShortDateString(),
                    Date = d.Date,
                    Hosts = 0,
                    Hits = 0
                });
            }

            var hits = StatisticManager.GetHitsByPeriod(Tenant.TenantId, from, to);
            var hosts = StatisticManager.GetHostsByPeriod(Tenant.TenantId, from, to);

            if (hits.Count == 0 || hosts.Count == 0) return points;

            hits.Sort((x, y) => x.VisitDate.CompareTo(y.VisitDate));
            hosts.Sort((x, y) => x.VisitDate.CompareTo(y.VisitDate));

            for (int i = 0, n = points.Count, hitsNum = 0, hostsNum = 0; i < n; i++)
            {
                while (hitsNum < hits.Count && points[i].Date.CompareTo(hits[hitsNum].VisitDate.Date) == 0)
                {
                    points[i].Hits += hits[hitsNum].VisitCount;
                    hitsNum++;
                }
                while (hostsNum < hosts.Count && points[i].Date.CompareTo(hosts[hostsNum].VisitDate.Date) == 0)
                {
                    points[i].Hosts++;
                    hostsNum++;
                }
            }

            return points;
        }

        [Read("storage")]
        public List<StorageWrapper> GetAllStorages()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            var current = StorageSettings.Load();
            var consumers = ConsumerFactory.GetAll<DataStoreConsumer>().ToList();
            return consumers.Select(consumer => new StorageWrapper(consumer, current)).ToList();
        }

        [Read("storage/progress", false)]
        public double GetStorageProgress()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

            if (!CoreContext.Configuration.Standalone) return -1;

            using var migrateClient = new ServiceClient();
            return migrateClient.GetProgress(Tenant.TenantId);
        }

        [Update("storage")]
        public StorageSettings UpdateStorage(StorageModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            if (!CoreContext.Configuration.Standalone) return null;

            var consumer = ConsumerFactory.GetByName(model.Module);
            if (!consumer.IsSet)
                throw new ArgumentException("module");

            var settings = StorageSettings.Load();
            if (settings.Module == model.Module) return settings;

            settings.Module = model.Module;
            settings.Props = model.Props.ToDictionary(r => r.Key, b => b.Value);

            try
            {
                StartMigrate(settings);
            }
            catch (Exception e)
            {
                LogManager.GetLogger("ASC").Error("UpdateStorage", e);
                throw;
            }

            return settings;
        }

        [Delete("storage")]
        public void ResetStorageToDefault()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            if (!CoreContext.Configuration.Standalone) return;

            var settings = StorageSettings.Load();

            settings.Module = null;
            settings.Props = null;

            try
            {
                StartMigrate(settings);
            }
            catch (Exception e)
            {
                LogManager.GetLogger("ASC").Error("ResetStorageToDefault", e);
                throw;
            }
        }

        [Read("storage/cdn")]
        public List<StorageWrapper> GetAllCdnStorages()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            if (!CoreContext.Configuration.Standalone) return null;

            var current = CdnStorageSettings.Load();
            var consumers = ConsumerFactory.GetAll<DataStoreConsumer>().Where(r => r.Cdn != null).ToList();
            return consumers.Select(consumer => new StorageWrapper(consumer, current)).ToList();
        }

        [Update("storage/cdn")]
        public CdnStorageSettings UpdateCdn(StorageModel model)
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            if (!CoreContext.Configuration.Standalone) return null;

            var consumer = ConsumerFactory.GetByName(model.Module);
            if (!consumer.IsSet)
                throw new ArgumentException("module");

            var settings = CdnStorageSettings.Load();
            if (settings.Module == model.Module) return settings;

            settings.Module = model.Module;
            settings.Props = model.Props.ToDictionary(r => r.Key, b => b.Value);

            try
            {
                using var migrateClient = new ServiceClient();
                migrateClient.UploadCdn(Tenant.TenantId, "/", WebHostEnvironment.ContentRootPath, settings);
            }
            catch (Exception e)
            {
                LogManager.GetLogger("ASC").Error("UpdateCdn", e);
                throw;
            }

            return settings;
        }

        [Delete("storage/cdn")]
        public void ResetCdnToDefault()
        {
            SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);
            if (!CoreContext.Configuration.Standalone) return;

            CdnStorageSettings.Load().Clear();
        }

        //[Read("storage/backup")]
        //public List<StorageWrapper> GetAllBackupStorages()
        //{
        //    SecurityContext.DemandPermissions(Tenant, SecutiryConstants.EditPortalSettings);

        //    var schedule = new BackupAjaxHandler().GetSchedule();
        //    var current = new StorageSettings();

        //    if (schedule != null && schedule.StorageType == Contracts.BackupStorageType.ThirdPartyConsumer)
        //    {
        //        current = new StorageSettings
        //        {
        //            Module = schedule.StorageParams["module"],
        //            Props = schedule.StorageParams.Where(r => r.Key != "module").ToDictionary(r => r.Key, r => r.Value)
        //        };
        //    }

        //    var consumers = ConsumerFactory.GetAll<DataStoreConsumer>().ToList();
        //    return consumers.Select(consumer => new StorageWrapper(consumer, current)).ToList();
        //}

        private void StartMigrate(StorageSettings settings)
        {
            using (var migrateClient = new ServiceClient())
            {
                migrateClient.Migrate(Tenant.TenantId, settings);
            }

            Tenant.SetStatus(TenantStatus.Migrating);
        }

        [Read("socket")]
        public object GetSocketSettings()
        {
            var hubUrl = ConfigurationManager.AppSettings["web:hub"] ?? string.Empty;
            if (hubUrl != string.Empty)
            {
                if (!hubUrl.EndsWith("/"))
                {
                    hubUrl += "/";
                }
            }

            return new { Url = hubUrl };
        }
    }
}