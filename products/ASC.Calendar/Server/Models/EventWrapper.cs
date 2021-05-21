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
using System.Runtime.Serialization;
using ASC.Calendar.ExternalCalendars;
using ASC.Common.Security;
using ASC.Core;
using ASC.Core.Common;
using ASC.Web.Core.Users;
using ASC.Core.Users;
using ASC.Api.Core;
using ASC.Web.Core.Calendars;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ASC.Common.Utils;
using ASC.Common;
using ASC.Calendar.iCalParser;
using ASC.Calendar.BusinessObjects;

namespace ASC.Calendar.Models
{
    [DataContract(Name = "event", Namespace = "")]
    public class EventWrapper
    {
        [DataMember(Name = "objectId", Order = 0)]
        public string Id { get; set; }

        [DataMember(Name = "uniqueId", Order = 140)]
        public string Uid { get; set; }

        public int TenantId { get; set; }

        public bool Todo { get; set; }

        [DataMember(Name = "sourceId", Order = 10)]
        public string CalendarId { get; set; }

        [DataMember(Name = "title", Order = 20)]
        public string Name { get; set; }

        [DataMember(Name = "description", Order = 30)]
        public string Description { get; set; }

        [DataMember(Name = "allDay", Order = 60)]
        public bool AllDayLong { get; set; }

        [DataMember(Name = "start", Order = 40)]
        public ApiDateTime Start { get; set; }

        [DataMember(Name = "end", Order = 50)]
        public ApiDateTime End { get; set; }

        [DataMember(Name = "repeatRule", Order = 70)]
        public string RepeatRule { get; set; }

        [DataMember(Name = "alert", Order = 110)]
        public EventAlertWrapper Alert { get; set; }

        [DataMember(Name = "isShared", Order = 80)]
        public bool IsShared { get; set; }

        [DataMember(Name = "canUnsubscribe", Order = 130)]
        public bool CanUnsubscribe { get; set; }

        [DataMember(Name = "isEditable", Order = 100)]
        public virtual bool IsEditable { get; set; }

        [DataMember(Name = "permissions", Order = 90)]
        public Permissions Permissions { get; set; }

        [DataMember(Name = "owner", Order = 120)]
        public UserParams Owner { get; set; }

        [DataMember(Name = "status", Order = 150)]
        public EventStatus Status { get; set; }

        public Guid UserId { get; set; }

        public static EventWrapper GetSample()
        {
            return new EventWrapper
            {
                Owner = UserParams.GetSample(),
                Permissions = Permissions.GetSample(),
                IsEditable = false,
                CanUnsubscribe = true,
                IsShared = true,
                Alert = EventAlertWrapper.GetSample(),
                RepeatRule = "",
                Start = new ApiDateTime(DateTime.Now.AddDays(1), TimeZoneInfo.Utc.GetOffset()),
                End = new ApiDateTime(DateTime.Now.AddDays(1), TimeZoneInfo.Utc.GetOffset()),
                AllDayLong = false,
                Description = "Event Description",
                Name = "Event Name",
                Id = "1",
                CalendarId = "calendarID",
                Status = (int)EventStatus.Tentative
            };
        }
    }

    [Scope]
    public class EventWrapperHelper
    {
        private TimeZoneInfo _timeZone;

        private DateTime _utcStartDate = DateTime.MinValue;
        private DateTime _utcEndDate = DateTime.MinValue;
        private DateTime _utcUpdateDate = DateTime.MinValue;


        private AuthManager Authentication { get; }
        private TenantManager TenantManager { get; }
        public UserManager UserManager { get; }
        private PermissionContext PermissionContext { get; }
        public DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }
        public PublicItemCollectionHelper PublicItemCollectionHelper { get; }
        private AuthContext AuthContext { get; }
        private TimeZoneConverter TimeZoneConverter { get; }
        private DataProvider DataProvider { get; }

