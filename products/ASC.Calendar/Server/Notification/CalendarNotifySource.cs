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
using ASC.Api.Core;
using ASC.Calendar.BusinessObjects;
using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Billing;
using ASC.Core.Notify;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.Notify;
using ASC.Notify.Model;
using ASC.Notify.Patterns;
using ASC.Notify.Recipients;
using ASC.Web.Core.Calendars;
using ASC.Web.Core.Users;
using ASC.Web.Studio.Utility;
using Microsoft.Extensions.DependencyInjection;
using NotifyContext = ASC.Notify.Context;

namespace ASC.Calendar.Notification
{
 
    [Scope]
    public class CalendarNotifyClient
    {
        private static INotifyClient _notifyClient;

        private static string _syncName = "calendarNotifySyncName";

        public AuthContext AuthContext { get; }
        public IServiceProvider ServiceProvider { get; }
        public UserManager UserManager { get; }
        private CommonLinkUtility CommonLinkUtility { get; }
        private CalendarNotifySource CalendarNotifySource { get; }
        public DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }

        public static NotifyContext NotifyContext { get; private set; }
        public CalendarNotifyClient(
            AuthContext authContext,
            IServiceProvider serviceProvider,
            UserManager userManager,
            CalendarNotifySource calendarNotifySource,
            CommonLinkUtility commonLinkUtility,
            DisplayUserSettingsHelper displayUserSettingsHelper)
        {
            AuthContext = authContext;
            ServiceProvider = serviceProvider;
            CalendarNotifySource = calendarNotifySource;
            UserManager = userManager;
            CommonLinkUtility = commonLinkUtility;
            DisplayUserSettingsHelper = displayUserSettingsHelper;

            NotifyContext = new NotifyContext(serviceProvider);
            _notifyClient = NotifyContext.NotifyService.RegisterClient(CalendarNotifySource, ServiceProvider.CreateScope());
        }