        public EventWrapperHelper(
            UserManager userManager,
            AuthManager authentication,
            TenantManager tenantManager,
            PermissionContext permissionContext,
            DisplayUserSettingsHelper displayUserSettingsHelper,
            PublicItemCollectionHelper publicItemCollectionHelper,
            AuthContext context,
            TimeZoneConverter timeZoneConverter,
            DataProvider dataProvider)
        {
            Authentication = authentication;
            TenantManager = tenantManager;
            UserManager = userManager;
            PublicItemCollectionHelper = publicItemCollectionHelper;
            DisplayUserSettingsHelper = displayUserSettingsHelper;
            PermissionContext = permissionContext;
            AuthContext = context;
            TimeZoneConverter = timeZoneConverter;
            DataProvider = dataProvider;
        }
        public EventWrapper Get(IEvent baseEvent, Guid userId, TimeZoneInfo timeZone, DateTime utcStartDate, DateTime utcEndDate, DateTime utcUpdateDate)
        {
            _utcStartDate = utcStartDate;
            _utcEndDate = utcEndDate;
            _utcUpdateDate = utcUpdateDate;

            return this.Get(baseEvent, userId, timeZone);
        }
        public EventWrapper Get(IEvent baseEvent, Guid userId, TimeZoneInfo timeZone)
        {
            var eventWraper = new EventWrapper();
            _timeZone = timeZone;
            var _baseEvent = baseEvent;

            eventWraper.UserId = userId;
            eventWraper.Id = _baseEvent.Id;
            eventWraper.Uid = _baseEvent.Uid;
            eventWraper.CalendarId = _baseEvent.CalendarId;
            eventWraper.Name = _baseEvent.Name;
            eventWraper.Description = _baseEvent.Description;
            eventWraper.AllDayLong = _baseEvent.AllDayLong; ;

            var icalendar = new iCalendar(AuthContext, TimeZoneConverter, TenantManager);
            //---
            var startD = _utcStartDate != DateTime.MinValue ? _utcStartDate : _baseEvent.UtcStartDate;
            startD = new DateTime(startD.Ticks, DateTimeKind.Utc);

            var updateD = _utcUpdateDate != DateTime.MinValue ? _utcUpdateDate : _baseEvent.UtcStartDate;

            if (_baseEvent.AllDayLong && _baseEvent.GetType().GetCustomAttributes(typeof(AllDayLongUTCAttribute), true).Length > 0)
            {
                eventWraper.Start = new ApiDateTime(startD, TimeZoneInfo.Utc.GetOffset());
            }
            else if (_baseEvent.GetType().Namespace == new BusinessObjects.Event(AuthContext, TimeZoneConverter, icalendar, DataProvider).GetType().Namespace)
            {
                eventWraper.Start = new ApiDateTime(startD, _timeZone.GetOffset(false, updateD));
            }
            else
            {
                eventWraper.Start = new ApiDateTime(startD, _timeZone.GetOffset());
            }

            //---
            var endD = _utcEndDate != DateTime.MinValue ? _utcEndDate : _baseEvent.UtcEndDate;
            endD = new DateTime(endD.Ticks, DateTimeKind.Utc);

            updateD = _utcUpdateDate != DateTime.MinValue ? _utcUpdateDate : _baseEvent.UtcStartDate;


            if (_baseEvent.AllDayLong && _baseEvent.GetType().GetCustomAttributes(typeof(AllDayLongUTCAttribute), true).Length > 0)
            {
                eventWraper.End = new ApiDateTime(endD, TimeZoneInfo.Utc.GetOffset());
            }
            else if (_baseEvent.GetType().Namespace == new BusinessObjects.Event(AuthContext, TimeZoneConverter, icalendar, DataProvider).GetType().Namespace)
            {
                eventWraper.End = new ApiDateTime(endD, _timeZone.GetOffset(false, updateD));
            }
            else
            {
                eventWraper.End = new ApiDateTime(endD, _timeZone.GetOffset());
            }

            eventWraper.RepeatRule = _baseEvent.RecurrenceRule.ToString();

            eventWraper.Alert = EventAlertWrapper.ConvertToTypeSurrogated(_baseEvent.AlertType);
            eventWraper.IsShared = _baseEvent.SharingOptions.SharedForAll || _baseEvent.SharingOptions.PublicItems.Count > 0;
            eventWraper.CanUnsubscribe = String.Equals(_baseEvent.CalendarId, SharedEventsCalendar.CalendarId, StringComparison.InvariantCultureIgnoreCase);

            if (_baseEvent is ISecurityObject)
            {
                eventWraper.IsEditable = PermissionContext.PermissionResolver.Check(Authentication.GetAccountByID(TenantManager.GetCurrentTenant().TenantId, userId), (ISecurityObject)_baseEvent, null, CalendarAccessRights.FullAccessAction);
            }
            else
            {
                eventWraper.IsEditable = false;
            }

            var p = new CalendarPermissions() { Data = PublicItemCollectionHelper.GetForEvent(_baseEvent) };
            foreach (var item in _baseEvent.SharingOptions.PublicItems)
            {
                if (item.IsGroup)
                    p.UserParams.Add(new UserParams() { Id = item.Id, Name = UserManager.GetGroupInfo(item.Id).Name });
                else
                    p.UserParams.Add(new UserParams() { Id = item.Id, Name = UserManager.GetUsers(item.Id).DisplayUserName(DisplayUserSettingsHelper) });
            }
            eventWraper.Permissions = p;

            var owner = new UserParams() { Id = _baseEvent.OwnerId, Name = "" };
            if (_baseEvent.OwnerId != Guid.Empty)
                owner.Name = UserManager.GetUsers(_baseEvent.OwnerId).DisplayUserName(DisplayUserSettingsHelper);

            eventWraper.Owner = owner;
            eventWraper.Status = _baseEvent.Status;
            eventWraper.UserId = userId;


            return eventWraper;

        }

        public List<EventWrapper> GetList(DateTime utcStartDate, DateTime utcEndDate, Guid userId, IEvent baseEvent)
        {
            var list = new List<EventWrapper>();
            var _baseEvent = baseEvent;

            if (_baseEvent.UtcStartDate == DateTime.MinValue)
                return list;

            var difference = _baseEvent.UtcEndDate - _baseEvent.UtcStartDate;

            var recurenceDates = new List<DateTime>();

            if (_baseEvent.RecurrenceRule.Freq == Frequency.Never)
            {
                if ((_baseEvent.UtcStartDate <= utcStartDate && _baseEvent.UtcEndDate >= utcStartDate) ||
                    (_baseEvent.UtcStartDate <= utcEndDate && _baseEvent.UtcEndDate >= utcEndDate) ||
                    (_baseEvent.UtcStartDate >= utcStartDate && _baseEvent.UtcEndDate <= utcEndDate))
                    recurenceDates.Add(_baseEvent.UtcStartDate);
            }
            else
            {
                recurenceDates = _baseEvent.RecurrenceRule.GetDates(_baseEvent.UtcStartDate, utcStartDate, utcEndDate);
            }

            foreach (var d in recurenceDates)
            {
                var endDate = _baseEvent.UtcEndDate;
                if (!_baseEvent.UtcEndDate.Equals(DateTime.MinValue))
                    endDate = d + difference;

                list.Add(Get(_baseEvent, userId, _timeZone, d, endDate, _baseEvent.UtcUpdateDate));
            }

            return list;
        }

    }
}