        private static bool _isRegistered = false;
        public static void RegisterSendMethod()
        {
            if (!_isRegistered)
            {
                lock (_syncName)
                {
                    if (!_isRegistered)
                    {
                       // _notifyClient.RegisterSendMethod(NotifyAbouFutureEvent, "0 * * ? * *");

                        _isRegistered = true;
                    }
                }
            }
        }
        /*private void NotifyAbouFutureEvent(DateTime scheduleDate)
        {
            try
            {
                using (var provider = new DataProvider())
                {
                    foreach (var data in provider.ExtractAndRecountNotifications(scheduleDate))
                    {
                        if (data.Event == null || data.Event.Status == EventStatus.Cancelled)
                        {
                            continue;
                        }

                        var tenant = CoreContext.TenantManager.GetTenant(data.TenantId);
                        if (tenant == null ||
                            tenant.Status != TenantStatus.Active ||
                            TariffState.NotPaid <= CoreContext.PaymentManager.GetTariff(tenant.TenantId).State)
                        {
                            continue;
                        }
                        CoreContext.TenantManager.SetCurrentTenant(tenant);

                        var r =
                            CalendarNotifySource.Instance.GetRecipientsProvider().GetRecipient(data.UserId.ToString());
                        if (r == null)
                        {
                            continue;
                        }

                        var startDate = data.GetUtcStartDate();
                        var endDate = data.GetUtcEndDate();

                        if (!data.Event.AllDayLong)
                        {
                            startDate = startDate.Add(data.TimeZone.GetOffset());
                            endDate = (endDate == DateTime.MinValue
                                ? DateTime.MinValue
                                : endDate.Add(data.TimeZone.GetOffset()));
                        }

                        _notifyClient.SendNoticeAsync(CalendarNotifySource.EventAlert,
                            null,
                            r,
                            true,
                            new TagValue("EventName", data.Event.Name),
                            new TagValue("EventDescription", data.Event.Description ?? ""),
                            new TagValue("EventStartDate",
                                startDate.ToShortDateString() + " " + startDate.ToShortTimeString()),
                            new TagValue("EventEndDate",
                                (endDate > startDate)
                                    ? (endDate.ToShortDateString() + " " + endDate.ToShortTimeString())
                                    : ""),
                            new TagValue("Priority", 1));
                    }
                }
            }
            catch (Exception error)
            {
                LogManager.GetLogger("ASC.Notify.Calendar").Error(error);
            }
        } */
        public void NotifyAboutSharingCalendar(ASC.Calendar.BusinessObjects.Calendar calendar)
        {
            NotifyAboutSharingCalendar(calendar, null);
        }
        public void NotifyAboutSharingCalendar(ASC.Calendar.BusinessObjects.Calendar calendar, ASC.Calendar.BusinessObjects.Calendar oldCalendar)
        {
            var initatorInterceptor = new InitiatorInterceptor(new DirectRecipient(AuthContext.CurrentAccount.ID.ToString(), AuthContext.CurrentAccount.Name));
            try
            {
                var usr = UserManager.GetUsers(AuthContext.CurrentAccount.ID);
                var userLink = PerformUrl(CommonLinkUtility.GetUserProfile(usr.ID.ToString(), false));

                foreach (var item in calendar.SharingOptions.PublicItems)
                {
                    if (oldCalendar != null && oldCalendar.SharingOptions.PublicItems.Exists(i => i.Id.Equals(item.Id)))
                        continue;

                    var r = CalendarNotifySource.GetRecipientsProvider().GetRecipient(item.Id.ToString());
                    if (r != null)
                    {
                        _notifyClient.SendNoticeAsync(CalendarNotifySource.CalendarSharing, null, r, true,
                            new TagValue("SharingType", "calendar"),
                            new TagValue("UserName", usr.DisplayUserName(DisplayUserSettingsHelper)),
                            new TagValue("UserLink", userLink),
                            new TagValue("CalendarName", calendar.Name));
                    }
                }
                _notifyClient.EndSingleRecipientEvent(_syncName);
            }
            finally
            {
                _notifyClient.RemoveInterceptor(initatorInterceptor.Name);
            }
        }
        public void NotifyAboutSharingEvent(ASC.Calendar.BusinessObjects.Event calendarEvent)
        {
            NotifyAboutSharingEvent(calendarEvent, null);
        }
        public void NotifyAboutSharingEvent(ASC.Calendar.BusinessObjects.Event calendarEvent, ASC.Calendar.BusinessObjects.Event oldCalendarEvent)
        {
            var initatorInterceptor = new InitiatorInterceptor(new DirectRecipient(AuthContext.CurrentAccount.ID.ToString(), AuthContext.CurrentAccount.Name));
            try
            {
                var usr = UserManager.GetUsers(AuthContext.CurrentAccount.ID);
                var userLink = PerformUrl(CommonLinkUtility.GetUserProfile(usr.ID.ToString(), false));

                foreach (var item in calendarEvent.SharingOptions.PublicItems)
                {
                    if (oldCalendarEvent != null && oldCalendarEvent.SharingOptions.PublicItems.Exists(i => i.Id.Equals(item.Id)))
                        continue;

                    var r = CalendarNotifySource.GetRecipientsProvider().GetRecipient(item.Id.ToString());
                    if (r != null)
                    {
                        _notifyClient.SendNoticeAsync(CalendarNotifySource.CalendarSharing, null, r, true,
                            new TagValue("SharingType", "event"),
                            new TagValue("UserName", usr.DisplayUserName(DisplayUserSettingsHelper)),
                            new TagValue("UserLink", userLink),
                            new TagValue("EventName", calendarEvent.Name));
                    }
                }
                _notifyClient.EndSingleRecipientEvent(_syncName);
            }
            finally
            {
                _notifyClient.RemoveInterceptor(initatorInterceptor.Name);
            }
        }

        private string PerformUrl(string url)
        {
            return CommonLinkUtility.GetFullAbsolutePath(url);
        }
    }

    [Scope]
    public class CalendarNotifySource : NotifySource
    {
        public static INotifyAction CalendarSharing = new NotifyAction("CalendarSharingPattern");
        public static INotifyAction EventAlert = new NotifyAction("EventAlertPattern");

        /*public CalendarNotifySource Instance
        {
            get;
            private set;
        }

        static CalendarNotifySource()
        {
            Instance = new CalendarNotifySource();
        }*/

        public CalendarNotifySource(UserManager userManager, IRecipientProvider recipientsProvider, SubscriptionManager subscriptionManager)
            : base(new Guid("{40650DA3-F7C1-424c-8C89-B9C115472E08}"), userManager, recipientsProvider, subscriptionManager)
        {
        }


        protected override IActionProvider CreateActionProvider()
        {
            return new ConstActionProvider(
                CalendarSharing,
                EventAlert);
        }

        protected override IPatternProvider CreatePatternsProvider()
        {
            return new XmlPatternProvider2(CalendarPatterns.calendar_patterns);
        }
    }
}
